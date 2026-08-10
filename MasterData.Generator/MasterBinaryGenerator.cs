using System.Collections;
using System.Globalization;
using System.Reflection;
using MasterMemory;
using YamlDotNet.Serialization;

namespace MasterData.Generator
{
    /// <summary>
    /// MasterData/Sources配下のYAMLから、MasterMemoryのバイナリ(MemoryDatabase)を生成する
    /// </summary>
    public static class MasterBinaryGenerator
    {
        private const string MasterDataProjectFileName = "MasterData.csproj";
        private const string SourcesDirectoryName = "Sources";
        private const string OutputDirectoryName = "Generated";
        private const string OutputFileName = "master.bytes";

        /// <summary>
        /// マスタバイナリを生成する
        /// </summary>
        public static void Generate()
        {
            var masterDataDir = FindMasterDataProjectDirectory();
            var sourcesRoot = Path.Combine(masterDataDir, SourcesDirectoryName);
            var outputPath = Path.Combine(masterDataDir, OutputDirectoryName, OutputFileName);

            var builder = new DatabaseBuilder();
            var deserializer = new DeserializerBuilder().Build();
            var builderMethods = typeof(DatabaseBuilder).GetMethods();

            foreach (var entityType in CollectEntityTypes())
            {
                AppendEntities(builder, builderMethods, deserializer, sourcesRoot, entityType);
            }

            var binary = builder.Build();
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllBytes(outputPath, binary);
            Console.WriteLine($"[MasterBinaryGenerator] Generated: {outputPath} ({binary.Length} bytes)");
        }

        /// <summary>
        /// 1エンティティ分のYAMLソースを読み込み、DatabaseBuilderに追加する
        /// </summary>
        private static void AppendEntities(
            DatabaseBuilder builder,
            MethodInfo[] builderMethods,
            IDeserializer deserializer,
            string sourcesRoot,
            Type entityType)
        {
            var appendMethod = FindAppendMethod(builderMethods, entityType);
            if (appendMethod == null)
            {
                Console.WriteLine($"[MasterBinaryGenerator] Append method not found for {entityType.Name}, skipped.");
                return;
            }

            var tableName = entityType.GetCustomAttribute<MemoryTableAttribute>()!.TableName;
            var yamlPath = Path.Combine(sourcesRoot, tableName + ".yml");
            if (!File.Exists(yamlPath))
            {
                Console.WriteLine($"[MasterBinaryGenerator] Source yaml not found: {yamlPath}, skipped.");
                return;
            }

            var listType = typeof(List<>).MakeGenericType(entityType);
            var combinedList = (IList)Activator.CreateInstance(listType)!;

            var yaml = File.ReadAllText(yamlPath);
            var rawList = deserializer.Deserialize<List<Dictionary<object, object>>>(new StringReader(yaml));
            if (rawList != null)
            {
                foreach (var dict in rawList)
                {
                    combinedList.Add(ConstructFromDictionary(entityType, dict));
                }
            }

            if (combinedList.Count == 0) return;

            appendMethod.Invoke(builder, [combinedList]);
            Console.WriteLine($"[MasterBinaryGenerator] Appended {combinedList.Count} records: {entityType.Name}");
        }

        /// <summary>
        /// エンティティ型に対応するDatabaseBuilder.Append(IEnumerable&lt;T&gt;)メソッドを取得する
        /// </summary>
        private static MethodInfo? FindAppendMethod(MethodInfo[] builderMethods, Type entityType)
        {
            var expectedParamType = typeof(IEnumerable<>).MakeGenericType(entityType);
            return Array.Find(
                builderMethods,
                m => m.Name == "Append"
                     && m.GetParameters().Length == 1
                     && m.GetParameters()[0].ParameterType == expectedParamType);
        }

        /// <summary>
        /// YAMLから読み込んだDictionaryをコンストラクタ引数にマッピングしてインスタンスを生成する
        /// </summary>
        private static object ConstructFromDictionary(Type type, Dictionary<object, object> dict)
        {
            var constructor = type.GetConstructors()[0];
            var parameters = constructor.GetParameters();
            var args = new object?[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                // PascalCase → camelCase の順でYAMLキーを照合（どちらの表記でも対応）
                var key = char.ToUpper(param.Name![0], CultureInfo.InvariantCulture) + param.Name[1..];
                var rawValue = dict.TryGetValue(key, out var v) ? v
                    : dict.TryGetValue(param.Name, out v) ? v
                    : null;
                args[i] = ConvertValue(rawValue, param.ParameterType);
            }

            return constructor.Invoke(args);
        }

        /// <summary>
        /// YamlDotNetが返すobject値を目的の型に変換する
        /// </summary>
        private static object? ConvertValue(object? raw, Type targetType)
        {
            // Nullable<T>（enum? 等）は下地の型で変換し、値なしのときはnullを返す
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null) return raw == null ? null : ConvertValue(raw, underlyingType);

            if (raw == null)
            {
                // 配列型はnullではなく長さ0の配列を返し、呼び出し側のnullチェックを不要にする
                if (targetType.IsArray) return Array.CreateInstance(targetType.GetElementType()!, 0);
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }

            if (targetType.IsAssignableFrom(raw.GetType())) return raw;

            if (targetType.IsArray)
            {
                var elementType = targetType.GetElementType()!;
                if (raw is List<object> rawList)
                {
                    var array = Array.CreateInstance(elementType, rawList.Count);
                    for (var i = 0; i < rawList.Count; i++)
                    {
                        array.SetValue(ConvertValue(rawList[i], elementType), i);
                    }

                    return array;
                }

                // スカラー値を単一要素配列として扱う
                var single = Array.CreateInstance(elementType, 1);
                single.SetValue(ConvertValue(raw, elementType), 0);
                return single;
            }

            var str = raw.ToString();
            if (targetType == typeof(string)) return str;
            if (targetType == typeof(int)) return int.Parse(str!, CultureInfo.InvariantCulture);
            if (targetType == typeof(float)) return float.Parse(str!, CultureInfo.InvariantCulture);
            if (targetType == typeof(double)) return double.Parse(str!, CultureInfo.InvariantCulture);
            if (targetType == typeof(bool)) return bool.Parse(str!);
            if (targetType.IsEnum) return Enum.Parse(targetType, str!);
            return Convert.ChangeType(str, targetType, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// MemoryTable属性を持つエンティティ型を、MasterDataアセンブリから収集する
        /// </summary>
        private static IEnumerable<Type> CollectEntityTypes()
        {
            return Assembly.Load("MasterData").GetTypes()
                .Where(t => t.GetCustomAttribute<MemoryTableAttribute>() != null);
        }

        /// <summary>
        /// 実行時のカレントディレクトリに依存せず、隣接するMasterDataプロジェクトのディレクトリを探索する
        /// </summary>
        private static string FindMasterDataProjectDirectory()
        {
            // ProjectReference先のMasterData.dllは自分自身の出力先にコピーされるため、
            // AppContext.BaseDirectory（自分の出力先）から上位へ辿ってMasterDataディレクトリを探す
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "MasterData", MasterDataProjectFileName);
                if (File.Exists(candidate))
                {
                    return Path.Combine(dir.FullName, "MasterData");
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException($"{MasterDataProjectFileName} が見つかりませんでした。");
        }
    }
}
