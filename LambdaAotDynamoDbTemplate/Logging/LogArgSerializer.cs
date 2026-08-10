using System.Text;
using System.Text.Json;

namespace Logging;

/// <summary>
/// ログ引数をシリアライズする。
/// プリミティブ型はそのまま、オブジェクトはApiSerializationContext経由でJSON化し、
/// キー名がトークン・秘密情報等に該当するプロパティはマスクする。
/// NativeAOT環境では未登録の型をリフレクションで拾えないため、
/// 未登録型は例外を投げず "unserializable: 型名" にフォールバックする。
/// </summary>
internal static class LogArgSerializer
{
    // JSONのプロパティ名にこの単語が部分一致（大文字小文字区別なし）したら値を"***"にマスクする対象キーワード。
    // 機微情報を扱うプロパティを増やす場合はここに追記する。
    private static readonly string[] SensitiveKeywords =
    [
        "token", "secret", "password", "cookie", "jwt", "recaptcha", "authorization"
    ];

    // "unserializable"部分だけ赤背景・白文字にするANSIエスケープシーケンス（コンソール表示時に目立たせるため）
    private const string UnserializableColor = "\x1b[41m\x1b[97m";
    private const string ColorReset = "\x1b[0m";

    /// <summary>
    /// 引数をログ出力用の値に変換する
    /// </summary>
    public static object Serialize(object arg)
    {
        // 日付・時刻系はApiSerializationContextへの登録なしにISO 8601表記へ変換する
        // （DateTimeOffset/DateOnly/TimeOnlyはTypeCode.Object扱いのため、下のTypeCode判定だけでは素通りできない）
        switch (arg)
        {
            case DateTime dateTime:
                return dateTime.ToString("O");
            case DateTimeOffset dateTimeOffset:
                return dateTimeOffset.ToString("O");
            case DateOnly dateOnly:
                return dateOnly.ToString("O");
            case TimeOnly timeOnly:
                return timeOnly.ToString("O");
        }

        var type = arg.GetType();
        if (Type.GetTypeCode(type) != TypeCode.Object)
        {
            return arg;
        }

        // ApiSerializationContextに未登録の型はnullが返る（ログに渡す型は都度登録する運用のため、気付けるようマーカーを残す）
        var typeInfo = ApiSerializationContext.Default.GetTypeInfo(type);
        if (typeInfo is null)
        {
            return $"{UnserializableColor}unserializable{ColorReset}: {type.Name}";
        }

        using var document = JsonSerializer.SerializeToDocument(arg, typeInfo);
        return WriteMasked(document.RootElement);
    }

    /// <summary>
    /// JSON要素をマスク処理しつつ文字列化する
    /// </summary>
    private static string WriteMasked(JsonElement element)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteMasked(element, writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteMasked(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    if (IsSensitiveKey(property.Name))
                    {
                        writer.WriteStringValue("***");
                    }
                    else
                    {
                        WriteMasked(property.Value, writer);
                    }
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteMasked(item, writer);
                }

                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static bool IsSensitiveKey(string key)
    {
        foreach (var keyword in SensitiveKeywords)
        {
            if (key.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
