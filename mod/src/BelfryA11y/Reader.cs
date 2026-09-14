using System;
using TMPro;
using UnityEngine;

namespace BelfryA11y
{
    /// <summary>
    /// 剧情朗读的状态机。
    ///
    /// === 数据从哪来 ===
    ///
    /// 《钟塔》的驱动链是：
    ///   ScriptEngine.DisplayNextLine
    ///     → DialogueCommandExecutor.ExecuteDialogue(scene, index, isInstant)
    ///         → UISceneController.UpdateCharacterUI(scene)      设说话人
    ///         → UISceneController.StartTyping(localizedDialogue) 显示台词
    ///         （手书行走 ShoushuDialoguePresenter.PresentLineAsync，不经过 StartTyping）
    ///
    /// 所以：
    ///   · ExecuteDialogue 的 Prefix 抓住「当前这一行的 DialogueScene」，
    ///     里面同时有 VoiceFilename（判定有没有配音）和 CharacterName。
    ///   · StartTyping / PresentLineAsync 才是真正「这一行开始显示」的时刻。
    ///
    /// === 三条显示路径，一条都不能漏 ===
    ///
    ///   1. 普通显示        → StartTyping(text)             → Consume(...)
    ///   2. 手书显示        → PresentLineAsync(text, false) → Consume(...)
    ///   3. **瞬时显示**    → ExecuteDialogue 的 isInstant 分支直接写
    ///                        dialogueText.text，**不调用 StartTyping**。
    ///                        快进（按住 Ctrl / 按 F）与回退都走这一条。
    ///
    /// 第 3 条最初被漏掉了，症状是：按住 Ctrl 快进一阵再松手，按退格念的是
    /// 快进之前那一句 —— 因为缓冲区从来没被快进经过的行更新过。
    ///
    /// === 重读缓冲区（_buffer）===
    ///
    /// 退格键念的就是它。**所有**会出现在屏幕上的台词都要进去，包括：
    ///
    ///   · 朗读了的无配音行
    ///   · **没有**朗读的有配音行 —— 补丁故意不出声（TTS 和角色语音叠在一起
    ///     两边都听不清），但玩家想听文字时按退格，TTS 就该把这一句念出来。
    ///     这是一条真实反馈：有配音的行如果只 Speech.Stop() 而不记缓冲区，
    ///     退格会念出**上一句**，玩家会以为「这一句翻不回来」。
    ///   · 快进经过的行（只记不念）
    ///   · 一批选项的整体播报
    ///
    /// 唯一不进缓冲区的是「已选择 2」「已退出导航模式」这类**状态提示** ——
    /// 它们不是剧情内容，占了缓冲区反而会把刚听到的台词挤掉。
    ///
    /// === 唯一出口 ===
    ///
    /// 除了状态提示，所有朗读都走 <see cref="Say"/>：它负责去重、进缓冲区、
    /// 再交给 Speech。这样「念过什么」和「能重念什么」永远是同一份名单。
    /// </summary>
    internal static class Reader
    {
        private sealed class Line
        {
            public string Speech = "";     // 已加工好的朗读文本（含说话人前缀）
            public bool HasVoice;          // 这一行有配音 → 不朗读，只放语音
            public string Raw = "";        // 原始台词
            public string Speaker = "";
            public string Type = "";
        }

        private static Line _pending;      // 刚从 ExecuteDialogue 拿到的行，尚未显示
        private static Line _current;      // 正在显示的那一行

        /// <summary>重读缓冲区 —— 退格键念的就是它。</summary>
        private static string _buffer = "";

        // 极短时间内同一句重复触发时的去重哨兵。**不是**重读缓冲区。
        private static string _lastSpoken = "";
        private static float _lastSpeakAt;

        public static bool HasJudged { get; private set; }

        // ================= 唯一出口 =================

        /// <summary>朗读一段新内容：去重 → 进重读缓冲区 → 出声。</summary>
        public static void Say(string text, bool interrupt)
        {
            if (string.IsNullOrEmpty(text)) return;
            text = text.Trim();
            if (text.Length == 0) return;

            // 同一句在极短时间内重复触发就跳过（同一行会被多条路径碰到）
            if (text == _lastSpoken && Time.realtimeSinceStartup - _lastSpeakAt < 0.4f) return;

            _lastSpoken = text;
            _lastSpeakAt = Time.realtimeSinceStartup;
            Remember(text);
            Speech.Speak(text, interrupt);
        }

