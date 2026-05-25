using AssetRipper.Assets;
using AssetRipper.Assets.Collections;
using AssetRipper.Export.Configuration;

namespace AssetRipper.Export.UnityProjects;

public interface IExportContainer
{
	long GetExportID(IUnityObjectBase asset);
	AssetType ToExportType(Type type);
	MetaPtr CreateExportPointer(IUnityObjectBase asset);

	UnityGuid ScenePathToGUID(string name);
	bool IsSceneDuplicate(int sceneID);

	AssetCollection File { get; }

	UnityVersion ExportVersion { get; }

	AssetPathExportMode AssetPathExportMode { get; }
}
