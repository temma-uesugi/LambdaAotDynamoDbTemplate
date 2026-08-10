using System.Text.Json.Serialization;
using Logging;

namespace Services;

/// <summary>
/// reCAPTCHA検証を行うサービス
/// </summary>
public class RecaptchaService
{
    private const string SiteVerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

    private readonly HttpClient _httpClient;
    private readonly string _secret;
    private readonly bool _isDevelopment;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public RecaptchaService(HttpClient httpClient, string secret, bool isDevelopment)
    {
        _httpClient = httpClient;
        _secret = secret;
        _isDevelopment = isDevelopment;
    }

    /// <summary>
    /// reCAPTCHAレスポンストークンを検証する
    /// </summary>
    public async Task<bool> VerifyAsync(string token, CancellationToken ct = default)
    {
        // ローカル環境では有効なトークン・シークレットを用意できないため、実API呼び出しをスキップして常に成功扱いにする
        if (_isDevelopment)
        {
            return true;
        }

        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = _secret,
                ["response"] = token,
            });

            using var response = await _httpClient.PostAsync(SiteVerifyUrl, content, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync(
                ApiSerializationContext.Default.RecaptchaSiteVerifyResponse, ct);

            if (result is null || !result.Success)
            {
                AppLog.Warning("reCAPTCHA検証がfalseまたは不正なレスポンスでした", result?.ErrorCodes);
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // タイムアウト・通信失敗時は検証失敗として扱う（在庫消費等、確定処理に繋がる用途では成功扱いにしないこと）
            AppLog.Error(ex, "reCAPTCHA検証のAPI呼び出しに失敗しました");
            return false;
        }
    }
}

/// <summary>
/// Google reCAPTCHA siteverify APIのレスポンス
/// </summary>
internal sealed record RecaptchaSiteVerifyResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("error-codes")] IReadOnlyList<string>? ErrorCodes
);
