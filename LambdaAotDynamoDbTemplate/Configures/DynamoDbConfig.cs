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

        // テーブル名の指定方式はprefix方式を既定とする。別会社がインフラを構築するなどテーブル名を決められない案件は、
        // docs/usage.md 3章の「パターン2」に書き換える（2つの方式は混ぜない）。
        // [DynamoDBTable]属性の名前にprefixを合成する。ローカルでは未設定なのでprefixなしになる
        var tableNamePrefix = System.Environment.GetEnvironmentVariable("DYNAMODB_TABLE_NAME_PREFIX") ?? string.Empty;
        builder.Services.AddSingleton<IDynamoDBContext>(sp =>
            new DynamoDBContextBuilder()
                .WithDynamoDBClient(sp.GetRequiredService<IAmazonDynamoDB>)
                .ConfigureContext(config => config.TableNamePrefix = tableNamePrefix)
                .Build());

        // 低レベルAPI（IAmazonDynamoDB）用の実テーブル名。IDynamoDBContextと同じprefixを使う
        builder.Services.AddSingleton(new DynamoDbTableNames(tableNamePrefix));
    }
}
