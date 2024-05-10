namespace Shos.DI.WebServer;

static class Program
{
    static void Main()
    {
        using var server = new SampleServer();
        server.Start(
            "http://+:8080/",
            "https://+:44301/");
        char ch;
        while ((ch = Console.ReadKey().KeyChar) != 'q')
            ;
    }
}
