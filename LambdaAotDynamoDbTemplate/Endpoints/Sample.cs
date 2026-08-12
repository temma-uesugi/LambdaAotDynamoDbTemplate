using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Configures;
using DBs;
using Logging;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace Endpoints;

/// <summary>
/// エンドポイント実装のサンプル。新規案件では本ファイルを参考に、機能ごとに`Endpoints/XxxEndpoints.cs`を作り、
/// `Program.cs`で`api.MapXxxEndpoints();`を呼ぶ形で追加していく。本サンプル自体は削除してよい。
/// </summary>
public static class SampleEndpoints
{
    /// <summary>
    /// エンドポイントを登録する
    /// </summary>
    public static void MapSampleEndpoints(this IEndpointRouteBuilder app)
    {
        // サンプル1: 最小構成のGET。パスパラメータを受け取り、単純な計算結果をそのまま返す（Dto不要な例）。
        // 案件でよく使うDI引数（RecaptchaService/IDynamoDBContext/IAmazonDynamoDB/JwtTokenService/IAppLogger）は
        // 未使用でもとりあえず引数に含めておき、必要になったらすぐ使えるようにしておく。
        app.MapGet("/sample/get/{amount:int}", (
                int amount,
                RecaptchaService recaptchaService,
                IDynamoDBContext dbContext,
                IAmazonDynamoDB dynamoDb,
                JwtTokenService jwtTokenService,
                IAppLogger logger) =>
            {
                // Loggerの使い方サンプル。第2引数以降(arg1〜arg5)にプリミティブ以外を渡すとJSON化して出力される
                logger.Information("サンプルGETを受信しました", amount);

                return TypedResults.Ok(amount + 1);
            })
            .WithName("SampleGet")
            .WithSummary("サンプル: GET")
            .WithDescription("パスパラメータamountを受け取り、amount+1を返す")
            .WithTags("9.Sample");

        // サンプル2: リクエスト・レスポンスをDtoクラス（Dto.SamplePostReq / Dto.SamplePostRes）として
        // 定義し、ApiSerializationContext.csにも登録した上で受け渡しする例。
        app.MapPost("/sample/post", (
                [FromBody] Dto.SamplePostReq req,
                RecaptchaService recaptchaService,
                IDynamoDBContext dbContext,
                IAmazonDynamoDB dynamoDb,
                JwtTokenService jwtTokenService,
                IAppLogger logger) =>
            {
                logger.Information("サンプルPOSTを受信しました", req);

                return TypedResults.Ok(new Dto.SamplePostRes(req.Value + 1));
            })
            .WithName("SamplePost")
            .WithSummary("サンプル: POST")
            .WithDescription("リクエストボディ(SamplePostReq)のValueを受け取り、Value+1をSamplePostResとして返す")
            .WithTags("9.Sample");

        // サンプル3: 正常時のDtoレスポンスに加えて、認証エラー時にUnauthorizedHttpResultを返すこともある
        // エンドポイントの書き方。戻り値の型をResults<Ok<T>, UnauthorizedHttpResult>にし、
        // 分岐ごとにTypedResults.Ok(...) / TypedResults.Unauthorized()を返す。
        app.MapGet("/sample/auth", async Task<Results<Ok<Dto.SampleAuthRes>, UnauthorizedHttpResult>> (
                HttpContext context,
                UserIdentityService userIdentityService,
                RecaptchaService recaptchaService,
                IDynamoDBContext dbContext,
                IAmazonDynamoDB dynamoDb,
                JwtTokenService jwtTokenService,
                IAppLogger logger) =>
            {
                var userId = await userIdentityService.TryGetUserIdAsync(context);
                if (userId is null)
                {
                    // Errorレベルの例: 第1引数(exception)はnull許容で、例外がない警告的なエラーでも使える
                    logger.Warning("Cookieのユーザ認証に失敗しました");
                    return TypedResults.Unauthorized();
                }

                logger.Information("認証済みユーザでサンプルAuthを受信しました", userId.Value);

                return TypedResults.Ok(new Dto.SampleAuthRes(userId.Value));
            })
            .WithName("SampleAuth")
            .WithSummary("サンプル: 認証必須のGET（複数の戻り値型を持つ書き方）")
            .WithDescription("CookieのJWTからユーザIDを取得できなければUnauthorizedHttpResult(401)を、取得できればSampleAuthResを返す")
            .WithTags("9.Sample");

        // サンプル4: DynamoDB（DBs/SampleItem.cs、テーブル名はConfigures/DynamoDbTableNames.cs経由）から
        // IDynamoDBContext.LoadAsyncで1件取得する例。見つからなければNotFoundを返す。
        app.MapGet("/sample/item/{id}", async Task<Results<Ok<Dto.SampleItemRes>, NotFound>> (
                string id,
                IDynamoDBContext dbContext,
                DynamoDbTableNames tableNames,
                RecaptchaService recaptchaService,
                IAmazonDynamoDB dynamoDb,
                JwtTokenService jwtTokenService,
                IAppLogger logger) =>
            {
                var config = new LoadConfig { OverrideTableName = tableNames.SampleItems };
                var item = await dbContext.LoadAsync<SampleItem>(id, config);
                if (item is null)
                {
                    logger.Warning("サンプルアイテムが見つかりませんでした", id);
                    return TypedResults.NotFound();
                }

                logger.Information("サンプルアイテムを取得しました", item);

                return TypedResults.Ok(new Dto.SampleItemRes(item.Id, item.Name));
            })
            .WithName("SampleGetItem")
            .WithSummary("サンプル: DynamoDBからの取得")
            .WithDescription("sample_itemsテーブルをidでLoadAsyncし、見つかればSampleItemRes、無ければ404を返す")
            .WithTags("9.Sample");
    }
}