        /// <summary>
        /// 只记不念。快进经过的行、以及有配音的行走这里。
        /// 空串不覆盖已有内容 —— 别让一个没台词的行把缓冲区清掉。
        /// </summary>
        public static void Remember(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            _buffer = text;
        }

        // ================= 供 Patch 调用 =================

        /// <summary>
        /// ExecuteDialogue 的 Prefix：记住这一行。
        ///
        /// isInstant 为 true 表示这一行是「直接写 dialogueText」显示的
        /// （快进 / 回退 / 非停顿指令），不会走 StartTyping，所以这里就得
        /// 把缓冲区更新掉，否则松开快进后按退格会念到快进之前的句子。
        /// </summary>
        public static void NoteScene(DialogueScene scene, bool isInstant)
        {
            if (scene == null) return;
            try
            {
                string type = scene.TypeName ?? "";
                var line = new Line
                {
                    Type = type,
                    Speaker = TextProc.CleanName(scene.CharacterName),
                    Raw = scene.Dialogue ?? "",
                    HasVoice = !string.IsNullOrEmpty(scene.VoiceFilename)
                };
                line.Speech = BuildSpeech(line.Speaker, line.Raw);

                if (isInstant)
                {
                    // 只有真正会显示出台词的类型才进缓冲区。
                    // 「成就」这类指令的 Dialogue 字段里装的是成就 ID（例如 ACV_1），
                    // 记进去会让退格念出一串代号。
                    if (!IsDisplayType(type)) return;
                    _current = line;
                    Remember(line.Speech);
                    return;
                }

                _pending = line;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("[Reader] NoteScene 失败: " + e.Message);
            }
        }

        /// <summary>UISceneController.StartTyping 的 Postfix。</summary>
        public static void OnTyping(string text)
        {
            Consume(text);
        }

        /// <summary>ShoushuDialoguePresenter.PresentLineAsync 的 Prefix。</summary>
        public static void OnShoushu(string text, bool instant)
        {
            // instant=true 是回退/快进路径，不朗读；缓冲区已由 NoteScene 处理
            if (instant) return;
            Consume(text);
        }

        /// <summary>与 ScriptEngine.IsStopPoint 同一套判定：真正停在屏幕上等玩家的类型。</summary>
        private static bool IsDisplayType(string t)
        {
            return t == "对话" || t == "旁白" || t == "心理" || t == "手书";
        }

        /// <summary>真正决定「这一句念不念」。</summary>
        private static void Consume(string shownText)
        {
            try
            {
                // 一句新台词开始显示，说明屏幕上的选项按钮一定已经没了。
                // 选项列表必须在这里作废，不能只靠「玩家按了数字键就清」——
                // 鼠标点选项根本不经过我们的按键处理，列表会一直留着，
                // 之后每次退格都会去念那几个早就没了的选项。
                Choices.ClearStory();

                Line line = _pending;
                _pending = null;

                if (line == null)
                {
                    // 没抓到 scene（例如从存档跳进来、或场景切换的边界）。
                    // 按配置决定是「照念」还是「不念」。
                    if (!Plugin.CfgSpeakWhenUnknown.Value) return;
                    string fallback = TextProc.ToSpeech(shownText);
                    if (TextProc.IsTrivial(fallback)) return;
                    HasJudged = true;
                    _current = new Line { Speech = fallback, Raw = shownText, HasVoice = false };
                    Say(fallback, true);
                    return;
                }

                _current = line;
                HasJudged = true;

                if (line.HasVoice)
                {
                    // 有配音：放语音，不朗读，并打断上一句未读完的朗读。
                    // 但这一行**仍然要进重读缓冲区** —— 语音错过了、
                    // 或者玩家没听清，按退格让 TTS 把它念一遍，正是重读键该干的事。
                    Speech.Stop();
                    Remember(line.Speech);
                    _lastSpoken = "";
                    return;
                }

                if (!Plugin.CfgReadUnvoiced.Value) return;
                if (string.IsNullOrEmpty(line.Speech) || TextProc.IsTrivial(line.Speech)) return;

                Say(line.Speech, true);
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("[Reader] Consume 失败: " + e.Message);
            }
        }

