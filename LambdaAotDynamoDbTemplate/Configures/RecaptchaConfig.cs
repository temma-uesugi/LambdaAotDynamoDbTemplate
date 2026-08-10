using Exceptions;
using Services;

namespace Configures;

/// <summary>
/// reCAPTCHAシークレットの取得とRecaptchaServiceの登録
/// </summary>
public static class RecaptchaConfig
{
    // Lambdaのタイムアウト内に収まる範囲で、案件ごとの余裕を見て調整する
    private static readonly TimeSpan VerifyTimeout = TimeSpan.FromSeconds(3);

    /// <summary>
    /// 定義
    /// </summary>
    public static void ConfigureRecaptcha(this WebApplicationBuilder builder)
    {
        var isDevelopment = builder.Environment.IsDevelopment();
        var parameterName = isDevelopment
            ? string.Empty
            : Environment.GetEnvironmentVariable("SSM_PARAM_RECAPTCHA")
                ?? throw new AppConfigurationException("環境変数 SSM_PARAM_RECAPTCHA が設定されていません。");

        builder.Services.AddSingleton(sp =>
        {
            var secretService = sp.GetRequiredService<SecretService>();
            var secret = secretService.GetByParameterStoreAsync(parameterName).GetAwaiter().GetResult();

            var httpClient = new HttpClient { Timeout = VerifyTimeout };

            return new RecaptchaService(httpClient, secret, isDevelopment);
        });
    }
}
