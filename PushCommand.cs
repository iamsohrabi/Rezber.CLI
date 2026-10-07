using System.Net.Http.Headers;

namespace Rezber.CLI;

internal sealed class PushCommand(ConfigStore configStore) : ICliCommand
{
    public IReadOnlyCollection<string> Aliases { get; } = ["push"];

    public async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0 || CliApplication.IsHelp(args[0]))
        {
            PrintHelp();
            return 0;
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
            return CliApplication.Fail($"Package file not found: {filePath}");

        var name = GetOption(args, "--name");
        var version = GetOption(args, "--version");
        var config = await configStore.LoadAsync();

        if (config == null)
            return CliApplication.Fail("Run 'Rezber login' before pushing a package.");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version))
            return CliApplication.Fail("Both --name and --version are required.");

        var fileName = Path.GetFileName(filePath);
        var endpoint = string.Join('/',
            config.ServerUrl.TrimEnd('/'),
            "api/packages/push",
            Uri.EscapeDataString(name),
            Uri.EscapeDataString(version),
            Uri.EscapeDataString(fileName));

        await using var file = File.OpenRead(filePath);
        using var content = new StreamContent(file);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        using var request = new HttpRequestMessage(HttpMethod.Put, endpoint)
        {
            Content = content
        };
        request.Headers.Add("X-Rezber-ApiKey", config.Token);

        Console.WriteLine($"Uploading {fileName}...");
        using var client = new HttpClient();
        using var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Package published successfully.");
            return 0;
        }

        var body = await response.Content.ReadAsStringAsync();
        var detail = string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body;
        return CliApplication.Fail($"Server returned {(int)response.StatusCode} ({response.StatusCode}): {detail}");
    }

    private static string? GetOption(string[] args, string option)
    {
        var index = Array.FindIndex(args, arg => string.Equals(arg, option, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  Rezber push <file> --name <package-name> --version <version>");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("  Rezber push ./dist/my-package.zip --name my-package --version 1.0.0");
    }
}