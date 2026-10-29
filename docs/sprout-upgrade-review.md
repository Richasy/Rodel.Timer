# Sprout 升级与项目审查记录

当前依赖已更新为 Azure 完整发布 `0.1.0-rodel.105.1.g5af5aae144af`，本地安装版为 `2.2609.2.0`。
以下主体保留首次升级至 Azure 104 的审查记录；105 的追加验证和部署见文末。

审查日期：2026-10-29。应用基线：`764e23e06b87bdd55f15c82e8e37e0ec10ef97be`。
本次在独立 `codex/timer-sprout-review` worktree 实施，没有修改 Sprout 源码或主工作目录。

## 发布来源

| 项目 | 旧值 | 新值 |
| --- | --- | --- |
| Sprout 包集 | `0.1.0-local.g3052b620dd96` | `0.1.0-rodel.104.1.g10d44ad338b7` |
| Sprout 提交 | `3052b620dd96b19e3931bacb72aef4e330fb2907` | `10d44ad338b76c29a465a6229bfa0cb1215b98d4` |
| 包源 | 相邻 Sprout 目录的 local-feed | 需要身份验证的 Azure Artifacts `sprout` 源 |
| Windows SDK BuildTools | `10.0.28000.1839` | `10.0.28000.2705`（新版 Packaging 的最低依赖） |

完整发布由同版本 `Sprout.ReleaseManifest` 的 `sprout-package-set.json` 确认：schema 1、34 个包，
`repositoryCommit` 与上表新提交一致。已下载的 14 个相关 nupkg（包含应用使用的全部 Sprout 包及其相关依赖）
逐个通过 manifest 长度与 SHA-256 比对；没有声称全检其余 20 个未使用包。核验结果保留在本次审查的
`sprout-review-evidence/package-integrity.json`。凭据由本机用户级配置提供，仓库没有保存凭据。

保留审查开始时的七项已有修改意图：包版本基线、两语言 README 的 Store 发布说明、更新说明使用显示版本、
MSIX/项目版本 `2.2609.1.0`，以及行投影测试修正。这些内容不是本次新设计。

## 与本项目相关的框架变化及处理

| 框架变化 | 本项目处理 |
| --- | --- |
| `fc7881568` DSL vNext、`7b31fd064` 统一 Ui Body | 13 个宿主视图文件从 `[Composite]`、`Stack(...)` / Orientation 及多参数子项迁至 `[View]`、Column/Row 和集合表达式；布局顺序、参数和产品界面保持原意。 |
| `0f9127be7` 自定义控件保留式输入、严格配置检查 | `MainView` 把稳定的 `TimerContentControl` 持有为 readonly 实例；没有机械拆分视图或增加工厂包装层。 |
| `4c651f098` 生成资源后的 DSL 标记恢复 | SettingsCard 的 Description 显式标明 string，解决同轮生成 Resources 造成的重载语义不明确，保留原声明式结构。 |
| `38d12efd6` UI 同步上下文与线程校验 | 关闭流程在 finally 中使用 `IScheduler.SwitchToUIThreadAsync()`，回到 home 后才读取保存信号和恢复关闭状态。 |
| 主题、列表与窗口恢复修复（如 `32b535b7e`、`9d9db5d40`） | 沿用按窗口主题、主题/语言修订、稳定 Guid 键和模式独立窗口放置；不重复实现框架已有的滚动、选择、DPI 与多屏恢复逻辑。 |
| `10d44ad33` NumberBox 紧凑弹出层修复 | 现有设置 NumberBox 直接受益，无需应用层复制样式。 |

新分析器 SPROUT073 明确指出，旧代码写在 retained DropDownButton 上的 `.AccessKey("F"/"H")` 没有功能承载，
原来会被静默丢弃。本次同时移除这两个无效声明与虚假的 UIA AccessKey 元数据；文件夹和历史仍保留原生的
焦点、键盘激活与 ExpandCollapse 语义。Alt+F / Alt+H 不列为已实现快捷键，后续框架支持后可统一恢复。

宿主没有调用 Sprout.WindowsAppSDK 的句柄/初始化桥接 API，也没有使用 Windows App SDK Foundation API。
已移除模板遗留的两个直接引用与 WindowsAppSDKSelfContained 配置；Windows.Storage 等 OS WinRT 投影仍由 Windows TFM 提供。
没有引入 WinUI。NativeAOT 与 MSIX 发布验证了此依赖精简。

## 项目审查与修复

审查覆盖 Core 的计时、不可变条目与文件夹、聚合不变量、JSON 映射、原子保存和 UWP 导入；Presentation 的响应式状态、
列表投影、布局/语言/窗口策略；宿主的启动、关闭、主题、弹出层、设置、辅助功能和打包边界。

1. 启动语言读取：原先只捕获 TimerDataAccessException，坏 JSON 会在窗口创建前抛出。提取可测试的
   StartupLanguagePreference 边界，捕获 TimerDataException，跟随系统语言并让正常 bootstrap 显示数据错误。
   缺失、损坏、文件被锁定及有效语言都有真实文件测试，坏文件保持原字节。
2. 今日总计：原来只订阅工作区修订，跨午夜没有失效通知。增加单一局部日期信号，空闲 tick 也检查日期；同日相等写入
   不产生通知，换日不增加工作区修订、不触发保存，正在运行的会话继续使用单调时钟。
