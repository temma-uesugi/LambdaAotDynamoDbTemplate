using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Logging;

/// <inheritdoc />
public sealed class AppLogger : IAppLogger
{
    private readonly ILogger<AppLogger> _logger;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public AppLogger(ILogger<AppLogger> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void Debug(
        string message,
        object? arg1 = null,
        object? arg2 = null,
        object? arg3 = null,
        object? arg4 = null,
        object? arg5 = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string filePath = "")
        => Log(LogLevel.Debug, null, message, arg1, arg2, arg3, arg4, arg5, lineNumber, filePath);

    /// <inheritdoc />
    public void Information(
        string message,
        object? arg1 = null,
        object? arg2 = null,
        object? arg3 = null,
        object? arg4 = null,
        object? arg5 = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string filePath = "")
        => Log(LogLevel.Information, null, message, arg1, arg2, arg3, arg4, arg5, lineNumber, filePath);

    /// <inheritdoc />
    public void Warning(
        string message,
        object? arg1 = null,
        object? arg2 = null,
        object? arg3 = null,
        object? arg4 = null,
        object? arg5 = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string filePath = "")
        => Log(LogLevel.Warning, null, message, arg1, arg2, arg3, arg4, arg5, lineNumber, filePath);

    /// <inheritdoc />
    public void Error(
        Exception? exception,
        string message,
        object? arg1 = null,
        object? arg2 = null,
        object? arg3 = null,
        object? arg4 = null,
        object? arg5 = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string filePath = "")
        => Log(LogLevel.Error, exception, message, arg1, arg2, arg3, arg4, arg5, lineNumber, filePath);

    /// <summary>
    /// 呼び出し元位置と引数をメッセージテンプレートに組み立ててILoggerへ渡す
    /// </summary>
    private void Log(
        LogLevel level,
        Exception? exception,
        string message,
        object? arg1,
        object? arg2,
        object? arg3,
        object? arg4,
        object? arg5,
        int lineNumber,
        string filePath)
    {
        // 無効なレベルなら引数のシリアライズ自体を省略する
        if (!_logger.IsEnabled(level))
        {
            return;
        }

        var templateBuilder = new StringBuilder("{CallSite} {Message}");
        var values = new List<object?> { GetCallSite(filePath, lineNumber), message };

        AppendArg(templateBuilder, values, 1, arg1);
        AppendArg(templateBuilder, values, 2, arg2);
        AppendArg(templateBuilder, values, 3, arg3);
        AppendArg(templateBuilder, values, 4, arg4);
        AppendArg(templateBuilder, values, 5, arg5);

        _logger.Log(level, exception, templateBuilder.ToString(), values.ToArray());
    }

    // コンソールのシンプルフォーマッタが1行目に付けるレベル表記("dbug: "等)の幅に合わせたインデント。
    // フォーマッタはメッセージ内の改行までは追ってインデントしてくれないため、ここで揃える。
    private const string NewLineIndent = "\n      ";

    /// <summary>
    /// 引数が渡されていればテンプレートと値リストへ追加する（1引数につき1行）
    /// </summary>
    private static void AppendArg(StringBuilder templateBuilder, List<object?> values, int index, object? arg)
    {
        if (arg is null)
        {
            return;
        }

        templateBuilder.Append(NewLineIndent).Append("{Arg").Append(index).Append('}');
        values.Add(LogArgSerializer.Serialize(arg));
    }

    /// <summary>
    /// ファイル名:行番号の形式で呼び出し元を作る（Path.GetFileNameのアロケーションを避ける）
    /// </summary>
    private static string GetCallSite(string filePath, int lineNumber)
    {
        var lastSlash = filePath.LastIndexOfAny(['\\', '/']);
        var fileName = lastSlash >= 0 ? filePath[(lastSlash + 1)..] : filePath;
        return $"{fileName}:{lineNumber}";
    }
}
