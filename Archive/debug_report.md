# Codebase Debugging & Analysis Report

After a detailed review of the generated codebase against `DEV.md` and `PRD.md`, the following bugs and architectural issues have been identified.

## 🚨 Critical Issues (Must Fix)

1.  **Missing Bootstrapper Initialization (Unpackaged App)**
    *   **File:** `Program.cs`
    *   **Issue:** The app appears to be designed as "Unpackaged" (Native AOT implies this). However, `Program.cs` lacks the `Bootstrap.Initialize()` call to load the Windows App SDK runtime before using APIs like `AppInstance` or `Application.Start`.
    *   **Consequence:** The application will likely crash immediately on startup with `DllNotFoundException` or fail to load WASDK components.

2.  **UI Thread Blocking during Data Scan**
    *   **File:** `Sources/FileSystemStartMenuSource.cs`
    *   **Issue:** `GetAppsAsync` returns `Task.FromResult`, meaning the entire LNK parsing logic runs synchronously. Since `MainWindow.LoadCatalogAsync` calls this from the UI thread, parsing hundreds of shortcuts will **freeze the UI** until completion.
    *   **Fix:** Wrap the scanning logic in `Task.Run` within `GetAppsAsync` or `AppCatalogService`.

3.  **App Lifecycle vs. Window Closure**
    *   **File:** `App.xaml.cs` -> `OnWindowClosed`
    *   **Issue:** `OnWindowClosed` calls `Dispose()` on services. If the user closes the window (e.g., Alt+F4), the services are dead. If the process is intended to stay alive (as a "Start Menu" replacement often does) for quick hot-wake, the next activation will fail or crash because services are disposed.
    *   **Fix:** override `Window.Closed` to prevent exit? Or ensure `App` exits when `Window` closes. Given the requirement for "Fast Startup" (Hot Wake), we should explicitly decide: does `Close` = `Exit Process`? If so, the current logic is fine. If `Close` = `Hide`, this is a bug. (PRD implies it acts like a menu, so `Hide` is preferred, but standard windows `Close` kills it).

## ⚠️ Major Bugs

4.  **LNK Parsing Safety**
    *   **File:** `Sources/FileSystemStartMenuSource.cs`
    *   **Issue:** `shellLink.GetPath` uses flag `0`. If a target is moved or network drive is offline, this might hang or fail.
    *   **Fix:** Use `SLGP_RAWPATH` if purely scanning, or ensure timeout safety.

5.  **Icon Extraction Ownership**
    *   **File:** `Services/IconService.cs`
    *   **Issue:** `Icon.FromHandle` does not transfer ownership. The code correctly uses `finally { PInvoke.DestroyIcon }`. *However*, `System.Drawing` usage in Native AOT requires careful validation (usually works, but `Bitmap` GDI+ wrappers can be tricky).

## 🔧 Minor / Potential Issues

6.  **P/Invoke Namespace**
    *   **File:** `Services/PositioningService.cs`
    *   **Issue:** Uses `PInvoke.GetCursorPos` and `POINT` struct. Need to ensure `CsWin32` is configured to generate these in the expected namespaces. If `POINT` is generated in `Windows.Win32.UI.WindowsAndMessaging` but the code assumes a global or `Windows.Win32.Foundation` namespace (or implicit), it will fail compilation.

7.  **Taskbar Fallback Logic**
    *   **File:** `Services/PositioningService.cs`
    *   **Issue:** Hardcoded `48` pixel fallback. If the user has a hidden taskbar or vertical taskbar (on Win10), this fallback might be inaccurate.

8.  **Mica Backwards Compatibility**
    *   **File:** `MainWindow.xaml.cs`
    *   **Issue:** `MicaBackdrop` properties might throw on older Windows builds even before assignment. The `try-catch` block is good, but `IsTypePresent` check is cleaner and safer.

## 📝 Compliance Check (vs Dev.md)

*   **AOT Compatibility:** Code avoids reflection. JSON is not used. `ComWrappers` is initialized. Looks good.
*   **Threading:** Icons are extracted on STA thread (`StaTaskScheduler`). `BitmapImage` created on UI thread via `DispatcherQueue`. Excellent.
*   **Virtualization:** `MainWindow.xaml` (not fully visible in snippets, but assuming `GroupRepeater` usage) needs to verify if `ItemsRepeater` is used with `StackLayout` (Virtualizing) or `StackPanel` (Non-Virtualizing). Code uses `GroupRepeater`, implies `ItemsRepeater`. Need to check XAML for `VirtualizingLayout`.

## Next Steps for Debugging

1.  **Add `Bootstrap.Initialize()`** to `Program.cs`.
2.  **Offload Scanning** to `Task.Run`.
3.  **Verify XAML** for Virtualization settings.
4.  **Compile & Run** on Windows to catch P/Invoke namespace mismatches.
