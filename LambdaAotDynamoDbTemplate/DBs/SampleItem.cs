using Amazon.DynamoDBv2.DataModel;

namespace DBs;

/// <summary>
/// DynamoDBテーブル定義のサンプル。新規案件では本クラスをコピーして実テーブルに合わせたプロパティに置き換える。
/// 併せてConfigures/DynamoDbTableNames.csにプロパティを追加し、
/// ローカル確認用にdocker/dynamodb/tables/配下へ同名のテーブル定義yamlを追加する
/// （docker/dynamodb/tables/・seed/は空でコミットされているため、案件ごとに追加する運用）。
/// </summary>
[DynamoDBTable("sample_items")]
public class SampleItem
{
    /// <summary>パーティションキー</summary>
    [DynamoDBHashKey("id")]
    public required string Id { get; set; }

    /// <summary>サンプル属性</summary>
    [DynamoDBProperty("name")]
    public string? Name { get; set; }
}
