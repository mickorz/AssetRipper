using AssetRipper.Assets;
using AssetRipper.Assets.Bundles;
using AssetRipper.Assets.Collections;
using AssetRipper.Export.Configuration;
using AssetRipper.Export.UnityProjects.Paths;
using AssetRipper.Import.Structure.Assembly.Managers;
using AssetRipper.Processing;
using AssetRipper.Processing.Spine;
using AssetRipper.Primitives;
using AssetRipper.SourceGenerated.Classes.ClassID_28;
using AssetRipper.SourceGenerated.Classes.ClassID_49;
using AssetRipper.SourceGenerated.Extensions;

namespace AssetRipper.Tests;

/*
 SpineResourceProcessorTests 的验证流程

 Spine 资源测试流程是这样的：

 测试方法
     ├─> 创建同名贴图和描述资源
     ├─> 执行 Spine 资源处理器
     ├─> 验证资源覆盖路径
     └─> 验证导出路径解析结果
*/
internal class SpineResourceProcessorTests
{
	[Test]
	public void SpineResourcesAreExportedDirectlyUnderSpineDirectory()
	{
		ProcessedAssetCollection collection = AssetCreator.CreateCollection(UnityVersion.V_2022);
		ITexture2D texture = CreateTexture(collection, "小红");
		ITextAsset json = CreateTextAsset(collection, "小红", "json");
		ITextAsset skel = CreateTextAsset(collection, "小红.skel", "bytes");
		ITextAsset atlas = CreateTextAsset(collection, "小红.atlas", "txt");
		ITextAsset unrelated = CreateTextAsset(collection, "普通文本", "txt");

		new SpineResourceProcessor().Process(CreateGameData(collection));

		using (Assert.EnterMultipleScope())
		{
			AssertSpinePath(texture, "小红", null);
			AssertSpinePath(json, "小红", "json");
			AssertSpinePath(skel, "小红.skel", "bytes");
			AssertSpinePath(atlas, "小红.atlas", "txt");
			Assert.That(unrelated.OverrideDirectory, Is.Null);
		}
	}

	[Test]
	public void SpineResourcesCanBeDetectedFromFullFileNames()
	{
		ProcessedAssetCollection collection = AssetCreator.CreateCollection(UnityVersion.V_2022);
		ITexture2D texture = CreateTexture(collection, "小红.png");
		ITextAsset json = CreateTextAsset(collection, "小红.json", null);
		ITextAsset skel = CreateTextAsset(collection, "小红.skel.bytes", null);
		ITextAsset atlas = CreateTextAsset(collection, "小红.atlas.TXT", null);

		new SpineResourceProcessor().Process(CreateGameData(collection));

		using (Assert.EnterMultipleScope())
		{
			AssertSpinePath(texture, "小红", null);
			AssertSpinePath(json, "小红", "json");
			AssertSpinePath(skel, "小红.skel", "bytes");
			AssertSpinePath(atlas, "小红.atlas", "txt");
		}
	}

	[Test]
	public void SpineResourcesOverridePreservedContainerPath()
	{
		ProcessedAssetCollection collection = AssetCreator.CreateCollection(UnityVersion.V_2022);
		collection.FilePath = "D:/Game/Cinderia_Data/sharedassets1.assets";
		ITexture2D texture = CreateTexture(collection, "小红");
		texture.OriginalPath = "Assets/SourceFolder/小红.png";
		ITextAsset json = CreateTextAsset(collection, "小红", "json");
		json.OriginalPath = "Assets/SourceFolder/小红.json";

		new SpineResourceProcessor().Process(CreateGameData(collection));

		ExportPathInfo texturePath = ExportPathResolver.Resolve(texture, AssetPathExportMode.PreserveContainerPath);
		ExportPathInfo jsonPath = ExportPathResolver.Resolve(json, AssetPathExportMode.PreserveContainerPath);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(texturePath.Directory, Is.EqualTo("Assets/Spine"));
			Assert.That(texturePath.Name, Is.EqualTo("小红"));
			Assert.That(jsonPath.Directory, Is.EqualTo("Assets/Spine"));
			Assert.That(jsonPath.Name, Is.EqualTo("小红"));
		}
	}

	private static ITexture2D CreateTexture(ProcessedAssetCollection collection, string name)
	{
		ITexture2D texture = collection.CreateTexture2D();
		texture.Name = name;
		return texture;
	}

	private static ITextAsset CreateTextAsset(ProcessedAssetCollection collection, string name, string? extension)
	{
		ITextAsset textAsset = collection.CreateTextAsset();
		textAsset.Name = name;
		textAsset.OriginalExtension = extension;
		textAsset.Script_C49 = "{}";
		return textAsset;
	}

	private static void AssertSpinePath(IUnityObjectBase asset, string name, string? extension)
	{
		Assert.That(asset.OverrideDirectory, Is.EqualTo("Assets/Spine"));
		Assert.That(asset.OverrideName, Is.EqualTo(name));
		Assert.That(asset.OverrideExtension, Is.EqualTo(extension));
	}

	private static GameData CreateGameData(ProcessedAssetCollection collection)
	{
		return new((GameBundle)collection.Bundle, collection.Version, new BaseManager(static _ => { }), null);
	}
}
