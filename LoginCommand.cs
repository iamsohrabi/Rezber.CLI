namespace Rezber.CLI;

internal sealed class LoginCommand(ConfigStore configStore) : ICliCommand
{
    public IReadOnlyCollection<string> Aliases { get; } = ["login"];

    public async Task<int> ExecuteAsync(string[] args)
    {
        Console.Write("Rezber server URL: ");
        var server = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(server))
            return CliApplication.Fail("A server URL is required.");

        Console.Write("API token: ");
        var token = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(token))
            return CliApplication.Fail("An API token is required.");

        var config = new CliConfig(server.TrimEnd('/'), token);
        await configStore.SaveAsync(config);
        Console.WriteLine($"Saved credentials for {config.ServerUrl}");
        return 0;
    }
}