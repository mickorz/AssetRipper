using AssetRipper.Assets;
using AssetRipper.SourceGenerated.Classes.ClassID_28;
using AssetRipper.SourceGenerated.Classes.ClassID_49;

namespace AssetRipper.Processing.Spine;

/*
 SpineResourceProcessor 的处理流程

 Spine 资源整理流程是这样的：

 Process()
     ├─> 收集候选资源
     │     ├─> 识别贴图资源
     │     └─> 识别文本资源
     ├─> 按基础名分组
     └─> 应用导出路径
           ├─> 判断是否包含贴图
           ├─> 判断是否包含描述文件
           └─> 设置到 Assets Spine 目录
*/
public sealed class SpineResourceProcessor : IAssetProcessor
{
	private const string SpineDirectory = "Assets/Spine";
	private const string AtlasSuffix = ".atlas";
	private const string SkelSuffix = ".skel";

	public void Process(GameData gameData)
	{
		Dictionary<string, SpineResourceGroup> groups = new(StringComparer.OrdinalIgnoreCase);
		foreach (IUnityObjectBase asset in gameData.GameBundle.FetchAssets())
		{
			if (TryCreateCandidate(asset, out SpineResourceCandidate candidate))
			{
				if (!groups.TryGetValue(candidate.BaseName, out SpineResourceGroup? group))
				{
					group = new SpineResourceGroup();
					groups.Add(candidate.BaseName, group);
				}
				group.Add(candidate);
			}
		}

		foreach (SpineResourceGroup group in groups.Values)
		{
			if (group.IsSpineResourceSet)
			{
				group.ApplyExportPath();
			}
		}
	}

	private static bool TryCreateCandidate(IUnityObjectBase asset, out SpineResourceCandidate candidate)
	{
		return asset switch
		{
			ITexture2D texture => TryCreateTextureCandidate(texture, out candidate),
			ITextAsset textAsset => TryCreateTextCandidate(textAsset, out candidate),
			_ => Fail(out candidate),
		};
	}

	private static bool TryCreateTextureCandidate(ITexture2D texture, out SpineResourceCandidate candidate)
	{
		string name = RemoveKnownExtension(texture.GetBestName(), "png");
		if (string.IsNullOrWhiteSpace(name))
		{
			return Fail(out candidate);
		}

		candidate = new(texture, name, name, null, SpineResourceKind.Texture);
		return true;
	}

	private static bool TryCreateTextCandidate(ITextAsset textAsset, out SpineResourceCandidate candidate)
	{
		string name = textAsset.GetBestName();
		string? extension = NormalizeExtension(textAsset.GetBestExtension());
		if (extension is null && TrySplitKnownTextExtension(name, out string splitName, out string splitExtension))
		{
			name = splitName;
			extension = splitExtension;
		}

		switch (extension)
		{
			case "json":
			{
				string baseName = RemoveKnownExtension(name, extension);
				candidate = new(textAsset, baseName, baseName, extension, SpineResourceKind.Description);
				return !string.IsNullOrWhiteSpace(baseName);
			}
			case "bytes" when RemoveKnownExtension(name, extension).EndsWith(SkelSuffix, StringComparison.OrdinalIgnoreCase):
			{
				string nameWithoutExtension = RemoveKnownExtension(name, extension);
				string baseName = nameWithoutExtension[..^SkelSuffix.Length];
				candidate = new(textAsset, baseName, $"{baseName}{SkelSuffix}", extension, SpineResourceKind.Description);
				return !string.IsNullOrWhiteSpace(baseName);
			}
			case "txt" when RemoveKnownExtension(name, extension).EndsWith(AtlasSuffix, StringComparison.OrdinalIgnoreCase):
			{
				string nameWithoutExtension = RemoveKnownExtension(name, extension);
				string baseName = nameWithoutExtension[..^AtlasSuffix.Length];
				candidate = new(textAsset, baseName, $"{baseName}{AtlasSuffix}", extension, SpineResourceKind.Description);
				return !string.IsNullOrWhiteSpace(baseName);
			}
			default:
				return Fail(out candidate);
		}
	}

	private static bool TrySplitKnownTextExtension(string name, out string baseName, out string extension)
	{
		int index = name.LastIndexOf('.');
		if (index > 0 && index < name.Length - 1)
		{
			string possibleExtension = name[(index + 1)..];
			string normalizedExtension = NormalizeExtension(possibleExtension) ?? string.Empty;
			if (normalizedExtension is "json" or "bytes" or "txt")
			{
				baseName = name[..index];
				extension = normalizedExtension;
				return true;
			}
		}

		baseName = string.Empty;
		extension = string.Empty;
		return false;
	}

	private static string RemoveKnownExtension(string name, string extension)
	{
		string suffix = $".{extension}";
		return name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
			? name[..^suffix.Length]
			: name;
	}

	private static string? NormalizeExtension(string? extension)
	{
		if (string.IsNullOrWhiteSpace(extension))
		{
			return null;
		}

		return extension.Trim().TrimStart('.').ToLowerInvariant();
	}

	private static bool Fail(out SpineResourceCandidate candidate)
	{
		candidate = default;
		return false;
	}

	private readonly record struct SpineResourceCandidate(
		IUnityObjectBase Asset,
		string BaseName,
		string ExportName,
		string? ExportExtension,
		SpineResourceKind Kind);

	private enum SpineResourceKind
	{
		Texture,
		Description,
	}

	/*
	 SpineResourceGroup 的分组流程

	 单个 Spine 分组流程是这样的：

	 Add()
	     ├─> 记录候选资源
	     ├─> 标记是否包含贴图
	     └─> 标记是否包含描述文件
	 ApplyExportPath()
	     └─> 把同组资源写入 Assets Spine 目录
	*/
	private sealed class SpineResourceGroup
	{
		private readonly List<SpineResourceCandidate> candidates = [];

		public bool IsSpineResourceSet => hasTexture && hasDescription;

		public void Add(SpineResourceCandidate candidate)
		{
			candidates.Add(candidate);
			if (candidate.Kind is SpineResourceKind.Texture)
			{
				hasTexture = true;
			}
			else if (candidate.Kind is SpineResourceKind.Description)
			{
				hasDescription = true;
			}
		}

		public void ApplyExportPath()
		{
			foreach (SpineResourceCandidate candidate in candidates)
			{
				candidate.Asset.OverrideDirectory = SpineDirectory;
				candidate.Asset.OverrideName = candidate.ExportName;
				candidate.Asset.OverrideExtension = candidate.ExportExtension;
			}
		}

		private bool hasTexture;
		private bool hasDescription;
	}
}
