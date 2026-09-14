using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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
    ///   · StartTyping / PresentLineAsync 才是真正「这一行开始显示」的时刻，
    ///     到那时再决定念不念。
    ///
    /// === 为什么用「待消费」而不是全局当前行 ===
    ///
    /// ExecuteDialogue 是 async：Prefix 跑完、方法体 await 出去，期间完全可能
    /// 有别的行插进来。用一个「还没被消费的那一行」做缓冲，比在 StartTyping
    /// 里去反查 ScriptEngine.currentDialogueIndex 稳 —— 后者在快进/回退时
    /// 未必对得上正在显示的文本。
    ///
    /// isInstant（快进 / 回退 / 非停顿指令）那一支**不**调用 StartTyping，
    /// 直接写 dialogueText.text，所以天然不会被念出来 —— 这正是我们要的：
    /// 快进时读屏不该刷屏。
    /// </summary>
    internal static class Reader
    {
        private sealed class Line
        {
            public string Speech = "";     // 已加工好的朗读文本（含说话人前缀）
            public bool HasVoice;          // 这一行有配音 → 不朗读，只放语音
            public string Raw = "";        // 原始台词，用于比对
            public string Speaker = "";
            public string Type = "";
        }

        private static Line _pending;      // 刚从 ExecuteDialogue 拿到的行，尚未显示
        private static Line _current;      // 正在显示、可供「重读」的那一行
        private static bool _consumed;     // _pending 是否已被某次显示消费

        /// <summary>是否处在「能判定」的状态。开头几行可能拿不到 scene。</summary>
        private static bool _everJudged;

        // ================= 供 Patch 调用 =================

        /// <summary>ExecuteDialogue 的 Prefix：记住这一行。</summary>
        public static void NoteScene(DialogueScene scene)
        {
            if (scene == null) return;
            try
            {
                var line = new Line
                {
                    Type = scene.TypeName ?? "",
                    Speaker = TextProc.CleanName(scene.CharacterName),
                    Raw = scene.Dialogue ?? "",
                    HasVoice = !string.IsNullOrEmpty(scene.VoiceFilename)
                };
                line.Speech = BuildSpeech(line.Speaker, line.Raw);
                _pending = line;
                _consumed = false;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("[Reader] NoteScene 失败: " + e.Message);
            }
        }

        /// <summary>UISceneController.StartTyping 的 Postfix。</summary>
        public static void OnTyping(string text)
        {
            Consume(text, isShoushu: false);
        }

        /// <summary>ShoushuDialoguePresenter.PresentLineAsync 的 Prefix。</summary>
        public static void OnShoushu(string text, bool instant)
        {
            // instant=true 是回退/快进路径，不朗读
            if (instant) return;
            Consume(text, isShoushu: true);
        }

        /// <summary>真正决定「这一句念不念」。</summary>
        private static void Consume(string shownText, bool isShoushu)
        {
            try
            {
                Line line = _pending;
                _consumed = true;

                if (line == null)
                {
                    // 没抓到 scene（例如从存档跳进来、或场景切换的边界）。
                    // 按配置决定是「照念」还是「不念」。
                    if (!Plugin.CfgSpeakWhenUnknown.Value) return;
                    string fallback = TextProc.ToSpeech(shownText);
                    if (TextProc.IsTrivial(fallback)) return;
                    _everJudged = true;
                    _current = new Line { Speech = fallback, Raw = shownText, HasVoice = false };
                    Speech.Speak(fallback, true);
                    return;
                }

                _current = line;
                _everJudged = true;

                if (line.HasVoice)
                {
                    // 这一行有配音：放语音，不朗读。但要把可能还在念的上一句打断，
                    // 否则读屏会和配音叠在一起。
                    Speech.Stop();
                    return;
                }

                if (!Plugin.CfgReadUnvoiced.Value) return;
                if (string.IsNullOrEmpty(line.Speech) || TextProc.IsTrivial(line.Speech)) return;

                Speech.Speak(line.Speech, true);
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

        // ================= 重读 =================

        /// <summary>重读当前这一句。Backspace。</summary>
        public static void Repeat()
        {
            try
            {
                if (_current != null && !string.IsNullOrEmpty(_current.Speech))
                {
                    Speech.Speak(_current.Speech, true);
                    return;
                }
                // 还没有任何一句被读过：把当前 TMP 上的文字念出来兜底
                var ui = UnityEngine.Object.FindObjectOfType<UISceneController>();
                string t = ui != null && ui.dialogueText != null ? ui.dialogueText.text : "";
                if (!string.IsNullOrEmpty(t))
                {
                    Speech.Speak(TextProc.ToSpeech(t), true);
                    return;
                }
                Speech.Speak("没有可以重读的内容。", true);
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("[Reader] Repeat 失败: " + e.Message);
            }
        }

        /// <summary>手机消息。</summary>
        public static void OnPhoneMessage(string text, bool isLeft, string speaker)
        {
            if (!Plugin.CfgReadPhone.Value) return;
            try
            {
                string body = TextProc.ToSpeech(text);
                if (string.IsNullOrEmpty(body)) return;
                string prefix = string.IsNullOrEmpty(speaker) ? (isLeft ? "对方：" : "我：") : speaker + "：";
                Speech.Speak(prefix + body, true);
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("[Reader] OnPhoneMessage 失败: " + e.Message);
            }
        }

        /// <summary>手机表情贴纸。</summary>
        public static void OnPhoneSticker(bool isLeft)
        {
            if (!Plugin.CfgReadPhone.Value || !Plugin.CfgReadPhoneSticker.Value) return;
            Speech.Speak(isLeft ? "对方发来一张表情图片。" : "我发了一张表情图片。", true);
        }

        /// <summary>进入/切换到一个新的界面时读一句提示（由 UiNav 调用）。</summary>
        public static void Announce(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Speech.Speak(text, true);
        }

        public static bool HasJudged { get { return _everJudged; } }

        /// <summary>场景切换时清掉残留状态，避免把上一场景的最后一句又念一遍。</summary>
        public static void Reset()
        {
            _pending = null;
            _current = null;
            _consumed = false;
        }
    }
}
