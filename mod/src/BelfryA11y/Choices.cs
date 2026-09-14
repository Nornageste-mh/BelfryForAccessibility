using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BelfryA11y
{
    /// <summary>
    /// 选项朗读与数字键选择。
    ///
    /// === 为什么不去调 jumpCallback，而是去点按钮 ===
    ///
    /// ChoiceHandler 里点击一个选项要做的**不止跳转**：
    ///   OnChoiceButtonClicked 会 StopRTC()、ClearAllReplyButtons()（把散在画面上
    ///   的选项按钮销毁）、ToggleMainUI(true)（限时选择时主 UI 是被藏起来的）、
    ///   再 playerState.RecordChoice(...)，最后才 jumpCallback(nextIndex)。
    ///
    /// 如果我们图省事直接调 scriptEngine.JumpToLine，按钮会留在屏幕上不消失，
    /// 限时选择的主 UI 也不会恢复。所以这里保存的是**按钮本身**，选中时直接
    /// `button.onClick.Invoke()` —— 走游戏自己的完整流程。
    ///
    /// 顺带解决一件事：ToNumber 为空或 "0.0" 的选项在游戏里是 `interactable = false`
    /// 的（GenerateReplyButton 里那条分支），这类按钮**没有挂监听**，
    /// Invoke 是空操作。我们照读，但标成「不可用」，也不会被数字键选中。
    ///
    /// === 本作有多少选项 ===
    ///
    /// 把五张表全扫过：TypeName == "选择" 的只有 4 行，集中在表 4 和表 5：
    ///   表4  564 找寻踪影 →566     565 跨过门扉 →571
    ///   表5  755 前往「彼岸」→756  869 回归「此岸」→870
    /// 没有 TypeName == "实时" 的行，所以本作没有限时选择。
    /// 限时那一套代码仍然保留：ChoiceHandler 里有 ExecuteRealTimeLogic /
    /// AutoDestroyAfterTime，游戏更新后一旦用上，补丁不会失灵。
    /// </summary>
    internal static class Choices
    {
        private sealed class Item
        {
            public string Text = "";
            public Button Button;
            public bool Disabled;
        }

        private static readonly List<Item> _story = new List<Item>();
        private static readonly List<Item> _phone = new List<Item>();

        private static Action _silence;         // 限时选择的「沉默」动作
        private static bool _timed;

        // ChoiceHandler.activeReplyButtons 是私有字段，只能反射读
        private static readonly FieldInfo ActiveButtonsField =
            typeof(ChoiceHandler).GetField("activeReplyButtons",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo ChoiceParentField =
            typeof(PhoneDialogueManager).GetField("choiceParentPrefab",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        public static bool HasChoices { get { return _story.Count > 0 || _phone.Count > 0; } }

        // ================= 注册 =================

        /// <summary>剧情选项（普通选择）。</summary>
        public static void RegisterSelection(ChoiceHandler handler, List<DialogueScene> scenes)
        {
            Build(handler, scenes, timed: false, silence: null);
        }

        /// <summary>剧情选项（限时）。</summary>
        public static void RegisterRealTime(ChoiceHandler handler, List<DialogueScene> scenes)
        {
            // 限时选择的「什么都不做」分支 = 这一批最后一条的 ToNumber。
            // 游戏在 AutoDestroyAfterTime 到点时就是跳这个值。
            Action silence = null;
            if (scenes != null && scenes.Count > 0)
            {
                string to = scenes[scenes.Count - 1]?.ToNumber;
                if (!string.IsNullOrEmpty(to))
                {
                    var engine = UnityEngine.Object.FindObjectOfType<ScriptEngine>();
                    if (engine != null)
                    {
                        int target;
                        if (int.TryParse(to, out target))
                            silence = delegate { engine.JumpToLine(target); };
                    }
                }
            }
            Build(handler, scenes, timed: true, silence: silence);
        }

        private static void Build(ChoiceHandler handler, List<DialogueScene> scenes, bool timed, Action silence)
        {
            _story.Clear();
            _timed = timed;
            _silence = silence;

            try
            {
                var buttons = ActiveButtonsField != null
                    ? ActiveButtonsField.GetValue(handler) as List<GameObject>
                    : null;
                if (buttons == null || buttons.Count == 0) return;

                for (int i = 0; i < buttons.Count; i++)
                {
                    GameObject go = buttons[i];
                    if (go == null) continue;

                    string text = scenes != null && i < scenes.Count
                        ? TextProc.ToSpeech(scenes[i]?.ChoiceText)
                        : "";
                    if (string.IsNullOrEmpty(text))
                    {
                        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
                        text = tmp != null ? TextProc.ToSpeech(tmp.text) : "";
                    }

                    var btn = go.GetComponent<Button>();
                    bool disabled = btn == null || !btn.interactable;

                    _story.Add(new Item { Text = text, Button = btn, Disabled = disabled });
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("[Choices] 注册选项失败: " + e.Message);
                return;
            }

            AnnounceChoices(_story, timed);
        }

        /// <summary>手机里的选项按钮。</summary>
        public static void RegisterPhone(PhoneDialogueManager mgr)
        {
            _phone.Clear();
            try
            {
                var parent = ChoiceParentField != null
                    ? ChoiceParentField.GetValue(mgr) as RectTransform
                    : null;
                if (parent == null) return;

                for (int i = 0; i < parent.childCount; i++)
                {
                    var child = parent.GetChild(i);
                    if (child == null) continue;
                    var tmp = child.GetComponentInChildren<TextMeshProUGUI>();
                    var btn = child.GetComponent<Button>();
                    _phone.Add(new Item
                    {
                        Text = tmp != null ? TextProc.ToSpeech(tmp.text) : "",
                        Button = btn,
                        Disabled = btn == null || !btn.interactable
                    });
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("[Choices] 注册手机选项失败: " + e.Message);
                return;
            }

            AnnounceChoices(_phone, timed: false);
        }

        public static void Clear()
        {
            _story.Clear();
            _phone.Clear();
            _silence = null;
            _timed = false;
        }

        // ================= 朗读 =================

        private static void AnnounceChoices(List<Item> list, bool timed)
        {
            if (!Plugin.CfgReadChoices.Value) return;
            if (list.Count == 0) return;

            var sb = new System.Text.StringBuilder();
            if (timed) sb.Append("限时选择。");
            sb.Append("出现 ").Append(list.Count).Append(" 个选项。");

            for (int i = 0; i < list.Count && i < 9; i++)
            {
                sb.Append("第 ").Append(i + 1).Append(" 项：");
                sb.Append(string.IsNullOrEmpty(list[i].Text) ? "（无文字）" : list[i].Text);
                if (list[i].Disabled) sb.Append("，不可选");
                sb.Append("。");
            }
            sb.Append("按数字键选择。");

            Speech.Speak(sb.ToString(), true);
            Plugin.Log?.LogInfo("[Choices] " + (timed ? "限时" : "") + "选项 x" + list.Count);
        }

        // ================= 输入 =================

        private static bool InputFieldFocused()
        {
            try
            {
                var es = EventSystem.current;
                if (es == null) return false;
                var go = es.currentSelectedGameObject;
                if (go == null) return false;
                return go.GetComponent<TMP_InputField>() != null;
            }
            catch { return false; }
        }

        /// <summary>由 Plugin.Update 每帧调用。</summary>
        public static void Update()
        {
            if (!Plugin.CfgChoiceHotkeys.Value) return;
            if (!HasChoices) return;
            if (InputFieldFocused()) return;

            if (_phone.Count > 0)
            {
                HandleList(_phone, "手机选项");
                return;
            }
            HandleList(_story, "选项");
        }

        private static void HandleList(List<Item> list, string what)
        {
            // 1-9 选对应项
            for (int n = 1; n <= 9; n++)
            {
                if (!DigitPressed(n)) continue;
                int idx = n - 1;
                if (idx >= list.Count)
                {
                    Speech.Speak("没有第 " + n + " 项。", true);
                    return;
                }
                var item = list[idx];
                if (item.Disabled || item.Button == null)
                {
                    Speech.Speak("第 " + n + " 项不可选。", true);
                    return;
                }
                try
                {
                    Speech.Speak("已选择 " + (string.IsNullOrEmpty(item.Text) ? ("第 " + n + " 项") : item.Text), false);
                    Plugin.Log?.LogInfo("[Choices] 数字键 " + n + " → " + what);
                    list.Clear();          // 点完就清，避免重复触发
                    ClearOther(list);
                    item.Button.onClick.Invoke();
                }
                catch (Exception e)
                {
                    Plugin.Log?.LogError("[Choices] 激活选项失败: " + e.Message);
                }
                return;
            }

            // 沉默键（只在限时选择里有意义）
            if (_timed && _silence != null && SilencePressed())
            {
                var act = _silence;
                _silence = null;
                _story.Clear();
                Speech.Speak("已选择沉默。", false);
                try { act(); } catch (Exception e) { Plugin.Log?.LogError("[Choices] 沉默失败: " + e.Message); }
            }
        }

        private static void ClearOther(List<Item> list)
        {
            if (ReferenceEquals(list, _phone)) _story.Clear();
            else _phone.Clear();
        }

        private static bool DigitPressed(int n)
        {
            return KeyEdge.DigitPressed(n);
        }

        /// <summary>
        /// 沉默键。配置里填单个数字时主键盘和小键盘都认。
        ///
        /// 注意坑：`Enum.TryParse("0")` 会**成功**并得到 KeyCode.None（数值 0），
        /// 而不是 Alpha0 —— 这是上一作 v0.5.7 踩过的坑，所以按键解析统一交给
        /// KeyEdge.Parse / KeyEdge.DigitPressed，不走裸 Enum.Parse。
        /// </summary>
        private static bool SilencePressed()
        {
            string cfg = Plugin.CfgSilenceKey.Value;
            if (string.IsNullOrEmpty(cfg)) return false;
            cfg = cfg.Trim();
            if (cfg.Length == 0) return false;

            if (cfg.Length == 1 && cfg[0] >= '0' && cfg[0] <= '9')
            {
                return KeyEdge.DigitPressed(cfg[0] - '0');
            }

            return KeyEdge.Pressed(KeyEdge.Parse(cfg));
        }
    }
}
