# StartDeck | [中文文档](README-zh.md)

![License](https://img.shields.io/badge/License-MIT-blue.svg) ![Platform](https://img.shields.io/badge/Platform-Windows%2011%20%2F%2010-blue) ![Status](https://img.shields.io/badge/Status-v0.1.0%20(Preview)-orange)

✅ **Instant Launch** (Native AOT) | **Zero Dependency** (Single .exe) | **Privacy First** (Local Only)
✅ **Native UI** (Mica Material) | **Smart Filtering** (No Junk) | **Hot Wake** (Always Ready)

<br/>

<div align="center">
  <img src="https://via.placeholder.com/800x500.png?text=StartDeck+Preview+(Place+Screenshot+Here)" alt="StartDeck Preview" width="800"/>
  <br/>
  <i>(Screenshot placeholder - Replace with actual app screenshot after build)</i>
</div>

<br/>

## ✨ Key Features

| Feature | Description |
| :--- | :--- |
| 🚀 **Instant Launch** | Compiled with **Native AOT** for <200ms startup time. No .NET Runtime installation required. |
| 🎨 **Native Design** | Built with **WinUI 3** and **Mica** material to blend perfectly with Windows 11 aesthetics. |
| ⚡ **Smart Indexing** | Automatically scans Start Menu, filters out uninstallers/help files, and prioritizes user shortcuts. |
| 🔒 **Reliable** | Enforces single-instance execution, supports hot-waking, and auto-hides on focus loss. |
| 🧹 **Zero Clutter** | No ads, no recommendations, no web search integration. Just your apps. |

## 🚀 Usage Guide

1.  **Download** the latest `StartDeck.exe` from [Releases](../../releases).
2.  **Run** the executable. No installation needed.
3.  **Pin** to Taskbar for easy access.
    *   *Tip:* Click the taskbar icon to toggle. Right-click apps for "Run as Admin".

## 📚 Technical Details

<details>
   <summary><strong>Requirements & Limits</strong></summary>

*   **OS:** Windows 11 (Build 22000+) or Windows 10 (Build 19041+).
*   **Architecture:** x64.
*   **Permissions:** Standard User (Admin only required for "Run as Admin" feature).
</details>

<details>
   <summary><strong>Developer Guide (Build Locally)</strong></summary>

1.  Install **Visual Studio 2022** with ".NET Desktop Development" and "Windows App SDK C# Templates".
2.  Open `StartDeck.sln`.
3.  Publish using Native AOT profile:
    ```bash
    dotnet publish -r win-x64 -c Release /p:PublishAot=true
    ```
</details>

<details>
   <summary><strong>Development Stack</strong></summary>

1.  **Frameworks:** .NET 8, Windows App SDK (WinUI 3).
2.  **Key Libraries:** CommunityToolkit.Mvvm, CsWin32 (Source Generator).
3.  **Language:** C# 12.
</details>

<details>
   <summary><strong>License</strong></summary>

Distributed under the MIT License. See `LICENSE` for more information.
</details>

## Contribution & Contact

Welcome to submit Issues and Pull Requests!
Any questions or suggestions? Please contact Max Kong (Carnegie Mellon University, Pittsburgh, PA).

Max Kong: kongzheyuan@outlook.com | zheyuank@andrew.cmu.edu
