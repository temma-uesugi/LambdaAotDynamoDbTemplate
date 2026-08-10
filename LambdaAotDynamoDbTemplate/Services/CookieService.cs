namespace Services;

/// <summary>
/// Cookieへの汎用的な値の保存・取得を行うサービス
/// </summary>
public class CookieService
{
    private readonly bool _isDevelopment;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public CookieService(IHostEnvironment environment)
    {
        _isDevelopment = environment.IsDevelopment();
    }

    /// <summary>
    /// 任意のキーで文字列を有効期限付きでCookieにセットする
    /// </summary>
    public void SetWithExpiry(HttpResponse response, string key, string value, TimeSpan expiration)
    {
        response.Cookies.Append(key, value, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.Add(expiration),
            HttpOnly = true,
            // ローカル(dotnet run、HTTP)でも動作確認できるよう、開発環境ではSecureを外す
            Secure = !_isDevelopment,
            SameSite = SameSiteMode.Lax,
        });
    }

    /// <summary>
    /// 任意のキーでCookieの値を取り出す
    /// </summary>
    public bool TryGet(HttpContext context, string key, out string value)
    {
        value = string.Empty;

        if (!context.Request.Cookies.TryGetValue(key, out var rawValue) || string.IsNullOrEmpty(rawValue))
        {
            return false;
        }

        value = rawValue;
        return true;
    }

    /// <summary>
    /// 任意のキーでCookieを削除する
    /// </summary>
    public void Delete(HttpResponse response, string key)
    {
        response.Cookies.Delete(key);
    }
}
