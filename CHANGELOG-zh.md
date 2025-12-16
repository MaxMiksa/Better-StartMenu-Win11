## v0.1.0 – 初始原型 (2025-12-16)

### 功能 1：Native AOT 与核心架构搭建
- **总结**: 建立了支持全量 Native AOT 编译的 WinUI 3 项目架构。
- **解决痛点**: 解决了传统 .NET 应用“臃肿”的问题。提供了一个轻量级、单文件（<10MB）的可执行程序，用户无需安装 .NET 运行时即可秒开。
- **功能细节**: 应用程序作为一个独立的 `.exe` 运行。它手动初始化 Windows App SDK 引导程序以支持非打包部署模式。
- **技术实现**: 
  - 在 `.csproj` 中配置 `<PublishAot>true</PublishAot>` 和 `<WindowsPackageType>None</WindowsPackageType>`。
  - 添加 `rd.xml` 以保留 `CommunityToolkit.Mvvm` 和 `Microsoft.UI.Xaml` 的元数据，防止 AOT 裁剪导致崩溃。
  - 在 `Program.cs` 中实现 `Bootstrap.Initialize()` 以加载非打包运行时。

### 功能 2：高性能数据扫描引擎
- **总结**: 实现了针对 Windows 开始菜单条目的递归、过滤和去重扫描引擎。
- **解决痛点**: 系统原生开始菜单充斥着卸载程序、帮助文件和网站链接，杂乱无章。
- **功能细节**: StartDeck 递归扫描全局和用户特定的开始菜单目录。它自动合并重复项（优先保留用户快捷方式），移除非可执行文件（如 `.txt` 或 `.url`），并基于关键词（如 "uninstall", "readme"）过滤掉“垃圾”快捷方式。
- **技术实现**: 
  - 创建 `FileSystemStartMenuSource`，使用 `Task.Run` 将扫描任务卸载到后台线程。
  - 使用 `CsWin32` 生成的 `IShellLinkW` 接口实现安全、兼容 AOT 的 LNK 目标解析（使用 `SLGP_RAWPATH`）。
  - 使用策略模式 (`IAppFilter`) 实现了 `KeywordFilter` 和 `ExtensionFilter`。

### 功能 3：优化的图标处理流水线
- **总结**: 专为 UI 流畅度设计的线程安全、带缓存的图标提取流水线。
- **解决痛点**: 提取图标（使用 `ExtractIconEx` 或 `SHGetFileInfo`）速度慢且需要 STA 线程。在 UI 线程执行会导致卡顿，而在普通后台线程执行会导致 COM 异常。
- **功能细节**: 图标异步加载。最初显示占位符，随后呈现高清图标。图标缓存在内存中，确保持续打开时瞬间显示。
- **技术实现**: 
  - 实现 `StaTaskScheduler` 以强制 Shell API 调用在专用的 STA 后台线程上执行。
  - 创建三阶段流水线：提取 (STA) -> 字节编组 -> Bitmap 创建 (通过 `DispatcherQueue` 在 UI 线程)。
  - 实现 `LruCache<string, byte[]>`，设定 500 项软限制，并包含窗口隐藏时的自动修剪逻辑。

### 功能 4：智能生命周期管理
- **总结**: 具备“热唤醒”能力的单实例强制运行机制。
- **解决痛点**: 防止同时打开多个 StartDeck 窗口。确保再次点击图标时是切换（Toggle）现有窗口的状态，而不是启动新进程。
- **功能细节**: 如果 StartDeck 已经在运行，新的启动尝试只会将现有窗口带到前台，然后退出。窗口在失去焦点时会自动隐藏（模拟系统菜单行为）。
- **技术实现**: 
  - 利用 `AppInstance.RedirectActivationToAsync` 实现健壮的实例重定向。
  - 在 `App.xaml.cs` 中实现 `ToggleWindow()` 逻辑以处理显示/隐藏状态。
  - 挂钩 `Window.Activated` 事件以实现“失焦自动隐藏”。