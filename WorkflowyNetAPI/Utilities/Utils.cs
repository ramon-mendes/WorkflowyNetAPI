using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorkflowyNetAPI.Utilities
{
    /// <summary>
    /// Converts Unix epoch seconds <-> DateTime (UTC).
    /// Accepts JSON numeric values (seconds) or stringified numbers.
    /// Serializes DateTime as epoch seconds (number).
    /// </summary>
    public sealed class UnixEpochDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // null should not reach here for non-nullable DateTime
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt64(out long seconds))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
                }
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (long.TryParse(s, out long seconds))
                    return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
            }

            throw new JsonException("Invalid Unix epoch timestamp for DateTime.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            long seconds = new DateTimeOffset(utc).ToUnixTimeSeconds();
            writer.WriteNumberValue(seconds);
        }
    }

    /// <summary>
    /// Nullable variant for DateTime? values.
    /// </summary>
    public sealed class NullableUnixEpochDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt64(out long seconds))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
                }
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (string.IsNullOrWhiteSpace(s)) return null;
                if (long.TryParse(s, out long seconds))
                    return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
            }

            throw new JsonException("Invalid Unix epoch timestamp for nullable DateTime.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            var utc = value.Value.Kind == DateTimeKind.Utc ? value.Value : value.Value.ToUniversalTime();
            long seconds = new DateTimeOffset(utc).ToUnixTimeSeconds();
            writer.WriteNumberValue(seconds);
        }
    }
}