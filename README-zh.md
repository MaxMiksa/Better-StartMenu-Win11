# StartDeck | [English Document](README.md)

![License](https://img.shields.io/badge/License-MIT-blue.svg) ![Platform](https://img.shields.io/badge/Platform-Windows%2011%20%2F%2010-blue) ![Status](https://img.shields.io/badge/Status-v0.1.0%20(Preview)-orange)

✅ **极速启动** (Native AOT) | **零依赖** (单一 .exe) | **隐私优先** (完全本地)
✅ **原生 UI** (Mica 云母材质) | **智能过滤** (拒绝垃圾) | **热唤醒** (随时待命)

<br/>

<div align="center">
  <img src="https://via.placeholder.com/800x500.png?text=StartDeck+Preview+(Place+Screenshot+Here)" alt="StartDeck Preview" width="800"/>
  <br/>
  <i>(截图占位符 - 请在构建后替换为实际截图)</i>
</div>

<br/>

## ✨ 核心特性

| 特性 | 描述 |
| :--- | :--- |
| 🚀 **极速启动** | 采用 **Native AOT** 编译，启动延迟低于 200ms。无需安装 .NET 运行时。 |
| 🎨 **原生设计** | 基于 **WinUI 3** 构建，使用 **Mica (云母)** 材质，完美融合 Win11 视觉风格。 |
| ⚡ **智能索引** | 自动递归扫描系统与用户开始菜单，过滤卸载程序/帮助文档，优先展示用户级应用。 |
| 🔒 **稳定可靠** | 强制单实例运行，支持热唤醒（Hot Wake），失焦自动隐藏。 |
| 🧹 **纯净无扰** | 无广告、无推荐内容、无必应搜索。只展示你的应用。 |

## 🚀 使用指南

1.  从 [Releases](../../releases) 下载最新的 `StartDeck.exe`。
2.  双击 **运行**。无需安装。
3.  **固定** 到任务栏以便快速访问。
    *   *提示:* 点击任务栏图标切换显示/隐藏。右键点击应用图标可选择“以管理员身份运行”。

## 📚 技术细节

<details>
   <summary><strong>系统要求与限制</strong></summary>

*   **操作系统:** Windows 11 (Build 22000+) 或 Windows 10 (Build 19041+).
*   **架构:** x64.
*   **权限:** 普通用户权限运行（仅在使用“管理员运行”功能时请求提权）。
</details>

<details>
   <summary><strong>开发者指南 (本地构建)</strong></summary>

1.  安装 **Visual Studio 2022**，勾选 ".NET 桌面开发" 和 "Windows App SDK C# 模板"。
2.  打开 `StartDeck.sln`。
3.  使用 Native AOT 配置文件发布：
    ```bash
    dotnet publish -r win-x64 -c Release /p:PublishAot=true
    ```
</details>

<details>
   <summary><strong>技术栈</strong></summary>

1.  **框架:** .NET 8, Windows App SDK (WinUI 3).
2.  **关键库:** CommunityToolkit.Mvvm, CsWin32 (源生成器).
3.  **语言:** C# 12.
</details>

<details>
   <summary><strong>许可证</strong></summary>

本项目采用 MIT 许可证分发。详情请参阅 `LICENSE` 文件。
</details>

## Contribution & Contact

Welcome to submit Issues and Pull Requests!
Any questions or suggestions? Please contact Zheyuan (Max) Kong (Carnegie Mellon University, Pittsburgh, PA).

Zheyuan (Max) Kong: kongzheyuan@outlook.com | zheyuank@andrew.cmu.edu
