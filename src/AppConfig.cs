using System.Text.Json;
using System.Text.Json.Serialization;

namespace DpiTray;

internal sealed class AppConfig
{
    [JsonPropertyName("selectedStrategy")]
    public string SelectedStrategy { get; set; } = "general";

    [JsonPropertyName("autoStart")]
    public bool AutoStart { get; set; }

    [JsonPropertyName("autoStartStrategy")]
    public bool AutoStartStrategy { get; set; } = true;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static AppConfig Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            }
        }
        catch
        {
            // ignore
        }

        var cfg = new AppConfig();
        cfg.Save(path);
        return cfg;
    }

    public void Save(string path)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }
}
