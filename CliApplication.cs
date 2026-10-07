using System.Globalization;

namespace Rezber.CLI;

internal sealed class CliApplication : ICommandDispatcher
{
    private readonly IReadOnlyDictionary<string, ICliCommand> _commands;
    private readonly InteractiveShell _interactiveShell;

    public CliApplication()
    {
        var configStore = new ConfigStore();
        var commands = new ICliCommand[]
        {
            new LoginCommand(configStore),
            new PackageInstallCommand(configStore),
            new PushCommand(configStore),
            new ServiceCommand()
        };

        _commands = commands
            .SelectMany(command => command.Aliases.Select(alias => (alias, command)))
            .ToDictionary(item => item.alias, item => item.command, StringComparer.OrdinalIgnoreCase);
        _interactiveShell = new InteractiveShell(this);
    }

    public Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
            return _interactiveShell.RunAsync();

        return DispatchAsync(args, interactive: false);
    }

    public Task<int> DispatchAsync(string[] args, bool interactive)
    {
        var commandName = args[0];
        if (IsHelp(commandName))
        {
            PrintHelp();
            return Task.FromResult(0);
        }

        if(IsClearConsole(commandName))
        {
            Console.Clear();
            return Task.FromResult(0);
        }

        if (!_commands.TryGetValue(commandName, out var command))
            return Task.FromResult(Fail(interactive
                ? $"Unknown command '{commandName}'. Type 'help'."
                : $"Unknown command '{commandName}'. Run 'Rezber help'."));

        return command.ExecuteAsync(args[1..]);
    }

    internal static bool IsClearConsole(string value) =>
        value.Equals("clear", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("cls", StringComparison.OrdinalIgnoreCase);

    internal static bool IsHelp(string value) =>
        value.Equals("help", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("-h", StringComparison.OrdinalIgnoreCase);

    internal static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Rezber CLI");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  Rezber login");
        Console.WriteLine("  Rezber package install <package-name> --version <version>");
        Console.WriteLine("  Rezber push <file> --name <package-name> --version <version>");
        Console.WriteLine("  Rezber service");
        Console.WriteLine();
        Console.WriteLine("With no arguments, Rezber starts interactive mode.");
        Console.WriteLine("Run 'Rezber package install --help' for package installation options.");
        Console.WriteLine("Run 'Rezber push --help' for push options.");
    }
}

internal interface ICommandDispatcher
{
    Task<int> DispatchAsync(string[] args, bool interactive);
}

internal interface ICliCommand
{
    IReadOnlyCollection<string> Aliases { get; }
    Task<int> ExecuteAsync(string[] args);
}