using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Services;

/// <summary>
/// JWTの発行・検証を集約するクラス
/// </summary>
public class JwtTokenService
{
    private readonly JsonWebTokenHandler _handler = new();
    private readonly SymmetricSecurityKey _signingKey;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public JwtTokenService(string signingKeySecret)
    {
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKeySecret));
    }

    /// <summary>
    /// ユーザIDのJWTを発行する。sub（ユーザID）以外にクレームを含めたい場合はadditionalClaimsで渡す
    /// （例: additionalClaims: new() { ["device_id"] = new Claim("device_id", deviceId) }）
    /// </summary>
    public string IssueToken(string userId, TimeSpan expiration, Dictionary<string, Claim>? additionalClaims = null)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, userId) };
        if (additionalClaims is not null)
        {
            claims.AddRange(additionalClaims.Values);
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(expiration),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256),
        };
        return _handler.CreateToken(descriptor);
    }

    /// <summary>
    /// AuthorizationヘッダのBearerトークンを検証し、クレーム一覧を取り出す（クレーム名 -> Claim）。
    /// 現状どこからも呼ばれていないが、Bearerヘッダ方式で認証したい案件向けにあえて残してある
    /// （Cookie方式を使うならこのメソッドは不要。Configures/SwaggerConfig.csのコメントアウトと対）。
    /// </summary>
    public Task<Dictionary<string, Claim>?> TryGetClaimsAsync(string? authorizationHeaderValue)
    {
        const string bearerPrefix = "Bearer ";
        if (authorizationHeaderValue is null || !authorizationHeaderValue.StartsWith(bearerPrefix, StringComparison.Ordinal))
        {
            return Task.FromResult<Dictionary<string, Claim>?>(null);
        }

        // "Bearer "プレフィックスを除いた部分（トークン本体）だけを取り出す
        return TryGetClaimsFromTokenAsync(authorizationHeaderValue.Substring(bearerPrefix.Length));
    }

    /// <summary>
    /// JWT文字列を直接検証し、クレーム一覧を取り出す（Cookie等、ヘッダを介さない用途向け）
    /// </summary>
    public async Task<Dictionary<string, Claim>?> TryGetClaimsFromTokenAsync(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var result = await _handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            IssuerSigningKey = _signingKey,
            ValidateIssuer = false,
            ValidateAudience = false,
        });

        if (!result.IsValid)
        {
            return null;
        }

        return result.ClaimsIdentity.Claims.ToDictionary(c => c.Type, c => c);
    }
}
