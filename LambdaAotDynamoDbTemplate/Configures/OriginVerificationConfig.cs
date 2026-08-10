using System.Security.Cryptography;
using System.Text;
using Exceptions;

namespace Configures;

/// <summary>
/// CloudFront経由以外からの直接アクセス（Lambda/API Gatewayの直叩き）を拒否するミドルウェアの設定
/// </summary>
public static class OriginVerificationConfig
{
    // CloudFrontが注入する秘匿ヘッダーのキー名。インフラ側と合意の上で固定値にする
    private const string HeaderName = "X-Origin-Secret";

    /// <summary>
    /// 秘匿ヘッダーの検証ミドルウェアを登録する。ローカル環境では登録しない
    /// </summary>
    public static void UseOriginVerification(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            return;
        }

        var expectedSecret = Environment.GetEnvironmentVariable("ORIGIN_VERIFY_SECRET")
            ?? throw new AppConfigurationException("環境変数 ORIGIN_VERIFY_SECRET が設定されていません。");

        app.Use(async (context, next) =>
        {
            var hasHeader = context.Request.Headers.TryGetValue(HeaderName, out var actual);
            if (!hasHeader || !SecretEquals(actual.ToString(), expectedSecret))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            await next(context);
        });
    }

    /// <summary>
    /// タイミング攻撃を避けるため、定数時間で文字列を比較する
    /// </summary>
    private static bool SecretEquals(string actual, string expected)
    {
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return actualBytes.Length == expectedBytes.Length && CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}
