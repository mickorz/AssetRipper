using AsmResolver.DotNet;
using AssetRipper.Assets.Bundles;
using AssetRipper.Assets.Collections;
using AssetRipper.Assets.Generics;
using AssetRipper.Export.UnityProjects;
using AssetRipper.Import.Logging;
using AssetRipper.Import.Structure.Assembly;
using AssetRipper.Import.Structure.Assembly.Managers;
using AssetRipper.Import.Structure.Assembly.Serializable;
using AssetRipper.Import.Structure.Platforms;
using AssetRipper.IO.Files;
using AssetRipper.Primitives;
using AssetRipper.Processing;
using AssetRipper.SerializationLogic;
using AssetRipper.SourceGenerated.Classes.ClassID_114;
using AssetRipper.SourceGenerated.Classes.ClassID_115;
using AssetRipper.SourceGenerated.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace AssetRipper.Tests;

/*
 UnloadedStructureTests 的验证流程

 懒加载结构测试流程是这样的：

 测试方法
     ├─> 创建同脚本类型资源
     ├─> 构造不匹配二进制数据
     ├─> 执行结构懒加载
     └─> 验证失败类型不会重复解析
*/
internal sealed class UnloadedStructureTests
{
	[Test]
	public void SameMismatchedScriptTypeIsNotParsedRepeatedly()
	{
		ProcessedAssetCollection collection = AssetCreator.CreateCollection(UnityVersion.V_2022);
		IMonoScript script = collection.CreateMonoScript();
		script.AssemblyName = "Assembly-CSharp";
		script.Namespace = "Rogue";
		script.ClassName_R = "场地物体";
		TestAssemblyManager assemblyManager = new(new TestBehaviourType());
		TestLogger logger = new();
		Logger.Add(logger);

		try
		{
			IMonoBehaviour first = CreateUnloadedBehaviour(collection, script, assemblyManager);
			IMonoBehaviour second = CreateUnloadedBehaviour(collection, script, assemblyManager);

			first.LoadStructure();
			second.LoadStructure();
		}
		finally
		{
			Logger.Remove(logger);
		}

		using (Assert.EnterMultipleScope())
		{
			Assert.That(assemblyManager.SerializableTypeRequests, Is.EqualTo(1));
			Assert.That(logger.MismatchedStructureErrors, Is.EqualTo(1));
		}
	}

	[Test]
	public void MismatchedScriptFailuresAreWrittenToDeduplicatedReport()
	{
		ProcessedAssetCollection collection = AssetCreator.CreateCollection(UnityVersion.V_2022);
		IMonoScript script = collection.CreateMonoScript();
		script.AssemblyName = "Assembly-CSharp";
		script.Namespace = "Rogue";
		script.ClassName_R = "场地物体";
		TestAssemblyManager assemblyManager = new(new TestBehaviourType());
		CreateUnloadedBehaviour(collection, script, assemblyManager);
		CreateUnloadedBehaviour(collection, script, assemblyManager);
		VirtualFileSystem fileSystem = new();

		new ExportHandler(new()).Export(CreateGameData(collection, assemblyManager), "output", fileSystem);

		const string ReportPath = "/output/ExportedProject/Assets/Docs/MonoBehaviourStructureFailures.log";
		Assert.That(fileSystem.File.Exists(ReportPath), Is.True);
		string report = fileSystem.File.ReadAllText(ReportPath);
		using (Assert.EnterMultipleScope())
		{
			Assert.That(report, Does.Contain("Rogue.场地物体"));
			Assert.That(report, Does.Contain("出现次数: 2"));
			Assert.That(CountOccurrences(report, "脚本: Rogue.场地物体"), Is.EqualTo(1));
		}
	}

	private static IMonoBehaviour CreateUnloadedBehaviour(ProcessedAssetCollection collection, IMonoScript script, IAssemblyManager assemblyManager)
	{
		IMonoBehaviour monoBehaviour = collection.CreateMonoBehaviour();
		monoBehaviour.ScriptP = script;
		UnloadedStructure structure = new(monoBehaviour, assemblyManager, ReadOnlyArraySegment<byte>.Empty);
		monoBehaviour.Structure = structure;
		return monoBehaviour;
	}

