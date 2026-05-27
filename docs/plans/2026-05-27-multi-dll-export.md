# Multi DLL Export Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a script export mode that decompiles game and third-party DLLs while excluding Unity and system assemblies.

**Architecture:** Keep the current `Hybrid` behavior intact and add a new explicit export mode. The decision stays centralized in `ScriptExporter.GetExportType`, with reusable assembly classification in `ReferenceAssemblies`.

**Tech Stack:** C#, NUnit, AssetRipper export pipeline, AsmResolver assembly metadata.

---

### Task 1: Add Export Decision Tests

**Files:**
- Modify: `Source/AssetRipper.Tests/ExportTests.cs`

**Step 1: Write the failing tests**

Add tests that configure `ScriptExportMode.MultiDllDecompiled` and verify:

```csharp
Assert.That(exporter.GetExportType("MoreMountains.Tools"), Is.EqualTo(AssemblyExportType.Decompile));
Assert.That(exporter.GetExportType("AK.Wwise.Unity.API"), Is.EqualTo(AssemblyExportType.Decompile));
Assert.That(exporter.GetExportType("Unity.Addressables"), Is.Not.EqualTo(AssemblyExportType.Decompile));
Assert.That(exporter.GetExportType("System.Runtime.CompilerServices.Unsafe"), Is.Not.EqualTo(AssemblyExportType.Decompile));
```

Also add a default settings test:

```csharp
Assert.That(new FullConfiguration().ExportSettings.ScriptExportMode, Is.EqualTo(ScriptExportMode.MultiDllDecompiled));
```

**Step 2: Run tests to verify failure**

Run:

```powershell
& 'D:\CrackALL\dotnet-sdk-10\dotnet.exe' test Source\AssetRipper.Tests\AssetRipper.Tests.csproj --no-restore --filter "MultiDll|DefaultScriptExport" --verbosity minimal
```

Expected: compilation fails because `MultiDllDecompiled` does not exist.

### Task 2: Add Export Mode And Filtering

**Files:**
- Modify: `Source/AssetRipper.Export/Configuration/ScriptExportMode.cs`
- Modify: `Source/AssetRipper.Export/Configuration/ExportSettings.cs`
- Modify: `Source/AssetRipper.Export.UnityProjects/Scripts/ReferenceAssemblies.cs`
- Modify: `Source/AssetRipper.Export.UnityProjects/Scripts/ScriptExporter.cs`

**Step 1: Add the enum value**

Add `MultiDllDecompiled` to `ScriptExportMode`.

**Step 2: Add classification helper**

Add `ReferenceAssemblies.IsUnityOrSystemAssembly(string assemblyName)` using prefix and exact-name checks for Unity and system assemblies.

**Step 3: Update export decision**

In `ScriptExporter.GetExportType`, add handling:

```csharp
else if (ExportMode is ScriptExportMode.MultiDllDecompiled)
{
	return ReferenceAssemblies.IsUnityOrSystemAssembly(assemblyName)
		? AssemblyExportType.Save
		: AssemblyExportType.Decompile;
}
```

**Step 4: Update default**

Set `ExportSettings.ScriptExportMode` default to `ScriptExportMode.MultiDllDecompiled`.

**Step 5: Run tests**

Run the same focused test command. Expected: tests pass.

### Task 3: Add UI Text

**Files:**
- Modify: `Source/AssetRipper.GUI.Web/Pages/Settings/DropDown/ScriptExportModeDropDownSetting.cs`
- Modify: `Localizations/en_US.json`
- Modify: `Localizations/zh_Hans.json`

**Step 1: Add display mapping**

Map `ScriptExportMode.MultiDllDecompiled` to a new localization key.

**Step 2: Add descriptions**

English display text: `Multi DLL Decompiled`

Chinese display text: `多 DLL 反编译`

Descriptions should state that game and third-party assemblies are decompiled while Unity and system assemblies are excluded.

**Step 3: Build GUI**

Run:

```powershell
& 'D:\CrackALL\dotnet-sdk-10\dotnet.exe' build .\Source\AssetRipper.GUI.Free\AssetRipper.GUI.Free.csproj --no-restore -c Debug --verbosity minimal
```

Expected: build succeeds.

### Task 4: Verify With Current Game Export

**Files:**
- No source file changes expected.

**Step 1: Restart local GUI**

Stop only the current AssetRipper process listening on port `6080`, then start the Debug GUI DLL on port `6080`.

**Step 2: Import game**

Run:

```powershell
Invoke-WebRequest -Uri 'http://127.0.0.1:6080/LoadFolder' -Method Post -Body @{ Path = 'D:\Stream\steamapps\common\Everything is Crab' } -UseBasicParsing -TimeoutSec 900
```

**Step 3: Export project**

Export to `D:\CrackALL\万物皆可蟹\破解资源1`.

**Step 4: Check output**

Verify that `Assets/Scripts` contains more directories than `Assembly-CSharp`, and that Unity/System assemblies are not decompiled into `Assets/Scripts`.

### Task 5: Commit And Push

**Files:**
- All modified source, tests, localization, and plan files.

**Step 1: Final checks**

Run:

```powershell
git diff --check
git status --short --branch
```

**Step 2: Commit**

Run:

```powershell
git add .
git commit -m "feat: decompile game and third-party dlls"
```

**Step 3: Push**

Run:

```powershell
git push -u origin feature/multi-dll-export
```
