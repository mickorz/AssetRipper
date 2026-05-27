using AssetRipper.NativeDialogs;
using AssetRipper.Export.Configuration;
using AssetRipper.Import.Logging;
using AssetRipper.IO.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace AssetRipper.GUI.Web.Pages;

public static class Commands
{
	private const string RootPath = "/";
	private const string CommandsPath = "/Commands";
	public const string UseScriptAssemblySelectionFormKey = "UseScriptAssemblySelection";
	public const string SelectedScriptAssembliesFormKey = nameof(ExportSettings.SelectedScriptAssemblies);

	/// <summary>
	/// For documentation purposes
	/// </summary>
	/// <param name="Path">The file system path.</param>
	internal record PathFormData(string Path);

	internal static RouteHandlerBuilder AcceptsFormDataContainingPath(this RouteHandlerBuilder builder)
	{
		return builder.Accepts<PathFormData>("application/x-www-form-urlencoded");
	}

	private static bool TryGetCreateSubfolder(IFormCollection form)
	{
		if (form.TryGetValue("CreateSubfolder", out StringValues values))
		{
			return values == "true";
		}

		return false;
	}

	public readonly struct LoadFile : ICommand
	{
		static async Task<string?> ICommand.Execute(HttpRequest request)
		{
			IFormCollection form = await request.ReadFormAsync();

			string[]? paths;
			if (form.TryGetValue("Path", out StringValues values))
			{
				paths = values;
			}
			else if (NativeDialog.Supported)
			{
				paths = await OpenFileDialog.OpenFiles();
			}
			else
			{
				return CommandsPath;
			}

			if (paths is { Length: > 0 })
			{
				GameFileLoader.LoadAndProcess(paths);
			}
			return null;
		}
	}

	public readonly struct LoadFolder : ICommand
	{
		static async Task<string?> ICommand.Execute(HttpRequest request)
		{
			IFormCollection form = await request.ReadFormAsync();

			string[]? paths;
			if (form.TryGetValue("Path", out StringValues values))
			{
				paths = values;
			}
			else if (NativeDialog.Supported)
			{
				paths = await OpenFolderDialog.OpenFolders();
			}
			else
			{
				return CommandsPath;
			}

			if (paths is { Length: > 0 })
			{
				GameFileLoader.LoadAndProcess(paths);
			}
			return null;
		}
	}

	public readonly struct ExportUnityProject : ICommand
	{
		static async Task<string?> ICommand.Execute(HttpRequest request)
		{
			IFormCollection form = await request.ReadFormAsync();

			string? path;
			if (form.TryGetValue("Path", out StringValues values))
			{
				path = values;
			}
			else
			{
				return CommandsPath;
			}

			if (!string.IsNullOrEmpty(path))
			{
				bool createSubfolder = TryGetCreateSubfolder(form);
				path = MaybeAppendTimestampedSubfolder(path, createSubfolder);
				ApplyScriptAssemblySelection(form);
				await GameFileLoader.ExportUnityProject(path);
			}
			return null;
		}
	}

	public readonly struct ExportPrimaryContent : ICommand
	{
		static async Task<string?> ICommand.Execute(HttpRequest request)
		{
			IFormCollection form = await request.ReadFormAsync();

			string? path;
			if (form.TryGetValue("Path", out StringValues values))
			{
				path = values;
			}
			else
			{
				return CommandsPath;
			}

			if (!string.IsNullOrEmpty(path))
			{
				bool createSubfolder = TryGetCreateSubfolder(form);
				path = MaybeAppendTimestampedSubfolder(path, createSubfolder);
				await GameFileLoader.ExportPrimaryContent(path);
			}
			return null;
		}
	}

	private static string MaybeAppendTimestampedSubfolder(string path, bool append)
	{
		if (append)
		{
			string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
			string subfolder = $"AssetRipper_export_{timestamp}";
			return Path.Combine(path, subfolder);
		}

		return path;
	}

	private static void ApplyScriptAssemblySelection(IFormCollection form)
	{
		if (!form.ContainsKey(UseScriptAssemblySelectionFormKey))
		{
			return;
		}

		GameFileLoader.Settings.ExportSettings.ScriptExportMode = ScriptExportMode.SelectedDlls;
		GameFileLoader.Settings.ExportSettings.SelectedScriptAssemblies = form.TryGetValue(SelectedScriptAssembliesFormKey, out StringValues values)
			? values
				.Where(static value => !string.IsNullOrWhiteSpace(value))
				.Select(static value => SpecialFileNames.RemoveAssemblyFileExtension(value!))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Order(StringComparer.OrdinalIgnoreCase)
				.ToList()
			: [];
		Logger.Info(LogCategory.Export, $"本次脚本反编译选择 DLL 数量: {GameFileLoader.Settings.ExportSettings.SelectedScriptAssemblies.Count}");
	}

	public readonly struct Reset : ICommand
	{
		static Task<string?> ICommand.Execute(HttpRequest request)
		{
			GameFileLoader.Reset();
			return Task.FromResult<string?>(null);
		}
	}

	public static async Task HandleCommand<T>(HttpContext context) where T : ICommand
	{
		string? redirectionTarget = await T.Execute(context.Request);
		context.Response.Redirect(redirectionTarget ?? RootPath);
	}
}
