using Amazon.Lambda.Serialization.SystemTextJson;

namespace Configures;

/// <summary>
/// Lambda（API Gateway REST API経由）向けホスティングの設定
/// </summary>
public static class LambdaConfig
{
    /// <summary>
    /// 定義
    /// </summary>
    public static void ConfigureLambda(this WebApplicationBuilder builder)
    {
        // API GatewayはREST APIを採用（WAFをAPI Gatewayのステージへ直接アタッチできるため）。
        // AOTでは`options =>`形式のオーバーロードは[RequiresUnreferencedCode]付きでIL2026警告が出るため、
        // シリアライザーを直接渡すジェネリックオーバーロードを使う。
        // Note: 通常起動(dotnet run等)では、素通りして通常のサーバが起動する為の記述
        // Note: 第2引数は、NativeAOTで、Jsonシリアライズ/デシリアライズする為のミドルウェア
        builder.Services.AddAWSLambdaHosting(
            LambdaEventSource.RestApi,
            new SourceGeneratorLambdaJsonSerializer<ApiSerializationContext>());

        //Json定義
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiSerializationContext.Default);
        });
    }
}
