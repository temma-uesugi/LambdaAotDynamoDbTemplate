using System.Runtime.CompilerServices;

namespace Logging;

/// <summary>
/// アプリケーション全体で使うログ出力インターフェース。
/// 呼び出し元のファイル:行番号を自動付与し、引数はプリミティブ以外JSON化して出力する。
/// </summary>
public interface IAppLogger
{
    /// <summary>
    /// Debugレベルでログ出力する
    /// </summary>
    void Debug(
        string message,
        object? arg1 = null,
        object? arg2 = null,
        object? arg3 = null,
        object? arg4 = null,
        object? arg5 = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string filePath = "");

    /// <summary>
    /// Informationレベルでログ出力する
    /// </summary>
    void Information(
        string message,
        object? arg1 = null,
        object? arg2 = null,
        object? arg3 = null,
        object? arg4 = null,
        object? arg5 = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string filePath = "");

    /// <summary>
    /// Warningレベルでログ出力する
    /// </summary>
    void Warning(
        string message,
        object? arg1 = null,
        object? arg2 = null,
        object? arg3 = null,
        object? arg4 = null,
        object? arg5 = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string filePath = "");

    /// <summary>
    /// Errorレベルでログ出力する
    /// </summary>
    void Error(
        Exception? exception,
        string message,
        object? arg1 = null,
        object? arg2 = null,
        object? arg3 = null,
        object? arg4 = null,
        object? arg5 = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string filePath = "");
}
