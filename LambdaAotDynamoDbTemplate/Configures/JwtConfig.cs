using Services;

namespace Configures;

/// <summary>
/// JWT署名鍵の取得とJwtTokenServiceの登録
/// </summary>
public static class JwtConfig
{
    /// <summary>
    /// 定義
    /// </summary>
    public static void ConfigureJwt(this WebApplicationBuilder builder)
    {
        var parameterName = builder.Environment.IsDevelopment()
            ? string.Empty
            : Environment.GetEnvironmentVariable("SSM_PARAM_JWT")
                ?? throw new InvalidOperationException("環境変数 SSM_PARAM_JWT が設定されていません。");

        builder.Services.AddSingleton(sp =>
        {
            var secretService = sp.GetRequiredService<SecretService>();

            // Note: JwtTokenServiceは文字列を受け取るだけで取得元を知らないため、
            // 取得経路を変えたい場合はこの1行を差し替えるだけでよい。
            // 例: Secrets Manager経由にする場合は
            //     secretService.GetBySecretsManagerAsync(secretId, "jwtSecret") に置き換える。
            var secret = secretService.GetByParameterStoreAsync(parameterName).GetAwaiter().GetResult();

            return new JwtTokenService(secret);
        });
    }
}
