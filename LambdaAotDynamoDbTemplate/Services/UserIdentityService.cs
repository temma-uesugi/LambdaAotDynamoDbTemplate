using Microsoft.IdentityModel.JsonWebTokens;

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
    public async Task<Guid?> TryGetUserIdAsync(HttpContext context)
    {
        if (!_cookieService.TryGet(context, Const.UserIdCookieKey, out var token))
        {
            return null;
        }

        var claims = await _jwtTokenService.TryGetClaimsFromTokenAsync(token);
        if (claims is null || !claims.TryGetValue(JwtRegisteredClaimNames.Sub, out var subClaim))
        {
            return null;
        }

        return Guid.TryParse(subClaim.Value, out var userId) ? userId : null;
    }
}
