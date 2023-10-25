namespace Shos.DI.WebApp
{
    public class HomeController
    {
        readonly Context context;

        public HomeController(Context context) => this.context = context;

        public string Index() => $"Index: {context}";
        public string Detail() => $"Detail: {context}";
    }

    public class Context
    {
        readonly ContextOption option;

        public Context(ContextOption option) => this.option = option;

        public override string ToString() => $"{nameof(Context)}: 1 ({nameof(ContextOption)}: {option})";
    }

    public class ContextOption
    {
        public override string ToString() => "1";
    }
}