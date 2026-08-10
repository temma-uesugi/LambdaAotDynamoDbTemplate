using System.Text.Json.Serialization;

/// <summary>
/// DTO
/// </summary>
public static class Dto
{
    /// <summary>
    /// サンプルアイテムのレスポンス。
    /// DynamoDBの内部モデル（DBs.SampleItem）とAPIレスポンス型を分離する例。新規案件では実際のAPIに合わせて置き換える。
    /// </summary>
    public record SampleItemRes(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string? Name
    );

    /// <summary>
    /// サンプルPOSTのリクエスト（Endpoints/Sample.cs参照）
    /// </summary>
    public record SamplePostReq(
        [property: JsonPropertyName("value")] int Value
    );

    /// <summary>
    /// サンプルPOSTのレスポンス（Endpoints/Sample.cs参照）
    /// </summary>
    public record SamplePostRes(
        [property: JsonPropertyName("value")] int Value
    );

    /// <summary>
    /// 認証必須サンプルのレスポンス（Endpoints/Sample.cs参照）
    /// </summary>
    public record SampleAuthRes(
        [property: JsonPropertyName("userId")] Guid UserId
    );
}
