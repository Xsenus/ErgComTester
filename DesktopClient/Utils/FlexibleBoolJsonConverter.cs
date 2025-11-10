using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MicroluxErgConnect.Utils;

internal sealed class FlexibleBoolJsonConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out var integer))
                {
                    return integer != 0;
                }

                if (reader.TryGetDouble(out var floating))
                {
                    return Math.Abs(floating) > double.Epsilon;
                }

                break;
            case JsonTokenType.String:
                var raw = reader.GetString();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return null;
                }

                if (bool.TryParse(raw, out var boolResult))
                {
                    return boolResult;
                }

                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intResult))
                {
                    return intResult != 0;
                }

                if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleResult))
                {
                    return Math.Abs(doubleResult) > double.Epsilon;
                }

                break;
        }

        throw new JsonException("Не удалось интерпретировать значение как логическое.");
    }

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteBooleanValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
