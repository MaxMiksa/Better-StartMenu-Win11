# StartDeck 开发执行 Checklist（基于 PRD + Dev + Dev_1.3）

## 0. 工程初始化
- [x] 建立 WinUI 3 (.NET 8) 项目骨架，启用 Native AOT (`<PublishAot>true</PublishAot>`)，禁用多余模板页。
- [x] 仅添加 `CommunityToolkit.Mvvm` 与 `CsWin32` 包；去除默认引用的 Newtonsoft.Json 等反射重包。
- [ ] 全局默认使用 `x:Bind`；如需 `{Binding}`，在 `rd.xml` 声明保留元数据。
- [x] 配置 CsWin32 生成所需 Win32/Shell 接口（`IShellLinkW`, `IPersistFile`, `SHGetFileInfo`, `ExtractIconEx`, `SHAppBarMessage`, `GetCursorPos` 等）。

## 1. 单实例与生命周期
- [x] 使用 `AppInstance` + `RedirectActivationToAsync` 实现单实例与唤醒，预留参数透传（如 `--search`）。
- [x] 冷启动：注册 key，预热窗口/数据但保持 Hidden；热唤醒：重定向并退出，主实例 Activated 中切 UI 线程显示。
- [x] 自动隐藏：Activated -> Deactivated 时 Hide，调试模式可关闭。
- [ ] 验证：二次启动被重定向且主实例弹窗。

## 2. 数据扫描与过滤
- [x] 数据源：系统/用户开始菜单目录递归；实现 `IAppSource` 便于扩展。
- [ ] 应用启动时执行一次全量扫描，保证初始列表完整；后续由监听/兜底维护。
- [x] FileSystemWatcher 监听新增/删除/重命名；失效或异常时，Show 时若 LastScanTime > 1h 触发静默全量扫描；退出/释放时调用 `Dispose()`。
- [x] LNK 解析：仅用 CsWin32 `IShellLinkW`/`IPersistFile`；COM 异常或目标缺失 -> `IsValid=false` 并移除。
- [x] 过滤链：`ExtensionFilter(.lnk)`；关键词屏蔽（uninstall/卸载/help/帮助/readme/说明/website/网站/url，不区分大小写）；用户级优先去重（同名以用户级覆盖系统级）。
- [x] 分组：根目录快捷方式归入“未分类/常用”组；一级文件夹为分组；空分组不展示。
- [x] 排序：分组 A-Z，组内 A-Z；中文按拼音首字母。

## 3. 图标流水线与缓存
- [x] STA worker 提取：`ExtractIconEx/SHGetFileInfo` -> `HICON` -> `Bitmap.FromHicon` -> Resize 32/48 -> PNG byte[] -> `DestroyIcon` + Dispose。
- [x] UI 线程：`BitmapImage.SetSourceAsync`，失败回退默认图标。
- [x] 缓存：`LruCache<string, byte[]>`，Soft limit 500，超限淘汰最久 10%；隐藏 5 分钟或内存压力触发 Trim，可在隐藏态触发一次 GC；严禁缓存 256x256。
- [ ] 监控：GDI Objects 不增长；内存峰值 <50MB，回落 <30MB。

## 4. UI/UX 规范实现
- [ ] 窗口：无标题栏，宽 600-700px，高 500-600px，圆角 8px，1px 高亮边框；背景材质优先级 MicaAlt (Win11 22000+)-> Acrylic (Win10 17134+)-> 纯色；使用 ThemeResource 不硬编码颜色。
- [ ] 布局：顶部标题+搜索位（可占位）+刷新；中部分组标题 14px/60% 透明，下边距 8px；应用单元 80-90px 宽/80px 高，图标 32px，文字 12px 居中两行省略；底部栏 48px 显示头像+用户名，电源菜单含关机/重启/睡眠。
- [ ] 动效：EntranceThemeTransition 入场；悬停高亮；点击 Tilt；骨架屏到真实数据使用 Opacity 平滑过渡避免闪烁。
- [ ] 列表性能：强制虚拟化（GridView/ItemsRepeater + VirtualizingLayout/ItemsStackPanel）；`x:Phase`（0 背景、1 文本、2 图标）分阶段渲染。

