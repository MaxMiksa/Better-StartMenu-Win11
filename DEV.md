这份 **Dev.md** 为 StartDeck 的最终工程实施蓝图，吸收AI评审的剩余意见：补全多屏任务栏定位、冷启动 200ms 策略与度量、FileSystemWatcher 失效兜底、AOT/绑定元数据保留细则，以及缓存与兼容性的执行性约束。

---

# StartDeck 架构与开发规范指南

## 1. 架构与技术栈
- 模式：MVVM（Model 负责 I/O 与 Shell COM，ViewModel 用 CommunityToolkit.Mvvm，View 纯 XAML + 极少 P/Invoke）。
- 框架：.NET 8 + Windows App SDK (WinUI 3)。
- 发布：Native AOT (`<PublishAot>true</PublishAot>`)，无额外 NuGet 依赖，唯一允许的包为 `CommunityToolkit.Mvvm` 与 `CsWin32`。
- AOT/绑定规则：
  - 强制使用 `x:Bind`（编译期绑定）；仅当 DataTemplate 需动态 Binding 时，必须在 `rd.xml` 显式保留相关类型/属性元数据。
  - 禁止 Newtonsoft.Json 等重度反射库；如需有限反射，先声明保留清单。

## 2. 单实例与启动
- 使用 `AppInstance` + `RedirectActivationToAsync` 统一单例与唤醒：
  - 冷启动：注册 Key -> 预热窗口/数据（Hidden） -> 待激活时显示。
  - 热唤醒：重定向激活参数给主实例，当前进程立即退出；主实例 `Activated` 中切到 UI 线程执行显示/定位。
- 激活参数预留：支持未来 `--search` 等参数透传到主实例。

## 3. 数据扫描与执行安全
- 源：文件系统开始菜单目录（系统 + 用户），后续扩展 `IAppSource` 可并行汇聚。
- 监听：优先 `FileSystemWatcher` (Created/Deleted/Renamed, IncludeSubdirectories=true)。若 watcher 抛异常或被禁用：
  - 在窗口 `Show()` 时检查 `LastScanTime`，若 >1 小时或 watcher 离线则触发静默全量扫描。
- LNK 解析：仅用 `CsWin32` 生成的 `IShellLinkW` + `IPersistFile`；损坏/缺失目标则 `IsValid=false` 且从列表移除，吞掉 `COMException`。
- 过滤：链式 `IAppFilter`（扩展/名称关键字等），保留用户级优先的去重策略。
- 启动进程：`ProcessStartInfo { UseShellExecute=true }`；捕获 `Win32Exception`，`NativeErrorCode == 1223` (UAC 取消) 静默处理；无需手动包裹引号。

## 4. 图标流水线与缓存
- 三阶段管线：
  1) **STA Worker**：独立 STA 线程调用 `ExtractIconEx`/`SHGetFileInfo`，`HICON -> Bitmap.FromHicon -> Resize(32/48) -> PNG byte[] -> DestroyIcon`；确保 Bitmap/GDI 及时 Dispose。
  2) **Marshalling**：通过 `DispatcherQueue` 传递字节流到 UI 线程。
  3) **UI Thread**：`BitmapImage.SetSourceAsync` 绑定；失败回退到内置默认图标。
- 缓存：`LruCache<string, byte[]>`。
  - Soft limit: 500 项；超限淘汰最久未用的 10%。
  - 释放：监听 `MemoryManager.AppMemoryUsageLimitChanging`、窗口隐藏超过 5 分钟时执行 `Trim()` 并可触发一次 `GC.Collect()`（仅在后台隐藏态）。
  - 严禁缓存 256x256 原图；最大尺寸 48px。

## 5. UI/UX 与渲染
- 视觉降级：
  1) Win11 Build 22000+: `MicaBackdrop (Alt)`。
  2) Win10 17134+: `DesktopAcrylicController`。
  3) 兜底：`SolidColorBrush`（根据系统主题取深浅灰）。使用 `ApiInformation.IsTypePresent`/OS Build 判断。
