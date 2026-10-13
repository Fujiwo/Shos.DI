using System.Reflection;

namespace Shos.DI;

public class DIContainer
{
    readonly Dictionary<Type, TypeInformation> typeInformations = [];
    readonly Lock                              syncRoot         = new();

    public T? GetInstance<T>() where T : class => GetInstance(typeof(T)) as T;
    public T? GetInstance<T>(params object?[] parameters) where T : class => GetInstance(typeof(T), parameters) as T;

    public object? GetInstance(string typeName)
    {
        var type = ToType(typeName);
        return type is null ? null : GetInstance(type);
    }

    public object? GetInstance(string typeName, params object?[] parameters)
    {
        var type = ToType(typeName);
        return type is null ? null : GetInstance(type, parameters);
    }

    public object? GetInstance(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        lock (syncRoot)
            return Resolve(type, []);
    }

    // Constructor arguments supplied by the caller: always creates a new instance (never cached).
    public object? GetInstance(Type type, params object?[] parameters)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(parameters);

        IEnumerable<ConstructorInfo> constructors;
        lock (syncRoot)
            constructors = typeInformations.TryGetValue(type, out var typeInformation)
                           ? typeInformation.Constructors
                           : TypeInformation.GetOrderedConstructors(type);

        return constructors.FirstOrDefault(constructor => constructor.CanAccept(parameters))?.Invoke(parameters);
    }

    public void Register<T>() => Register(typeof(T));

    public void Register(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (!type.IsInstantiable())
            throw new ArgumentException($"{type} cannot be registered: it must be a non-abstract, non-generic-definition class.", nameof(type));
        lock (syncRoot)
            typeInformations[type] = new TypeInformation(type);
    }

    public bool Register(string typeName)
    {
        var type = ToType(typeName);
        if (type is null)
            return false;
        Register(type);
        return true;
    }

    object? Resolve(Type type, HashSet<Type> resolving)
    {
        if (!typeInformations.TryGetValue(type, out var typeInformation))
            return type.GetInstance();                                  // unregistered: parameterless constructor only

        if (typeInformation.Instance is not null)
            return typeInformation.Instance;                            // registered: one instance per container

        if (!resolving.Add(type))
            throw new InvalidOperationException($"Circular dependency detected: {string.Join(" -> ", resolving.Append(type))}");

        try {
            foreach (var constructor in typeInformation.Constructors) { // greediest first
                var parameters = constructor.GetParameters().Select(parameter => Resolve(parameter.ParameterType, resolving)).ToArray();
                if (parameters.Any(parameter => parameter is null))
                    continue;
                return typeInformation.Instance = constructor.Invoke(parameters);
            }
            return null;
        } finally {
            resolving.Remove(type);
        }
    }

    static Type? ToType(string typeName)
        => AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(typeName))
                                                  .FirstOrDefault(type => type is not null);
}

class TypeInformation(Type type)
{
    ConstructorInfo[]? constructors = null;

    public Type    Type     { get; } = type;
    public object? Instance { get; set; }

    public IEnumerable<ConstructorInfo> Constructors => constructors ??= GetOrderedConstructors(Type);

    public static ConstructorInfo[] GetOrderedConstructors(Type type)
        => type.GetConstructors().OrderByDescending(constructor => constructor.GetParameters().Length)
                                 .ToArray();
}

static class TypeExtensions
{
    public static bool IsInstantiable(this Type @this)
        => @this.IsClass && !@this.IsAbstract && !@this.ContainsGenericParameters;

    public static object? GetInstance(this Type @this)
        => @this.GetConstructor(Type.EmptyTypes)?.Invoke([]);

    public static bool CanAccept(this ConstructorInfo @this, object?[] arguments)
    {
        var parameters = @this.GetParameters();
        return parameters.Length == arguments.Length &&
               parameters.Zip(arguments).All(pair => pair.First.ParameterType.CanAccept(pair.Second));
    }

    static bool CanAccept(this Type @this, object? argument)
        => argument is null ? !@this.IsValueType || Nullable.GetUnderlyingType(@this) is not null
                            : @this.IsInstanceOfType(argument);
}
