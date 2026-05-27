# Multi DLL Export Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a Web-selectable DLL export mode that decompiles selected game and third-party assemblies.

**Architecture:** Keep existing script export modes intact and add a new `SelectedDlls` mode. The Web export form owns the selected assembly list after a game is loaded, and `ScriptExporter.GetExportType` remains the single decision point for decompile versus save.

**Tech Stack:** C#, ASP.NET minimal endpoints, AssetRipper Web HTML helpers, NUnit, AssetRipper export pipeline.

---

### Task 1: Add Export Decision Tests

**Files:**
- Modify: `Source/AssetRipper.Tests/ExportTests.cs`

**Step 1: Write the failing tests**

Add tests for:

```csharp
Assert.That(new FullConfiguration().ExportSettings.ScriptExportMode, Is.EqualTo(ScriptExportMode.SelectedDlls));
```

Default candidate behavior:

```csharp
ScriptExporter exporter = CreateScriptExporter(ScriptExportMode.SelectedDlls);
Assert.That(exporter.GetExportType("Assembly-CSharp"), Is.EqualTo(AssemblyExportType.Decompile));
Assert.That(exporter.GetExportType("MoreMountains.Tools"), Is.Not.EqualTo(AssemblyExportType.Decompile));
Assert.That(exporter.GetExportType("Unity.Addressables"), Is.Not.EqualTo(AssemblyExportType.Decompile));
```

Explicit Web selection behavior:

```csharp
ScriptExporter exporter = CreateScriptExporter(ScriptExportMode.SelectedDlls, ["MoreMountains.Tools"]);
Assert.That(exporter.GetExportType("MoreMountains.Tools"), Is.EqualTo(AssemblyExportType.Decompile));
Assert.That(exporter.GetExportType("Assembly-CSharp"), Is.Not.EqualTo(AssemblyExportType.Decompile));
```

**Step 2: Run tests to verify failure**

Run:

```powershell
& 'D:\CrackALL\dotnet-sdk-10\dotnet.exe' test Source\AssetRipper.Tests\AssetRipper.Tests.csproj --no-restore --filter "DefaultScriptExportModeUsesSelectedDlls|SelectedDllsExportMode" --verbosity minimal
```

Expected: compilation fails until `SelectedDlls` and `SelectedScriptAssemblies` exist.

### Task 2: Add Export Mode And Selection Logic

**Files:**
- Modify: `Source/AssetRipper.Export/Configuration/ScriptExportMode.cs`
- Modify: `Source/AssetRipper.Export/Configuration/ExportSettings.cs`
- Modify: `Source/AssetRipper.Export.UnityProjects/Scripts/ReferenceAssemblies.cs`
- Modify: `Source/AssetRipper.Export.UnityProjects/Scripts/ScriptExporter.cs`

**Step 1: Add enum value**

Add `SelectedDlls` to `ScriptExportMode`.

**Step 2: Add selected list**

Add nullable `List<string>? SelectedScriptAssemblies` to `ExportSettings`. Null means use the default candidate selection. Empty list means decompile none.

**Step 3: Add default candidate helper**

Add `ReferenceAssemblies.IsDefaultSelectedAssembly(string assemblyName)`. It returns true only for predefined Unity game script assemblies such as `Assembly-CSharp`.

**Step 4: Update export decision**

In `ScriptExporter.GetExportType`, handle `SelectedDlls`:

```csharp
return IsSelectedForDecompilation(assemblyName)
	? AssemblyExportType.Decompile
	: AssemblyExportType.Save;
```

**Step 5: Run focused tests**

Run the command from Task 1. Expected: tests pass.

### Task 3: Add Web DLL Filter

**Files:**
- Modify: `Source/AssetRipper.GUI.Web/Pages/CommandsPage.cs`
- Modify: `Source/AssetRipper.GUI.Web/Pages/Commands.cs`
- Modify: `Source/AssetRipper.GUI.Web/GameFileLoader.cs`

**Step 1: Render DLL checkboxes**

On the loaded `Commands` page, inside the Unity project export form:

- Add hidden `UseScriptAssemblySelection=true`
- Render one checkbox per `GameFileLoader.AssemblyManager.GetAssemblies()`
- Checkbox name: `SelectedScriptAssemblies`
- Default checked state uses `ReferenceAssemblies.IsDefaultSelectedAssembly`

**Step 2: Parse submitted DLLs**

In `Commands.ExportUnityProject`, if `UseScriptAssemblySelection` is present:

- Set `ExportSettings.ScriptExportMode = ScriptExportMode.SelectedDlls`
- Set `ExportSettings.SelectedScriptAssemblies` to the posted values
- If no values are posted, set it to an empty list

**Step 3: Reset selection on new load**

After loading a new game, set `SelectedScriptAssemblies = null` so each game starts from default candidates.

### Task 4: Build And Export Verification

**Files:**
- No source file changes expected.

**Step 1: Build GUI**

Run:

```powershell
& 'D:\CrackALL\dotnet-sdk-10\dotnet.exe' build .\Source\AssetRipper.GUI.Free\AssetRipper.GUI.Free.csproj --no-restore -c Debug --verbosity minimal
```

Expected: build succeeds.

**Step 2: Restart local GUI**

Stop only the current AssetRipper process listening on port `6080`, then start the Debug GUI DLL on port `6080`.

**Step 3: Import and inspect Web page**

Load:

```powershell
Invoke-WebRequest -Uri 'http://127.0.0.1:6080/LoadFolder' -Method Post -Body @{ Path = 'D:\Stream\steamapps\common\Everything is Crab' } -UseBasicParsing -TimeoutSec 900
```

Open `http://127.0.0.1:6080/Commands` and verify the DLL checkbox list appears.

**Step 4: Export and inspect output**

Export to `D:\CrackALL\万物皆可蟹\破解资源1`.

Verify the default export only decompiles predefined game script assemblies. Then manually select one third-party DLL on the Web page, export again, and verify `Assets/Scripts/<程序集名>` exists for the selected DLL.

### Task 5: Commit And Push

**Files:**
- All modified source, tests, and plan files.

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
git commit -m "feat: add selectable dll script export"
```

**Step 3: Push**

Run:

```powershell
git push -u origin feature/multi-dll-export
```
