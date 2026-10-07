namespace Rezber.CLI;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            return await new CliApplication().RunAsync(args);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Error: {exception.Message}");
            return 1;
        }
    }
}
