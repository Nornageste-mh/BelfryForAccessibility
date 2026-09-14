# TheBelfryForAccessibility

本仓库是**专门针对 Steam 游戏《钟塔》（The Belfry，AppID 2373260）** 的屏幕阅读器
辅助补丁 —— 一个游戏内 BepInEx 5 插件，通过 Tolk / NVDA Controller Client /
Windows SAPI 朗读屏幕上的文字。

> ## 🎮 关于本作
>
> | | |
> |---|---|
> | 中文名 | **钟塔** |
> | 英文名 | The Belfry |
> | 开发 / 发行 | **可味玩KawayiPlay** |
> | Steam | [AppID 2373260](https://store.steampowered.com/app/2373260/) |
> | VNDB | [v61157](https://vndb.org/v61157) |
> | 发售 | **2026 年 4 月 1 日** |
> | 试玩版 | 2026 年 1 月 31 日（免费） |
> | 类型 | 短篇视觉小说（Galgame），约 10 万字，**全流程配音** |
> | 引擎 | Unity 2022.3.43f1c1，**Mono** 后端 |
> | 商店特性 | 单人游戏 · 成就（18 项）· 云存档 · 家庭共享 · 完全支持控制器 |
>
> 主角是「韩冬」。剧本共 **3812 行**，其中 **1491 行有配音（39.1%）** ——
> 剩下那 61% 就是本补丁存在的理由。
>
> **这个补丁是玩家自制的第三方工具，与可味玩KawayiPlay 没有任何关系，
> 也没有得到他们的授权或背书。** 游戏的著作权归他们所有，
> 本仓库只发布自己的补丁代码，不含任何游戏资源。
>
> 脚注（哪些是本仓库亲自核实的）：
>
> - **开发商 / 发行商 / 发售日期 / 商店特性**：由项目维护者在 Steam 客户端
>   商店页核实（开发与发行均显示 **可味玩KawayiPlay**，发行日期 **2026 年 4 月 1 日**）
> - **引擎与规模**：本机核实 —— `globalgamemanagers` 头部版本串、
>   `Managed/` 下 127 个托管程序集、以及从 `extracted/` 五张表统计出的行数与配音覆盖
> - **VNDB ID、试玩版日期、字数**：公开资料转述，本仓库未直接核实

---

> ## 致 可味玩KawayiPlay
>
> 我们欠你们一句道歉。
>
> 做这个补丁的过程中，我们反编译了你们的游戏（`Assembly-CSharp.dll`）、
> 解包了 Addressables、把剧本文字提取了出来 —— **这些都没有事先问过你们。**
>
> 技术上这是绕不开的一步：这个游戏把标题菜单的五个图标文字画成了图片
> （TMP 里一个字都没有），选项按钮又按圆环坐标摆放，不反编译就找不到能挂钩子的地方。
> 但**"必要"不等于"可以不打招呼"**。
> 我们做这件事的出发点不是不尊重这部作品，恰恰相反 ——
> 是因为觉得它值得被更多人玩到，才想让读屏玩家也能走完一遍。
>
> 已经做的补救：
>
> - 提取的剧本与反编译的代码**从未进入版本库**，只在本机用于分析，
>   已被 `.gitignore` 排除，也**不随发布包分发**
> - 证据目录 `probe/evidence/` 只收录**不含剧本文本**的材料；
>   含台词的会话日志与游戏截图故意不入库
> - 仓库与发布包**不含任何游戏资源**，只有我们自己的补丁代码和文档
> - 发给玩家的文档里没有任何剧情内容，也没有泄露结局相关的信息
> - **如果你们认为任何部分不妥，请联系我们 —— 我们会立刻调整，或者整体下架**
>
> 也请读到这里、并且用得上这个补丁的玩家：
> **它让你能玩上这款游戏，不是让你不必买这款游戏。请去买一份正版。**
> 这部作品是可味玩KawayiPlay 的。

> ## ⚠️ 郑重警告（请务必阅读）
>
> - 本项目是 **Vibe coding 产物**（AI 辅助生成），**非官方**，
>   与开发兼发行 **可味玩KawayiPlay** **没有任何关系**。
> - **请务必支持正版：本辅助仅面向已在 Steam 购买《钟塔》的玩家。**
>   **强烈要求每一位使用者通过 Steam 购买正版游戏。** 我们坚决反对任何形式的盗版、
>   破解、未授权传播；请勿将本工具用于协助获取或游玩盗版副本。
>   **这个补丁是让你能玩上这款游戏，不是让你不必买它** ——
>   可味玩KawayiPlay 的劳动成果值得被正当地支持。
> - 本项目**不保证可用、不保证稳定**，代码可能存在各种问题（兼容性、稳定性、安全性等），
>   **无任何维护承诺**。使用风险自负，仅供个人学习/研究参考，请勿用于商业或分发牟利。
> - **建议在完全理解代码的前提下再使用**，自行承担一切后果。
> - 本仓库**不包含任何游戏资源**，只发布运行时补丁代码与文档。
> - **如您是 可味玩KawayiPlay 的成员**，或本作的版权方，认为本仓库有任何不妥，
>   请联系我们，我们会立即配合调整或移除。

---

## 说明

本辅助的作用：让使用读屏软件的玩家，也能基本正常地游玩《钟塔》。

- 朗读**没有配音**的剧情文本（旁白、主角「韩冬」的内心、无配音配角），
  有配音的台词只放语音并打断朗读
- 朗读**手书演出**（流程后段那一段手绘过场，44 行，走的是另一套显示通道）
- 朗读选项，并用数字键 `1`-`9` 选择
- 朗读手机聊天消息与表情
- 主菜单 / 存读档 / 设置 / 画廊的键盘导航与朗读
  （标题菜单是一个**旋转环形菜单**，游戏原本的上下键是在「转」它、回车无条件
  执行正中间那一项；导航模式下改成位置固定的线性列表）

游戏版本：Unity 2022.3.43f1c1，**Mono** 后端。面向**通过 Steam 购买的正式版**
（AppID 2373260）。

> 试玩版（AppID 4169180）**理论上**同一套补丁也能用，但**本仓库没有实机验证过**，
> 所以不作承诺。

---

## 安装（仅限正版玩家）

**最快的装法**：到 [Releases](../../releases) 下载 `BelfryA11y-<版本>.zip`，
解压后把里面的东西**整体**拷进游戏根目录（有 `The Belfry.exe` 的那一层），
提示「是否合并/替换」时选**是**。

**没有安装程序** —— 补丁由 BepInEx 在运行时挂载，不修改任何游戏文件，
所以安装就是拷文件。zip 内容就是下文的 `mod\package\`，
其中 `licenses\` 是第三方组件的许可证与声明，**别删**。

想自己构建、或只想往已有的 BepInEx 里加一个插件，看下面。

把 `mod\package\` 里的东西**全部复制进游戏根目录**（有 `The Belfry.exe` 的那一层），
保持目录结构不变。三条容易踩的坑：

- `winhttp.dll`、`doorstop_config.ini`、`.doorstop_version` 必须在**游戏根目录**
- `BepInEx\core\` 里 16 个文件一个都不能少
- `nvdaControllerClient.dll` 放游戏根目录（Mono 查找 DLL 时先看应用目录）

如果已经装过别的 BepInEx 模组，只需要两个文件：
`BelfryA11y.dll` → `BepInEx\plugins\`，`nvdaControllerClient.dll` → 游戏根目录。
**不要**覆盖对方已有的 `winhttp.dll` 和 `BepInEx\core\`。

卸载：删掉游戏目录下的 `winhttp.dll` 即完全失效；连同 `BepInEx\` 一并删掉就彻底干净。
存档在 `%USERPROFILE%\AppData\LocalLow\Kawayi Play\The Belfry\`，不受影响。

详细步骤、按键表、每个配置项的含义见
[`mod/package/安装说明.md`](mod/package/安装说明.md)；
遇到疑问先翻 [`mod/package/常见问题.md`](mod/package/常见问题.md)。

> 再次提醒：请通过 Steam 购买正版《钟塔》后再使用本辅助。

---

## 主要快捷键

| 快捷键 | 功能 |
| --- | --- |
| `1` - `9` | 选择对应编号的选项（剧情选项、手机选项通用） |
| `0` | 限时选择里「立刻沉默」（本作目前没有限时选择，保留备用） |
| `Backspace` | 重读当前这一句 |
| `Tab` | 进入 / 退出界面导航模式 |
| `↑` `↓` | 上一项 / 下一项 |
| `←` `→` | 调整滑条 |
| `回车` / `空格` | 导航模式下激活控件；否则交给游戏推进剧情 |
| `Home` / `End` | 第一项 / 最后一项 |
| `PageUp` / `PageDown` | 切换面板组 |

**游戏原生占用的键**（模组不会去抢，也不要拿来当重读键）：
`A` 自动、`F` 快进、`S` 快速存档、`D` 快退、`Q` 下一选项、`W` 流程图、
`P` 返回、`N` 下一场景、`B` 返回选项、`V` 收藏语音、`R` 历史、`L` 读档、
`Ctrl` 跳过、`Del` 开关界面、`F1`-`F10` 各项功能、
`空格`/`回车`/小键盘回车 推进、`Esc`/右键 返回。

后四个模组自用的键都可以在配置里改或关掉。

---

## 已知问题 / 注意事项

- 通过读取游戏运行时信息工作，**游戏更新后可能失效**（补丁点是按游戏内方法名挂的）
- 标题画面的五个菜单图标（开始游戏 / 读取存档 / 画廊 / 系统设置 / 退出游戏）
  文字**画在图片上**，读屏永远拿不到。模组按 Unity 对象名做了中文映射，
  见 `UiNav.NameAlias`，依据写在注释里
- 社交链接条（Steam / 小黑盒 / Bilibili）默认**从导航里排除**，
  见配置项「排除的对象名」
- 未覆盖所有界面与交互，下列内容还没做：CG / Spine 立绘的口述影像、
  视频口述影像、成就弹出提示
- 语音朗读依赖所选后端（Tolk / NVDA / SAPI），中文需要中文语音

---

## 构建

```powershell
cd mod
.\build.ps1
```

会自动复用/下载 BepInEx 5.4.23.5 (win x64) 与 NVDA Controller Client (x64)，
编译插件并组装 `mod\package\`。第三方二进制不入库，全靠这个脚本复现。
需要 .NET SDK（本项目用 10.0.301 验证过）。

> `build.ps1` 是 `.ps1`，必须保存为 **UTF-8 带 BOM** —— Windows PowerShell 5.1
> 读无 BOM 的脚本会按 GBK 解码，中文注释变乱码并直接语法错误。

### 目录结构

```
.
├─ 无障碍可行性验证.md          技术可行性分析（含实测数据）
├─ 测试问卷.md                  实机验收清单
│
├─ mod/
│  ├─ build.ps1                 下依赖 → 编译 → 组包
│  ├─ release.ps1               打发布 zip（走 GitHub Releases）
│  ├─ src/BelfryA11y/
│  │  ├─ Plugin.cs              配置、生命周期与全部 Harmony 补丁点
│  │  ├─ Reader.cs              剧情朗读状态机（配音判定、重读缓冲区）
│  │  ├─ Choices.cs             选项朗读与数字键选择
│  │  ├─ KeyEdge.cs             带「松开闩锁」的单次按键检测
│  │  ├─ Speech.cs              Tolk / NVDA / SAPI 后端调度
│  │  ├─ Nvda.cs                NVDA Controller Client 封装
│  │  ├─ Sapi.cs                SAPI 兜底（纯 P/Invoke，不走 COM 后期绑定）
│  │  ├─ TextProc.cs            说话人名与台词的朗读加工
│  │  └─ UiNav.cs               界面键盘导航与朗读
│  ├─ licenses/                 第三方许可证与 THIRD-PARTY-NOTICES
│  └─ package/                  安装包（直接拷进游戏根目录）
│     ├─ 安装说明.md            ← 用户文档，先看这个
│     └─ 常见问题.md            故障排查
│
├─ probe/                       实机探针：验证补丁点、转储场景树、驱动按键
│  └─ evidence/                 实机证据（**只收录不含剧本文本的材料**）
│
├─ tools/                       分析脚本（UnityPy 提取剧本、统计）与提交闸门
│
└─ （以下目录不入库，见 .gitignore）
   extracted/    从游戏提取的剧本文本（版权内容）
   decompiled/   反编译的游戏代码（版权内容）
   mod/dist/     发布 zip（只进 GitHub Releases）
```

### 模组做了什么

| 功能 | 挂载点 |
|---|---|
| 抓住当前行 + 配音判定 | `DialogueCommandExecutor.ExecuteDialogue`（Prefix） |
| 朗读无配音剧情文本 | `UISceneController.StartTyping`（Postfix） |
| 朗读手书演出 | `ShoushuDialoguePresenter.PresentLineAsync`（Prefix） |
| 朗读选项 | `ChoiceHandler.ExecuteSelectionLogic` / `ExecuteRealTimeLogic`（Postfix） |
| 数字键 1-9 选择选项 | 反射读 `ChoiceHandler.activeReplyButtons`，直接 `onClick.Invoke()` |
| 限时选择延长倒计时 | `ChoiceHandler.AutoDestroyAfterTime`（只改 `ref float delay`） |
| 朗读手机消息 / 表情 | `PhoneDialogueManager.AddMessage` / `HandleExpressionMessage` |
| 手机选项 | `PhoneDialogueManager.HandleChoiceMessages` |
| 菜单 / 存档 / 设置键盘导航 | 无挂载点，`UiNav` 每帧扫描 `Selectable` |
| 按键归属：回车 / 空格 | `GameFlowManager.HandleAdvanceInput`、`MainUIController.ExecuteClickOnSelectedElement`、`StandaloneInputModule.SendSubmitEventToSelectedObject` |
| 标题旋转菜单让位 | `ClockwiseMenuController.HandleKeyboardGamepadInput` |

**判定有无配音的方式**：`ExecuteDialogue` 收到的 `DialogueScene.VoiceFilename`
是否为空 —— 全作 3812 行里 1491 行有配音（39.1%）。

### 几条踩过的坑（改之前请先读）

- **绝不对迭代器方法用「Prefix 返回 false 跳过」**：那会让它返回 `null`，
  而调用方是 `StartCoroutine(...)`。`ChoiceHandler.AutoDestroyAfterTime` 只能改 `ref` 参数
- **不要学上一作常驻关掉 `EventSystem.sendNavigationEvents`**：《钟塔》自己就在用
  EventSystem 选中态（横条导航、面板提交、焦点修正、存档槽 `ISubmitHandler`）。
  只在导航模式确实要接管那一下按键时才掐 uGUI 的 submit
- **Harmony 前缀的参数名必须和运行时一致，不是和反编译结果一致**：
  `HandleExpressionMessage` 的参数在反编译里叫 `dialogue`，
  运行时也叫 `dialogue` —— 写错名字会让 `PatchAll` 抛异常
  （其他补丁仍会生效，所以症状很隐蔽：只有一个功能不工作）
- **`Enum.TryParse("0")` 会成功但得到 `KeyCode.None`**（数值 0），不是 `Alpha0`。
  按键配置项要先自己映射单个数字
- **别用 PowerShell 的文本 cmdlet 改源码**：`Get-Content -Raw` 在
  Windows PowerShell 5.1 下按 ANSI 代码页读取无 BOM 的 UTF-8，中文会变乱码
- **BepInEx 的日志是缓冲异步刷盘的**（约 2 秒一次）。用脚本读日志来驱动自动化
  测试时，读到「上一拍」是常态，必须等够时间

---

## 如何复现分析

分析过程依赖若干 Python 脚本（需 `UnityPy`）：

```powershell
pip install UnityPy
python tools/dump_textassets.py     # 导出 resources.assets 里的 5 张剧本表
python tools/script_stats.py        # 剧本规模与配音覆盖统计
python tools/scan_assets.py         # 扫描各 Unity 资源文件的对象类型
```

反编译游戏逻辑（需 `ilspycmd`）：

```powershell
dotnet tool install --global ilspycmd
ilspycmd -p -o decompiled "<游戏目录>\The Belfry_Data\Managed\Assembly-CSharp.dll"
```

实机探针（把 BepInEx 挂进游戏，转储运行时场景树与补丁点清单）：

```powershell
cd probe
.\run_probe.ps1
```

生成的 `extracted/`、`decompiled/` 均为游戏版权内容，**已被 .gitignore 排除，
请勿提交或分发**。

---

## 合规说明

- 依据《计算机软件保护条例》第十六条第（三）项，合法复制品所有人为
  改进软件功能性能而进行的**个人自用**修改受法律明文许可；
  但**向第三方提供修改后的软件须经著作权人许可**。
- 本模组因此**不包含任何游戏资源**，仅发布补丁代码，且不对外分发安装包。
- 模组不绕过任何技术保护措施，不修改、不替换、不再分发游戏文件。
- 证据目录 `probe/evidence/` 只收录**不含剧本文本**的材料：

  | 文件 | 是否含游戏内容 |
  | --- | --- |
  | `scene_tree.txt` | 否 —— 只有 Unity 对象名与组件类型 |
  | `probe_full_dump.log` | 否 —— 探针不打台词，只记类型名、方法名、标志位 |
  | `README.md` | 否 —— 结论摘要，引用处已隐去原文 |

  会话日志（`[朗读]` 行就是剧本原文）与游戏截图**故意不入库**，
  见 `.gitignore` 第 3 节。

---

## 许可

补丁代码与文档：见仓库内说明。

### 发布包里的第三方组件

发布包里除了我们自己的 `BelfryA11y.dll`，还有十来个第三方二进制 ——
**BepInEx 官方 zip 里一个许可证文件都不带**，所以这些得我们自己列。

| 组件 | 上游 | 版本 | 许可证 |
|---|---|---|---|
| `BepInEx.dll` / `BepInEx.Preloader.dll` | [BepInEx/BepInEx](https://github.com/BepInEx/BepInEx) `v5-lts` | 5.4.23.5 | MIT |
| `BepInEx.Harmony.dll` / `HarmonyXInterop.dll` / `0Harmony20.dll` | [BepInEx/BepInEx.Harmony](https://github.com/BepInEx/BepInEx.Harmony) | 5.4.23.5 内置 | MIT |
| `0Harmony.dll` | [BepInEx/HarmonyX](https://github.com/BepInEx/HarmonyX) | 2.9.0 | MIT |
| `Mono.Cecil` / `.Mdb` / `.Pdb` / `.Rocks` | [jbevain/cecil](https://github.com/jbevain/cecil) | 0.10.4 | MIT |
| `MonoMod.RuntimeDetour.dll` / `MonoMod.Utils.dll` | [MonoMod/MonoMod](https://github.com/MonoMod/MonoMod) | 22.01.29.01 | MIT |
| `winhttp.dll` | [NeighTools/UnityDoorstop](https://github.com/NeighTools/UnityDoorstop) | 4.5.0 | **LGPL-2.1** |
| `nvdaControllerClient.dll` | NV Access Controller Client | API 2.0 | **LGPL-2.1** |

完整版权行与各许可证全文见 [`mod/licenses/`](mod/licenses/)，
逐项说明见 [`mod/licenses/THIRD-PARTY-NOTICES.txt`](mod/licenses/THIRD-PARTY-NOTICES.txt)。

### 一条容易记反的事

**BepInEx 5 是 MIT，BepInEx 6 才是 LGPL-2.1。**

同一个仓库，`master` 分支是 v6、`v5-lts` 分支是 v5，凭印象很容易记反。
本项目用的一直是 5.4.23.5（Mono 后端），走的是 MIT。

（上一个仓库 TransparentHer 恰好在这里踩过：动手前以为 BepInEx 5 是 LGPL-2.1，
逐个版本标签去取 LICENSE 才发现反了。本仓库在写文档时也凭印象写错过一次，
是同族仓库的 README 把这条纠正回来的。）

### LGPL 组件怎么合规

`winhttp.dll`（UnityDoorstop）与 `nvdaControllerClient.dll`（NVDA Controller
Client）以 LGPL-2.1 授权。二者都以**未经修改的独立动态库**形式随包分发，
本补丁既没改它们，也没把它们静态链接进自己的程序集 ——
即 LGPL-2.1 第 6(b) 条所说的「使用共享库机制」，因此**不触发源码分发义务**，
但仍须随包附上许可证全文 + 显著声明 + 源码获取途径。
这三件事都写在 `THIRD-PARTY-NOTICES.txt` 的第二、三节，许可证全文随包分发。

### 我们不分发的东西

- 游戏本体、任何游戏资源（立绘、语音、剧本原文）
- `extracted/`（提取的剧本）与 `decompiled/`（反编译产物）—— 已 gitignore
- 含台词的会话日志与游戏截图 —— 已 gitignore，见 `.gitignore` 第 3 节
- `BepInEx/` 运行时生成物（`interop/`、`cache/`、`config/`）
