using System.Collections.Concurrent;
using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Exceptions;

namespace Services;

/// <summary>
/// アプリで使用するシークレットの取得を集約する。
/// Secrets Manager・Parameter Storeのどちらから取るかはメソッドで使い分ける（混在利用も可）。
/// 一度取得した値・パース結果はキャッシュし、以降は再利用する。
/// ローカル環境ではどちらのメソッドもダミー値を返す。
/// </summary>
public class SecretService
{
    // Note: HS256(JWT署名)の鍵としても使われるため、256bit(32バイト)以上の長さが必要
    private const string DummySecret = "local-development-dummy-secret-key-padding";
    private const string SecretsManagerPrefix = "sm-";
    private const string ParameterStorePrefix = "ps-";

    private readonly bool _isDevelopment;
    private readonly ConcurrentDictionary<string, Lazy<Task<string>>> _rawCache = new();
    private readonly ConcurrentDictionary<string, Lazy<Task<IReadOnlyDictionary<string, string>>>> _jsonCache = new();

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public SecretService(IHostEnvironment environment)
    {
        _isDevelopment = environment.IsDevelopment();
    }

    /// <summary>
    /// Secrets Managerからシークレット文字列を取得する
    /// </summary>
    public Task<string> GetBySecretsManagerAsync(string secretId)
    {
        return GetRawAsync(SecretsManagerPrefix + secretId, () => FetchFromSecretsManagerAsync(secretId));
    }

    /// <summary>
    /// Secrets ManagerからJSON形式のシークレットを取得し、指定キーの値を返す
    /// </summary>
    public async Task<string> GetBySecretsManagerAsync(string secretId, string jsonKey)
    {
        if (_isDevelopment)
        {
            return DummySecret;
        }

        var json = await _jsonCache
            .GetOrAdd(secretId, _ => new Lazy<Task<IReadOnlyDictionary<string, string>>>(() => FetchAndParseJsonAsync(secretId)))
            .Value;

        return json.TryGetValue(jsonKey, out var value)
            ? value
            : throw new AppConfigurationException($"シークレット'{secretId}'に'{jsonKey}'が含まれていません。");
    }

    /// <summary>
    /// Parameter Storeからシークレット文字列を取得する
    /// </summary>
    public Task<string> GetByParameterStoreAsync(string parameterName)
    {
        return GetRawAsync(ParameterStorePrefix + parameterName, () => FetchFromParameterStoreAsync(parameterName));
    }

    /// <summary>
    /// キャッシュ経由で生のシークレット文字列を取得する
    /// </summary>
    private Task<string> GetRawAsync(string cacheKey, Func<Task<string>> fetch)
    {
        if (_isDevelopment)
        {
            return Task.FromResult(DummySecret);
        }

        return _rawCache.GetOrAdd(cacheKey, _ => new Lazy<Task<string>>(fetch)).Value;
    }

    /// <summary>
    /// Secrets Managerの値をJSONとして取得しパースする
    /// </summary>
    private async Task<IReadOnlyDictionary<string, string>> FetchAndParseJsonAsync(string secretId)
    {
        var raw = await GetRawAsync(SecretsManagerPrefix + secretId, () => FetchFromSecretsManagerAsync(secretId));
        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty);
    }

    /// <summary>
    /// Secrets Manager APIを呼び出す
    /// </summary>
    private static async Task<string> FetchFromSecretsManagerAsync(string secretId)
    {
        using var client = new AmazonSecretsManagerClient();
        var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId });
        return response.SecretString
            ?? throw new AppConfigurationException($"シークレット'{secretId}'の中身が空です。");
    }

    /// <summary>
    /// Parameter Store APIを呼び出す
    /// </summary>
    private static async Task<string> FetchFromParameterStoreAsync(string parameterName)
    {
        using var client = new AmazonSimpleSystemsManagementClient();
        var response = await client.GetParameterAsync(new GetParameterRequest
        {
            Name = parameterName,
            WithDecryption = true,
        });
        return response.Parameter.Value;
    }
}
