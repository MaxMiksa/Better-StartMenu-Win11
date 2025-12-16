# StartDeck Dev Codex Notes

## Feasibility
- Stack (.NET 8 + WinUI 3 + MVVM) with Native AOT is viable for a lightweight launcher; zero-dependency goal supports small single-exe output.

## Strengths
- Clean layering: Model for I/O/LNK/icon, ViewModel for state/filters, View in XAML with minimal code-behind.
- Extensible data pipeline: IAppFilter/IAppSource abstractions keep future sources (Steam/UWP) pluggable.
- Async + caching for icons aligns with fast reopen and smooth scrolling; default icons prevent blank UI.
- Visual adaptability: MicaAlt with Acrylic/solid fallback, ThemeResource usage, DPI awareness.
- Ops checklist covers AOT, DPI, multi-screen, memory leak, and path robustness.

## Risks and Gaps
- Single-instance/IPCs: Mutex/pipes can fail across install paths/sessions; prefer AppInstance.RegisterForKey + RedirectActivation to handle wake and args reliably.
- 200ms cold-start: WinUI 3 + AOT startup may exceed budget; need preload of window/data while hidden and avoid heavy sync work on UI thread.
- Virtualization: ItemsRepeater/ListView not virtualized by default; >100 items require VirtualizingLayout/ItemsStackPanel and ContainerContentChanging to keep 60fps and memory under control.
- Icon threading: SHGetFileInfo/ExtractIconEx must run on STA; Task.Run uses MTA. Use a dedicated STA icon worker and marshal results to UI to avoid COM/GDI issues and leaks.
- Cache bounds: Dictionary cache without limits can exceed 30MB; add size cap/LRU and release on memory pressure or after hide cooldown.
- Window placement: Need concrete API plan (DisplayArea + taskbar position via SHAppBarMessage/AppWindowPresenter) to pop on correct monitor/edge; otherwise may appear on wrong screen or overlap taskbar.
- Focus hide: Global mouse hook is heavy and AV-sensitive; Deactivated plus limited hook only when needed is safer.
- AOT/trimming: Must declare preserved metadata for bindings/COM/reflection; add trimming descriptors/RD.xml or source generators to avoid runtime missing-type failures.
- OS fallback: Detect OS build; MicaAlt requires 22000+, Acrylic may fail on some Win10 builds—provide solid color fallback.
- Path/process edge cases: Use full paths and ProcessStartInfo with UseShellExecute=true to handle spaces, non-ASCII, and special chars; cover UAC runas cancel handling for admin action.

## Recommendations
- Adopt AppInstance single-instance + activation redirection; keep IPC payload ready for future args (e.g., search).
- Prewarm hidden window and data scan; defer icon loads off UI thread; keep UI ready to show instantly.
- Enforce item virtualization for large lists; consider incremental load if needed.
- Implement STA icon worker with disposal of GDI/Bitmap resources; marshal to UI via DispatcherQueue.
- Add cache cap and eviction, and release on memory pressure or after hide; measure to stay under 30MB.
- Implement OS/version checks and taskbar-aware positioning per display; test multi-monitor/top/left taskbar.
- Prefer Deactivated for auto-hide; only enable low-level hooks when absolutely required.
- Ship trimming config for AOT; avoid unbounded reflection.
- Harden process launch paths and admin verb flow with user-friendly cancellation handling.
