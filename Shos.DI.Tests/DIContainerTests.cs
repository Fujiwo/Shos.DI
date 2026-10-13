namespace Shos.DI.Tests;

public class DIContainerTests
{
    [Fact]
    public void RegisteredTypeWithParameterlessConstructorIsSingleton()
    {
        var container = new DIContainer();
        container.Register<Leaf>();

        var first  = container.GetInstance<Leaf>();
        var second = container.GetInstance<Leaf>();

        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    [Fact]
    public void RegisteredTypeWithInjectedConstructorIsSingleton()
    {
        var container = new DIContainer();
        container.Register<Leaf>();
        container.Register<Branch>();

        var first  = container.GetInstance<Branch>();
        var second = container.GetInstance<Branch>();

        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    [Fact]
    public void RegisteredDependencyIsShared()
    {
        var container = new DIContainer();
        container.Register<Leaf>();
        container.Register<Branch>();

        var branch = container.GetInstance<Branch>();

        Assert.NotNull(branch);
        Assert.Same(container.GetInstance<Leaf>(), branch.Leaf);
    }

    [Fact]
    public void UnregisteredDependencyWithParameterlessConstructorIsCreated()
    {
        var container = new DIContainer();
        container.Register<Branch>();

        var branch = container.GetInstance<Branch>();

        Assert.NotNull(branch);
        Assert.NotNull(branch.Leaf);
    }

    [Fact]
    public void UnregisteredTypeIsTransient()
    {
        var container = new DIContainer();

        Assert.NotSame(container.GetInstance<Leaf>(), container.GetInstance<Leaf>());
    }

    [Fact]
    public void UnregisteredTypeWithoutParameterlessConstructorIsNull()
        => Assert.Null(new DIContainer().GetInstance<Branch>());

    [Fact]
    public void ExplicitArgumentsAlwaysCreateNewInstanceWithThoseArguments()
    {
        var container = new DIContainer();
        container.Register<Branch>();
        var leaf1     = new Leaf();
        var leaf2     = new Leaf();

        var branch1   = container.GetInstance<Branch>(leaf1);
        var branch2   = container.GetInstance<Branch>(leaf2);

        Assert.NotNull(branch1);
        Assert.NotNull(branch2);
        Assert.NotSame(branch1, branch2);
        Assert.Same(leaf1, branch1.Leaf);
        Assert.Same(leaf2, branch2.Leaf);
    }

    [Fact]
    public void ExplicitArgumentsDoNotAffectResolvedSingleton()
    {
        var container = new DIContainer();
        container.Register<Leaf>();
        container.Register<Branch>();

        var explicitBranch = container.GetInstance<Branch>(new Leaf());
        var resolvedBranch = container.GetInstance<Branch>();

        Assert.NotSame(explicitBranch, resolvedBranch);
        Assert.Same(resolvedBranch, container.GetInstance<Branch>());
    }

    [Fact]
    public void ExplicitArgumentOfDerivedTypeMatchesBaseTypeParameter()
    {
        var container = new DIContainer();
        container.Register<Branch>();
        var leaf      = new DerivedLeaf();

        var branch    = container.GetInstance<Branch>(leaf);

        Assert.NotNull(branch);
        Assert.Same(leaf, branch.Leaf);
    }

    [Fact]
    public void ExplicitNullArgumentMatchesReferenceTypeParameter()
    {
        var container = new DIContainer();
        container.Register<Branch>();

        var branch    = container.GetInstance<Branch>((object?)null);

        Assert.NotNull(branch);
        Assert.Null(branch.Leaf);
    }

    [Fact]
    public void ExplicitArgumentsWithoutMatchingConstructorReturnNull()
        => Assert.Null(new DIContainer().GetInstance<Branch>("not a leaf"));

    [Fact]
    public void ExplicitArgumentsWorkForUnregisteredType()
    {
        var leaf   = new Leaf();
        var branch = new DIContainer().GetInstance<Branch>(leaf);

        Assert.NotNull(branch);
        Assert.Same(leaf, branch.Leaf);
    }

    [Fact]
    public void GreediestSatisfiableConstructorIsChosen()
    {
        var container = new DIContainer();
        container.Register<Leaf>();
        container.Register<Branch>();
        container.Register<MultipleConstructors>();

        Assert.Equal(2, container.GetInstance<MultipleConstructors>()?.ParameterCount);
    }

    [Fact]
    public void UnsatisfiableConstructorIsSkipped()
    {
        var container = new DIContainer();
        container.Register<NeedsString>();

        Assert.Equal(0, container.GetInstance<NeedsString>()?.ParameterCount);
    }

    [Fact]
    public void CircularDependencyThrows()
    {
        var container = new DIContainer();
        container.Register<Cycle1>();
        container.Register<Cycle2>();

        var exception = Assert.Throws<InvalidOperationException>(() => container.GetInstance<Cycle1>());
        Assert.Contains(nameof(Cycle2), exception.Message);
    }

    [Theory]
    [InlineData(typeof(IService))]
    [InlineData(typeof(AbstractService))]
    [InlineData(typeof(List<>))]
    public void RegisteringNonInstantiableTypeThrows(Type type)
        => Assert.Throws<ArgumentException>(() => new DIContainer().Register(type));

    [Fact]
    public void RegisterAndResolveByName()
    {
        var container = new DIContainer();
        Assert.True(container.Register(typeof(Leaf).FullName!));
        Assert.True(container.Register(typeof(Branch).FullName!));

        var first  = container.GetInstance(typeof(Branch).FullName!);
        var second = container.GetInstance(typeof(Branch).FullName!);

        Assert.IsType<Branch>(first);
        Assert.Same(first, second);
    }

    [Fact]
    public void UnknownTypeNameIsRejected()
    {
        var container = new DIContainer();

        Assert.False(container.Register("No.Such.Type"));
        Assert.Null(container.GetInstance("No.Such.Type"));
    }

    [Fact]
    public void ReRegisteringDiscardsCachedInstance()
    {
        var container = new DIContainer();
        container.Register<Leaf>();
        var first     = container.GetInstance<Leaf>();

        container.Register<Leaf>();

        Assert.NotSame(first, container.GetInstance<Leaf>());
    }

    [Fact]
    public async Task ConcurrentResolutionYieldsSingleInstance()
    {
        var container = new DIContainer();
        container.Register<Leaf>();
        container.Register<Branch>();

        var branches  = await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(container.GetInstance<Branch>)));

        Assert.All(branches, branch => Assert.Same(branches[0], branch));
    }
}

public class Leaf
{}

public class DerivedLeaf : Leaf
{}

public class Branch(Leaf? leaf)
{
    public Leaf? Leaf { get; } = leaf;
}

public class MultipleConstructors
{
    public int ParameterCount { get; }

    public MultipleConstructors()                  => ParameterCount = 0;
    public MultipleConstructors(Leaf _)            => ParameterCount = 1;
    public MultipleConstructors(Leaf _, Branch __) => ParameterCount = 2;
}

public class NeedsString
{
    public int ParameterCount { get; }

    public NeedsString()         => ParameterCount = 0;
    public NeedsString(string _) => ParameterCount = 1;
}

public class Cycle1
{
    public Cycle1(Cycle2 _) {}
}

public class Cycle2
{
    public Cycle2(Cycle1 _) {}
}

public interface IService
{}

public abstract class AbstractService
{}
