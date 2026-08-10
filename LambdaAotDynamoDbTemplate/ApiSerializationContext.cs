using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Services;
/// <summary>
/// AOT環境でリフレクションを使わずにJSONシリアライズ/デシリアライズを行うためのソース生成コンテキスト。
/// API Gateway (REST API) のリクエスト/レスポンス型と、アプリ独自のDTO型の両方を登録する必要がある。
/// 新規のDTO・内部モデルを追加した場合は、ここにも[JsonSerializable]を追加すること（AOTはリフレクションで拾えない）。
/// </summary>
[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(Dto.SampleItemRes))]
[JsonSerializable(typeof(Dto.SamplePostReq))]
[JsonSerializable(typeof(Dto.SamplePostRes))]
[JsonSerializable(typeof(Dto.SampleAuthRes))]
[JsonSerializable(typeof(RecaptchaSiteVerifyResponse))]
[JsonSerializable(typeof(List<string>))]
internal partial class ApiSerializationContext : JsonSerializerContext
{
}
