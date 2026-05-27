using AssetRipper.Assets;
using AssetRipper.Export.Configuration;

namespace AssetRipper.Export.UnityProjects.Paths;

/*
 ExportPathResolver 的路径解析流程

 导出路径解析流程是这样的：

 Resolve()
     ├─> 优先使用显式覆盖路径
     ├─> 尝试使用资源原始路径
     ├─> 尝试使用源文件路径
     └─> 使用兜底目录和名称
*/
public static class ExportPathResolver
{
	private const string AssetsDirectory = "Assets";
	private const char Separator = '/';

	public static ExportPathInfo Resolve(IUnityObjectBase asset, AssetPathExportMode mode, IUnityObjectBase? sourceFilePathAsset = null)
	{
		if (HasOverridePath(asset))
		{
			return ResolveFallback(asset, true);
		}

		if (mode is AssetPathExportMode.PreserveContainerPath
			&& !string.IsNullOrEmpty(asset.OriginalPath)
			&& TryResolveOriginalPath(asset.OriginalPath, out ExportPathInfo originalPath))
		{
			return originalPath;
		}

		if (mode is AssetPathExportMode.PreserveContainerPath
			&& TryResolveSourceFilePath(asset, sourceFilePathAsset ?? asset, out ExportPathInfo sourceFilePath))
		{
			return sourceFilePath;
		}

		bool ignoreOriginalDirectory = mode is AssetPathExportMode.PreserveContainerPath && !string.IsNullOrEmpty(asset.OriginalPath);
		return ResolveFallback(asset, ignoreOriginalDirectory);
	}

	private static bool HasOverridePath(IUnityObjectBase asset)
	{
		return asset.OverrideDirectory is not null || asset.OverrideName is not null;
	}

	private static bool TryResolveOriginalPath(string originalPath, out ExportPathInfo exportPath)
	{
		exportPath = default;

		string normalizedPath = originalPath.Replace('\\', Separator);
		if (Path.IsPathRooted(normalizedPath))
		{
			normalizedPath = TryRecoverAssetsPath(normalizedPath);
			if (string.IsNullOrEmpty(normalizedPath))
			{
				return false;
			}
		}

		string[] segments = normalizedPath.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (segments.Length == 0 || segments.Any(IsUnsafeSegment))
		{
			return false;
		}

		string fileName = segments[^1];
		string name = Path.GetFileNameWithoutExtension(fileName);
		if (string.IsNullOrWhiteSpace(name))
		{
			return false;
		}

		string directory = segments.Length == 1
			? AssetsDirectory
			: string.Join(Separator, segments.AsSpan(0, segments.Length - 1));

		exportPath = new(
			NormalizeDirectory(directory),
			NormalizeName(name));
		return true;
	}

	private static string TryRecoverAssetsPath(string path)
	{
		string[] segments = path.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		for (int i = 0; i < segments.Length; i++)
		{
			if (string.Equals(segments[i], AssetsDirectory, StringComparison.OrdinalIgnoreCase))
			{
				segments[i] = AssetsDirectory;
				return string.Join(Separator, segments.AsSpan(i));
			}
		}
		return string.Empty;
	}

	private static bool TryResolveSourceFilePath(IUnityObjectBase asset, IUnityObjectBase sourceFilePathAsset, out ExportPathInfo exportPath)
	{
		exportPath = default;
		if (!TryGetSafeSourcePathSegments(sourceFilePathAsset.Collection.FilePath, out string[] sourceSegments))
		{
			return false;
		}

		List<string> directorySegments = [AssetsDirectory];
		directorySegments.AddRange(sourceSegments);
		AddCollectionNameIfNeeded(sourceFilePathAsset, directorySegments);
		AddFallbackDirectorySegments(asset, directorySegments);

		exportPath = new(
			NormalizeDirectory(string.Join(Separator, directorySegments)),
			NormalizeName(GetFallbackName(asset)));
		return true;
	}

