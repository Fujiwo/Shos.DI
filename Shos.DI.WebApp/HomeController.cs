namespace Shos.DI.WebApp
{
    public class HomeController(ContextX context, HogeHoge hogeHoge)
    {
        readonly ContextX context = context;

        public string Index() => $"{nameof(Index)}: {context}";
        public string Detail() => $"{nameof(Detail)}: {context}";
    }

    public class HogeHoge
    { }

    public class ContextX(ContextOptionY option)
    {
        readonly ContextOptionY option = option;

        public override string ToString() => $"{nameof(ContextX)}({nameof(ContextOptionY)}: {option})";
    }

    public class ContextOptionY
    {
        public override string ToString() => $"Option-Y";
    }
}