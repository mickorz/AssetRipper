using AssetRipper.Assets;
using AssetRipper.Export.Configuration;

namespace AssetRipper.Export.UnityProjects.Paths;

public static class ExportPathResolver
{
	private const string AssetsDirectory = "Assets";
	private const char Separator = '/';

	public static ExportPathInfo Resolve(IUnityObjectBase asset, AssetPathExportMode mode)
	{
		if (mode is AssetPathExportMode.PreserveContainerPath
			&& !string.IsNullOrEmpty(asset.OriginalPath)
			&& TryResolveOriginalPath(asset.OriginalPath, out ExportPathInfo originalPath))
		{
			return originalPath;
		}

		bool ignoreOriginalDirectory = mode is AssetPathExportMode.PreserveContainerPath && !string.IsNullOrEmpty(asset.OriginalPath);
		return ResolveFallback(asset, ignoreOriginalDirectory);
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

	private static ExportPathInfo ResolveFallback(IUnityObjectBase asset, bool ignoreOriginalDirectory)
	{
		string directory = ignoreOriginalDirectory
			? asset.OverrideDirectory ?? AssetsDirectory
			: asset.GetBestDirectory();
		string name = asset.GetBestName();
		name = FileSystem.RemoveCloneSuffixes(name);
		name = FileSystem.RemoveInstanceSuffixes(name);
		name = name.Trim();
		if (string.IsNullOrEmpty(name))
		{
			name = asset.ClassName;
		}

		return new(
			NormalizeDirectory(directory),
			NormalizeName(name));
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