	private static void AddFallbackDirectorySegments(IUnityObjectBase asset, List<string> directorySegments)
	{
		string fallbackDirectory = NormalizeDirectory(asset.GetBestDirectory());
		string[] fallbackSegments = fallbackDirectory.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		int startIndex = fallbackSegments.Length > 0 && string.Equals(fallbackSegments[0], AssetsDirectory, StringComparison.OrdinalIgnoreCase)
			? 1
			: 0;

		if (startIndex >= fallbackSegments.Length || HasUnsafeSegment(fallbackSegments.AsSpan(startIndex)))
		{
			directorySegments.Add(asset.ClassName);
			return;
		}

		directorySegments.AddRange(fallbackSegments.AsSpan(startIndex).ToArray());
	}

	private static bool HasUnsafeSegment(ReadOnlySpan<string> segments)
	{
		foreach (string segment in segments)
		{
			if (IsUnsafeSegment(segment))
			{
				return true;
			}
		}

		return false;
	}

	private static bool TryGetSafeSourcePathSegments(string sourceFilePath, out string[] segments)
	{
		segments = [];
		if (string.IsNullOrWhiteSpace(sourceFilePath))
		{
			return false;
		}

		string normalizedPath = sourceFilePath.Replace('\\', Separator);
		string[] allSegments = normalizedPath.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (allSegments.Length == 0 || allSegments.Any(IsUnsafeSegment))
		{
			return false;
		}

		int startIndex = Path.IsPathRooted(normalizedPath) ? GetSourcePathStartIndex(allSegments) : 0;
		segments = allSegments.AsSpan(startIndex).ToArray();
		return segments.Length > 0;
	}

	private static int GetSourcePathStartIndex(string[] segments)
	{
		for (int i = 0; i < segments.Length; i++)
		{
			if (segments[i].EndsWith("_Data", StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}

		return segments.Length - 1;
	}

	private static void AddCollectionNameIfNeeded(IUnityObjectBase asset, List<string> directorySegments)
	{
		string collectionName = asset.Collection.Name;
		if (string.IsNullOrWhiteSpace(collectionName))
		{
			return;
		}

		string normalizedCollectionName = collectionName.Replace('\\', Separator);
		string[] collectionSegments = normalizedCollectionName.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (collectionSegments.Length == 0 || collectionSegments.Any(IsUnsafeSegment))
		{
			return;
		}

		string lastSourceSegment = directorySegments[^1];
		if (collectionSegments.Length == 1 && string.Equals(collectionSegments[0], lastSourceSegment, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		directorySegments.AddRange(collectionSegments);
	}

	private static ExportPathInfo ResolveFallback(IUnityObjectBase asset, bool ignoreOriginalDirectory)
	{
		string directory = ignoreOriginalDirectory
			? asset.OverrideDirectory ?? AssetsDirectory
			: asset.GetBestDirectory();
		string name = GetFallbackName(asset);
		return new(
			NormalizeDirectory(directory),
			NormalizeName(name));
	}

	private static string GetFallbackName(IUnityObjectBase asset)
	{
		string name = asset.GetBestName();
		name = FileSystem.RemoveCloneSuffixes(name);
		name = FileSystem.RemoveInstanceSuffixes(name);
		name = name.Trim();
		if (string.IsNullOrEmpty(name))
		{
			name = asset.ClassName;
		}

		return name;
	}

	private static string NormalizeDirectory(string directory)
	{
		string normalizedDirectory = directory.Replace('\\', Separator);
		normalizedDirectory = FileSystem.FixInvalidPathCharacters(normalizedDirectory);
		return string.IsNullOrWhiteSpace(normalizedDirectory) ? AssetsDirectory : normalizedDirectory;
	}

	private static string NormalizeName(string name)
	{
		string normalizedName = FileSystem.FixInvalidFileNameCharacters(name.Trim());
		return string.IsNullOrEmpty(normalizedName) ? "asset" : normalizedName;
	}

	private static bool IsUnsafeSegment(string segment)
	{
		return segment is "." or ".." || string.IsNullOrWhiteSpace(segment);
	}
}
