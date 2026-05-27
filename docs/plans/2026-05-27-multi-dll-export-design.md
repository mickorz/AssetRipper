# 多 DLL 导出功能设计

## 背景

当前导出的 Unity 工程中，默认脚本导出模式是 `Hybrid`。这个模式只把 `Assembly-CSharp` 等 Unity 预定义游戏程序集反编译到 `Assets/Scripts`，其他非引用程序集会保存为 DLL 到 `Assets/Plugins`。

用户期望的是：除了 Unity 和 System 自带程序集之外，游戏和第三方 DLL 也反编译为 C# 脚本，放入 `Assets/Scripts/<程序集名>`。

## 目标

新增一个可在 Web 前台筛选 DLL 的脚本反编译模式，并作为默认导出策略：

- `Assembly-CSharp` 继续反编译到 `Assets/Scripts/Assembly-CSharp`
- 默认只勾选 Unity 预定义游戏脚本程序集，避免一次导出反编译过多 DLL
- 第三方或游戏 DLL 由用户手动勾选，并反编译到 `Assets/Scripts/<程序集名>`
- 默认不勾选 Unity 自带程序集
- 默认不勾选 System 和 Microsoft 等系统程序集
- 用户可以在 Web 导出页面手动调整勾选列表
- 保留现有 `Hybrid` 行为，避免需要旧逻辑时无法回退

## 设计方案

### 脚本导出模式

在 `ScriptExportMode` 中新增模式：

```csharp
SelectedDlls
```

该模式语义为：反编译用户选择的 DLL，未选择的 DLL 保存为插件。

### 过滤规则

在 `ReferenceAssemblies` 中补充判断函数：

```csharp
IsUnityOrSystemAssembly(string assemblyName)
```

判断为 Unity 或系统程序集的典型前缀：

- `Unity`
- `UnityEngine`
- `UnityEditor`
- `System`
- `Microsoft`
- `mscorlib`
- `netstandard`
- `Mono`
- `mcs`

这些程序集默认不勾选。默认候选规则只勾选 `Assembly-CSharp`、`Assembly-CSharp-firstpass` 等 Unity 预定义游戏脚本程序集；第三方 DLL 需要用户按需勾选。

### 导出决策

`ScriptExporter.GetExportType` 在新模式下按以下顺序处理：

1. 已知引用程序集仍然 `Skip`
2. 如果 Web 提交了选择列表，只反编译列表中的程序集
3. 如果没有 Web 选择列表，使用默认候选规则
4. 未选择的程序集使用 `Save`

这样可以得到：

- 默认 `Assembly-CSharp.dll` 反编译
- 默认 `MoreMountains.Tools.dll` 不反编译
- 默认 `AK.Wwise.Unity.API.dll` 不反编译
- 默认 `Sirenix.Utilities.dll` 不反编译
- 默认 `Newtonsoft.Json.dll` 不反编译
- 默认 `Unity.Addressables.dll` 不反编译
- 默认 `System.Runtime.CompilerServices.Unsafe.dll` 不反编译
- 用户勾选 `MoreMountains.Tools.dll` 后，它进入反编译流程

### Web 前台

导入游戏后，在 `Commands` 页面导出 Unity 工程的表单中显示 DLL 筛选区域：

- 每个程序集一个复选框
- 默认只选中 `Assembly-CSharp` 这类预定义游戏脚本程序集
- 第三方 DLL 默认不选中，避免长时间卡在脚本反编译
- Unity/System 类程序集默认不选中
- 提交导出时把 `SelectedScriptAssemblies` 一起提交到后端

后端只在表单带有 `UseScriptAssemblySelection` 标记时更新选择列表。这样命令行或 API 直接调用 `/Export/UnityProject` 时，不会因为没有复选框字段而误清空默认选择。

## 类比理解

当前 `Hybrid` 模式像是只把主剧本翻译成可编辑文本，其他剧本包都原封不动放到旁边。

新的筛选模式像是给每个剧本包前面放一个开关。默认只打开主剧本，第三方剧本包先保持关闭；需要看哪个包，就在导出前打开哪个开关，避免一开始就把所有包都拆开导致等待过久。

## 验证标准

- 默认脚本导出模式变为 `SelectedDlls`
- 无 Web 选择列表时，`ScriptExporter.GetExportType("Assembly-CSharp")` 返回 `Decompile`
- 无 Web 选择列表时，`ScriptExporter.GetExportType("MoreMountains.Tools")` 不返回 `Decompile`
- 无 Web 选择列表时，`ScriptExporter.GetExportType("Unity.Addressables")` 不返回 `Decompile`
- 无 Web 选择列表时，`ScriptExporter.GetExportType("System.Runtime.CompilerServices.Unsafe")` 不返回 `Decompile`
- Web 选择列表只包含 `MoreMountains.Tools` 时，只有该程序集进入反编译流程
- 勾选第三方 DLL 后，导出结果会在 `Assets/Scripts/<程序集名>` 下生成对应脚本

## 引用说明

- 本地代码：`Source/AssetRipper.Export/Configuration/ScriptExportMode.cs`
- 本地代码：`Source/AssetRipper.Export/Configuration/ExportSettings.cs`
- 本地代码：`Source/AssetRipper.Export.UnityProjects/Scripts/ScriptExporter.cs`
- 本地代码：`Source/AssetRipper.Export.UnityProjects/Scripts/ReferenceAssemblies.cs`
- 本地代码：`Source/AssetRipper.Export.UnityProjects/Scripts/ScriptExportCollection.cs`
- 本地代码：`Source/AssetRipper.GUI.Web/Pages/CommandsPage.cs`
- 本地代码：`Source/AssetRipper.GUI.Web/Pages/Commands.cs`
