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
                return new MemoryDatabase(bytes);
            });
        }
    }
}
