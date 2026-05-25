using AssetRipper.Assets.Collections;
using AssetRipper.Export.Configuration;
using AssetRipper.Export.UnityProjects.Paths;
using AssetRipper.Primitives;
using AssetRipper.SourceGenerated.Classes.ClassID_114;

namespace AssetRipper.Tests;

internal sealed class ExportPathResolverTests
{
	[Test]
	public void DefaultModeUsesBestDirectoryAndName()
	{
		ProcessedAssetCollection collection = AssetCreator.CreateCollection(UnityVersion.V_2022);
		IMonoBehaviour asset = collection.CreateMonoBehaviour();
		asset.Name = "RuntimeName";
		asset.OriginalPath = "Assets/SourceFolder/OriginalName.asset";

		ExportPathInfo path = ExportPathResolver.Resolve(asset, AssetPathExportMode.Default);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(path.Directory, Is.EqualTo("Assets/SourceFolder"));
			Assert.That(path.Name, Is.EqualTo("RuntimeName"));
		}
	}

	[Test]
	public void PreserveContainerPathUsesOriginalDirectoryAndFileName()
	{
		ProcessedAssetCollection collection = AssetCreator.CreateCollection(UnityVersion.V_2022);
		IMonoBehaviour asset = collection.CreateMonoBehaviour();
		asset.Name = "RuntimeName";
		asset.OriginalPath = "Assets/Textures/Hero.png";

		ExportPathInfo path = ExportPathResolver.Resolve(asset, AssetPathExportMode.PreserveContainerPath);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(path.Directory, Is.EqualTo("Assets/Textures"));
			Assert.That(path.Name, Is.EqualTo("Hero"));
		}
	}

	[Test]
	public void PreserveContainerPathRejectsParentDirectoryTraversal()
	{
		ProcessedAssetCollection collection = AssetCreator.CreateCollection(UnityVersion.V_2022);
		IMonoBehaviour asset = collection.CreateMonoBehaviour();
		asset.Name = "RuntimeName";
		asset.OriginalPath = "../escape/Hero.png";

		ExportPathInfo path = ExportPathResolver.Resolve(asset, AssetPathExportMode.PreserveContainerPath);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(path.Directory, Is.EqualTo("Assets"));
			Assert.That(path.Name, Is.EqualTo("RuntimeName"));
		}
	}

	[Test]
	public void PreserveContainerPathRecoversAssetsSegmentFromRootedPath()
	{
		ProcessedAssetCollection collection = AssetCreator.CreateCollection(UnityVersion.V_2022);
		IMonoBehaviour asset = collection.CreateMonoBehaviour();
		asset.Name = "RuntimeName";
		asset.OriginalPath = "C:/Project/Assets/Characters/Hero.prefab";

		ExportPathInfo path = ExportPathResolver.Resolve(asset, AssetPathExportMode.PreserveContainerPath);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(path.Directory, Is.EqualTo("Assets/Characters"));
			Assert.That(path.Name, Is.EqualTo("Hero"));
		}
	}
}
