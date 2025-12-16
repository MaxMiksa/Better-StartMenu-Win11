这份更新后的 **Dev.md (v1.1)** 融合了 Codex 和 Gemini 的技术审查意见，修正了原文档中关于 LNK 解析、进程通信（IPC）及线程模型的潜在风险，是目前最稳健的工程实施指南。

---

# StartDeck 架构与开发规范指南 (v1.1)

## 1. 架构总览 (Architecture Overview)

### 1.1 核心设计模式
采用标准的 **MVVM (Model-View-ViewModel)** 架构。
*   **Model (数据层):** 负责文件系统的 I/O 操作。**关键约束**：所有涉及 Windows Shell (LNK 解析, 图标提取) 的操作必须通过 COM 互操作层进行，严禁手写二进制解析逻辑。
*   **ViewModel (逻辑层):** 负责数据的清洗、分组、状态管理。它是 UI 与数据的桥梁，使用 `CommunityToolkit.Mvvm` 源生成器实现 `INotifyPropertyChanged`。
*   **View (表现层):** 纯 XAML 定义的 WinUI 3 界面。Code-Behind 仅处理窗口消息循环和特定的 P/Invoke 互操作。

### 1.2 技术栈约束
*   **框架:** .NET 8 + Windows App SDK (WinUI 3)
*   **发布模式:** Native AOT (PublishAot = true)。
*   **允许的库:**
    *   `CommunityToolkit.Mvvm` (MVVM 基础设施，AOT 友好)
    *   `CsWin32` (Source Generator 方式生成 P/Invoke 代码，替代手写 DllImport)
    *   严禁引入 Newtonsoft.Json 或其他重度依赖反射的库。

---

## 2. 核心模块设计原则

### 2.1 启动与进程管理模块 (Lifecycle Manager)
**原则：基于 AppInstance 的重定向机制 (Redirection).**
*   **单例互斥：** 废弃传统的 Mutex/NamedPipe 方案。**必须使用** Windows App SDK 的 `Microsoft.Windows.AppLifecycle.AppInstance` API。
*   **激活流程：**
    1.  程序启动时，获取 `AppInstance.GetCurrent()`。
    2.  调用 `RedirectActivationToAsync(mainInstance)` 检查是否存在主实例。
    3.  **场景 A (冷启动):** 若无主实例 -> 初始化 -> 注册 Key -> 驻留。
    4.  **场景 B (热唤醒):** 若存在主实例 -> 重定向激活参数 -> 当前进程立即退出。
    5.  **主实例响应：** 订阅 `AppInstance.Activated` 事件，在收到信号后执行 `DispatcherQueue` 调度以显示窗口。

### 2.2 数据扫描模块 (Scanner Service)
**原则：COM 互操作，策略模式，健壮性优先。**
*   **LNK 解析方案：**
    *   严禁尝试手动解析 LNK 文件二进制流。
    *   **必须使用** `CsWin32` 生成的 `IShellLink` 和 `IPersistFile` 接口进行解析。
    *   必须处理 `COMException`，对于损坏的快捷方式（Target 不存在）应静默忽略或标记为无效。
*   **过滤器接口 (`IAppFilter`):** 保持链式设计 (`NameFilter`, `ExtensionFilter`)。
*   **数据源接口 (`IAppSource`):** 保持 `FileSystemSource` 实现，预留未来扩展。

### 2.3 图标处理模块 (Icon Engine)
**原则：严格的线程隔离 (Thread Isolation)，流水线处理。**
*   **线程模型风险：** `Shell32` API (如 `SHGetFileInfo`, `ExtractIconEx`) 往往需要 **STA** 线程；而 `BitmapImage` 必须在 **UI** 线程创建。混合调用会导致崩溃或内存泄漏。
*   **流水线设计：**
    1.  **Stage 1 (Worker Thread):** 创建一个独立的 **STA 线程**或 Task Scheduler。在此线程中调用 Shell API 获取 `HICON`，将其转换为 `byte[]` 或 `MemoryStream`，然后立即销毁 `HICON` (DestroyIcon) 防止 GDI 泄漏。
    2.  **Stage 2 (Marshalling):** 通过 `DispatcherQueue` 将二进制数据流传递给主线程。
    3.  **Stage 3 (UI Thread):** 在 UI 线程中创建 `BitmapImage` 并调用 `SetSourceAsync`。
