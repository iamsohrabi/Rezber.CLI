namespace Rezber.CLI;

internal sealed class ServiceCommand : ICliCommand
{
    public IReadOnlyCollection<string> Aliases { get; } = ["service", "daemon"];

    public async Task<int> ExecuteAsync(string[] args)
    {
        Console.WriteLine("Rezber CLI service started. Waiting for commands...");
        using var stopping = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            stopping.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;

        try
        {
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, stopping.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Rezber CLI service stopped.");
            }
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }

        return 0;
    }
}