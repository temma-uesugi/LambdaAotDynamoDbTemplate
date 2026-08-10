using System.Runtime.CompilerServices;
using Logging;

namespace Exceptions;

/// <summary>
/// IAppLogger.Errorでのログ出力を伴うアプリ内例外。特に意味づけせず使う場合はこのままthrowしてよい
/// </summary>
public class AppException : Exception
{
    /// <summary>
    /// コンストラクタ。呼び出し元のファイル:行番号付きでErrorログを出力してから例外を構築する
    /// </summary>
    public AppException(
        string message,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string filePath = "")
        : base(message)
    {
        // Note: throw前のコンストラクタ内で呼んでいるためスタックトレースがまだ乗っておらず、
        // exceptionにthisを渡すと型名+メッセージの再掲にしかならない（[GetType().Name]で既にログ済み）ためnullにしている。
        // lineNumber/filePathは名前付き引数のまま渡すこと。位置引数にするとAppLog.Errorのarg1/arg2に
        // ズレて入り、[CallerLineNumber]/[CallerFilePath]がこの呼び出し自身の行を誤ってキャプチャしてしまう。
        AppLog.Error(null, $"[{GetType().Name}] {message}", lineNumber: lineNumber, filePath: filePath);
    }
}
