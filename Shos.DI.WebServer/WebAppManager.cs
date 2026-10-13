using System.Reflection;

namespace Shos.DI.WebServer;

class WebAppManager
{
    const string controllerSuffix  = "Controller";
    const string defaultController = "Home";
    const string defaultAction     = "Index";

    readonly DIContainer         container = new();
    readonly IReadOnlyList<Type> controllerTypes;

    public WebAppManager()
    {
        const string appFolderName = "Apps";

        var appFolder = Path.Combine(AppContext.BaseDirectory, appFolderName);
        var types     = Directory.GetFiles(appFolder, "*.dll")
                                 .Select(Assembly.LoadFrom)
                                 .SelectMany(assembly => assembly.GetTypes())
                                 .Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters)
                                 .ToList();
        foreach (var type in types)
            container.Register(type);

        controllerTypes = [.. types.Where(type => type.Name.EndsWith(controllerSuffix, StringComparison.Ordinal))];
    }

    public string? GetView(Uri url)
    {
        var (controllerName, actionName) = Split(url);
        return GetView(controllerName: controllerName, actionName: actionName);
    }

    string? GetView(string controllerName, string actionName)
    {
        var controllerType = controllerTypes.FirstOrDefault(type => string.Equals(type.Name, $"{controllerName}{controllerSuffix}", StringComparison.OrdinalIgnoreCase));
        if (controllerType is null)
            return null;

        var action = GetAction(controllerType, actionName);
        if (action is null)
            return null;

        var controller = container.GetInstance(controllerType);
        return controller is null ? null : action.Invoke(controller, []) as string;
    }

    // An action is a public, parameterless instance method declared by the controller itself that returns a string.
    // Inherited members (ToString, GetType, ...), overrides, property accessors and static methods are not actions.
    static MethodInfo? GetAction(Type controllerType, string actionName)
    {
        var action = controllerType.GetMethod(actionName,
                                              BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.IgnoreCase,
                                              Type.EmptyTypes);
        return action is not null && !action.IsSpecialName && action.ReturnType == typeof(string) && action.GetBaseDefinition() == action
               ? action
               : null;
    }

    static (string controllerName, string actionName) Split(Uri url)
    {
        // "http://localhost:8080/Home/Index?x=1" → AbsolutePath "/Home/Index" → ["Home", "Index"]
        var segments = url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
                                       .Select(Uri.UnescapeDataString)
                                       .ToArray();
        return (segments.ElementAtOrDefault(0) ?? defaultController, segments.ElementAtOrDefault(1) ?? defaultAction);
    }
}
