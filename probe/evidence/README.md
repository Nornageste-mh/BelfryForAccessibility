# 实机验证证据

本目录只放**不含剧本文本**的证据。

游戏截图与含台词原文的会话日志**故意不入库** —— 画面本身就是游戏美术资源，
`[朗读]` 日志行就是剧本原文。仓库的「不包含任何游戏资源」必须字面成立。
需要复核原始日志时，请在本机按 `README.md`「如何复现分析」重新跑一遍。

---

## 本目录的文件

| 文件 | 内容 |
| --- | --- |
| `scene_tree.txt` | 标题场景的运行时 GameObject 层级 + 挂载的组件类型 |
| `probe_full_dump.log` | 探针插件的完整输出：补丁点清单、场景树、活着的 MonoBehaviour 类型 |

探针**不记录任何台词文本**（见 `../plugin/ProbePlugin.cs`，只在日志里写
类型名、方法名、对象名与控件标签）。

---

## 实测结论（2026 年 9 月，开发机）

环境：Windows，Unity 2022.3.43f1c1，BepInEx 5.4.23.5，NVDA 运行中。

### 1. BepInEx 能挂进游戏

```
[Message:   BepInEx] BepInEx 5.4.23.5 - The Belfry
[Info   :   BepInEx] Running under Unity v2022.3.43.8004226
[Info   :   BepInEx] CLR runtime version: 4.0.30319.42000
[Info   :   BepInEx] 1 plugin to load
[Info   :   BepInEx] Loading [The Belfry A11y Reader 0.1.0.0]
```

结论：**Mono 后端确认**，不需要 IL2CPP / Il2CppInterop。

### 2. 全部补丁点在运行时存在

`probe_full_dump.log` 里逐类型列出了方法名，下面这些是补丁实际挂的：

```
UISceneController     : StartTyping, UpdateCharacterUI, ToggleMainUI, ...
ScriptEngine          : DisplayNextLine, JumpToLine, IsStopPoint, ...
ChoiceHandler         : ExecuteSelectionLogic, ExecuteRealTimeLogic,
                        AutoDestroyAfterTime, GenerateReplyButton, ...
DialogueCommandExecutor: ExecuteDialogue, ExecuteSelection, ExecuteRealTime, ...
DialogueSceneManager  : （本作保留的旧管理器，仍在程序集里）
PhoneDialogueManager  : AddMessage, HandleChoiceMessages,
                        HandleExpressionMessage, ...
MainMenuHandler       : StartGame, LoadSave, OpenGallery, OpenSettings, QuitGame
MainBarNavigationController: SetSelectedGameObject 式的横条导航
ClockwiseMenuController: 标题旋转环形菜单
GlobalFocusFixer      : 每 0.2 秒修正焦点
SaveSlotUI            : ISubmitHandler
```

结论：`Harmony PatchAll` 全部成功，日志里没有 `Failed to patch`。

### 3. 标题菜单是纯图片按钮，且带社交链接噪音

`scene_tree.txt` 里标题菜单的实际结构：

```
Canvas
  Panel
    Background
      Title
      Menu
        1
          Buttons        [ClockwiseMenuController]
            Gallery      [Image,Button,CanvasGroup]    ← 无 TMP 文本
            Load         [Image,Button,CanvasGroup]    ← 无 TMP 文本
            Start        [Image,Button,CanvasGroup]    ← 无 TMP 文本
            Options      [Image,Button,CanvasGroup]    ← 无 TMP 文本
            Exit         [Image,Button,CanvasGroup]    ← 无 TMP 文本
        Attention        [Image,VerticalLayoutGroup,LinkManager,Button]
          Steam          [Image,Button]
            Image        [Image,Button]                ← 嵌套的同名按钮
          Xiaoheihe      [Image,Button]
          Bilibili       [Image,Button]
```

两条结论：

1. 五个菜单按钮**一个字都没有**（没有 `TextMeshProUGUI` 文本），
   所以必须按对象名做中文别名 —— 这就是 `UiNav.NameAlias` 里那五条的依据。
2. `Attention` 子树会往导航列表里塞 5 项无关的外部链接
   （其中还有一个嵌套的同名按钮），所以默认把它整棵子树排除。

### 4. 导航模式的朗读序列

按下 `Tab` 后逐项下移，日志里记录的播报文本（**控件名，非剧情**）：

```
导航模式，第 1 组，共 5 项。开始游戏，按钮。1 / 5
读取存档，按钮。2 / 5
画廊，按钮。3 / 5
系统设置，按钮。4 / 5
退出游戏，按钮。5 / 5
```

结论：排除名单生效（`Attention` 子树不见了），五个别名全部命中。

### 5. 剧情朗读与配音跳过

进入剧情后推进 45 行，日志里出现 38 条 `[朗读]` 记录
（具体文本属剧本内容，本文不复制）。

核对方式：把日志里的 `[朗读]` 序列与 `extracted/textasset_TheBelfrySchedule1.txt`
的前若干行逐条比对，结果是 ——

- `TypeName == "旁白"`（无说话人）的行全部被朗读
- `VoiceFilename` 为空的对话行被朗读，且带「说话人：」前缀
- `VoiceFilename` **非空**的行（有配音）**没有**出现在 `[朗读]` 序列里

结论：配音判定按设计工作，3812 行里 1491 行有配音的那部分正确地让位给了游戏语音。

### 6. 开发期发现并修掉的问题

| 现象 | 原因 | 处理 |
| --- | --- | --- |
| `Harmony 补丁失败: Parameter "phoneDialogue" not found` | 补丁方法的前缀参数名写成了反编译里的名字，运行时的参数名是 `dialogue` | 改参数名。注意：`PatchAll` 里单个方法失败**不会**阻止其它补丁生效，症状很隐蔽 |
| 发一次 `Tab`，出现「进入导航 → 退出导航 → 进入导航」三次迁移 | 合成输入（`keybd_event`）下 `Input.GetKeyDown` 会在一次按下里报两次 | 新增 `KeyEdge` 按下闩锁：一次「按下-抬起」只算一次 |
| 标题导航里混进 5 项社交链接 | `Attention` 子树 | 新增配置「排除的对象名」，默认 `Attention`，按祖先链整棵子树排除 |
