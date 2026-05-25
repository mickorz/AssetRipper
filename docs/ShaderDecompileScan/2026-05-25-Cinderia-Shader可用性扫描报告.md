# Cinderia Shader 可用性扫描报告

## 补充修正

2026-05-25 复查 Unity 编译日志后，本报告中的“可用候选”需要降级理解为“有程序体候选”，不能理解为“Unity 可编译”。后续严格扫描显示，这 71 个文件全部命中常量缓冲变量注释化、Unity 内置变量重复声明等问题，当前没有证据能确认它们可直接编译使用。

详细复盘见：

`D:\CrackALL\AssetRipper\Docs\ShaderDecompileScan\2026-05-25-USCSandbox-Shader编译失败复盘.md`

## 扫描对象

- 目录：`D:\CrackALL\灰烬之国\破解资源1\CinderiaFix\Assets\Shaders`
- 文件范围：当前目录下 243 个 `.shader` 文件
- 明细 CSV：`D:\CrackALL\AssetRipper\Docs\ShaderDecompileScan\cinderia_shader_uscsandbox_usable_scan.csv`

## 扫描结论

USCSandbox 复制到 Unity 项目的 Shader 中，只有 71 个属于静态“可用候选”，另外 172 个属于“空壳不可用”。因此当前反编译结果并不完整，尤其是人物 Spine 相关 Shader 基本没有实际程序体。

| 分类 | 数量 | 判断 |
| --- | ---: | --- |
| 可用候选 | 71 | 有程序块和顶点片元入口 |
| 空壳不可用 | 172 | 有 Pass 但没有 CG 或 HLSL 程序块 |
| 疑似不完整 | 0 | 本轮未发现 |
| 不可用 | 0 | 本轮未发现已知失败标记 |
| 总数 | 243 | 全部扫描文件 |

## 可用候选

这些文件静态结构上更接近可编译 Shader，但还需要 Unity 导入编译验证。

| Shader 名称 | 文件数 | 示例文件 |
| --- | ---: | --- |
| `Shader Graphs/Slash_AB_Slashfx` | 32 | `Shader Graphs_Slash_AB_Slashfx.shader` |
| `Shader Graphs/Slash_Slashfx` | 31 | `Shader Graphs_Slash_Slashfx.shader` |
| `Shader Graphs/Fx_dissolve_particle_apb` | 6 | `Shader Graphs_Fx_dissolve_particle_apb.shader` |
| `Shader Graphs/BlobLight` | 1 | `Shader Graphs_BlobLight.shader` |
| `Shader Graphs/BlobShadow` | 1 | `Shader Graphs_BlobShadow.shader` |

## 空壳不可用

这些文件只保留了 ShaderLab 外壳，缺少实际 CG 或 HLSL 程序块。它们不能按完整反编译结果使用。

| Shader 名称 | 文件数 | 示例文件 |
| --- | ---: | --- |
| `_URP/人物Spine` | 128 | `_URP_人物Spine.shader` |
| `_URP/扭曲` | 12 | `_URP_扭曲.shader` |
| `_URP/特殊效果_黑白` | 10 | `_URP_特殊效果_黑白.shader` |
| `_URP/环境/默认` | 9 | `_URP_环境_默认.shader` |
| `_URP/特殊效果_扭曲` | 3 | `_URP_特殊效果_扭曲.shader` |
| `_URP/环境/触手` | 2 | `_URP_环境_触手.shader` |
| `_URP/UI/屏幕效果` | 1 | `_URP_UI_屏幕效果.shader` |
| `_URP/环境/氛围光` | 1 | `_URP_环境_氛围光.shader` |
| `_URP/环境/水` | 1 | `_URP_环境_水.shader` |
| `_URP/人物Spine_无阴影` | 1 | `_URP_人物Spine_无阴影.shader` |
| `_URP/整理过的/地面` | 1 | `_URP_整理过的_地面.shader` |
| `_URP/整理过的/有高度地面` | 1 | `_URP_整理过的_有高度地面.shader` |
| `_URP/整理过的/有高度地面深度` | 1 | `_URP_整理过的_有高度地面深度.shader` |
| `GroundDepth` | 1 | `GroundDepth.shader` |

## 判断说明

本轮是静态扫描，不等于 Unity 编译测试。判断“可用候选”的最低证据是文件中存在程序块、顶点入口、片元入口和函数形态代码。判断“空壳不可用”的关键证据是文件存在 Pass，但没有 `CGPROGRAM` 或 `HLSLPROGRAM`。

更严格的下一步应该是在 Unity Editor 内执行导入编译验证，并收集 Console 编译错误。只有通过 Unity 编译的 Shader 才能算“项目内可用”。

## 引用说明

- Unity Manual: Writing HLSL shader programs
  https://docs.unity3d.com/Manual/SL-ShaderPrograms.html
- Unity Manual: Pass block in ShaderLab reference
  https://docs.unity3d.com/Manual/SL-Pass.html
- Unity Manual: HLSL pragma directives reference
  https://docs.unity3d.com/Manual/SL-PragmaDirectives.html
- Unity Manual: Fallback block in ShaderLab reference
  https://docs.unity3d.com/Manual/SL-Fallback.html
