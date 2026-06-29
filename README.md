# GGTT Helper / 零西五笔工坊

> 一个五笔码表相关的辅助工具。

这个仓库从 2016 年的 PyQt4 `ggtt_helper` 继续往前走。根目录里仍保留几个十年前的原始文件，不修改、不覆盖：

- `bm.txt`
- `main.py`
- `main.ui`
- `Ui_main.py`

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
