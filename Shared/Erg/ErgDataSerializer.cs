using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ErgData;

public static class ErgDataSerializer
{
    public static JsonSerializerOptions JsonOptions { get; }

    static ErgDataSerializer()
    {
        JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public static string ToJson(ErgPatient patient)
        => JsonSerializer.Serialize(patient, JsonOptions);

    public static void SaveJson(string path, ErgPatient patient)
    {
        var json = ToJson(patient);
        File.WriteAllText(path, json, Encoding.UTF8);
    }
}
