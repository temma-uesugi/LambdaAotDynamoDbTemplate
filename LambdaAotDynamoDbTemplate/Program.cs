using Configures;
using Endpoints;
using Logging;
using MasterData;
using Microsoft.AspNetCore.Routing.Constraints;
using Services;

// Lambda Native AOTビルドに適した軽量版ビルダー
var builder = WebApplication.CreateSlimBuilder(args);

builder.ConfigureLambda();
builder.ConfigureDynamoDb();
builder.ConfigureMasterData();
builder.ConfigureSwagger();
builder.ConfigureJwt();
builder.ConfigureRecaptcha();

// CORS設定
// Note: Cookie認証を使う場合、AllowAnyOriginとAllowCredentialsは併用できない。
// フロントとAPIを同一ドメイン（リバースプロキシ等でパスを振り分ける構成）で運用するならCORS自体不要。
// 別ドメイン構成にする場合のみ、固定オリジン+AllowCredentialsで以下を有効化する。
// builder.Services.AddCors(options =>
// {
//     options.AddDefaultPolicy(policy =>
//     {
//         policy.WithOrigins("https://example.com")
//             .AllowAnyMethod()
//             .AllowAnyHeader()
//             .AllowCredentials();
//     });
// });

// CreateSlimBuilderは最小限のルーティング機能しか登録しないため、
// Swashbuckleが内部で使うregex制約を明示的に登録する必要がある。
builder.Services.Configure<RouteOptions>(options =>
    options.SetParameterPolicy<RegexInlineRouteConstraint>("regex"));

builder.Services.AddSingleton<IAppLogger, AppLogger>();
builder.Services.AddSingleton<SecretService>();
builder.Services.AddSingleton<CookieService>();
builder.Services.AddSingleton<UserIdentityService>();

var app = builder.Build();

// DIせずAppLog.Debug(...)等で呼べるようにする（内部の実処理はIAppLoggerに委譲）
AppLog.Initialize(app.Services.GetRequiredService<IAppLogger>());

app.UseOriginVerification();
app.UseSecurityHeaders();
// app.UseCors();

// Swashbuckle Swaggerの設定（本番では無効化）
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CloudFront等を前段に挟み、リクエストパスが/apiを含んだまま転送される構成を想定し、
// アプリ側で/apiをベースパスとして受け付ける。
var api = app.MapGroup("/api");

// Endpoints/Sample.csがGET/POST/認証必須エンドポイントの実装サンプル。
// 案件のAPIを実装したら、このMapSampleEndpoints()呼び出しごとサンプルは削除してよい。
api.MapSampleEndpoints();

app.Run();
