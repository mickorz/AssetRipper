using AssetRipper.Import.Configuration;
using AssetRipper.Import.Logging;
using AssetRipper.Import.Structure.Assembly.Managers;
using AssetRipper.Import.Structure.Assembly.Serializable;
using AssetRipper.IO.Files;
using System.Text;

namespace AssetRipper.Export.UnityProjects.Diagnostics;

/*
 MonoBehaviourStructureFailureLogExporter 的导出流程

 结构失败日志导出流程是这样的：

 Export()
     ├─> 读取去重失败条目
     ├─> 创建 Assets Docs 目录
     ├─> 生成可读日志内容
     └─> 写入失败报告文件
*/
public static class MonoBehaviourStructureFailureLogExporter
{
	private const string DocsDirectoryName = "Docs";
	private const string ReportFileName = "MonoBehaviourStructureFailures.log";

	public static void Export(IAssemblyManager assemblyManager, CoreConfiguration settings, FileSystem fileSystem)
	{
		IReadOnlyList<MonoBehaviourStructureFailureEntry> entries = MonoBehaviourStructureFailureReporter.GetEntries(assemblyManager);
		if (entries.Count == 0)
		{
			return;
		}

		string docsDirectory = fileSystem.Path.Join(settings.AssetsPath, DocsDirectoryName);
		fileSystem.Directory.Create(docsDirectory);
		string reportPath = fileSystem.Path.Join(docsDirectory, ReportFileName);
		fileSystem.File.WriteAllText(reportPath, CreateReport(entries));
		Logger.Info(LogCategory.Export, $"MonoBehaviour structure failure report saved to {reportPath}");
	}

	private static string CreateReport(IReadOnlyList<MonoBehaviourStructureFailureEntry> entries)
	{
		StringBuilder builder = new();
		builder.AppendLine("MonoBehaviour 结构解析失败报告");
		builder.AppendLine();
		builder.AppendLine($"脚本类型数量: {entries.Count}");
		builder.AppendLine($"资源出现次数: {entries.Sum(entry => entry.Occurrences)}");
		builder.AppendLine();
		builder.AppendLine("说明: 每个脚本类型只记录一次，出现次数表示同类 MonoBehaviour 资源数量。");
		builder.AppendLine();

		foreach (MonoBehaviourStructureFailureEntry entry in entries)
		{
			builder.AppendLine($"脚本: {entry.ScriptName}");
			builder.AppendLine($"Unity版本: {entry.UnityVersion}");
			builder.AppendLine($"失败原因: {entry.Reason}");
			builder.AppendLine($"出现次数: {entry.Occurrences}");
			builder.AppendLine();
		}

		return builder.ToString();
	}
}
