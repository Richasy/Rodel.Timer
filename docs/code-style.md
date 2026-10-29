# Working Timer 代码风格

以仓库 `.editorconfig`、.NET 分析器和 Sprout 当前声明式契约为准。按项目适配并借鉴
[ub-mode（57a8bdd）](https://github.com/unclebob/ub-mode/blob/57a8bdd28e45ed41900ace0bafa2ff76dd9ed70f/SKILL.md)：
一个决策放在一处，先固定行为，再做最小且可验证的修改。清楚、局部的代码保持原状。

## 模型与状态

- `Core` 承载计时、工作区、不变量和持久化；Windows API、窗口和弹出层留在宿主。
- 在文件、旧设置和用户输入进入模型时验证并规范化。外部枚举字符串必须同时通过解析和定义值检查。
- `TimerAppStore` 是运行工作区的唯一写入者。加载结果保有同一聚合，成功变更统一递增 `Revision` 并安排保存。
- 列表从同一修订的快照读取，变更时换新引用；以文件夹或历史记录的稳定 `Guid` 作键，不能以行号代替身份。
- 业务计算保持纯函数；时钟、时区、标识工厂及调度器显式传入。日期更新不写用户文件，也不让空闲时钟每秒重绘。
- 异步 I/O 可以离开 UI 线程；随后读取信号或修改关闭状态前，必须回到注入的 home scheduler。
- 保存策略只定义一次。错误通过可观察结果保留，只有真实成功才清除错误；重试必须实际执行一次新写入。

## Sprout 视图

- 纯片段用 `[View] readonly partial record struct`，有状态及资源生命周期的组件用 `sealed partial class : Control`。
- `Ui Body` 只写一个声明式表达式；运行时辅助方法返回具体视图或控件类型。子项用 `Column([...])`、`Row([...])`、
  `Overlay([...])` 或 `Grid(columns, rows, [...])`，通过官方 DSL formatter 展示树层级。
- 一个界面含义清楚且只用一次的子树可以内联。只在复用、领域命名或独立生命周期带来收益时提取组件。
- 同一生命周期的自有控件持有在 `readonly` 字段中；列表和可变控件通过框架支持的保留式配置更新，避免重建身份。
- 在响应式宿主读取主题及本地化，向纯视图传值；保留现有主题/语言修订对同键行的失效作用。
- 同轮生成的 `Resources` 参与 `Description(string)` / `Description(IView)` 重载时，显式标明字符串类型。
  不通过关闭分析器隐藏保留配置或交互标记被丢弃的问题。

## 验证

修改边界行为时，先写失败的行为测试，再修正原因；使用手动时钟和队列调度器，不依赖等待、真实计时数据或网络时序。
常规门禁为 `dotnet format --verify-no-changes`、Release 构建与全套测试；框架升级还验证 NativeAOT 发布及
`sprout-devtools selftest`。声明式树再通过 Sprout 源码 `tools/Sprout.Format` 独立构建的 formatter 执行 `--check src`；
该工具不在本次 Azure 包集中，也不假设已全局安装。

ub-mode 的 crapper、dryer、mutator 当前官方语言支持列表不含 C#，本项目不伪造 CRAP、重复率或变异分数。
使用实际 .NET 分析器、格式化、编译和行为测试作为可复核证据。
