using System.Diagnostics;

namespace Rezber.CLI;

internal sealed class PackageInstallCommand(ConfigStore configStore) : ICliCommand
{
    public IReadOnlyCollection<string> Aliases { get; } = ["package"];

    public async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0 || CliApplication.IsHelp(args[0]))
        {
            PrintHelp();
            return 0;
        }

        if (!args[0].Equals("install", StringComparison.OrdinalIgnoreCase))
            return CliApplication.Fail($"Unknown package command '{args[0]}'. Use 'Rezber package install'.");

        var installArgs = args[1..];
        if (installArgs.Length == 0 || CliApplication.IsHelp(installArgs[0]))
        {
            PrintHelp();
            return 0;
        }

        var packageName = installArgs[0];
        var version = GetOption(installArgs, "--version");
        if (string.IsNullOrWhiteSpace(version))
            return CliApplication.Fail("The --version option is required.");

        var config = await configStore.LoadAsync();
        if (config == null)
            return CliApplication.Fail("Run 'Rezber login' before installing a package.");
        if (!Uri.TryCreate(config.ServerUrl, UriKind.Absolute, out var serverUri))
            return CliApplication.Fail("The saved Rezber server URL is invalid. Run 'Rezber login' again.");

        var packageUri = new Uri(
            serverUri.ToString().TrimEnd('/') + "/api/packages/" +
            Uri.EscapeDataString(packageName) + "/" + Uri.EscapeDataString(version) + "/download");
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), $"Rezber-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(packageUri);
            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync();
                return CliApplication.Fail(string.IsNullOrWhiteSpace(detail)
                    ? $"Rezber returned {(int)response.StatusCode} ({response.StatusCode})."
                    : $"Rezber returned {(int)response.StatusCode} ({response.StatusCode}): {detail}");
            }

            var packagePath = Path.Combine(temporaryDirectory, "package.nupkg");
            await using (var packageFile = File.Create(packagePath))
                await response.Content.CopyToAsync(packageFile);

            var source = $"{temporaryDirectory};https://api.nuget.org/v3/index.json";
            var startInfo = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("add");
            startInfo.ArgumentList.Add("package");
            startInfo.ArgumentList.Add(packageName);
            startInfo.ArgumentList.Add("--version");
            startInfo.ArgumentList.Add(version);
            startInfo.ArgumentList.Add("--source");
            startInfo.ArgumentList.Add(source);

            using var process = Process.Start(startInfo);
            if (process == null)
                return CliApplication.Fail("Could not start the .NET SDK.");

            await process.WaitForExitAsync();
            return process.ExitCode;
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private static string? GetOption(string[] args, string option)
    {
        var index = Array.FindIndex(args, arg => string.Equals(arg, option, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  Rezber package install <package-name> --version <version>");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("  Rezber package install Fundation.Abstractions --version 1.0.0");
        Console.WriteLine();
        Console.WriteLine("Run this command from a .NET project directory.");
    }
}