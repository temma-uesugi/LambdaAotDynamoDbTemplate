namespace Configures;

/// <summary>
/// 環境ごとに異なるDynamoDBの実テーブル名を保持する。
/// インフラ側がテーブルごとに任意の名前を割り当てるため、テーブルごとに個別の環境変数で受け取る。
/// 新規案件でテーブルを追加する場合は、このクラスにプロパティを追加し、FromEnvironmentで対応する
/// 環境変数を読み込む（対応するDBs/*.csの[DynamoDBTable]属性、docker/dynamodb/tables/*.yamlも合わせて追加する）。
/// 以下のSampleItemsは使い方のサンプル。
/// </summary>
public class DynamoDbTableNames
{
    /// <summary>sample_itemsテーブルの実テーブル名（サンプル。実際のテーブルに合わせて置き換える）</summary>
    public required string SampleItems { get; init; }

    /// <summary>
    /// 環境変数からテーブル名を読み込む
    /// </summary>
    public static DynamoDbTableNames FromEnvironment()
    {
        return new DynamoDbTableNames
        {
            SampleItems = System.Environment.GetEnvironmentVariable("TABLE_SAMPLE_ITEMS") ?? "sample_items",
        };
    }
}
