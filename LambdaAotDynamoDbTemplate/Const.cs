/// <summary>
/// 定数
/// </summary>
public static class Const
{
    /// <summary>JWTトークンを格納するヘッダー名（Bearerヘッダ方式を使う場合。Services/JwtTokenService.cs参照）</summary>
    public const string JWTToken = "Authorization";

    /// <summary>ユーザ識別Cookieの有効期限</summary>
    public static readonly TimeSpan CookieExpiration = TimeSpan.FromDays(365);

    /// <summary>ユーザ識別Cookieのキー名</summary>
    public const string UserIdCookieKey = "uid";
}