3. 保存与关闭：关闭在读取保存结果及更新关闭状态前显式回 home scheduler。Flush 统一处理待保存写入和此前失败的重试，
   Close 复用此策略。错误页重试核对 SaveState 后才清除提示；连续失败仍可重试。
4. 旧主题导入：Enum.TryParse 会接受 `999`、`-1` 等未定义数值，原先会在构造偏好时阻断迁移。
   入口补充 Enum.IsDefined，按原有恢复策略回退 System 并记录警告。

启动坏文件、空闲跨午夜、失败保存重试和非法旧主题均先观察到预期失败，再修复通过；关闭测试按队列分步验证
保存结果与关闭恢复分别在 home scheduler 提交。现有保存串行化、重试关闭、稳定行身份和语言切换回归继续保留。

不采用的额外改动：不改计时语义、数据 schema、Store 标识和已保存字段；不引入全局状态层、通用仓储、偏好复制框架，
不把清晰局部的 DSL 拆成大量组件，不重复实现框架虚拟化/主题/窗口功能。风格原则见 [code-style.md](code-style.md)。

## 验证与限制

本次 formatter 从新提交的 `tools/Sprout.Format/Program.cs` 与 `Sprout.DslFormatting` 源码隔离构建，语义引用来自
Azure 104 包。它不属于发布 manifest，也没有全局安装；独立构建产物保留在本次审查会话的工具目录。
以下 `$formatterDll` 指向该构建结果，也可替换为开发者自己的独立构建路径。

| 最终检查 | 结果 | 证据 |
| --- | --- | --- |
| Azure 包源还原 | 成功，无 local-feed / Sprout 项目引用 | NuGet.config、Directory.Packages.props、项目资产文件 |
| 官方 DSL formatter / git diff --check | 78 个源文件已符合树格式；无差异错误 | formatter --check 输出、Git 检查 |
| dotnet format --verify-no-changes | 成功，退出码 0 | artifacts/format-verify.log（无诊断输出） |
| Release 全解决方案构建 | 成功，0 警告、0 错误 | artifacts/build-release.log |
| Release 全套测试 | 258 个通过（Core 40 + Presentation 218），0 失败、0 跳过 | artifacts/test-release.log |
| Windows x64 NativeAOT 与已签名 MSIX | 成功；仅产出文件，没有安装 | artifacts/publish-review.log、artifacts/publish/review |
| Sprout.DevTools selftest | core / persistence / store / localization / ui 全部 ok，PASS，leaked=0 | artifacts/devtools-selftest/result.json 及 stdout/stderr |

可复核命令：

```pwsh
dotnet $formatterDll --check src
dotnet format Rodel.Timer.slnx --no-restore --verify-no-changes
dotnet build Rodel.Timer.slnx -c Release --no-restore
dotnet test Rodel.Timer.slnx -c Release --no-build --no-restore
dotnet publish src/Rodel.Timer/Rodel.Timer.csproj -c Release -r win-x64 --output artifacts/publish/review
sprout-devtools selftest artifacts/publish/review/Rodel.Timer.exe --target direct `
  --expect-token core --expect-token persistence --expect-token store --expect-token localization --expect-token ui `
  --artifacts artifacts/devtools-selftest --json
```

`--selftest` 在启动应用前使用自建临时目录，既不读写用户真实计时文件，也不安装 MSIX。
其中 ui token 是无窗口的布局/辅助功能契约检查，**不是实际窗口截图或真实 UIA 操作验证**。
本次没有执行会使用真实数据的应用启动、包安装、Store 上传或 Git 推送；Windows Arm64、真实多屏/高对比度及
完整交互界面的人工回归仍需在隔离用户环境中验证。C# 不在 ub-mode 三个配套指标工具的官方支持范围，未报告虚构分数。

## Azure 105 追加更新

2026-10-29，四个直接 Sprout 依赖统一升为 `0.1.0-rodel.105.1.g5af5aae144af`，对应提交
`5af5aae144af60d05866635bfc8f2f61e717331b`（#1066），包含静态绘制、指针观察、语言表与根 raster 退休优化。
同版本 ReleaseManifest 含 34 个包，已下载的 14 个包长度和 SHA-256 均匹配；
应用解析的八个 Sprout 包全部来自 Azure 源并保持同一版本。

为原位更新，项目、清单和 `CurrentVersion` 同步升为 `2.2609.2.0`；
更新说明正文与 `WhatsNewVersion` 保持原意，不将框架维护误报为一组新产品功能。
继续使用默认 Direct2D 缓存策略，没有启用可选的 64 MiB 阈值。

Release 测试 258/258（Core 40 + Presentation 218）、格式验证、x64 NativeAOT 和已签名 MSIX 均通过。
发布可执行文件通过第一方 DevTools 五项无窗口自检。随后完成同身份签名原位更新，保留应用数据，
从已安装路径启动 `2.2609.2.0` 并完成 WGC 捕获；这补充了首次审查时尚未进行的实际窗口启动检查，
不等于全部交互回归。

证据集中在 `artifacts/azure105/`：`test.log`、`format.log`、`publish.log`、`resolved-packages.json`、
`package-integrity.json`、`selftest/result.json`、`deploy/result.json` 和 `launch/result.json`。
