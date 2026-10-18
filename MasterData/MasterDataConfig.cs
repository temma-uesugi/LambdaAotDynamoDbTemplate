using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MasterData
{
    /// <summary>
    /// マスタデータ(MemoryDatabase)の読み込みとDI登録
    /// </summary>
    public static class MasterDataConfig
    {
        private const string MasterDataFileName = "master.bytes";

        /// <summary>
        /// マスタ読み込み用のリゾルバ。Native AOT では、ジェネリック型のフォーマッタを実行時に組み立てられない
        /// (Dictionary や enum の配列で「No parameterless constructor」になる)ため、マスタで使うジェネリック型を明示して登録する。
        /// ローカルの JIT 実行では再現せず、AOT でデプロイした環境でだけ失敗する。
        /// マスタのエンティティに新しいジェネリック型(Dictionary や enum の配列など)を足したときは、ここにも足すこと
        /// </summary>
        private static readonly IFormatterResolver MasterResolver = CompositeResolver.Create(
            new IMessagePackFormatter[]
            {
                // 現在の MasterSample にはジェネリック型がないため空。足し方の例:
                // new DictionaryFormatter<string, string>(),
                // new ArrayFormatter<SomeEnum>(),
            },
            new IFormatterResolver[]
            {
                GeneratedMessagePackResolver.Instance,
                StandardResolver.Instance,
            });

        /// <summary>
        /// 定義
        /// </summary>
        public static void ConfigureMasterData(this WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton(_ =>
            {
                var path = Path.Combine(AppContext.BaseDirectory, MasterDataFileName);
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException(
                        $"マスタデータ({MasterDataFileName})が見つかりません。MasterData.Generatorで生成してください。", path);
                }

                var bytes = File.ReadAllBytes(path);
                return new MemoryDatabase(bytes, formatterResolver: MasterResolver);
            });
        }
    }
}
