using System.Net;
using System.Reflection;

namespace Shos.DI.WebServer
{
    class WebAppManager
    {
        DIContainer container = new();
        IEnumerable<Type>? types = null;

        public WebAppManager()
        {
            const string appFileEnd = ".dll";
            var files = Directory.GetFiles("Apps").Where(fileName => fileName.EndsWith(appFileEnd));
            var assemblies = files.Select(file => Assembly.LoadFrom(file));
            var types = assemblies.Select(assembly => assembly.GetTypes())
                                  .SelectMany(_ => _);
            this.types = types;
            types.ToList().ForEach(type => container.Register(type));
        }


        public string? GetView(HttpListenerRequest request)
        {
            if (request.Url is null)
                return null;

            var (contollerName, actionName) = Split(request.Url);
            return GetView(controllerName: contollerName, actionName: actionName);
        }

        string? GetView(string controllerName, string actionName)
        {
            const string controllerSuffix = "Controller";
            var controllerType = types?.FirstOrDefault(type => type.Name == $"{controllerName}{controllerSuffix}");
            if (controllerType is null)
                return null;

            var controller = container.GetInstance(controllerType.FullName ?? "");
            if (controller is null)
                return null;

            var action = controllerType.GetMethod(actionName);
            return action?.Invoke(controller, new object[0]) as string;
        }

        static (string controllerName, string actionName) Split(Uri uri)
        {
            var uriAbsoluteUri = uri.AbsoluteUri;
            var texts          = uriAbsoluteUri.Split('/').Skip(3).Take(2).ToArray();
            return texts.Length switch {
                0 => ("", ""),
                1 => (texts[0], ""),
                _ => (texts[0], texts[1])
            };
        }
    }
}
