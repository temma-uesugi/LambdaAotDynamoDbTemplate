namespace Configures;

/// <summary>
/// 低レベルAPI（IAmazonDynamoDB）で使う実テーブル名を保持する。
/// IDynamoDBContext経由の操作はTableNamePrefixで自動的に解決されるので、ここを参照しない
/// （OverrideTableNameを併用するとprefixが二重に付くため、使わないこと）。
/// 新規テーブルを低レベルAPIで操作する場合は、このクラスにプロパティを追加する。
/// テーブル名を決められない案件での書き換えはdocs/usage.md 3章の「パターン2」を参照。
/// </summary>
public class DynamoDbTableNames
{
    /// <summary>sample_itemsテーブルの実テーブル名（サンプル。実際のテーブルに合わせて置き換える）</summary>
    public string SampleItems { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public DynamoDbTableNames(string prefix)
    {
        SampleItems = prefix + "sample_items";
    }
}
