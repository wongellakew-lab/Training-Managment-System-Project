using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TmsApi.Api.Converters;

/// <summary>
/// Robust JSON converter for Nullable DateTime properties.
/// Gracefully handles null, empty strings (""), whitespace, "null", "undefined",
/// ISO-8601 strings, and common standard date formats without throwing JsonException.
/// </summary>
public class NullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    private static readonly string[] FallbackFormats = new[]
    {
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFZ",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "MM/dd/yyyy",
        "M/d/yyyy"
    };

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrWhiteSpace(stringValue) ||
                string.Equals(stringValue.Trim(), "null", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(stringValue.Trim(), "undefined", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var trimmed = stringValue.Trim();

            if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
            {
                return date;
            }

            if (DateTime.TryParse(trimmed, CultureInfo.CurrentCulture, DateTimeStyles.None, out var localDate))
            {
                return localDate;
            }

            foreach (var format in FallbackFormats)
            {
                if (DateTime.TryParseExact(trimmed, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exactDate))
                {
                    return exactDate;
                }
            }

            // If none matched, safely return null instead of throwing an unhandled deserialization 400 JsonException
            return null;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

/// <summary>
/// Robust JSON converter for non-nullable DateTime properties.
/// Gracefully handles empty strings and whitespace without throwing JsonException.
/// </summary>
public class CustomDateTimeJsonConverter : JsonConverter<DateTime>
{
    private static readonly string[] FallbackFormats = new[]
    {
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFZ",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "MM/dd/yyyy",
        "M/d/yyyy"
    };

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrWhiteSpace(stringValue) ||
                string.Equals(stringValue.Trim(), "null", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(stringValue.Trim(), "undefined", StringComparison.OrdinalIgnoreCase))
            {
                return default;
            }

            var trimmed = stringValue.Trim();

            if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
            {
                return date;
            }

            if (DateTime.TryParse(trimmed, CultureInfo.CurrentCulture, DateTimeStyles.None, out var localDate))
            {
                return localDate;
            }

            foreach (var format in FallbackFormats)
            {
                if (DateTime.TryParseExact(trimmed, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exactDate))
                {
                    return exactDate;
                }
            }

            return default;
        }

        return reader.GetDateTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
    }
}
