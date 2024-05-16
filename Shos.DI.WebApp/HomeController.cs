namespace Shos.DI.WebApp
{
    public class HomeController(Context context)
    {
        readonly Context context = context;

        public string Index() => $"{nameof(Index)}: {context}";
        public string Detail() => $"{nameof(Detail)}: {context}";
    }

    public class Context(ContextOption option)
    {
        readonly ContextOption option = option;

        public override string ToString() => $"{nameof(Context)}-X ({nameof(ContextOption)}: {option})";
    }

    public class ContextOption
    {
        public override string ToString() => $"{nameof(ContextOption)}-X";
    }
}