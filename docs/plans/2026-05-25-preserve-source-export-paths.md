# Preserve Source Export Paths Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add an opt-in Unity project export mode that preserves asset container paths from `OriginalPath` when available.

**Architecture:** Add an export setting, centralize export destination calculation in a small helper, then route `AssetExportCollection` through that helper. The default mode keeps current behavior; the new mode uses sanitized `OriginalPath` directory and file name while retaining exporter-controlled extensions.

**Tech Stack:** C# 13, .NET solution `AssetRipper.slnx`, NUnit tests in `Source/AssetRipper.Tests`, existing `VirtualFileSystem` export tests.

---

### Task 1: Add Export Mode Configuration

**Files:**
- Create: `Source/AssetRipper.Export/Configuration/AssetPathExportMode.cs`
- Modify: `Source/AssetRipper.Export/Configuration/ExportSettings.cs`
- Modify: `Source/AssetRipper.GUI.Web/Pages/Settings/DropDown/AssetPathExportModeDropDownSetting.cs`
- Modify: `Source/AssetRipper.GUI.Web/Pages/Settings/SettingsPage.cs`
- Modify: `Source/AssetRipper.GUI.Web/Pages/Settings/SettingsPage.g.cs`
- Modify: `Source/AssetRipper.GUI.Localizations/Localization.cs`

**Step 1: Add enum**

Create `AssetPathExportMode`:

```csharp
namespace AssetRipper.Export.Configuration;

public enum AssetPathExportMode
{
	Default,
	PreserveContainerPath,
}
```

**Step 2: Add setting**

Add to `ExportSettings`:

```csharp
public AssetPathExportMode AssetPathExportMode { get; set; } = AssetPathExportMode.Default;
```

Add to `Log()`:

```csharp
Logger.Info(LogCategory.General, $"{nameof(AssetPathExportMode)}: {AssetPathExportMode}");
```

**Step 3: Add settings UI**

Create a dropdown setting class matching existing `*DropDownSetting` style. Add English or Chinese localization properties in `Localization.cs` because the project currently defaults to `zh-Hans`.

**Step 4: Update generated settings page**

Either regenerate `SettingsPage.g.cs` with the source generator or update it manually using existing generated patterns:

```csharp
case nameof(ExportSettings.AssetPathExportMode):
	Configuration.ExportSettings.AssetPathExportMode = TryParseEnum<AssetPathExportMode>(value);
	break;
```

And add:

```csharp
private static void WriteDropDownForAssetPathExportMode(TextWriter writer)
{
	WriteDropDown(writer, AssetPathExportModeDropDownSetting.Instance, Configuration.ExportSettings.AssetPathExportMode, nameof(ExportSettings.AssetPathExportMode));
}
```

**Step 5: Commit**

```powershell
git add Source/AssetRipper.Export/Configuration Source/AssetRipper.GUI.Web/Pages/Settings Source/AssetRipper.GUI.Localizations/Localization.cs
git commit -m "feat: add asset path export mode setting"
```

### Task 2: Add Export Path Resolver

**Files:**
- Create: `Source/AssetRipper.Export.UnityProjects/Paths/ExportPathInfo.cs`
- Create: `Source/AssetRipper.Export.UnityProjects/Paths/ExportPathResolver.cs`
- Test: `Source/AssetRipper.Tests/ExportPathResolverTests.cs`

**Step 1: Write tests**

Cover these cases:

- Default mode returns existing directory and sanitized best name.
- Preserve mode with `Assets/Textures/Hero.png` returns directory `Assets/Textures` and name `Hero`.
- Preserve mode keeps exporter extension separate, so output can become `Hero.asset` when the exporter says `asset`.
- Preserve mode rejects `../escape/Hero.png` and falls back.
- Preserve mode rejects rooted paths that cannot be normalized to a safe project-relative path.

**Step 2: Implement helper**

The helper should:

- Accept `IUnityObjectBase asset`, `AssetPathExportMode mode`.
- Return sanitized relative directory and base file name without extension.
- Use `OriginalPath` only in `PreserveContainerPath`.
- Normalize slashes before parsing.
- Reject empty segments, `.` and `..`.
- Reject rooted paths unless an `Assets` segment can be safely recovered.
- Use `FileSystem.FixInvalidPathCharacters` for directories and `FileSystem.FixInvalidFileNameCharacters` for names.

**Step 3: Run focused tests**

```powershell
dotnet test Source\AssetRipper.Tests\AssetRipper.Tests.csproj --no-restore --filter ExportPathResolverTests --verbosity minimal
```

