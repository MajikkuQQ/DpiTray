using System.Text.Json;
using System.Text.Json.Serialization;

namespace DpiTray;

internal sealed class StrategyDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("args")]
    public List<string> Args { get; set; } = new();

    public static List<StrategyDefinition> LoadAll(string strategiesDir)
    {
        var list = new List<StrategyDefinition>();
        if (!Directory.Exists(strategiesDir))
            return list;

        foreach (var file in Directory.EnumerateFiles(strategiesDir, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var item = JsonSerializer.Deserialize<StrategyDefinition>(File.ReadAllText(file));
                if (item != null && !string.IsNullOrWhiteSpace(item.Id))
                    list.Add(item);
            }
            catch
            {
                // skip
            }
        }

        return list;
    }

    public IEnumerable<string> ExpandArgs(string binDir, string listsDir)
    {
        var bin = EnsureTrailingSep(binDir);
        var lists = EnsureTrailingSep(listsDir);

        // winws/cygwin стабильнее на forward-slash путях
        bin = bin.Replace('\\', '/');
        lists = lists.Replace('\\', '/');

        foreach (var a in Args)
        {
            yield return a
                .Replace("{BIN}", bin, StringComparison.OrdinalIgnoreCase)
                .Replace("{LISTS}", lists, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string EnsureTrailingSep(string path)
        => path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
}
