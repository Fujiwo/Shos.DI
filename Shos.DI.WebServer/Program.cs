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
        const char quitCharacter = 'q';
        while (Console.ReadKey().KeyChar != quitCharacter)
            ;
    }
}
