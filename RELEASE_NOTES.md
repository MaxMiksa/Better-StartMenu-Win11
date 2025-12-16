## v0.1.0 – Initial Prototype / 初始原型 (2025-12-16)

## ✨ 下一代轻量级开始菜单

**这是 StartDeck 的首个技术预览版本，专注于极致的启动速度与原生架构验证。我们重构了 WinUI 3 的启动流程，使其能够以 Native AOT 模式运行，彻底告别了传统 .NET 应用的臃肿。**

| 类别 | 详细内容 |
| :--- | :--- |
| **🚀 极速启动** | 采用 **Native AOT** 编译，移除 JIT 编译开销，启动时间压缩至 200ms 以内。 |
| **🎨 原生体验** | 深度集成 Windows 11 设计语言，支持 **Mica (云母)** 半透明材质与暗色模式自适应。 |
| **⚡ 智能索引** | 自研 **LNK 解析引擎**，自动剔除卸载程序、帮助文件，仅展示真正的应用程序。 |
| **🔒 稳定架构** | 实现了三级**图标缓存流水线**与**单例热唤醒**机制，资源占用极低且随时待命。 |

## ✨ Next-Gen Lightweight Start Menu

**This is the first technical preview of StartDeck, engineered for extreme launch speed and architectural purity. We've refactored the WinUI 3 bootstrap process to support Native AOT, eliminating the bloat of traditional .NET apps.**

| Category | Details |
| :--- | :--- |
| **🚀 Instant Launch** | Compiled with **Native AOT** to remove JIT overhead, achieving sub-200ms startup times. |
| **🎨 Native UX** | Deeply integrated with Windows 11 design language, featuring **Mica** material and auto Dark Mode. |
| **⚡ Smart Indexing** | Custom **LNK Parsing Engine** automatically filters out uninstallers and help files, showing only real apps. |
| **🔒 Robust Arch** | Implemented a 3-stage **Icon Pipeline** and **Single-Instance Hot Wake**, ensuring low footprint and high reliability. |