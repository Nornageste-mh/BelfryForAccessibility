# TheBelfryForAccessibility

本仓库是**专门针对 Steam 游戏《钟塔》（The Belfry）** 的屏幕阅读器辅助模组
（游戏内 BepInEx 插件，通过 Tolk / NVDA Controller Client / SAPI 朗读）。

> ## ⚠️ 郑重警告（请务必阅读）
>
> - 本项目是 **Vibe coding 产物**（AI 辅助生成），**非官方**，与游戏开发商
>   **可味玩 KawayiPlay** 无关。
> - **请务必支持正版：本辅助仅面向已在 Steam 购买《钟塔》的玩家。**
>   **强烈要求每一位使用者通过 Steam 购买正版游戏。** 我们坚决反对任何形式的
>   盗版、破解、未授权传播；请勿将本工具用于协助获取或游玩盗版副本，
>   请尊重开发者的劳动成果。
> - 本项目**不保证可用、不保证稳定**，代码可能存在各种问题（兼容性、稳定性、
>   安全性等），**无任何维护承诺**。使用风险自负，仅供个人学习/研究参考，
>   请勿用于商业或分发牟利。
> - **建议在完全理解代码的前提下再使用**，自行承担一切后果。
> - 本仓库**不包含任何游戏资源**，只发布运行时补丁代码与文档。
> - 如您是《钟塔》的开发者或版权方，认为本仓库构成侵权，请联系我们移除。

---

## 说明

本辅助的作用：让使用读屏软件的玩家，也能基本正常地游玩《钟塔》。

- 朗读**没有配音**的剧情文本（旁白、主角「韩冬」的内心、无配音配角），
  有配音的台词只放语音并打断朗读
- 朗读**手书演出**（第 5 章那一段手绘过场，44 行，走的是另一套显示通道）
- 朗读选项，并用数字键 `1`-`9` 选择
- 朗读手机聊天消息与表情
- 主菜单 / 存读档 / 设置 / 画廊的键盘导航与朗读
  （标题菜单是一个**旋转环形菜单**，游戏原本的上下键是在「转」它、回车无条件
  执行正中间那一项；导航模式下改成位置固定的线性列表）

游戏版本：Unity 2022.3.43f1c1，**Mono** 后端。同一套补丁同时兼容
Steam 正式版。

---

## 安装（仅限正版玩家）

**最快的装法**：到 [Releases](../../releases) 下载整合包 zip，解压后把里面的东西
**整体**拷进游戏根目录（有 `The Belfry.exe` 的那一层）。zip 内容就是下文的
`mod\package\`，另多一个 `licenses\` —— 第三方组件的许可证与来源说明，**别删**。

想自己构建、或只想往已有的 BepInEx 里加一个插件，看下面。

补丁由 BepInEx 在运行时挂载，**不修改任何游戏文件**，所以安装就是「拷文件」。
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

详细步骤、按键表、配置说明、故障排查见
[`mod/package/安装说明.md`](mod/package/安装说明.md)。

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
│  │  ├─ Reader.cs              剧情朗读状态机（配音判定）
│  │  ├─ Choices.cs             选项朗读与数字键选择
│  │  ├─ Speech.cs              Tolk / NVDA / SAPI 后端调度
│  │  ├─ Nvda.cs                NVDA Controller Client 封装
│  │  ├─ Sapi.cs                SAPI 兜底（纯 P/Invoke，不走 COM 后期绑定）
│  │  ├─ TextProc.cs            说话人名与台词的朗读加工
│  │  └─ UiNav.cs               界面键盘导航与朗读
│  └─ package/                  安装包（直接拷进游戏根目录）
│     └─ 安装说明.md            ← 用户文档，先看这个
│
├─ probe/                       实机探针：验证补丁点、转储场景树、驱动按键
│  └─ evidence/                 实机证据（场景树转储、会话日志）
│
├─ tools/                       分析脚本（UnityPy 提取剧本、统计、探针）
│
└─ （以下目录不入库，见 .gitignore）
   extracted/    从游戏提取的剧本文本（版权内容）
   decompiled/   反编译的游戏代码（版权内容）
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

补丁代码与文档：见仓库内说明。第三方组件：
BepInEx 5.4.23.5（LGPL-2.1）、NVDA Controller Client（LGPL-2.1）。
