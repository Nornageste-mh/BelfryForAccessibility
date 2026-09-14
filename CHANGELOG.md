# 变更记录

本项目遵循《透明的她与真实的我》无障碍补丁的版本号习惯：
四位纯数字（`主.次.修订.构建`）。

> **注意**：`BepInPlugin` 的版本号必须能被解析成合法的 .NET `Version`。
> 写成 `0.1.0a` 这种带字母的形式，BepInEx 会判定 `version is invalid`
> 并**静默跳过整个插件** —— 日志里只有一行 Warning，游戏里一片安静，
> 极难排查。想表达「修订 a」请用第四位，例如 `0.1.0.1`。
> `mod/build.ps1` 里有一道守卫会拦住这种情况。

---

## 0.1.0.0 — 首个版本

面向 Steam 正式版《钟塔》（The Belfry，AppID 2373260）。

### 朗读

- **朗读无配音剧情文本**。挂 `UISceneController.StartTyping`。
  判定方式是 `DialogueCommandExecutor.ExecuteDialogue` 收到的
  `DialogueScene.VoiceFilename` 是否为空 —— 全作 3812 行里
  1491 行有配音（39.1%），其余 2321 行靠这个功能才听得到。
  有配音的行不朗读，只放语音，并打断上一句读屏。
- **朗读手书演出**。第 5 章那 44 行走的是
  `ShoushuDialoguePresenter.PresentLineAsync`，和普通台词不是同一条通道，
  所以单独挂了一个补丁点。
- **朗读时带说话人**。剧本里存的名字是 `【 韩冬 】`（全角括号 + 两侧空格），
  照读会变成「左黑括号 空格 韩冬 空格 右黑括号」，所以做了剥离；
  `【 ?  ? 】` 这种遮蔽名统一读作「？？？」；
  `【 许堇/韩冬 】` 读作「许堇、韩冬」。
- **重读按键**（默认 `Backspace`）。

### 选项

- **朗读选项**，一次念完并编号。
- **数字键 1-9 选择**。不是直接调跳转回调，而是保存按钮对象、
  选中时 `button.onClick.Invoke()` —— 因为 `ChoiceHandler.OnChoiceButtonClicked`
  还要销毁散在画面上的选项按钮、恢复主 UI、记录选择，
  漏掉这些会让按钮留在屏幕上。
- 本作剧情选项只有 3 处（表 4 两处、表 5 一处）。
- **限时选择**：`AutoDestroyAfterTime` 的 `ref float delay` 会被拉长到
  配置值（默认 20 秒）。本作的剧本表里目前没有 `TypeName == "实时"` 的行，
  这一项是为游戏更新留的保险。**倒计时只能拉长不能取消** ——
  到点会走这一批最后一条的 `ToNumber`，也就是「什么都不做」的沉默分支。

### 手机

- 朗读手机聊天消息（`PhoneDialogueManager.AddMessage`）。
- 朗读表情贴纸提示（`HandleExpressionMessage`）。
- 手机里的选项也能用数字键选（`HandleChoiceMessages`）。

### 界面导航

- **`Tab` 进入 / 退出导航模式**，上下选、左右调滑条、回车空格激活、
  Home/End 跳首尾、PageUp/PageDown 换面板组。
- 标题画面的旋转环形菜单在导航模式下换成位置固定的线性列表，
  并给五个纯图片按钮（开始游戏 / 读取存档 / 画廊 / 系统设置 / 退出游戏）
  做了中文别名 —— 它们的文字画在图片上，TMP 里一个字都没有。
- **退出前二次确认**：标题的「退出游戏」按一下就会关掉整个游戏，
  而且它和「开始游戏」在同一个圆环上，读屏用户区分不了，所以补了一道确认。
- 默认把社交链接栏（`Attention`：Steam / 小黑盒 / Bilibili）从导航里排除。
- 导航模式下 `拦截游戏自己的「推进剧情」（`GameFlowManager.HandleAdvanceInput`），
  避免一次空格既激活控件又推进剧情。

### 与上一作不同的三处技术决定

1. **没有**常驻关掉 `EventSystem.sendNavigationEvents`。
   《钟塔》自己就在用 EventSystem 选中态（横条导航、面板提交、
   `GlobalFocusFixer` 焦点修正、存档槽 `ISubmitHandler`），
   常驻关掉会打断这些原生功能。改成只在导航模式确实要接管那一下按键时，
   挂 `StandaloneInputModule.SendSubmitEventToSelectedObject` 掐掉 uGUI 的提交。
2. **`KeyEdge` 按下闩锁**。开发期用脚本驱动游戏时观测到合成输入下
   `Input.GetKeyDown` 会在一次按下里报两次，导致导航模式「进入→退出→进入」。
   现在所有一次性动作键（Tab / 方向键 / 回车 / 空格 / 数字键 / 重读键）
   都要求「一次按下-抬起才算一次」。
3. **补丁点全部按运行时方法名实查**，不靠猜。

### 已知限制

- 不覆盖：CG / Spine 立绘的口述影像、视频口述影像、成就弹出提示、
  历史回顾面板的逐条朗读。
- 未做配音台词的 TTS 预生成。
- 中文朗读质量取决于所选后端与系统语音包。
