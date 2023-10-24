using System.Reflection;

namespace Shos.DI
{
    static class Extensions
    {
        public static void ForEach<TElement>(this IEnumerable<TElement> @this, Action<TElement> action)
        {
            foreach (var element in @this)
                action(element);
        }

        public static object? GetInstance(this Type @this)
            => @this.GetConstructor(new Type[0])?.Invoke(new object[0]);
    }

    public class DIContainer
    {
        Dictionary<Type, TypeInformation> typeInformations = new();

        public object? GetInstance<T>() => GetInstance(typeof(T));
        public void Register<T>() => Register(typeof(T));

        object? GetInstance(Type type)
        {
            var instance = type.GetInstance();
            if (instance is not null)
                return instance;

            if (typeInformations.TryGetValue(type, out var typeInformation)) {
                instance = typeInformation.GetInstance();
                if (instance is not null)
                    return instance;

                foreach (var constructor in typeInformation.Constructors) {
                    var parameterTypes = typeInformation[constructor];
                    if (parameterTypes is null)
                        continue;
                    var parameters = parameterTypes.Select(parameterType => GetInstance(parameterType)).ToArray();
                    if (parameters.Any(parameter => parameter is null))
                        continue;
                    return constructor.Invoke(parameters);
                }
            }
            return null;
        }

        void Register(Type type) => typeInformations[type] = new TypeInformation(type);
    }

    public class TypeInformation
    {
        Dictionary<Type[], ConstructorInfo>? constructorInfoTable = null;
        Dictionary<ConstructorInfo, Type[]>? parameterTypesTable  = null;
        Dictionary<Type[], object         >? instanceTable        = null;
        ConstructorInfo[]?                   constructors         = null;

        public Type Type { get; private set; }

        public IEnumerable<ConstructorInfo> Constructors {
            get {
                Initialize();
                return constructors!;
            }
        }

        public Type[]? this[ConstructorInfo constructor] {
            private set {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));
                if (parameterTypesTable is null)
                    parameterTypesTable = new();
                parameterTypesTable[constructor] = value;
            }
            get {
                Initialize();
                if (parameterTypesTable is null)
                    return null;
                return  parameterTypesTable.TryGetValue(constructor, out var parameterTypes) ? parameterTypes : null;
            }
        }

        ConstructorInfo? this[Type[] parameter] {
            set {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));
                if (constructorInfoTable is null)
                    constructorInfoTable = new();
                constructorInfoTable[parameter] = value;
            }
            get {
                Initialize();
                if (constructorInfoTable is null)
                    return null;
                return constructorInfoTable.TryGetValue(parameter, out var constructor) ? constructor : null;
            }
        }

        public TypeInformation(Type type) => Type = type;

        public object? GetInstance() => GetInstance(new object[0]);

        public object? GetInstance(object[] parameters)
        {
            Initialize();

            var parameterTypes = parameters.Select(parameter => parameter.GetType()).ToArray();
            var instance       = GetInstance(parameterTypes);
            if (instance is not null)
                return instance;

            var constructor    = this[parameterTypes];
            if (constructor is null)
                return null;

            instance           = constructor.Invoke(parameters);
            SetInstance(parameterTypes, instance);
            return instance;
        }

        void Initialize()
        {
            if (constructors is not null)
                return;

            constructors = Type.GetConstructors();
            var pairs    = constructors.Select(constructor => (constructor.GetParameters().Select(parameterInfo => parameterInfo.ParameterType).ToArray(), constructor)).ToList();

            pairs.ForEach(pair => {
                this[pair.Item1] = pair.Item2;
                this[pair.Item2] = pair.Item1;
            });
        }

        void SetInstance(Type[] parameterTypes, object instance)
        {
            if (instanceTable is null)
                instanceTable = new();
            instanceTable[parameterTypes] = instance;
        }

        object? GetInstance(Type[] parameterTypes)
        {
            Initialize();
            if (instanceTable is null)
                return null;
            return instanceTable.TryGetValue(parameterTypes, out var instance) ? instance : null;
        }
    }
}