- 主题与 DPI：禁止硬编码颜色；用 ThemeResource；所有尺寸逻辑像素，测试 100%-200% 缩放。
- 列表渲染：强制开启虚拟化（GridView/ItemsRepeater 配 VirtualizingLayout/ItemsStackPanel）；使用 `x:Phase`（0 背景占位，1 文本，2 图标）减少首次滚动抖动。
- 搜索预留：ViewModel 提供 `CollectionViewSource.Filter` 接口，即使 UI 未暴露搜索框。

## 6. 窗口行为与定位
- 显示顺序：初始化 Hidden -> 计算坐标 -> Move -> Show；失焦 (Activated -> Deactivated) 时 Hide，调试模式可临时禁用自动隐藏。
- 多屏/任务栏定位算法：
  1) `GetCursorPos` 获取点击时指针位置。
  2) `DisplayArea.GetFromPoint` 获取当前屏幕的工作区 `WorkArea`/`DisplayRect`。
  3) 任务栏估计：
     - 先用 `SHAppBarMessage(ABM_GETTASKBARPOS)` 获取主任务栏方向/尺寸。
     - 对当前屏幕，比较 `DisplayRect` 与 `WorkArea` 差值来推断该屏幕任务栏边（顶部/底部/左/右）。
     - 若无法确定（如第三方任务栏），回退为当前屏幕底部居中并向上偏移 48px。
  4) 根据任务栏边计算弹出位置，使窗口紧贴任务栏且不遮挡；最后对 X/Y 做 Clamp，确保窗口在 `WorkArea` 内。
- 多屏校验：在副屏/不同任务栏位置手动验收，若定位回落到主屏视为缺陷。

## 7. 性能与冷启动策略
- 目标：冷启动至窗口可见 ≤200ms；静默驻留内存 <30MB（短暂峰值 <50MB，GC 回落）。
- 策略：
  - 进程启动即创建主窗口和基础 UI 树，但保持 Hidden；首屏渲染完毕再接受显示信号。
  - 启动并行：UI 线程只做轻量初始化；数据扫描与图标提取在后台线程（逐批）进行，首次显示可先呈现骨架/占位。
  - 延迟加载：图标按需加载；列表虚拟化防止一次性材料化全部项。
  - 计时：在 App 入口使用 `Stopwatch` 记录“Activation -> First Show”时长，必要时输出 ETW/EventSource 事件便于性能回归。

## 8. 兼容性与稳定性
- AOT/裁剪：提供 `rd.xml` 或 Trimming 配置，保留 WinUI 绑定类型、CommunityToolkit.Mvvm 生成器输出、任何反射访问的类型。
- GDI/句柄：监控 GDI Objects，确保 `HICON`、Bitmap 等及时 Dispose；提取失败不崩溃、显示默认图标。
- 路径/编码：全程使用 `UseShellExecute=true` + 原始字符串，支持中文、空格、特殊字符；测试 `C:\Test & Folder\应用 (v1.0).lnk` 等案例。
- Win10 降级：确保 Acrylic 不可用时平滑退到纯色且无崩溃。

## 9. 自检清单（发布前必须全部通过）
- [ ] 单实例：二次启动被重定向，主实例弹出窗口。
- [ ] 多屏/任务栏：副屏、顶部/左侧任务栏定位正确；无法确定时至少落在当前屏幕底部居中且不遮挡。
- [ ] UAC/管理员：右键管理员运行，UAC 取消不报错不崩溃。
- [ ] 路径特例：特殊字符/中文/空格的 LNK 可正常启动。
- [ ] 性能：冷启动到可见 ≤200ms；滚动 60fps；内存峰值 <50MB，回落 <30MB；GDI Objects 无持续增长。
- [ ] 兼容：Win10 虚拟机运行，材质自动降级；AOT 单文件运行无缺失元数据/绑定失效。

## 10. 隐藏陷阱（执行提示）
- 定位容差：计算 `DisplayRect` 与 `WorkArea` 差值推断任务栏边时，DPI 缩放会带来 1px 误差，比较时需加入容差（例如 ±1px）。
- 骨架屏过渡：骨架屏收起与真实数据呈现要用 Opacity 动画平滑过渡，避免闪烁。
- Watcher 释放：使用 `FileSystemWatcher` 后在退出或对象销毁时务必调用 `Dispose()`，否则可能占用句柄导致开始菜单快捷方式无法删除。
