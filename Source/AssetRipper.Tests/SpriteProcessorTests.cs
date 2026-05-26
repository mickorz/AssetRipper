using AssetRipper.Assets.Bundles;
using AssetRipper.Assets.Collections;
using AssetRipper.Import.Structure.Assembly.Managers;
using AssetRipper.Processing;
using AssetRipper.Processing.Textures;
using AssetRipper.Primitives;
using AssetRipper.SourceGenerated.Classes.ClassID_213;
using AssetRipper.SourceGenerated.Classes.ClassID_28;
using AssetRipper.SourceGenerated.Classes.ClassID_687078895;
using AssetRipper.SourceGenerated.Extensions;
using System.Drawing;

namespace AssetRipper.Tests;

/// <summary>
/// SpriteProcessor 回归测试流程
///
/// 创建测试资源
///          |
///          v
/// 设置贴图已有主资源
///          |
///          v
/// 执行精灵处理器
///          |
///          v
/// 验证主资源不被覆盖
/// </summary>
internal sealed class SpriteProcessorTests
{
	[Test]
	public void ProcessSkipsSpriteGroupingWhenTextureAlreadyHasMainAsset()
	{
		ProcessedAssetCollection collection = AssetCreator.CreateCollection(UnityVersion.V_2022);
		ITexture2D texture = collection.CreateTexture2D();
		ISprite sprite = collection.CreateSprite();
		sprite.RD.Texture.SetAsset(collection, texture);
		sprite.Rect.CopyValues(new RectangleF(0, 0, 32, 32));
		sprite.RD.TextureRect.CopyValues(new RectangleF(0, 0, 32, 32));
		texture.MainAsset = texture;

		new SpriteProcessor().Process(CreateGameData(collection));

		using (Assert.EnterMultipleScope())
		{
			Assert.That(texture.MainAsset, Is.SameAs(texture));
			Assert.That(sprite.MainAsset, Is.Null);
		}
	}

	[Test]
	public void ProcessSkipsSpriteGroupingWhenSpriteAlreadyHasMainAsset()
	{
		ProcessedAssetCollection collection = AssetCreator.CreateCollection(UnityVersion.V_2022);
		ITexture2D texture = collection.CreateTexture2D();
		ISprite sprite = collection.CreateSprite();
		sprite.RD.Texture.SetAsset(collection, texture);
		sprite.Rect.CopyValues(new RectangleF(0, 0, 32, 32));
		sprite.RD.TextureRect.CopyValues(new RectangleF(0, 0, 32, 32));
		sprite.MainAsset = sprite;

		new SpriteProcessor().Process(CreateGameData(collection));

		Assert.That(sprite.MainAsset, Is.SameAs(sprite));
	}

	[Test]
	public void ProcessAllowsMultipleSpriteGroupsToShareAtlas()
	{
		ProcessedAssetCollection collection = AssetCreator.CreateCollection(UnityVersion.V_2022);
		ITexture2D texture1 = collection.CreateTexture2D();
		ITexture2D texture2 = collection.CreateTexture2D();
		ISprite sprite1 = CreateSprite(collection, texture1);
		ISprite sprite2 = CreateSprite(collection, texture2);
		ISpriteAtlas atlas = collection.CreateSpriteAtlas();
		sprite1.SpriteAtlasP = atlas;
		sprite2.SpriteAtlasP = atlas;

		Assert.DoesNotThrow(() => new SpriteProcessor().Process(CreateGameData(collection)));
	}

	private static ISprite CreateSprite(ProcessedAssetCollection collection, ITexture2D texture)
	{
		ISprite sprite = collection.CreateSprite();
		sprite.RD.Texture.SetAsset(collection, texture);
		sprite.Rect.CopyValues(new RectangleF(0, 0, 32, 32));
		sprite.RD.TextureRect.CopyValues(new RectangleF(0, 0, 32, 32));
		return sprite;
	}

	private static GameData CreateGameData(ProcessedAssetCollection collection)
	{
		return new((GameBundle)collection.Bundle, collection.Version, new BaseManager((s) => { }), null);
	}
}
