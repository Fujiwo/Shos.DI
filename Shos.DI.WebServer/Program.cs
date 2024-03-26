namespace Shos.DI.WebServer;

class Program
{
    static void Main(string[] args)
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