Expected: all new resolver tests pass when .NET 10 SDK is available.

**Step 4: Commit**

```powershell
git add Source/AssetRipper.Export.UnityProjects/Paths Source/AssetRipper.Tests/ExportPathResolverTests.cs
git commit -m "feat: resolve export paths from original container paths"
```

### Task 3: Wire Resolver Into Unity Project Export

**Files:**
- Modify: `Source/AssetRipper.Export.UnityProjects/AssetExportCollection.cs`
- Modify: `Source/AssetRipper.Export.UnityProjects/ProjectExporter.cs` if settings need to be passed differently
- Test: `Source/AssetRipper.Tests/ExportTests.cs`

**Step 1: Add regression tests**

Add tests in `ExportTests`:

```csharp
[Test]
public void DefaultExportIgnoresOriginalPath()
{
	ProcessedAssetCollection collection = AssetCreator.CreateCollection(UnityVersion.V_2022);
	IMonoBehaviour monoBehaviour = collection.CreateMonoBehaviour();
	monoBehaviour.Name = "Name";
	monoBehaviour.OriginalPath = "Assets/SourceFolder/OriginalName.asset";

	VirtualFileSystem fileSystem = Export(collection);

	Assert.That(fileSystem.File.Exists("/output/ExportedProject/Assets/MonoBehaviour/Name.asset"));
}

[Test]
public void PreserveContainerPathUsesOriginalPath()
{
	ProcessedAssetCollection collection = AssetCreator.CreateCollection(UnityVersion.V_2022);
	IMonoBehaviour monoBehaviour = collection.CreateMonoBehaviour();
	monoBehaviour.Name = "RuntimeName";
	monoBehaviour.OriginalPath = "Assets/SourceFolder/OriginalName.asset";

	FullConfiguration configuration = new();
	configuration.ExportSettings.AssetPathExportMode = AssetPathExportMode.PreserveContainerPath;
	VirtualFileSystem fileSystem = Export(collection, configuration: configuration);

	Assert.That(fileSystem.File.Exists("/output/ExportedProject/Assets/SourceFolder/OriginalName.asset"));
}
```

Adjust the local `Export` helper to accept an optional `FullConfiguration`.

**Step 2: Apply resolver**

In `AssetExportCollection.Export`, replace direct `Asset.GetBestDirectory()` and `GetUniqueFileName(Asset, subPath, fileSystem)` calculation with:

```csharp
ExportPathInfo exportPath = ExportPathResolver.Resolve(Asset, container.AssetPathExportMode);
string subPath = fileSystem.Path.Join(projectDirectory, exportPath.Directory);
string fileName = GetUniqueFileName(subPath, $"{exportPath.Name}.{GetExportExtension(Asset)}", fileSystem);
```

Expose the selected mode through `IExportContainer.AssetPathExportMode`; `ProjectAssetContainer` can populate it from `FullConfiguration.ExportSettings`.

**Step 3: Run focused tests**

```powershell
dotnet test Source\AssetRipper.Tests\AssetRipper.Tests.csproj --no-restore --filter "ExportTests|ExportPathResolverTests" --verbosity minimal
```

Expected: new tests and existing export tests pass when .NET 10 SDK is available.

**Step 4: Commit**

```powershell
git add Source/AssetRipper.Export.UnityProjects Source/AssetRipper.Tests/ExportTests.cs
git commit -m "feat: preserve container paths during project export"
```

### Task 4: Final Verification

**Files:**
- No code edits unless failures reveal a defect.

**Step 1: Diff check**

```powershell
git diff --check
```

Expected: no output and exit code 0.

**Step 2: Full test attempt**

```powershell
dotnet test .\AssetRipper.slnx --no-restore --verbosity minimal
```

Expected: pass when .NET 10 SDK is installed. If local SDK is still .NET 9, record `NETSDK1045` as environment limitation.

**Step 3: Inspect final status**

```powershell
git status --short --branch
```

Expected: clean working tree after final commit.

## 引用说明

- `docs/plans/2026-05-25-preserve-source-export-paths-design.md`
- `Source/AssetRipper.Processing/Scenes/OriginalPathProcessor.cs`
- `Source/AssetRipper.Export.UnityProjects/AssetExportCollection.cs`
- `Source/AssetRipper.Tests/ExportTests.cs`
- `D:\CrackALL\assetstudio\AssetStudio.CLI\Studio.cs`
