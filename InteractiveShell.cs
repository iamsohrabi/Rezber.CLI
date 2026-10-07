namespace Rezber.CLI;

internal sealed class InteractiveShell(ICommandDispatcher dispatcher)
{
    public async Task<int> RunAsync()
    {
        Console.WriteLine("Rezber CLI interactive mode. Type 'help' for commands or 'exit' to quit.");
        while (true)
        {
            Console.Write("Rezber> ");
            var line = Console.ReadLine();
            if (line == null || line.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                line.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase))
                return 0;

            var args = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (args.Length == 0)
                continue;

            try
            {
                var exitCode = await dispatcher.DispatchAsync(args, interactive: true);
                if (exitCode != 0)
                    Console.WriteLine($"Command failed with exit code {exitCode}.");
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Error: {exception.Message}");
            }
        }
    }
}
