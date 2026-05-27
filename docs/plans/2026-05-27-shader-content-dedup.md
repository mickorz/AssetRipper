# Shader Content Dedup Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Export only one copy of shaders whose generated shader text is exactly identical, while keeping same-name shaders with different content as separate files.

**Architecture:** Dedup happens after export collections are created, before the project asset container is built. The first shader collection with a given exported content hash remains exportable, and later identical shader assets become redirect collections pointing to the first shader GUID and file ID. Different content hashes are not merged even when the shader display name is the same.

**Tech Stack:** C#, AssetRipper Unity project export pipeline, NUnit, SHA256 content hashing.

---

## Scope Check

涉及 3 个模块：

- Shader 导出集合
- ProjectExporter 集合创建流程
- NUnit 导出测试

范围适合单份计划。

## 类比理解

现在的导出像复印资料时只看文件名冲突，遇到同名就自动改成 `_0`、`_1`。新逻辑像先看资料正文：正文完全一样就只留第一份，后面的索引都指向第一份；正文不同就继续保留多份，哪怕封面标题一样。

## Task 1: 写失败测试

**Files:**

- Modify: `Source/AssetRipper.Tests/ExportTests.cs`

**Steps:**

1. 新增一个测试，创建两个导出内容完全一致的 Shader。
2. 导出 Unity 工程。
3. 断言 `Assets/Shader` 或对应 Shader 目录下只生成一个 `.shader`。
4. 新增一个测试，创建两个同名但内容不同的 Shader。
5. 导出 Unity 工程。
6. 断言两个 `.shader` 都存在。

运行：

```powershell
& 'D:\CrackALL\dotnet-sdk-10\dotnet.exe' test Source\AssetRipper.Tests\AssetRipper.Tests.csproj --no-restore --filter "ShaderContentDedup" --verbosity minimal
```

预期：第一个测试失败，因为当前实现会通过 `GetUniqueName` 导出多份相同 Shader。

## Task 2: 实现 Shader 内容签名

**Files:**

- Modify: `Source/AssetRipper.Export.UnityProjects/Shaders/ShaderExportCollection.cs`

**Steps:**

1. 给 Shader 导出集合增加计算导出内容哈希的能力。
2. 哈希基于最终要写入 `.shader` 的文本或字节内容。
3. 如果无法计算内容，返回空值并跳过去重。

## Task 3: 实现重复 Shader 重定向

**Files:**

- Modify: `Source/AssetRipper.Export.UnityProjects/ProjectExporter.cs`

**Steps:**

1. 在 `CreateCollections` 中创建 Shader 集合后检查内容哈希。
2. 第一份内容哈希保留原集合。
3. 后续相同哈希替换成 `SingleRedirectExportCollection`，指向第一份集合的 GUID、main export ID 和 `AssetType.Meta`。
4. 内容不同则保持原集合，继续走现有唯一文件名逻辑。

## Task 4: 验证

运行：

```powershell
& 'D:\CrackALL\dotnet-sdk-10\dotnet.exe' test Source\AssetRipper.Tests\AssetRipper.Tests.csproj --no-restore --filter "ShaderContentDedup" --verbosity minimal
& 'D:\CrackALL\dotnet-sdk-10\dotnet.exe' build .\Source\AssetRipper.GUI.Free\AssetRipper.GUI.Free.csproj --no-restore -c Debug --verbosity minimal
git diff --check
```

预期：

- Shader 内容去重测试通过
- GUI 构建通过
- 没有空白错误

## 引用说明

- 本地代码：`Source/AssetRipper.Export.UnityProjects/ProjectExporter.cs`
- 本地代码：`Source/AssetRipper.Export.UnityProjects/Shaders/ShaderExportCollection.cs`
- 本地代码：`Source/AssetRipper.Export.UnityProjects/Shaders/DummyShaderTextExporter.cs`
- 本地代码：`Source/AssetRipper.Export.UnityProjects/SingleRedirectExportCollection.cs`
