using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BelfryA11yProbe
{
    /// <summary>
    /// 探针插件：只做只读观测，用来确认
    ///   1) BepInEx 能挂进《钟塔》
    ///   2) 关键 MonoBehaviour 在运行时到底在不在、叫什么名字
    ///   3) 我们打算挂的补丁点能不能被 Harmony 找到
    /// 输出写到游戏目录下的 BepInEx\probe_belfry.log（纯文本，方便脚本读）。
    /// </summary>
    [BepInPlugin(Guid, "Belfry A11y Probe", "0.0.0.1")]
    public class ProbePlugin : BaseUnityPlugin
    {
        public const string Guid = "belfry.a11y.probe";
        private static ManualLogSource L;
        private static string _dumpPath;

        private void Awake()
        {
            L = Logger;
            _dumpPath = Path.Combine(Paths.GameRootPath, "BepInEx", "probe_belfry.log");
            File.WriteAllText(_dumpPath, "=== Belfry A11y Probe ===\n");
            Say("Awake: BepInEx 已挂载，游戏根目录 = " + Paths.GameRootPath);

            try
            {
                var h = new Harmony(Guid);
                h.PatchAll(typeof(ProbePatches));
                Say("Harmony PatchAll 完成");
                ReportPatchTargets(h);
            }
            catch (Exception e)
            {
                Say("Harmony 异常: " + e);
            }

            StartCoroutine(DumpLater());
        }

        internal static void Say(string msg)
        {
            L?.LogInfo(msg);
            try { File.AppendAllText(_dumpPath, msg + "\n", Encoding.UTF8); } catch { }
        }

        private IEnumerator DumpLater()
        {
            // 等游戏把标题场景初始化完
            yield return new WaitForSeconds(8f);
            DumpScenes();
            yield return new WaitForSeconds(20f);
            DumpScenes();
        }

        private static void ReportPatchTargets(Harmony h)
        {
            var asm = typeof(ScriptEngine).Assembly;
            string[] want =
            {
                "UISceneController", "ScriptEngine", "ChoiceHandler", "DialogueCommandExecutor",
                "DialogueSceneManager", "PhoneUIManager", "PhoneDialogueManager", "MainMenuHandler",
                "MainBarNavigationController", "SaveManager", "MainUIController", "HistoryManager",
                "ShoushuDialoguePresenter", "SettingsUIManager", "ClockwiseMenuController",
                "AudioPlaybackManager", "SpineVisualsManager", "PersistentDataManager",
                "MessageBubble", "SaveSlotUI", "GroupedCGGalleryManager", "TabGroupController",
                "SettingsSlider", "SettingsSelectionGroup", "FlowChartPanelController"
            };
            foreach (var name in want)
            {
                var t = asm.GetType(name);
                if (t == null) { Say($"[type] {name}: 未找到"); continue; }
                var ms = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                          .Where(m => !m.IsSpecialName)
                          .Select(m => m.Name).Distinct().OrderBy(x => x);
                Say($"[type] {name}: OK  方法: {string.Join(", ", ms)}");
            }
        }

        private static void DumpScenes()
        {
            Say("");
            Say("################ 场景树转储 " + DateTime.Now.ToString("HH:mm:ss") + " ################");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sc = SceneManager.GetSceneAt(i);
                if (!sc.isLoaded) continue;
                Say($"===== Scene[{i}] {sc.name} (rootObjects={sc.rootCount}) =====");
                foreach (var go in sc.GetRootGameObjects())
                    Walk(go.transform, 0);
            }
            var all = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            var byType = all.Where(m => m != null)
                            .GroupBy(m => m.GetType().Name)
                            .OrderByDescending(g => g.Count())
                            .Take(60);
            Say("---- 活着的 MonoBehaviour 类型（Top60）----");
            foreach (var g in byType) Say($"  {g.Key} x{g.Count()}");
        }

        private static void Walk(Transform t, int depth)
        {
            if (depth > 6) return;
            var go = t.gameObject;
            var sb = new StringBuilder();
            sb.Append(new string(' ', depth * 2));
            sb.Append(go.activeSelf ? "+ " : "- ");
            sb.Append(t.name);
            var comps = go.GetComponents<Component>()
                           .Where(c => c != null)
                           .Select(c => c.GetType().Name)
                           .Where(n => n != "Transform" && n != "RectTransform" && n != "CanvasRenderer");
            string cs = string.Join(",", comps);
            if (cs.Length > 0) sb.Append("  [" + cs + "]");
            // 把可见文字一并带出来，方便对名字
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp != null && !string.IsNullOrEmpty(tmp.text))
            {
                string txt = tmp.text.Replace("\n", "\\n");
                if (txt.Length > 60) txt = txt.Substring(0, 60) + "…";
                sb.Append("  text=\"" + txt + "\"");
            }
            var ui = go.GetComponent<Text>();
            if (ui != null && !string.IsNullOrEmpty(ui.text))
            {
                string txt = ui.text.Replace("\n", "\\n");
                if (txt.Length > 60) txt = txt.Substring(0, 60) + "…";
                sb.Append("  legacyText=\"" + txt + "\"");
            }
            Say(sb.ToString());
            for (int i = 0; i < t.childCount; i++)
                Walk(t.GetChild(i), depth + 1);
        }
    }

    internal static class ProbePatches
    {
        // 探针只记录**结构信息**（类型、行号、有没有配音、说话人），
        // 绝不把台词原文写进日志：
        //   · 台词是游戏剧本，属于著作权人的文字资产，落到日志文件里再被
        //     提交进版本库就等于再分发；
        //   · 验证「有没有配音」「朗读有没有被触发」根本不需要原文，
        //     只需要长度和标志位。
        // 同理，选项文字也只记条数，不记内容。
        private static string Shape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "(空)";
            return "(len=" + s.Length + ")";
        }

        [HarmonyPatch(typeof(UISceneController), nameof(UISceneController.StartTyping))]
        [HarmonyPostfix]
        private static void StartTyping_Post(UISceneController __instance, string text)
        {
            string name = __instance != null && __instance.characterNameText != null
                ? __instance.characterNameText.text : "";
            ProbePlugin.Say($"[hook] UISceneController.StartTyping text={Shape(text)} nameText=\"{name}\"");
        }

        [HarmonyPatch(typeof(DialogueCommandExecutor), nameof(DialogueCommandExecutor.ExecuteDialogue))]
        [HarmonyPrefix]
        private static void ExecuteDialogue_Pre(DialogueScene scene)
        {
            if (scene == null) return;
            ProbePlugin.Say($"[hook] ExecuteDialogue type={scene.TypeName} num={scene.Number} "
                + $"hasVoice={!string.IsNullOrEmpty(scene.VoiceFilename)} "
                + $"name=\"{scene.CharacterName}\" dlg={Shape(scene.Dialogue)} "
                + $"choice={Shape(scene.ChoiceText)}");
        }

        [HarmonyPatch(typeof(ChoiceHandler), nameof(ChoiceHandler.ExecuteSelectionLogic))]
        [HarmonyPostfix]
        private static void Selection_Post(List<DialogueScene> selectionScenes)
        {
            ProbePlugin.Say("[hook] ExecuteSelectionLogic: " + Describe(selectionScenes));
        }

        [HarmonyPatch(typeof(ChoiceHandler), nameof(ChoiceHandler.ExecuteRealTimeLogic))]
        [HarmonyPostfix]
        private static void RealTime_Post(List<DialogueScene> realtimeScenes)
        {
            ProbePlugin.Say("[hook] ExecuteRealTimeLogic: " + Describe(realtimeScenes));
        }

        [HarmonyPatch(typeof(ScriptEngine), "IsStopPoint")]
        [HarmonyPostfix]
        private static void IsStopPoint_Post(string typeName, ref bool __result)
        {
            // 只观测，不改
        }

        private static string Describe(List<DialogueScene> list)
        {
            if (list == null) return "(null)";
            // 只记「第几项 → 跳到第几行」和文本长度，不记选项原文
            return list.Count + " 项: " + string.Join(" | ",
                list.Select(s => $"{s.Number}:{Shape(s.ChoiceText)}->{s.ToNumber}"));
        }
    }
}
