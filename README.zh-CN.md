<div align="center">

<img src="docs/assets/logo.png" width="96" alt="Working Timer logo" />

# Working Timer

**专注的正向计时器（Windows）—— 基于 [Sprout](https://github.com/Richasy/Sprout) 以 Fluent Design 重写。**

[![Platform](https://img.shields.io/badge/Windows-10%20(19041)%2B-0078D6?logo=windows&logoColor=white)](#-构建与运行)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](#-构建与运行)
[![NativeAOT](https://img.shields.io/badge/NativeAOT-single%20binary-2ea44f)](#-构建与运行)
[![UI](https://img.shields.io/badge/UI-Sprout-1f6feb)](https://github.com/Richasy/Sprout)
[![许可证](https://img.shields.io/badge/许可证-MIT-yellow.svg)](LICENSE)

[![从 Microsoft Store 获取](https://img.shields.io/badge/Microsoft%20Store-下载-0078D6?logo=microsoftstore&logoColor=white)](https://apps.microsoft.com/detail/9N71W9G714RK)

[English](README.md) · **简体中文**

</div>

<p align="center">
  <img src="docs/assets/screenshots/hero-dark.png" width="840" alt="Working Timer 主界面" />
</p>

**Working Timer** 是已归档的 [Work‑Timer](https://github.com/Richasy/Work-Timer) 的 Sprout 版继任者。它保留原来的正向（累加）
计时流程与 Microsoft Store 应用标识，同时把旧的 UWP UI 与自定义样式层替换为**完全自绘**的 Fluent 外壳 —— 每一个控件、
计时数字、弹出层与紧凑悬浮窗都由 Sprout 在 Direct2D / DirectComposition 上绘制，并以单个 **NativeAOT** 二进制发布。

> 本仓库（`Richasy/Rodel.Timer`）是代码库；**Working Timer** 是应用与商店中展示的产品名。

---

## ⬇️ 安装

**Working Timer 已在 Microsoft Store 免费上架** —— 推荐从商店安装，可自动更新（支持 x64 与 Arm64，Windows 10 19041+）：

<p align="center">
  <a href="https://apps.microsoft.com/detail/9N71W9G714RK">
    <img src="https://get.microsoft.com/images/zh-cn%20dark.svg" width="220" alt="从 Microsoft Store 下载 Working Timer" />
  </a>
</p>

> **商店页面：** https://apps.microsoft.com/detail/9N71W9G714RK

---

## ✨ 功能特性

- ⏱️ **正向计时** —— 确定性、单调递增，无暂停态；时间只向前走。
- 🗂️ **文件夹与历史** —— 把会话归入文件夹，并在标题栏直接浏览按文件夹分组的历史。
- 🎯 **每日目标** —— 设定每日专注目标（分钟），看着「今日专注」不断累积。
- 🎨 **主题与材质** —— 跟随系统 / 浅色 / 深色，以及五种窗口材质（Mica、Mica Alt、Acrylic、Acrylic Thin、Liquid Glass Wallpaper）。
- 🔤 **字体** —— 可为整个应用选择任意已安装字体。
- 🌐 **实时本地化** —— 中英文（简体 / 繁体）即时切换，无需重启。
- 🪟 **紧凑悬浮** —— 折叠为一个小巧、置顶的计时器继续工作。
- ♿ **无障碍优先** —— 稳定的 UI Automation id、可访问的名称与描述、键盘访问键，以及事务化的屏幕阅读器播报。
- 🔒 **安全升级** —— 一次性、带兜底地导入你已有的 Work‑Timer 文件夹、历史与设置。

---

## 📸 界面截图

<table>
  <tr>
    <td width="50%">
      <img src="docs/assets/screenshots/main-light.png" alt="浅色主题" /><br/>
      <sub><b>浅色主题</b> —— 外壳实时跟随系统的浅色 / 深色。</sub>
    </td>
    <td width="50%">
      <img src="docs/assets/screenshots/settings.png" alt="设置" /><br/>
      <sub><b>设置</b> —— 主题、窗口材质、字体、每日目标、语言与关于。</sub>
    </td>
  </tr>
  <tr>
    <td width="50%">
      <img src="docs/assets/screenshots/folders.png" alt="文件夹" /><br/>
      <sub><b>文件夹</b> —— 选择当前会话归属的文件夹。</sub>
    </td>
    <td width="50%">
      <img src="docs/assets/screenshots/history.png" alt="历史" /><br/>
      <sub><b>历史</b> —— 按文件夹的会话，含时长与时间。</sub>
    </td>
  </tr>
  <tr>
    <td colspan="2" align="center">
      <img src="docs/assets/screenshots/compact.png" width="460" alt="紧凑悬浮" /><br/>
      <sub><b>紧凑悬浮</b> —— 小巧、置顶的计时器。</sub>
    </td>
  </tr>
</table>

---

## 🏗️ 基于 Sprout 构建

Working Timer 是 **[Sprout](https://github.com/Richasy/Sprout)** 的示范应用 —— 一个实验性的自绘、编译期 reconcile、
GPU 渲染的 Windows UI 框架。代码按 UI 无关的分层拆分：

```text
src/
  Rodel.Timer/               Sprout 应用外壳、Fluent UI、MSIX 标识、Windows 旧数据适配
  Rodel.Timer.Core/          UI 无关的领域：计时、文件夹、历史、持久化、迁移
  Rodel.Timer.Presentation/  响应式应用状态与 UI 无关的呈现策略
tests/
  Rodel.Timer.Core.Tests/
  Rodel.Timer.Tests/
```

- **`Rodel.Timer.Core`** 承载领域逻辑 —— 单调计时、带显式不变量的文件夹/历史模型、NativeAOT 安全的带版本 JSON（原子替换
  写入），以及旧数据导入器。
- **`Rodel.Timer.Presentation`** 持有响应式 UI 状态，与具体控件无关。
- **`Rodel.Timer`** 是 Sprout 外壳 —— 响应式 Fluent 界面、标题栏的文件夹/历史弹出层、开放式画布工作区、紧凑悬浮窗，以及
  Windows/MSIX 胶水层。

---

## 🚀 构建与运行

> ⚠️ **外部暂时无法构建。** Working Timer 依赖 **[Sprout](https://github.com/Richasy/Sprout)**，而 Sprout 目前仍在
> **内部开发**、**尚未公开发行任何包**。`NuGet.config` 把 `Sprout*` 包解析到一个本地打包源
> （`..\Sprout\artifacts\local-feed`），它依赖尚未公开的 Sprout 仓库，因此外部开发者在此 `dotnet restore` 会失败。
> 待 Sprout 发布到 nuget.org 后，再补充公开的构建步骤。
>
> **只想使用应用？** 直接从 **[Microsoft Store](https://apps.microsoft.com/detail/9N71W9G714RK)** 免费安装，无需构建。

作为参考，等 Sprout 包就绪后，本应用可用 **.NET 10 SDK** 在 Windows 10 (19041)+ 上构建（Sprout 版本固定在
`Directory.Packages.props`）：

```pwsh
dotnet build   Rodel.Timer.slnx
dotnet test    Rodel.Timer.slnx
dotnet publish src\Rodel.Timer\Rodel.Timer.csproj -c Release -r win-x64
```

`dotnet publish` 会在 `artifacts/bin/Rodel.Timer/Release/.../publish` 下产出 NativeAOT 可执行文件与已签名的 MSIX 包。

---

## 📄 许可证

Working Timer 基于 [MIT 许可证](LICENSE) 开源。

---

## 🙏 致谢

- 基于 **[Sprout](https://github.com/Richasy/Sprout)** —— 自绘 Windows UI 框架。
- 继承自 **[Work‑Timer](https://github.com/Richasy/Work-Timer)** —— 原 UWP 应用。
