using System.Text.Json;

namespace Rezber.CLI;

internal sealed record CliConfig(string ServerUrl, string Token);

internal sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".Rezber",
        "config.json");

    public async Task SaveAsync(CliConfig config)
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(FilePath, JsonSerializer.Serialize(config, JsonOptions));
    }

    public async Task<CliConfig?> LoadAsync()
    {
        if (!File.Exists(FilePath))
            return null;

        await using var stream = File.OpenRead(FilePath);
        return await JsonSerializer.DeserializeAsync<CliConfig>(stream);
    }
}