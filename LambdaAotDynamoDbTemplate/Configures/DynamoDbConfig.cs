using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;

namespace Configures;

/// <summary>
/// DynamoDB関係
/// </summary>
public static class DynamoDbConfig
{
    /// <summary>
    /// 定義
    /// </summary>
    public static void ConfigureDynamoDb(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // docker-compose.ymlのDynamoDB Localを参照する。認証情報は検証されないのでダミー値でよい。
                // ホストでdotnet runする場合はlocalhost:8001（ポート公開分）、
                // アプリ自体もdocker-compose上で動かす場合はDYNAMODB_LOCAL_ENDPOINTでdb:8000を渡す。
                var endpoint = System.Environment.GetEnvironmentVariable("DYNAMODB_LOCAL_ENDPOINT") ?? "http://localhost:8001";
                var config = new AmazonDynamoDBConfig { ServiceURL = endpoint };
                return new AmazonDynamoDBClient(new BasicAWSCredentials("local", "local"), config);
            }

            return new AmazonDynamoDBClient();
        });
        builder.Services.AddSingleton<IDynamoDBContext>(sp =>
            new DynamoDBContextBuilder()
                .WithDynamoDBClient(sp.GetRequiredService<IAmazonDynamoDB>)
                .Build());

        // テーブルごとの実テーブル名（環境変数経由でインフラ側から受け取る。未設定ならデフォルト名を使う）
        builder.Services.AddSingleton(DynamoDbTableNames.FromEnvironment());
    }
}
