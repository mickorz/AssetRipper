using AssetRipper.Assets;
using AssetRipper.Assets.Cloning;
using AssetRipper.Assets.Generics;
using AssetRipper.SourceGenerated.Classes.ClassID_1007;
using AssetRipper.SourceGenerated.Classes.ClassID_48;
using AssetRipper.SourceGenerated.Subclasses.PPtr_Texture;
using System.Security.Cryptography;

namespace AssetRipper.Export.UnityProjects.Shaders;

public class ShaderExportCollection : AssetExportCollection<IShader>
{
	public ShaderExportCollection(ShaderExporterBase assetExporter, IShader asset) : base(assetExporter, asset)
	{
	}

	internal string? GetExportContentHash()
	{
		if (exportContentHashInitialized)
		{
			return exportContentHash;
		}

		exportContentHashInitialized = true;
		using MemoryStream stream = new();
		if (AssetExporter is ShaderExporterBase shaderExporter
			&& shaderExporter.TryExportForContentHash(Asset, stream))
		{
			exportContentHash = Convert.ToHexString(SHA256.HashData(stream.ToArray()));
		}
		return exportContentHash;
	}

	protected override IUnityObjectBase CreateImporter(IExportContainer container)
	{
		IShaderImporter importer = ShaderImporter.Create(container.File, container.ExportVersion);
		if (importer.Has_NonModifiableTextures() && Asset.Has_NonModifiableTextures())
		{
			PPtrConverter converter = new(Asset, importer);
			foreach ((Utf8String name, PPtr_Texture_5 pptr) in Asset.NonModifiableTextures)
			{
				AssetPair<Utf8String, PPtr_Texture_5> pair = importer.NonModifiableTextures.AddNew();
				pair.Key = name;
				pair.Value.CopyValues(pptr, converter);
			}
		}
		if (importer.Has_AssetBundleName_R() && Asset.AssetBundleName is not null)
		{
			importer.AssetBundleName_R = Asset.AssetBundleName;
		}
		return importer;
	}

	private bool exportContentHashInitialized;
	private string? exportContentHash;
}
