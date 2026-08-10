using System.Runtime.CompilerServices;

namespace Logging;

/// <summary>
/// DIせずどこからでも呼べる静的なログ出力の入口。
/// Program.cs起動時に一度だけInitializeを呼んでおけば、以降はAppLog.Debug(...)等で使える。
/// 内部の実処理はIAppLoggerに委譲する（DIで受け取りたい場所ではIAppLoggerを直接使えばよい）。
/// </summary>
public static class AppLog
{
    private static IAppLogger? _logger;

    /// <summary>
    /// アプリ起動時に一度だけ呼び出し、内部で使うIAppLoggerを設定する
    /// </summary>
    public static void Initialize(IAppLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Debugレベルでログ出力する
    /// </summary>
    public static void Debug(
        string message,
        object? arg1 = null,
        object? arg2 = null,
        object? arg3 = null,
        object? arg4 = null,
        object? arg5 = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string filePath = "")
        => GetLogger().Debug(message, arg1, arg2, arg3, arg4, arg5, lineNumber, filePath);

    /// <summary>
    /// Informationレベルでログ出力する
    /// </summary>
    public static void Information(
        string message,
        object? arg1 = null,
        object? arg2 = null,
        object? arg3 = null,
        object? arg4 = null,
        object? arg5 = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string filePath = "")
        => GetLogger().Information(message, arg1, arg2, arg3, arg4, arg5, lineNumber, filePath);

    /// <summary>
    /// Warningレベルでログ出力する
    /// </summary>
    public static void Warning(
        string message,
        object? arg1 = null,
        object? arg2 = null,
        object? arg3 = null,
        object? arg4 = null,
        object? arg5 = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string filePath = "")
        => GetLogger().Warning(message, arg1, arg2, arg3, arg4, arg5, lineNumber, filePath);

    /// <summary>
    /// Errorレベルでログ出力する
    /// </summary>
    public static void Error(
        Exception? exception,
        string message,
        object? arg1 = null,
        object? arg2 = null,
        object? arg3 = null,
        object? arg4 = null,
        object? arg5 = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string filePath = "")
        => GetLogger().Error(exception, message, arg1, arg2, arg3, arg4, arg5, lineNumber, filePath);

    private static IAppLogger GetLogger()
        => _logger ?? throw new InvalidOperationException("AppLog.Initialize が呼び出されていません。Program.csで一度だけ呼び出す必要があります。");
}
