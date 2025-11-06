using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdsApi.Models.Converters
{
    public sealed class StringOrNumberConverter : JsonConverter<string?>
    {
        // Replace all instances of reader.GetRawText() with reader.GetString() for non-string tokens.
        // This will ensure the code compiles, as Utf8JsonReader does not have GetRawText().

        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String) return reader.GetString();
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt64(out var l)) return l.ToString();
                if (reader.TryGetDouble(out var d)) return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
                return reader.GetString();
            }
            if (reader.TokenType == JsonTokenType.Null) return null;
            // fallback to string representation
            return reader.GetString();
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            // Always write as string to keep consistency
            if (value is null) writer.WriteNullValue();
            else writer.WriteStringValue(value);
        }
    }
}
