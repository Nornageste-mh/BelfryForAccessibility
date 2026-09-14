using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BelfryA11y
{
    /// <summary>
    /// 《钟塔》读屏无障碍补丁。
    ///
    /// 挂载方式与《透明的她与真实的我》一致：BepInEx 5 运行时注入，
    /// **不修改任何游戏文件**，安装就是往游戏根目录拷文件，卸载就是删掉。
    ///
    /// 补丁点一览（方法名全部来自 Assembly-CSharp.dll 的反编译实查）：
    ///
    ///   功能                        挂载点
    ///   ------------------------    ------------------------------------------
    ///   抓住当前这一行（含配音判定） DialogueCommandExecutor.ExecuteDialogue
    ///   朗读无配音台词              UISceneController.StartTyping
    ///   朗读手书（第 5 章 44 行）   ShoushuDialoguePresenter.PresentLineAsync
    ///   朗读选项 + 数字键选择        ChoiceHandler.ExecuteSelectionLogic / ExecuteRealTimeLogic
    ///   限时选择延长倒计时           ChoiceHandler.AutoDestroyAfterTime（只改 ref delay）
    ///   朗读手机消息                PhoneDialogueManager.AddMessage
    ///   手机选项                    PhoneDialogueManager.HandleChoiceMessages
    ///   手机表情贴纸                PhoneDialogueManager.HandleExpressionMessage
    ///   拦住游戏自己的「推进剧情」   GameFlowManager.HandleAdvanceInput
    ///   拦住面板里的「回车=点选中项」MainUIController.ExecuteClickOnSelectedElement
    ///   标题旋转菜单让位给导航       ClockwiseMenuController.HandleKeyboardGamepadInput
    ///   菜单 / 存档 / 设置键盘导航   无挂载点，UiNav 每帧扫描 Selectable
    /// </summary>
    [BepInPlugin(Guid, "The Belfry A11y Reader", "0.1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "belfry.a11y.reader";

        internal static ManualLogSource Log;

        // ---- 朗读 ----
        internal static ConfigEntry<bool> CfgReadUnvoiced;
        internal static ConfigEntry<bool> CfgSpeakWhenUnknown;
        internal static ConfigEntry<bool> CfgSpeakName;
        internal static ConfigEntry<bool> CfgReadChoices;
        internal static ConfigEntry<bool> CfgChoiceHotkeys;
        internal static ConfigEntry<float> CfgRealTimeSeconds;
        internal static ConfigEntry<string> CfgSilenceKey;
        internal static ConfigEntry<string> CfgRepeatKey;
        internal static ConfigEntry<string> CfgSpeechBackend;
        internal static ConfigEntry<bool> CfgReadPhone;
        internal static ConfigEntry<bool> CfgReadPhoneSticker;

        // ---- 界面导航 ----
        internal static ConfigEntry<bool> CfgMenuNav;
        internal static ConfigEntry<bool> CfgSortByPosition;
        internal static ConfigEntry<bool> CfgVisibleOnly;
        internal static ConfigEntry<bool> CfgQuitConfirm;
        internal static ConfigEntry<string> CfgQuitNames;
        internal static ConfigEntry<string> CfgExcludeNames;

        // ---- 其它 ----
        internal static ConfigEntry<bool> CfgStartupHint;
        internal static ConfigEntry<bool> CfgDiagLog;

        private Harmony _harmony;
        private float _hintAt = -1f;
        private bool _hinted;

        private void Awake()
        {
            Log = Logger;

            // ============ 朗读 ============
            CfgReadUnvoiced = Config.Bind("朗读", "朗读无配音剧情", true,
                "朗读没有配音的剧情文本（旁白、主角内心、无配音配角）。\n" +
                "有配音的台词不朗读，只放语音——避免读屏和语音叠在一起。\n" +
                "全作 3812 行里 1491 行有配音（39.1%），其余 2321 行靠这个开关才听得到。");

            CfgSpeakWhenUnknown = Config.Bind("朗读", "无法判定时也朗读", true,
                "当抓不到当前行的剧本数据时（例如刚读档跳进剧情），选择朗读而不是跳过。\n" +
                "关掉可以避免把上一句的残留念错，但代价是极少数行会听不到。");

            CfgSpeakName = Config.Bind("朗读", "朗读时带上说话人", true,
                "在台词前加上说话人名字，例如「许堇：冬，快要到站了，醒醒」。\n" +
                "旁白没有名字，不会加前缀。");

            CfgReadChoices = Config.Bind("朗读", "朗读选项", true,
                "出现选项时，把全部选项一次念完并编号。\n" +
                "本作剧情选项只有 3 处（表4 两处、表5 一处），另有手机里的选项。");

            CfgChoiceHotkeys = Config.Bind("朗读", "数字键选择选项", true,
                "用数字键 1-9 选择对应编号的选项（剧情选项与手机选项通用）。\n" +
                "选项按钮是散在画面上的图标，读屏无法用键盘定位，所以需要这个。");

            CfgRealTimeSeconds = Config.Bind("朗读", "限时选择时长", 20f,
                "限时选择的倒计时秒数，游戏原版硬编码 8 秒。\n" +
                "读屏念完选项需要更多时间，所以默认放宽到 20 秒；填 8 即恢复原版节奏。\n" +
                "**倒计时只能拉长，不能取消**：到点会走这一批最后一条的 ToNumber，\n" +
                "也就是「什么都不做」的沉默分支，取消掉等于删掉一个剧情分支。\n" +
                "注意：本作的剧本表里目前没有 TypeName == \"实时\" 的行，\n" +
                "这项是为游戏更新后留的保险，眼下不会生效。");

            CfgSilenceKey = Config.Bind("朗读", "沉默按键", "0",
                "在限时选择里立刻选择「沉默」（什么都不做），不必等倒计时走完。\n" +
                "填一个数字（0-9）时主键盘和小键盘都认；也可以填 KeyCode 名称，例如 Z、F1。\n" +
                "留空则关闭。选项用 1-9，所以 0 不会冲突。");

            CfgRepeatKey = Config.Bind("朗读", "重读按键", "Backspace",
                "重新朗读当前这一句的按键。填 KeyCode 名称，例如 Backspace、Tab、F1、Home。\n" +
                "留空则关闭这个功能。\n" +
                "警告：游戏原生占用了 A 自动、F 快进、S 快速存档、D 快退、Q 下一选项、\n" +
                "W 流程图、P 返回、N 下一场景、B 返回选项、V 收藏语音、R 历史、L 读档、\n" +
                "F1-F10 各功能、Ctrl 跳过 —— 不要填这些。");

            CfgSpeechBackend = Config.Bind("朗读", "语音后端", "自动",
                "用哪个后端朗读。默认「自动」按 Tolk → NVDA → SAPI 的顺序挑第一个可用的。\n" +
                "可以填：自动 / Tolk / NVDA / SAPI。\n" +
                "钉死某一个主要用来排查问题（例如填 SAPI 就能确认系统语音这条路通不通）；\n" +
                "钉死的后端不可用时**不会**回退到别的后端，而是彻底不出声。");

            CfgReadPhone = Config.Bind("朗读", "朗读手机消息", true,
                "朗读游戏里的手机聊天消息。游戏里共 3 段电话/聊天剧情。");

            CfgReadPhoneSticker = Config.Bind("朗读", "朗读手机表情", true,
                "手机聊天里的表情图片提示为「表情图片」。");

            // ============ 界面导航 ============
            CfgMenuNav = Config.Bind("界面导航", "菜单键盘导航", true,
                "让标题菜单 / 存读档 / 设置 / 画廊可以用键盘操作并被朗读。\n" +
                "游戏原本这些界面几乎只能鼠标点。\n" +
                "  Tab          进入 / 退出导航模式\n" +
                "  上 / 下      上一项 / 下一项\n" +
                "  左 / 右      调整滑条\n" +
                "  回车 / 空格  激活（按钮点击、开关切换、输入框聚焦）\n" +
                "  Home / End   第一项 / 最后一项\n" +
                "  PageUp/PageDown  切换面板组\n" +
                "回车 / 空格的归属：只有「导航模式下且有选中项」时才是激活控件；\n" +
                "其余情况一律归还给游戏，也就是照常推进剧情。");

            CfgSortByPosition = Config.Bind("界面导航", "按屏幕位置排序控件", true,
                "导航时按控件在屏幕上的位置排序（先上后下、同一行先左后右），\n" +
                "让「第几项」和画面对得上。\n" +
                "关掉则改回按渲染层级（兄弟节点序号）排序。");

            CfgVisibleOnly = Config.Bind("界面导航", "只导航看得见的控件", true,
                "只把画面上真正能看到、能点到的控件纳入导航。\n" +
                "游戏里不少控件是 active 的却停在画面外或被面板挡住\n" +
                "（标题场景里整套画廊/设置面板都是这样）。\n" +
                "只有在发现正常按钮被误排除时才需要关掉。");

            CfgQuitConfirm = Config.Bind("界面导航", "退出前二次确认", true,
                "标题画面的旋转菜单里，「退出游戏」和「开始游戏」是同一圈上的图标，\n" +
                "靠位置区分——而这种视觉信息读屏拿不到，按错一次整个游戏直接关掉。\n" +
                "开启后，在导航模式下激活下面列出的控件会先朗读一次确认，\n" +
                "再按一次回车或空格才真的退出；按方向键即取消。");

            CfgQuitNames = Config.Bind("界面导航", "退出确认对象名", "Exit",
                "哪些控件需要上面那道二次确认，按 Unity 里的对象名精确匹配。\n" +
                "默认 Exit —— 这个名字是从游戏资源里实查的：标题场景的旋转菜单是\n" +
                "Gallery / Load / Start / Options / Exit 五个按钮（见 probe 的场景树转储）。\n" +
                "剧情中的「返回标题」游戏自己会弹确认框，不在这个列表里。\n" +
                "多个名字用逗号分隔；留空则关闭。");

            CfgExcludeNames = Config.Bind("界面导航", "排除的对象名", "Attention",
                "这些对象**及其整棵子树**里的控件不纳入导航。\n" +
                "默认 Attention —— 标题画面旋转菜单旁边那条社交链接条\n" +
                "（Steam / 小黑盒 / Bilibili），实测会往菜单里塞 5 项噪音，\n" +
                "其中还有一个嵌套的同名按钮。\n" +
                "对象名从 probe 的运行时场景树转储里实查。多个名字用逗号分隔；留空则关闭。");

            // ============ 其它 ============
            CfgStartupHint = Config.Bind("其它", "启动时播报", true,
                "游戏启动后朗读一句「无障碍补丁已加载」的提示，用来确认读屏通路是通的。\n" +
                "如果你已经能听到剧情朗读，可以关掉它。");

            CfgDiagLog = Config.Bind("其它", "界面诊断日志", false,
                "把进入导航模式时扫描到的控件全部写进 LogOutput.log，包括：\n" +
                "  · 每一组、每一项的朗读文本、屏幕行号与层级路径\n" +
                "  · 同一场景里「存在但没被纳入导航」的控件，以及被排除的原因\n" +
                "用于排查「某个控件定位不到」「只念类型不念文字」。\n" +
                "排查完请关掉，否则日志会变得很大。");

            try
            {
                Speech.Init(Log);
                if (Speech.Current == Speech.Backend.None)
                    Log.LogWarning("语音不可用: " + Speech.LastError);
                else
                    Log.LogInfo("语音后端就绪: " + Speech.BackendName);
            }
            catch (Exception e)
            {
                Log.LogWarning("语音初始化异常: " + e.Message);
            }

            _harmony = new Harmony(Guid);
            try
            {
                _harmony.PatchAll(typeof(Patches));
                Log.LogInfo("Harmony 补丁已应用。");
            }
            catch (Exception e)
            {
                Log.LogError("Harmony 补丁失败: " + e);
            }

            _hintAt = Time.realtimeSinceStartup + 6f;
        }

        private void Update()
        {
            try
            {
                if (CfgMenuNav != null && CfgMenuNav.Value) UiNav.Update();
                Choices.Update();

                HandleRepeatKey();
                HandleStartupHint();
            }
            catch (Exception e)
            {
                // 每帧的异常不能刷屏：只记一次
                if (!_updateErrorLogged)
                {
                    _updateErrorLogged = true;
                    Log.LogError("[Plugin] Update 异常: " + e);
                }
            }
        }

        private bool _updateErrorLogged;

        private KeyCode _repeatKey = KeyCode.None;
        private bool _repeatKeyParsed;

        private void HandleRepeatKey()
        {
            if (!_repeatKeyParsed)
            {
                _repeatKeyParsed = true;
                _repeatKey = KeyEdge.Parse(CfgRepeatKey != null ? CfgRepeatKey.Value : "");
                if (_repeatKey != KeyCode.None)
                    Log.LogInfo("重读按键: " + _repeatKey);
            }
            if (_repeatKey == KeyCode.None) return;

            if (KeyEdge.Pressed(_repeatKey)) Reader.Repeat();
        }

        private void HandleStartupHint()
        {
            if (_hinted) return;
            if (_hintAt < 0f || Time.realtimeSinceStartup < _hintAt) return;
            _hinted = true;
            if (CfgStartupHint == null || !CfgStartupHint.Value) return;
            if (!Speech.Ready()) return;
            Speech.Speak("钟塔无障碍补丁已加载。按 Tab 进入菜单导航，退格键重读当前这一句。", true);
        }

        private void OnDestroy()
        {
            try { Speech.Shutdown(); } catch { }
            try { _harmony?.UnpatchSelf(); } catch { }
        }
    }

    // ==================== 补丁 ====================

    internal static class Patches
    {
        // ---------- 剧情 ----------

        [HarmonyPatch(typeof(DialogueCommandExecutor), nameof(DialogueCommandExecutor.ExecuteDialogue))]
        [HarmonyPrefix]
        private static void ExecuteDialogue_Pre(DialogueScene scene)
        {
            Reader.NoteScene(scene);
        }

        [HarmonyPatch(typeof(UISceneController), nameof(UISceneController.StartTyping))]
        [HarmonyPostfix]
        private static void StartTyping_Post(string text)
        {
            Reader.OnTyping(text);
        }

        [HarmonyPatch(typeof(ShoushuDialoguePresenter), nameof(ShoushuDialoguePresenter.PresentLineAsync))]
        [HarmonyPrefix]
        private static void PresentLine_Pre(string text, bool instant)
        {
            Reader.OnShoushu(text, instant);
        }

        // ---------- 选项 ----------

        [HarmonyPatch(typeof(ChoiceHandler), nameof(ChoiceHandler.ExecuteSelectionLogic))]
        [HarmonyPostfix]
        private static void Selection_Post(ChoiceHandler __instance, List<DialogueScene> selectionScenes)
        {
            Choices.RegisterSelection(__instance, selectionScenes);
        }

        [HarmonyPatch(typeof(ChoiceHandler), nameof(ChoiceHandler.ExecuteRealTimeLogic))]
        [HarmonyPostfix]
        private static void RealTime_Post(ChoiceHandler __instance, List<DialogueScene> realtimeScenes)
        {
            Choices.RegisterRealTime(__instance, realtimeScenes);
        }

        /// <summary>
        /// 限时选择的倒计时。
        ///
        /// **绝不能用「Prefix 返回 false 跳过」** —— AutoDestroyAfterTime 是迭代器，
        /// 跳过会让它返回 null，而调用方是 StartCoroutine(...)，直接 NRE。
        /// 只能改 ref 参数。倒计时是一条真剧情分支（到点走沉默），不能取消。
        /// </summary>
        [HarmonyPatch(typeof(ChoiceHandler), "AutoDestroyAfterTime")]
        [HarmonyPrefix]
        private static void AutoDestroy_Pre(ref float delay)
        {
            try
            {
                float want = Plugin.CfgRealTimeSeconds.Value;
                if (want < 1f) want = 1f;
                if (want > 600f) want = 600f;
                if (want > delay) delay = want;
                Plugin.Log?.LogInfo("[Patches] 限时选择倒计时 " + delay + " 秒");
            }
            catch { }
        }

        // ---------- 手机 ----------

        [HarmonyPatch(typeof(PhoneDialogueManager), "AddMessage")]
        [HarmonyPostfix]
        private static void AddMessage_Post(string messageText, bool isLeft)
        {
            Reader.OnPhoneMessage(messageText, isLeft, null);
        }

        // HandleChoiceMessages / HandleExpressionMessage 都是 private，
        // 外部拿不到 nameof，只能写字符串；名字来自反编译实查。
        [HarmonyPatch(typeof(PhoneDialogueManager), "HandleChoiceMessages")]
        [HarmonyPostfix]
        private static void PhoneChoice_Post(PhoneDialogueManager __instance)
        {
            Choices.RegisterPhone(__instance);
        }

        [HarmonyPatch(typeof(PhoneDialogueManager), "HandleExpressionMessage")]
        [HarmonyPostfix]
        private static void PhoneExpression_Post(PhoneDialogue dialogue)
        {
            bool isLeft = dialogue != null && !string.IsNullOrEmpty(dialogue.LeftText);
            Reader.OnPhoneSticker(isLeft);
        }

        // ---------- 按键归属 ----------

        /// <summary>
        /// 导航模式下拦掉游戏自己的「推进剧情」。
        ///
        /// GameFlowManager.Update 是 DefaultExecutionOrder(20)，它读的是
        /// InputManager.IsActionTriggered(Advance)（空格 / 回车 / 小键盘回车 /
        /// 左键 / 滚轮）。导航模式下这几下属于我们，不能同时把剧情也推一行。
        /// </summary>
        [HarmonyPatch(typeof(GameFlowManager), "HandleAdvanceInput")]
        [HarmonyPrefix]
        private static bool HandleAdvanceInput_Pre()
        {
            return !UiNav.BlockGameAdvance;
        }

        /// <summary>
        /// 面板打开时，MainUIController 也会把「回车 / 空格」当成「点击当前选中项」。
        /// 我们已经在 UiNav 里激活过了，这里要拦掉，否则一次回车点两下
        /// （例如存档槽会被确认两次）。
        /// </summary>
        [HarmonyPatch(typeof(MainUIController), "ExecuteClickOnSelectedElement")]
        [HarmonyPrefix]
        private static bool ExecuteClickOnSelectedElement_Pre()
        {
            return !UiNav.BlockGameAdvance;
        }

        /// <summary>
        /// 标题画面是一个旋转环形菜单（ClockwiseMenuController）：
        /// 上/下方向键是在「转动」菜单，回车/空格无条件执行**正中间**那一项，
        /// 跟画面上高亮哪个无关。
        ///
        /// 导航模式打开时我们要用方向键在控件列表里走，所以把游戏的旋转输入
        /// 让开；否则一次方向键会既换我们的项、又转一次菜单。
        /// 导航模式关闭时完全保持原版行为。
        /// </summary>
        [HarmonyPatch(typeof(ClockwiseMenuController), "HandleKeyboardGamepadInput")]
        [HarmonyPrefix]
        private static bool ClockwiseInput_Pre()
        {
            return !UiNav.Active;
        }

        /// <summary>
        /// uGUI 自己的那条 submit 通路。
        ///
        /// StandaloneInputModule 在回车/空格/手柄 Submit 时会向
        /// EventSystem.currentSelectedGameObject 发一次 submit。UiNav 为了让
        /// 游戏自己的高亮态生效，会把当前项设成选中对象（SelectByUs），
        /// 于是同一次回车会既被我们处理、又被 uGUI 提交一次 —— 存档槽那种
        /// 会真的存两次。
        ///
        /// 这里只在我们**确实要接管这一下**时返回 false；非导航模式下完全不碰，
        /// 游戏原本的横条导航 / 存档槽 OnSubmit / 手柄提交全部照常。
        /// </summary>
        [HarmonyPatch(typeof(StandaloneInputModule), "SendSubmitEventToSelectedObject")]
        [HarmonyPrefix]
        private static bool SendSubmit_Pre()
        {
            return !UiNav.ShouldMuteUnitySubmit;
        }
    }
}