*   **缓存策略：**
    *   使用 `Dictionary<string, byte[]>` 缓存二进制数据（比缓存 BitmapImage 更省内存且线程安全）。
    *   仅在绑定 View 时才生成 BitmapImage。
    *   实现 LRU 策略或监听 `MemoryManager.AppMemoryUsageLimitChanging` 事件，在内存压力大时清空缓存。

---

## 3. UI/UX 实现规范

### 3.1 窗口行为 (Windowing)
*   **API 选型：** 优先使用 `AppWindow` (Windowing API) 进行窗口尺寸和层级管理，仅在获取任务栏位置时使用 `CsWin32` 调用 `SHAppBarMessage`。
*   **显示逻辑：**
    *   窗口初始化时应 `Hide()`。
    *   触发显示时，先计算坐标，再调用 `Move()`，最后 `Show()`，防止位置跳变。
*   **隐藏逻辑 (Auto-Hide)：**
    *   监听窗口的 `Activated` 事件。当 `WindowActivationState` 变为 `Deactivated` 时，执行隐藏操作。
    *   **注意：** 调试模式下需提供开关禁用此功能，否则无法断点调试。

### 3.2 列表渲染 (List Rendering)
*   **虚拟化 (Virtualization):**
    *   **强制开启：** 无论数据量多少，`GridView` / `ListView` 必须保持 UI 虚拟化开启。
    *   **容器复用：** 确保 ItemTemplate 结构简单，减少嵌套。
*   **渐进式渲染 (Phasing):**
    *   使用 `x:Phase` 属性。
    *   Phase 0: 渲染背景和占位符。
    *   Phase 1: 渲染文字。
    *   Phase 2: 渲染图标。
    *   这能显著提升快速滚动时的帧率 (FPS)。

---

## 4. 性能与优化标准

### 4.1 启动速度优化
*   **AOT 元数据：** 在 `rd.xml` 或项目文件中配置 Trimming 规则，确保 `CommunityToolkit.Mvvm` 的反射元数据被保留，防止运行时 crash。
*   **预热机制 (Pre-warming):**
    *   首次运行后，窗口只执行 `Hide()` 而非 `Close()`。
    *   数据扫描应在后台线程定期（如每 10 分钟）静默刷新，而不是每次打开窗口时刷新。

### 4.2 内存管理
*   **GDI 对象监控：** 开发阶段需使用 Task Manager 或工具监控 GDI Objects 数量，确保图标提取逻辑没有泄漏句柄。
*   **大图处理：** 提取图标时，直接请求所需的尺寸（如 32x32 或 48x48），避免加载 256x256 的大图后在 UI 层缩放，浪费内存。

---

## 5. 未来扩展性规划

*   **搜索架构：** ViewModel 中预留 `CollectionViewSource`，利用其内置的 `Filter` 属性实现搜索，避免手动操作 ObservableCollection 导致 UI 频繁重绘。
*   **插件化准备：** 保持 `IAppSource` 接口的独立性。未来若支持 Steam，只需实现 `SteamSource : IAppSource`，无需修改 UI 层代码。

---

## 6. 开发自检清单 (Definition of Done)

在提交代码前，必须通过以下严格检查：

*   [ ] **AOT 编译检查：** `dotnet publish -r win-x64 -c Release` 必须成功，且运行无 `MissingMetadataException`。
*   [ ] **单例检查：** 此时运行第二个实例，第二个实例应瞬间退出，且第一个实例的窗口应弹出。
*   [ ] **线程安全检查：** 快速滚动列表时，无 `RPC_E_WRONG_THREAD` 或 `COMException` 抛出。
*   [ ] **GDI 泄漏检查：** 反复刷新列表 50 次，GDI 对象总数应保持稳定（通常 < 500）。
*   [ ] **DPI 与位置：** 在 150% 缩放的副显示器上点击任务栏，窗口应准确弹出在副显示器任务栏上方，无偏移。