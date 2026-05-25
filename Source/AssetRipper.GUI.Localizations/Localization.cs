namespace AssetRipper.GUI.Localizations;

// Localization 默认语言初始化流程
//
// Localization 的默认语言初始化流程是这样的：
//
// Localization 类加载
//     ├─> CurrentLanguageCode 初始化为简体中文
//     ├─> WebApplicationLauncher.Launch()
//     │     └─> Localization.LoadLanguage()
//     │           ├─> 配置中有有效语言时切换为配置语言
//     │           └─> 配置为空时保持简体中文
//     └─> DefaultPage 使用当前语言渲染界面
public static partial class Localization
{
	private const string DefaultLanguageCode = "zh-Hans";

	/// <summary>
	/// <see href="https://www.iana.org/assignments/language-subtag-registry/language-subtag-registry" >IANA</see> language code
	/// </summary>
	public static string CurrentLanguageCode { get; private set; } = DefaultLanguageCode;

	public static event Action? OnLanguageChanged;

	public static void LoadLanguage(string? code)
	{
		string? value = LanguageCodes.AsHyphenatedLanguageCode(code);
		if (CurrentLanguageCode != value && LanguageCodes.Exists(value))
		{
			CurrentLanguageCode = value;
			OnLanguageChanged?.Invoke();
		}
	}
}
