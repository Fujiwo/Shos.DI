namespace Shos.DI.WebServer;

static class Program
{
    static void Main()
    {
        try {
            using var server = new SampleServer();
            server.Start("http://+:8080/", "https://+:44301/");
            Wait();
        } catch (Exception e) {
            Console.WriteLine($"Error: {e}");
        }
    }

    static void Wait()
    {
        if (Console.IsInputRedirected) {
            // Console.ReadKey cannot be used without a console (CI, containers, nohup, ...): wait for Ctrl+C instead.
            using var quit = new ManualResetEventSlim();
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true;
                quit.Set();
            };
            Console.WriteLine("Press Ctrl+C to quit.");
            quit.Wait();
            return;
        }

        const char quitCharacter = 'q';
        Console.WriteLine($"Press '{quitCharacter}' to quit.");
        while (Console.ReadKey().KeyChar != quitCharacter)
            ;
    }
}
