namespace Shos.DI.WebServer;

class Program
{
    static void Main(string[] args)
    {
        var server = new SampleServer();
        server.Start(
            "http://+:8080/",
            "https://+:44300/");
        char ch;
        while ((ch = Console.ReadKey().KeyChar) != 'q')
            ;
        server.Stop();
    }
}