## 5. 窗口定位与容差
- [ ] 定位流程：获取光标 -> `DisplayArea.GetFromPoint` -> 计算 WorkArea/DisplayRect 差值推断任务栏边；主任务栏信息通过 `SHAppBarMessage(ABM_GETTASKBARPOS)` 参考。
- [ ] 无法确定任务栏位置时，回退当前屏幕底部居中上移 48px；最终 Clamp 保证窗口在 WorkArea 内。
- [ ] 考虑 DPI 缩放 ±1px 容差，避免差分误判。
- [ ] 验证：副屏、顶部/左侧任务栏场景定位正确（回落到主屏视为缺陷）。
- [ ] 任务栏点击行为：点击任务栏图标显示窗口，再次点击或失焦隐藏，显示/隐藏切换无闪烁。

## 6. 应用执行与菜单
- [ ] 左键启动：`ProcessStartInfo UseShellExecute=true`；捕获 `Win32Exception`，UAC 取消 (`NativeErrorCode==1223`) 静默。
- [ ] 右键菜单：Run as Administrator（Verb=runas，取消静默）与 Open File Location（定位到 LNK 所在目录并选中）。
- [ ] 启动后立即隐藏窗口。

## 7. 性能与度量
- [ ] 冷启动计时：使用 `Stopwatch` 记录 Activation -> First Show，目标 ≤200ms；必要时输出 ETW/EventSource 方便回归。
- [ ] 滚动体验：虚拟化 + 分层渲染确保 60fps；列表大数据量下无掉帧。
- [ ] 内存：静默驻留 <30MB；峰值 <50MB，GC 回落。

## 8. 兼容性与稳定性
- [ ] DPI 100%-200% 文字/图标清晰无模糊；高分屏验证。
- [ ] Win10 降级：Acrylic 不可用时自动退纯色且不崩溃。
- [ ] AOT 发布：rd.xml/Trimming 配置完整；单文件运行无 MissingMetadata/绑定失效。
- [ ] 路径特例：含空格、中文、特殊字符的 LNK（如 `C:\Test & Folder\应用 (v1.0).lnk`）可正常启动。
- [ ] FileSystemWatcher Dispose：退出或对象销毁时释放，防止占用句柄影响删除快捷方式。

## 9. 发布前自检（基于 Dev.md 最终版）
- [ ] 单实例重定向正常；热唤醒弹窗。
- [ ] 多屏/任务栏定位正确或至少落当前屏底部居中不遮挡。
- [ ] UAC 取消无崩溃无报错；管理员运行正常。
- [ ] 功能/UI 满足 PRD 尺寸、动效、布局要求；底部栏显示头像+用户名，电源菜单包含关机/重启/睡眠。
- [ ] 性能、内存、GDI 句柄指标达标；骨架屏过渡无闪烁。
- [ ] Win10/Win11 兼容通过；AOT 单文件运行正常；开始菜单快捷方式处理稳定，无损坏 LNK 崩溃。

## 10. 版本发布流程 (遵守 AI_RELEASE_RULES.md)
- [ ] 分析变更：基于 `git diff` 提取真实变更，不虚构功能。
- [ ] 更新版本号：修改 `package.json` 中的 `version` 字段。
- [ ] 生成文档：
    - [ ] `CHANGELOG-zh.md` & `CHANGELOG.md` (详细技术日志)。
    - [ ] `RELEASE_NOTES.md` (用户向发布说明，含 Emoji 与表格)。
    - [ ] 若有重大 UI/功能变更，更新 `README.md` & `README-zh.md` (Hook, Demo, Usage)。
- [ ] Git 操作：Commit (`chore: release vX.Y.Z`) -> Tag -> Push -> GitHub Release (Body 复制 RELEASE_NOTES)。
