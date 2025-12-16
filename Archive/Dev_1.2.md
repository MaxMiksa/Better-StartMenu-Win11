这份 **Dev.md (v1.2)** 是基于 v1.1 的进一步完善。它针对评估报告中指出的“缓存量化缺失、多屏定位模糊、系统降级策略未定义、进程边界风险”等**最后的一公里**问题进行了补全。

这是 **StartDeck** 工程实施的最终蓝图。

---

# StartDeck 架构与开发规范指南 (v1.2)

## 1. 架构总览 (Architecture Overview)

### 1.1 核心设计模式
采用标准的 **MVVM (Model-View-ViewModel)** 架构。
*   **Model (数据层):** 负责文件系统 I/O。**约束：** 所有 Windows Shell 操作 (LNK, Icon) 必须通过 `CsWin32` 生成的 COM 接口进行，严禁手写二进制解析。
*   **ViewModel (逻辑层):** 使用 `CommunityToolkit.Mvvm`。
*   **View (表现层):** 纯 XAML + WinUI 3。Code-Behind 仅负责窗口消息与 P/Invoke。

### 1.2 技术栈与构建约束
*   **框架:** .NET 8 + Windows App SDK (WinUI 3)
*   **发布:** Native AOT (`<PublishAot>true</PublishAot>`)。
*   **依赖:** 仅允许 `CommunityToolkit.Mvvm` 和 `CsWin32`。
*   **AOT 补充规则:**
    *   UI 绑定必须强制使用 `x:Bind` (编译时绑定)，严禁使用 `{Binding}` (运行时反射)，以避免 AOT 裁剪导致绑定失效。
    *   若必须使用反射，需在 `rd.xml` 中手动声明保留类型。

---

## 2. 核心模块设计原则

### 2.1 启动与进程管理 (Lifecycle)
*   **单例机制:** 使用 `AppInstance.RedirectActivationToAsync`。
    *   **热唤醒:** 主实例订阅 `Activated` 事件。
    *   **冷启动:** 注册 Key -> 初始化 -> 预热 (不显示窗口)。
*   **后台策略:**
    *   **扫描频率:** 废弃高频轮询。采用 `FileSystemWatcher` 监听开始菜单目录变更（Created/Deleted/Renamed）。
    *   **兜底:** 仅在窗口 `Show()` 时检查一次 `LastScanTime`，若超过 1 小时则静默重新全量扫描。

### 2.2 数据扫描与解析 (Scanner)
*   **LNK 解析:** 使用 `CsWin32` (`IShellLinkW`, `IPersistFile`)。遇到 COM 异常或 Target 不存在时，标记 Item 为 `IsValid=false` 并从列表移除。
*   **执行安全 (Execution Safety):**
    *   使用 `ProcessStartInfo`。
    *   **关键属性:** `UseShellExecute = true` (确保能处理 LNK、URL 和特殊路径)。
    *   **异常处理:** 必须 `try-catch` 捕获 `Win32Exception`。特别是 `NativeErrorCode == 1223` (用户在 UAC 提示框点击了“取消”)，此类异常应忽略，不弹窗报错。
    *   **路径处理:** 路径字符串无需手动添加引号，`Process.Start` 会自动处理空格和中文。

### 2.3 图标流水线 (Icon Pipeline)
**原则：三阶段流水线，严格内存控制。**
1.  **Stage 1 (STA Worker):** 独立线程调用 `ExtractIconEx` / `SHGetFileInfo`。
    *   **关键操作:** 获取 `HICON` -> `Bitmap.FromHicon` -> `Resize (Max 48px)` -> `SaveToMemoryStream (PNG)` -> `DestroyIcon`。
    *   **约束:** 必须将图片 Resize 到 48x48 或 32x32，严禁缓存 256x256 的原图，这是控制 30MB 内存的关键。
2.  **Stage 2 (Marshalling):** `byte[]` 数据传递至 UI 线程。
3.  **Stage 3 (UI Thread):** `BitmapImage.SetSourceAsync`。

### 2.4 缓存管理 (Memory Control)
*   **数据结构:** `LruCache<string, byte[]>`.
*   **容量限制:**
    *   **Soft Limit:** 500 个图标 (约 2MB - 5MB 内存)。
    *   **Eviction Policy (淘汰策略):** 当缓存满时，淘汰最久未使用的 10%。
*   **主动释放:** 监听 `MemoryManager.AppMemoryUsageLimitChanging` 或在窗口隐藏超过 5 分钟后，主动执行 `Cache.Trim()` 和 `GC.Collect()`。

---

## 3. UI/UX 实现规范

### 3.1 视觉降级策略 (Visual Fallback)
*   **材质优先级:**
    1.  **Win 11 (Build 22000+):** `SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt }`。
    2.  **Win 10 (Build 17134+):** 使用 `DesktopAcrylicController` (需 P/Invoke 支持)。
    3.  **Fallback:** 若以上均失败或崩溃，回退到 `SolidColorBrush` (深灰色 #202020 或依据系统主题色)。
*   **实现:** 在 `Window_Loaded` 中通过 `ApiInformation.IsTypePresent` 检测系统版本并应用材质。

### 3.2 窗口定位算法 (Positioning)
**目标：在光标所在的屏幕，紧贴任务栏弹出。**
1.  **获取光标位置:** `GetCursorPos`。
2.  **确定屏幕:** 使用 `DisplayArea.GetFromPoint(cursorPos)` 获取当前激活的显示器句柄和工作区 (`WorkArea`)。
3.  **确定任务栏:**
    *   调用 `SHAppBarMessage` (`ABM_GETTASKBARPOS`) 获取**主**任务栏位置。
    *   *修正:* 对于多显示器，需检测 `WorkArea` 与屏幕分辨率的差值来推断该屏幕的任务栏位置（Win11 任务栏通常不可移动，但在 Win10 或某些设置下可能在左/右/上）。
    *   *简化策略:* 若无法精确判定副屏任务栏，默认将窗口定位在 `DisplayArea` 的**底部居中** (Bottom-Center)，并向上偏移 48px (预估任务栏高度)。
4.  **防溢出:** 计算出的 (X, Y) 必须进行 `Clamp` 操作，确保窗口完全在 `WorkArea` 内部，不被屏幕边缘截断。

### 3.3 列表渲染
*   **虚拟化:** `GridView` 必须开启。
*   **分层渲染:** 使用 `x:Phase` 优先渲染文字，最后渲染图标。

---

## 4. 自检与验收标准 (Checklist)

### 4.1 核心功能自检
*   [ ] **多屏测试:** 在副屏点击托盘/快捷键，窗口是否出现在副屏？(若出现在主屏则 FAIL)。
*   [ ] **UAC 测试:** 右键 -> 管理员运行 -> 在 UAC 弹窗点“否”。程序不应崩溃，不应报错。
*   [ ] **特殊路径:** 测试名为 `C:\Test & Folder\应用 (v1.0).lnk` 的快捷方式能否正常启动。

### 4.2 性能自检
*   [ ] **内存峰值:** 连续快速滚动列表到底部，任务管理器中内存不应超过 50MB (GC 后应回落至 <30MB)。
*   [ ] **句柄泄漏:** 使用 Task Manager 观察 `GDI Objects`，反复开关窗口 50 次，数量不应持续增加。

### 4.3 兼容性自检
*   [ ] **Win10 测试:** 在 Windows 10 虚拟机中运行，确保背景为 Acrylic 或纯色，且不崩溃。
*   [ ] **AOT 测试:** 确保发布的 Release 包（无依赖单文件）能正常运行，无 `MethodNotFound` 或绑定失效。