	private static GameData CreateGameData(ProcessedAssetCollection collection, IAssemblyManager assemblyManager)
	{
		return new((GameBundle)collection.Bundle, collection.Version, assemblyManager, null);
	}

	private static int CountOccurrences(string text, string value)
	{
		int count = 0;
		int index = 0;
		while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
		{
			count++;
			index += value.Length;
		}
		return count;
	}

	/*
	 TestAssemblyManager 的解析流程

	 测试程序集管理流程是这样的：

	 GetScriptID()
	     └─> 返回固定脚本标识
	 TryGetSerializableType()
	     ├─> 记录请求次数
	     └─> 返回固定序列化类型
	*/
	private sealed class TestAssemblyManager : IAssemblyManager
	{
		private readonly SerializableType serializableType;

		public TestAssemblyManager(SerializableType serializableType)
		{
			this.serializableType = serializableType;
		}

		public int SerializableTypeRequests { get; private set; }
		public bool IsSet => true;
		public ScriptingBackend ScriptingBackend => ScriptingBackend.IL2Cpp;

		public void Initialize(PlatformGameStructure gameStructure)
		{
		}

		public void Load(string filePath, FileSystem fileSystem)
		{
		}

		public void Add(AssemblyDefinition assembly)
		{
		}

		public void Read(Stream stream, string fileName)
		{
		}

		public void Unload(string fileName)
		{
		}

		public bool IsAssemblyLoaded(string assembly) => true;
		public bool IsPresent(ScriptIdentifier scriptID) => true;
		public bool IsValid(ScriptIdentifier scriptID) => true;

		public bool TryGetSerializableType(
			ScriptIdentifier scriptID,
			UnityVersion version,
			[NotNullWhen(true)] out SerializableType? scriptType,
			[NotNullWhen(false)] out string? failureReason)
		{
			SerializableTypeRequests++;
			scriptType = serializableType;
			failureReason = null;
			return true;
		}

		public TypeDefinition GetTypeDefinition(ScriptIdentifier scriptID)
		{
			throw new NotSupportedException();
		}

		public IEnumerable<AssemblyDefinition> GetAssemblies() => [];

		public ScriptIdentifier GetScriptID(string assembly, string @namespace, string name)
		{
			return new ScriptIdentifier(assembly, @namespace, name);
		}

		public Stream GetStreamForAssembly(AssemblyDefinition assembly)
		{
			throw new NotSupportedException();
		}

		public void ClearStreamCache()
		{
		}

		public void Dispose()
		{
		}
	}

	/*
	 TestBehaviourType 的类型流程

	 测试类型流程是这样的：

	 构造函数
	     └─> 添加一个整数字段
	 读取结构
	     └─> 空数据读取整数会触发布局不匹配
	*/
	private sealed class TestBehaviourType : SerializableType
	{
		public TestBehaviourType() : base("Rogue", PrimitiveType.Complex, "场地物体")
		{
			Fields = [new Field(new IntType(), 0, "value", false)];
			MaxDepth = 1;
		}
	}

	/*
	 IntType 的类型流程

	 整数字段类型流程是这样的：

	 构造函数
	     └─> 声明为基础整数类型
	*/
	private sealed class IntType : SerializableType
	{
		public IntType() : base(null, PrimitiveType.Int, "int")
		{
			MaxDepth = 0;
		}
	}

	/*
	 TestLogger 的记录流程

	 测试日志记录流程是这样的：

	 Log()
	     ├─> 判断是否为布局错误
	     └─> 累加错误次数
	*/
	private sealed class TestLogger : ILogger
	{
		public int MismatchedStructureErrors { get; private set; }

		public void Log(LogType type, LogCategory category, string message)
		{
			if (type is LogType.Error
				&& category is LogCategory.Import
				&& message.Contains("layout mismatched binary content", StringComparison.Ordinal))
			{
				MismatchedStructureErrors++;
			}
		}

		public void BlankLine(int numLines)
		{
		}
	}
}
