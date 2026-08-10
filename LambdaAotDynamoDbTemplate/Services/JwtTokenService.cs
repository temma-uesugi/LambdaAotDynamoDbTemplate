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
    /// ユーザIDのJWTを発行する
    /// </summary>
    public string IssueToken(Guid userId, TimeSpan expiration)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())]),
            Expires = DateTime.UtcNow.Add(expiration),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256),
        };
        return _handler.CreateToken(descriptor);
    }

    /// <summary>
    /// AuthorizationヘッダのBearerトークンを検証し、ユーザIDを取り出す。
    /// 現状どこからも呼ばれていないが、Bearerヘッダ方式で認証したい案件向けにあえて残してある
    /// （Cookie方式を使うならこのメソッドは不要。Configures/SwaggerConfig.csのコメントアウトと対）。
    /// </summary>
    public Task<Guid?> TryGetUserIdAsync(string? authorizationHeaderValue)
    {
        const string bearerPrefix = "Bearer ";
        if (authorizationHeaderValue is null || !authorizationHeaderValue.StartsWith(bearerPrefix, StringComparison.Ordinal))
        {
            return Task.FromResult<Guid?>(null);
        }

        // "Bearer "プレフィックスを除いた部分（トークン本体）だけを取り出す
        return TryGetUserIdFromTokenAsync(authorizationHeaderValue.Substring(bearerPrefix.Length));
    }

    /// <summary>
    /// JWT文字列を直接検証し、ユーザIDを取り出す（Cookie等、ヘッダを介さない用途向け）
    /// </summary>
    public async Task<Guid?> TryGetUserIdFromTokenAsync(string? token)
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

        var sub = result.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }
}
