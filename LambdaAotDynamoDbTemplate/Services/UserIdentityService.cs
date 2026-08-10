namespace Services;

/// <summary>
/// Cookieに格納されたJWTを検証し、ユーザIDを取り出すサービス
/// </summary>
public class UserIdentityService
{
    private readonly CookieService _cookieService;
    private readonly JwtTokenService _jwtTokenService;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public UserIdentityService(CookieService cookieService, JwtTokenService jwtTokenService)
    {
        _cookieService = cookieService;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// CookieからユーザIDを取り出す（JWT署名検証込み）
    /// </summary>
    public Task<Guid?> TryGetUserIdAsync(HttpContext context)
    {
        return _cookieService.TryGet(context, Const.UserIdCookieKey, out var token)
            ? _jwtTokenService.TryGetUserIdFromTokenAsync(token)
            : Task.FromResult<Guid?>(null);
    }
}
