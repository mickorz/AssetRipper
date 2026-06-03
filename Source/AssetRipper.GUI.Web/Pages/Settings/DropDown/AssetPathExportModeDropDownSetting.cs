using AssetRipper.Export.Configuration;

namespace AssetRipper.GUI.Web.Pages.Settings.DropDown;

public sealed class AssetPathExportModeDropDownSetting : DropDownSetting<AssetPathExportMode>
{
	public static AssetPathExportModeDropDownSetting Instance { get; } = new();

	public override string Title => Localization.AssetPathExportTitle;

	protected override string GetDisplayName(AssetPathExportMode value) => value switch
	{
		AssetPathExportMode.Default => Localization.AssetPathExportDefault,
		AssetPathExportMode.PreserveContainerPath => Localization.AssetPathExportPreserveContainerPath,
		AssetPathExportMode.PreserveAddressablePath => Localization.AssetPathExportPreserveAddressablePath,
		_ => base.GetDisplayName(value),
	};

	protected override string? GetDescription(AssetPathExportMode value) => value switch
	{
		AssetPathExportMode.Default => Localization.AssetPathExportDefaultDescription,
		AssetPathExportMode.PreserveContainerPath => Localization.AssetPathExportPreserveContainerPathDescription,
		AssetPathExportMode.PreserveAddressablePath => Localization.AssetPathExportPreserveAddressablePathDescription,
		_ => base.GetDescription(value),
	};
}
