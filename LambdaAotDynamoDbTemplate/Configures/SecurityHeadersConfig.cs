namespace Configures;

/// <summary>
/// レスポンスにセキュリティ関連ヘッダーを付与するミドルウェアの設定
/// </summary>
public static class SecurityHeadersConfig
{
    /// <summary>
    /// セキュリティヘッダー付与ミドルウェアを登録する
    /// </summary>
    public static void UseSecurityHeaders(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            await next(context);
        });
    }
}
