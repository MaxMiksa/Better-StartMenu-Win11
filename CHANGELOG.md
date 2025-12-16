## v0.1.0 – Initial Prototype (2025-12-16)

### Feature 1: Native AOT & Core Architecture
- **Summary**: Established the WinUI 3 project structure with full Native AOT compilation support.
- **Problem Solved**: Solves the "bloat" issue of traditional .NET apps. Provides a lightweight, single-file executable (<10MB) that launches instantly without requiring the user to install the .NET Runtime.
- **Feature Details**: The application runs as a standalone `.exe`. It initializes the Windows App SDK bootstrapper manually to support unpackaged deployment.
- **Technical Implementation**: 
  - Configured `.csproj` with `<PublishAot>true</PublishAot>` and `<WindowsPackageType>None</WindowsPackageType>`.
  - Added `rd.xml` to preserve metadata for `CommunityToolkit.Mvvm` and `Microsoft.UI.Xaml` to prevent trimming crashes.
  - Implemented `Program.cs` with `Bootstrap.Initialize()` for unpackaged runtime loading.

### Feature 2: High-Performance Data Scanning Engine
- **Summary**: Implemented a recursive, filtered, and deduplicated scanning engine for Windows Start Menu entries.
- **Problem Solved**: The default Windows Start Menu is cluttered with uninstallers, help files, and website links.
- **Feature Details**: StartDeck recursively scans both Global and User-specific Start Menu directories. It automatically merges duplicates (prioritizing user shortcuts), removes non-executable files (like `.txt` or `.url`), and filters out "junk" shortcuts based on keywords (e.g., "uninstall", "readme").
- **Technical Implementation**: 
  - Created `FileSystemStartMenuSource` using `Task.Run` to offload scanning to a background thread.
  - Implemented `CsWin32` generated `IShellLinkW` interface for safe, AOT-compatible LNK target resolution (using `SLGP_RAWPATH`).
  - Implemented `KeywordFilter` and `ExtensionFilter` using a Strategy Pattern (`IAppFilter`).

### Feature 3: Optimized Icon Pipeline
- **Summary**: A thread-safe, cached icon extraction pipeline designed for UI fluidity.
- **Problem Solved**: Extracting icons (using `ExtractIconEx` or `SHGetFileInfo`) is slow and requires an STA thread. Doing this on the UI thread causes stuttering; doing it blindly on background threads causes COM exceptions.
- **Feature Details**: Icons load asynchronously. A placeholder is shown initially, followed by the high-res icon. Icons are cached in memory to ensure subsequent opens are instant.
- **Technical Implementation**: 
  - Implemented `StaTaskScheduler` to force Shell API calls onto a dedicated STA background thread.
  - Created a 3-stage pipeline: Extraction (STA) -> Byte Marshalling -> Bitmap Creation (UI Thread via `DispatcherQueue`).
  - Implemented `LruCache<string, byte[]>` with a soft limit of 500 items and auto-trimming logic on window hide.

### Feature 4: Smart Lifecycle Management
- **Summary**: Single-instance enforcement with "Hot Wake" capability.
- **Problem Solved**: Prevents multiple StartDeck windows from opening. Ensures that clicking the icon again toggles the existing window instead of launching a new process.
- **Feature Details**: If StartDeck is already running, a new launch attempt simply brings the existing window to the front and then exits. The window also automatically hides when it loses focus (mimicking system menu behavior).
- **Technical Implementation**: 
  - Utilized `AppInstance.RedirectActivationToAsync` for robust instance redirection.
  - Implemented `ToggleWindow()` logic in `App.xaml.cs` to handle Show/Hide states.
  - Hooked `Window.Activated` events to implement "Auto-Hide on Blur".