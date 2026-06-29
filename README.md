# GGTT Helper / 零西五笔工坊

> 文艺复兴：五笔 AI 时代易用性改造计划。

五笔不该只停留在“能打”，还应该可查、可改、可恢复、可解释。  
词库是可读数据，不是黑箱数据库；排序是明确意图，不是模糊词频漂移。  
AI 可以辅助造词、解释冲突和整理领域包，但日常输入路径必须稳定、本地、由用户掌控。

现代部分已经升级为 **零西五笔工坊**：Windows / .NET 8 / WPF 桌面工具，保留 Alt+Z 快速查拆，并增加词库工坊，用来查询同码候选、置顶、确认首位、放候选 4、屏蔽、加入词库、部署 Rime。

## 构建

```powershell
dotnet build .\ZeroXiWubiWorkshop.csproj -c Release
```

发布单文件：

```powershell
dotnet publish .\ZeroXiWubiWorkshop.csproj -c Release `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:SelfContained=false `
  -o .\publish
```

## 与 zeroxi-wubi 的关系

`GGTT_Helper` 是 Windows 工具插件，可以独立开发；`zeroxi-wubi` 是输入法/Rime/Next 主仓库，可以通过子模块包含本工具。

词库工坊默认会向上寻找 `zeroxi-wubi\integrations\rime\source`。如果独立运行，可以在设置里手动选择主仓库路径。