        /// <summary>说话人名 + 台词，拼成要念的一整句。</summary>
        private static string BuildSpeech(string speaker, string raw)
        {
            string body = TextProc.ToSpeech(raw);
            if (string.IsNullOrEmpty(body)) return "";

            if (!Plugin.CfgSpeakName.Value || string.IsNullOrEmpty(speaker)) return body;
            return speaker + "：" + body;
        }

        /// <summary>直接从屏幕上的 TMP 读当前台词（兜底用）。</summary>
        private static string LiveSpeech()
        {
            try
            {
                var ui = UnityEngine.Object.FindObjectOfType<UISceneController>();
                if (ui == null || ui.dialogueText == null) return "";

                string body = TextProc.ToSpeech(ui.dialogueText.text);
                if (string.IsNullOrEmpty(body)) return "";

                string name = ui.characterNameText != null
                    ? TextProc.CleanName(ui.characterNameText.text) : "";
                if (Plugin.CfgSpeakName.Value && !string.IsNullOrEmpty(name))
                    return name + "：" + body;
                return body;
            }
            catch { return ""; }
        }

        // ================= 重读 =================

        /// <summary>重读当前这一句。Backspace。</summary>
        public static void Repeat()
        {
            try
            {
                // 停在选项上时优先重念选项 —— 这时屏幕上没有「一句台词」可读，
                // 玩家按退格想听的就是那几个选项。
                if (Choices.RepeatAnnounce()) return;

                if (!string.IsNullOrEmpty(_buffer))
                {
                    // 直接用 Speech，不走 Say：Say 有 0.4 秒同句去重，
                    // 会把「刚念完马上按退格再听一遍」这个正当操作吃掉。
                    Speech.Speak(_buffer, true);
                    return;
                }

                // 缓冲区还是空的（刚进游戏就按了退格）：念屏幕上现成的那一句
                string live = LiveSpeech();
                if (!string.IsNullOrEmpty(live))
                {
                    Speech.Speak(live, true);
                    return;
                }

                Speech.Speak("没有可以重读的内容。", true);
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("[Reader] Repeat 失败: " + e.Message);
            }
        }

        // ================= 快进停下来 =================

        /// <summary>
        /// 快进（按住 Ctrl / 按 F）停止时调用：把停在的那一句补念出来，
        /// 否则玩家松开 Ctrl 之后屏幕上是什么完全不知道。
        ///
        /// 停在选项上时不念 —— 选项出现的那一刻已经完整播报过一遍了，
        /// 再念一次只是重复。
        /// </summary>
        public static void AnnounceAfterSkipStop()
        {
            try
            {
                if (Choices.HasChoices) return;

                string text = !string.IsNullOrEmpty(_buffer) ? _buffer : LiveSpeech();
                if (string.IsNullOrEmpty(text)) return;

                // 走 Say：如果停下来的正好是刚刚念过的那一句，去重会拦住它，
                // 不会出现「停下来又念一遍一样的」。
                Say(text, true);
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("[Reader] AnnounceAfterSkipStop 失败: " + e.Message);
            }
        }

        /// <summary>快进期间是否应该继续更新缓冲区（永远要，见 NoteScene 的注释）。</summary>
        public static string BufferForDiag { get { return _buffer; } }

        // ================= 手机 =================

        public static void OnPhoneMessage(string text, bool isLeft, string speaker)
        {
            if (!Plugin.CfgReadPhone.Value) return;
            try
            {
                // 手机聊天的后续消息不走 StartTyping，所以剧情选项那一侧的清理
                // 不会顺手发生，这里要自己把手机选项作废。
                Choices.ClearPhone();

                string body = TextProc.ToSpeech(text);
                if (string.IsNullOrEmpty(body)) return;
                string prefix = string.IsNullOrEmpty(speaker)
                    ? (isLeft ? "对方：" : "我：")
                    : speaker + "：";
                Say(prefix + body, true);
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("[Reader] OnPhoneMessage 失败: " + e.Message);
            }
        }

        public static void OnPhoneSticker(bool isLeft)
        {
            if (!Plugin.CfgReadPhone.Value || !Plugin.CfgReadPhoneSticker.Value) return;
            Say(isLeft ? "对方发来一张表情图片。" : "我发了一张表情图片。", true);
        }

        /// <summary>场景切换时清掉残留状态。</summary>
        public static void Reset()
        {
            _pending = null;
            _current = null;
        }
    }
}
