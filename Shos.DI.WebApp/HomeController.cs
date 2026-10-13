namespace Shos.DI.WebApp;

// hogeHoge is intentionally unused: it shows that every constructor parameter is resolved, used or not.
#pragma warning disable CS9113 // Parameter is unread
public class HomeController(ContextX context, HogeHoge hogeHoge)
{
    public string Index() => $"{nameof(Index)}: {context}";
    public string Detail() => $"{nameof(Detail)}: {context}";
}
#pragma warning restore CS9113

public class HogeHoge
{}

public class ContextX(ContextOptionY option)
{
    public override string ToString() => $"{nameof(ContextX)}({nameof(ContextOptionY)}: {option})";
}

public class ContextOptionY
{
    public override string ToString() => "Option-Y";
}