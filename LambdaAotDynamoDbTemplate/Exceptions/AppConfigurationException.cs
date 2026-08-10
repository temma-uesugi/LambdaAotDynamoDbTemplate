using System.Runtime.CompilerServices;

namespace Exceptions;

/// <summary>
/// 環境変数・SSM・Secrets Managerなど設定値/シークレットの取得に失敗した場合の例外
/// </summary>
public sealed class AppConfigurationException : AppException
{
    /// <summary>
    /// コンストラクタ
    /// </summary>
    // ReSharper disable once ExplicitCallerInfoArgument
    public AppConfigurationException(
        string message,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string filePath = "")
        : base(message, lineNumber, filePath)
    {
    }
}
