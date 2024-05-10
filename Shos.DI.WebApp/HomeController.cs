namespace Shos.DI.WebApp
{
    public class HomeController(Context context)
    {
        readonly Context context = context;

        public string Index() => $"Index: {context}";
        public string Detail() => $"Detail: {context}";
    }

    public class Context(ContextOption option)
    {
        readonly ContextOption option = option;

        public override string ToString() => $"{nameof(Context)}: 1 ({nameof(ContextOption)}: {option})";
    }

    public class ContextOption
    {
        public override string ToString() => "1";
    }
}