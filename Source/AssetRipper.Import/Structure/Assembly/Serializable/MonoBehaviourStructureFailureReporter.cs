using AssetRipper.Import.Structure.Assembly.Managers;
using System.Runtime.CompilerServices;

namespace AssetRipper.Import.Structure.Assembly.Serializable;

/*
 MonoBehaviourStructureFailureReporter 的收集流程

 MonoBehaviour 结构失败收集流程是这样的：

 RecordFailure()
     ├─> 按程序集管理器获取缓存
     ├─> 按脚本类型去重
     └─> 记录首次失败原因
 RecordSkipped()
     └─> 累加同类型出现次数
 GetEntries()
     └─> 返回去重后的失败报告条目
*/
public static class MonoBehaviourStructureFailureReporter
{
	private static readonly ConditionalWeakTable<IAssemblyManager, FailureCache> Caches = new();

	public static bool IsKnownFailure(IAssemblyManager assemblyManager, string scriptKey)
	{
		return GetCache(assemblyManager).Contains(scriptKey);
	}

	public static void RecordFailure(
		IAssemblyManager assemblyManager,
		string scriptKey,
		string scriptName,
		string unityVersion,
		string reason)
	{
		GetCache(assemblyManager).RecordFailure(scriptKey, scriptName, unityVersion, reason);
	}

	public static void RecordSkipped(IAssemblyManager assemblyManager, string scriptKey)
	{
		GetCache(assemblyManager).RecordSkipped(scriptKey);
	}

	public static IReadOnlyList<MonoBehaviourStructureFailureEntry> GetEntries(IAssemblyManager assemblyManager)
	{
		return GetCache(assemblyManager).GetEntries();
	}

	private static FailureCache GetCache(IAssemblyManager assemblyManager)
	{
		return Caches.GetValue(assemblyManager, static _ => new FailureCache());
	}

	/*
	 FailureCache 的缓存流程

	 失败缓存流程是这样的：

	 Contains()
	     └─> 查询脚本类型是否已经失败
	 RecordFailure()
	     └─> 添加或累加失败条目
	 GetEntries()
	     └─> 输出稳定排序的报告数据
	*/
	private sealed class FailureCache
	{
		private readonly Dictionary<string, MutableFailureEntry> entries = new(StringComparer.Ordinal);
		private readonly Lock cacheLock = new();

		public bool Contains(string scriptKey)
		{
			lock (cacheLock)
			{
				return entries.ContainsKey(scriptKey);
			}
		}

		public void RecordFailure(string scriptKey, string scriptName, string unityVersion, string reason)
		{
			lock (cacheLock)
			{
				if (entries.TryGetValue(scriptKey, out MutableFailureEntry? entry))
				{
					entry.Occurrences++;
				}
				else
				{
					entries.Add(scriptKey, new MutableFailureEntry(scriptName, unityVersion, reason));
				}
			}
		}

		public void RecordSkipped(string scriptKey)
		{
			lock (cacheLock)
			{
				if (entries.TryGetValue(scriptKey, out MutableFailureEntry? entry))
				{
					entry.Occurrences++;
				}
			}
		}

		public IReadOnlyList<MonoBehaviourStructureFailureEntry> GetEntries()
		{
			lock (cacheLock)
			{
				return entries
					.Values
					.Select(entry => entry.ToEntry())
					.OrderBy(entry => entry.ScriptName, StringComparer.Ordinal)
					.ThenBy(entry => entry.UnityVersion, StringComparer.Ordinal)
					.ToArray();
			}
		}
	}

	/*
	 MutableFailureEntry 的计数流程

	 可变失败条目流程是这样的：

	 构造函数
	     └─> 记录首次失败信息
	 ToEntry()
	     └─> 转换为只读报告条目
	*/
	private sealed class MutableFailureEntry
	{
		public MutableFailureEntry(string scriptName, string unityVersion, string reason)
		{
			ScriptName = scriptName;
			UnityVersion = unityVersion;
			Reason = reason;
		}

		public string ScriptName { get; }
		public string UnityVersion { get; }
		public string Reason { get; }
		public int Occurrences { get; set; } = 1;

		public MonoBehaviourStructureFailureEntry ToEntry()
		{
			return new MonoBehaviourStructureFailureEntry(ScriptName, UnityVersion, Reason, Occurrences);
		}
	}
}

/*
 MonoBehaviourStructureFailureEntry 的数据流程

 失败报告条目流程是这样的：

 构造记录
     ├─> 保存脚本名
     ├─> 保存 Unity 版本
     ├─> 保存失败原因
     └─> 保存出现次数
*/
public sealed record MonoBehaviourStructureFailureEntry(
	string ScriptName,
	string UnityVersion,
	string Reason,
	int Occurrences);
