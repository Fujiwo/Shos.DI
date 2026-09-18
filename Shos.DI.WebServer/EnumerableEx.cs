namespace Shos.DI.WebServer;

public static class EnumerableEx
{
    public static void ForEach<T>(this IEnumerable<T> @this, Action<T> action)
    {
        foreach (var item in @this)
            action(item);
    }
}
