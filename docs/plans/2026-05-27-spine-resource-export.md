# Spine Resource Export Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Detect Spine resource sets during processing and export their files directly under `Assets/Spine`.

**Architecture:** Add a processing-stage classifier that scans all assets, groups likely Spine resources by base name, and sets export override directory/name/extension for matching assets. Keep existing exporters unchanged so normal path resolution and file writing continue to work.

**Tech Stack:** C#, AssetRipper processing pipeline, AssetRipper Unity project exporter, NUnit.

---

## Scope Check

涉及 3 个模块：

- Processing 阶段资源识别
- ExportHandler 处理器注册
- NUnit 导出测试

范围适合单份计划。

## Task 1: 写失败测试

**Files:**

- Modify: `Source/AssetRipper.Tests/ExportTests.cs`

**Steps:**

1. 创建包含 `小红` Spine 资源的测试集合。
2. 添加贴图资源和 TextAsset 资源。
3. 执行完整处理和导出。
4. 断言 `小红.png`、`小红.json`、`小红.skel.bytes`、`小红.atlas.txt` 位于 `/output/ExportedProject/Assets/Spine`。
5. 断言非 Spine TextAsset 不被移动到 Spine 目录。

运行：

```powershell
& 'D:\CrackALL\dotnet-sdk-10\dotnet.exe' test Source\AssetRipper.Tests\AssetRipper.Tests.csproj --no-restore --filter "SpineResources" --verbosity minimal
```

预期：测试失败，因为当前没有 Spine 特殊路径处理。

## Task 2: 新增 SpineResourceProcessor

**Files:**

- Create: `Source/AssetRipper.Processing/Spine/SpineResourceProcessor.cs`

**Steps:**

1. 扫描 `gameData.GameBundle.FetchAssets()`。
2. 对 Texture2D 和 TextAsset 提取 Spine 候选信息。
3. 按基础名分组。
4. 如果一组同时包含贴图和 Spine 描述文件，则设置覆盖导出路径。

## Task 3: 注册处理器

**Files:**

- Modify: `Source/AssetRipper.Export.UnityProjects/ExportHandler.cs`

**Steps:**

1. 在 `GetProcessors()` 中注册 `SpineResourceProcessor`。
2. 放在 `OriginalPathProcessor` 之后，让它可以覆盖已有路径。
3. 放在导出前其他资源处理之前，避免后续逻辑拿到旧路径。

## Task 4: 验证

运行：

```powershell
& 'D:\CrackALL\dotnet-sdk-10\dotnet.exe' test Source\AssetRipper.Tests\AssetRipper.Tests.csproj --no-restore --filter "SpineResources" --verbosity minimal
& 'D:\CrackALL\dotnet-sdk-10\dotnet.exe' build .\Source\AssetRipper.GUI.Free\AssetRipper.GUI.Free.csproj --no-restore -c Debug --verbosity minimal
git diff --check
```

预期：

- Spine 资源导出测试通过。
- GUI 构建通过。
- 没有空白错误。

## 引用说明

- 本地代码：`Source/AssetRipper.Processing/Scenes/OriginalPathProcessor.cs`
- 本地代码：`Source/AssetRipper.Export.UnityProjects/ExportHandler.cs`
- 本地代码：`Source/AssetRipper.Tests/ExportTests.cs`

