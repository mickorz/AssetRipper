# 多 DLL 导出功能设计

## 背景

当前导出的 Unity 工程中，默认脚本导出模式是 `Hybrid`。这个模式只把 `Assembly-CSharp` 等 Unity 预定义游戏程序集反编译到 `Assets/Scripts`，其他非引用程序集会保存为 DLL 到 `Assets/Plugins`。

用户期望的是：除了 Unity 和 System 自带程序集之外，游戏和第三方 DLL 也反编译为 C# 脚本，放入 `Assets/Scripts/<程序集名>`。

## 目标

新增一个明确的多 DLL 脚本反编译模式，并作为默认导出策略：

- `Assembly-CSharp` 继续反编译到 `Assets/Scripts/Assembly-CSharp`
- 第三方或游戏 DLL 反编译到 `Assets/Scripts/<程序集名>`
- Unity 自带程序集不反编译
- System 和 Microsoft 等系统程序集不反编译
- 保留现有 `Hybrid` 行为，避免需要旧逻辑时无法回退

## 设计方案

### 脚本导出模式

在 `ScriptExportMode` 中新增模式：

```csharp
MultiDllDecompiled
```

该模式语义为：反编译游戏和第三方程序集，跳过或保存 Unity 和系统程序集。

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

这些程序集不进入多 DLL 反编译流程。

### 导出决策

`ScriptExporter.GetExportType` 在新模式下按以下顺序处理：

1. 已知引用程序集仍然 `Skip`
2. Unity 或系统程序集使用 `Save`
3. 其他程序集使用 `Decompile`

这样可以得到：

- `MoreMountains.Tools.dll` 反编译
- `AK.Wwise.Unity.API.dll` 反编译
- `Sirenix.Utilities.dll` 反编译
- `Newtonsoft.Json.dll` 反编译
- `Unity.Addressables.dll` 不反编译
- `System.Runtime.CompilerServices.Unsafe.dll` 不反编译

## 类比理解

当前 `Hybrid` 模式像是只把主剧本翻译成可编辑文本，其他剧本包都原封不动放到旁边。

新的多 DLL 模式像是把主剧本和第三方剧本包都拆开翻译，但 Unity 引擎说明书和系统说明书不拆。这样工程里能直接看到更多业务代码，同时不会把引擎和系统依赖混进脚本源码。

## 验证标准

- 默认脚本导出模式变为 `MultiDllDecompiled`
- `ScriptExporter.GetExportType("MoreMountains.Tools")` 返回 `Decompile`
- `ScriptExporter.GetExportType("Unity.Addressables")` 不返回 `Decompile`
- `ScriptExporter.GetExportType("System.Runtime.CompilerServices.Unsafe")` 不返回 `Decompile`
- 导出后 `Assets/Scripts` 下不再只有 `Assembly-CSharp`

## 引用说明

- 本地代码：`Source/AssetRipper.Export/Configuration/ScriptExportMode.cs`
- 本地代码：`Source/AssetRipper.Export/Configuration/ExportSettings.cs`
- 本地代码：`Source/AssetRipper.Export.UnityProjects/Scripts/ScriptExporter.cs`
- 本地代码：`Source/AssetRipper.Export.UnityProjects/Scripts/ReferenceAssemblies.cs`
- 本地代码：`Source/AssetRipper.Export.UnityProjects/Scripts/ScriptExportCollection.cs`
