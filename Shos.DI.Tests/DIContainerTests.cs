using System.Reflection;

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
    [InlineData(typeof(int))]
    [InlineData(typeof(Point))]
    [InlineData(typeof(StaticClass))]
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
        container.Register<Counter>();
        container.Register<SlowConstruction>();
        var counter   = container.GetInstance<Counter>();

        var instances = await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(container.GetInstance<SlowConstruction>)));

        Assert.All(instances, instance => Assert.Same(instances[0], instance));
        Assert.Equal(1, counter?.Value);
    }

    [Fact]
    public void DiamondDependencyIsResolvedWithSharedInstance()
    {
        var container = new DIContainer();
        container.Register<Bottom>();
        container.Register<Left>();
        container.Register<Right>();
        container.Register<Top>();

        var top       = container.GetInstance<Top>();

        Assert.NotNull(top);
        Assert.Same(top.Left.Bottom, top.Right.Bottom);
    }

    [Fact]
    public void UnresolvableDependencyRequestedTwiceIsNotReportedAsCircular()
    {
        var container = new DIContainer();
        container.Register<NeedsService>();
        container.Register<NeedsServiceTwice>();

        Assert.Equal(0, container.GetInstance<NeedsServiceTwice>()?.ParameterCount);
    }

    [Fact]
    public void SelfDependencyThrows()
    {
        var container = new DIContainer();
        container.Register<SelfCycle>();

        Assert.Throws<InvalidOperationException>(() => container.GetInstance<SelfCycle>());
    }

    [Fact]
    public void ConstructorExceptionPropagatesAndIsNotCached()
    {
        var container = new DIContainer();
        container.Register<Throwing>();
        container.Register<NeedsThrowing>();

        var exception = Assert.Throws<TargetInvocationException>(() => container.GetInstance<NeedsThrowing>());
        Assert.IsType<NotSupportedException>(exception.InnerException);
        Assert.Throws<TargetInvocationException>(() => container.GetInstance<NeedsThrowing>());
    }

    [Theory]
    [InlineData(typeof(NeedsService))]
    [InlineData(typeof(NeedsInt))]
    public void RegisteredTypeWithoutSatisfiableConstructorIsNull(Type type)
    {
        var container = new DIContainer();
        container.Register(type);

        Assert.Null(container.GetInstance(type));
    }

    [Fact]
    public void TypeWithoutPublicConstructorIsNull()
    {
        var container = new DIContainer();
        Assert.Null(container.GetInstance<PrivateConstructor>());

        container.Register<PrivateConstructor>();
        Assert.Null(container.GetInstance<PrivateConstructor>());
    }

    [Fact]
    public void ExplicitArgumentsMatchValueTypeParameter()
        => Assert.Equal(42, new DIContainer().GetInstance<NeedsInt>(42)?.Value);

    [Fact]
    public void ExplicitNullArgumentDoesNotMatchValueTypeParameter()
        => Assert.Null(new DIContainer().GetInstance<NeedsInt>((object?)null));

    [Fact]
    public void ExplicitArgumentsMatchNullableValueTypeParameter()
    {
        var container = new DIContainer();

        var withNull  = container.GetInstance<NeedsNullableInt>((object?)null);
        var withValue = container.GetInstance<NeedsNullableInt>(42);

        Assert.NotNull(withNull);
        Assert.Null(withNull.Value);
        Assert.Equal(42, withValue?.Value);
    }

    [Fact]
    public void ResolveByNameWithExplicitArguments()
    {
        var container = new DIContainer();
        var leaf      = new Leaf();

        var branch    = container.GetInstance(typeof(Branch).FullName!, leaf);

        Assert.Same(leaf, Assert.IsType<Branch>(branch).Leaf);
        Assert.Null(container.GetInstance("No.Such.Type", leaf));
    }

    [Fact]
    public void NullArgumentsThrow()
    {
        var container = new DIContainer();

        Assert.Throws<ArgumentNullException>(() => container.GetInstance((Type)null!));
        Assert.Throws<ArgumentNullException>(() => container.GetInstance((Type)null!, new Leaf()));
        Assert.Throws<ArgumentNullException>(() => container.GetInstance(typeof(Leaf), (object?[])null!));
        Assert.Throws<ArgumentNullException>(() => container.Register((Type)null!));
    }

    [Fact]
    public void ClosedGenericTypeCanBeRegistered()
    {
        var container = new DIContainer();
        container.Register<List<int>>();

        var list      = container.GetInstance<List<int>>();

        Assert.NotNull(list);
        Assert.Same(list, container.GetInstance<List<int>>());
    }

    [Fact]
    public void ContainersDoNotShareSingletons()
    {
        var container1 = new DIContainer();
        var container2 = new DIContainer();
        container1.Register<Leaf>();
        container2.Register<Leaf>();

        Assert.NotSame(container1.GetInstance<Leaf>(), container2.GetInstance<Leaf>());
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

public struct Point
{}

public static class StaticClass
{}

public class Bottom
{}

public class Left(Bottom bottom)
{
    public Bottom Bottom { get; } = bottom;
}

public class Right(Bottom bottom)
{
    public Bottom Bottom { get; } = bottom;
}

public class Top(Left left, Right right)
{
    public Left  Left  { get; } = left;
    public Right Right { get; } = right;
}

public class SelfCycle
{
    public SelfCycle(SelfCycle _) {}
}

public class Throwing
{
    public Throwing() => throw new NotSupportedException();
}

public class NeedsThrowing
{
    public NeedsThrowing(Throwing _) {}
}

public class NeedsService
{
    public NeedsService(IService _) {}
}

public class NeedsServiceTwice
{
    public int ParameterCount { get; }

    public NeedsServiceTwice()                                 => ParameterCount = 0;
    public NeedsServiceTwice(NeedsService _, NeedsService __) => ParameterCount = 2;
}

public class NeedsInt(int value)
{
    public int Value { get; } = value;
}

public class NeedsNullableInt(int? value)
{
    public int? Value { get; } = value;
}

public class PrivateConstructor
{
    PrivateConstructor() {}
}

public class Counter
{
    int value;

    public int  Value       => Volatile.Read(ref value);
    public void Increment() => Interlocked.Increment(ref value);
}

public class SlowConstruction
{
    public SlowConstruction(Counter counter)
    {
        counter.Increment();
        Thread.Sleep(20);
    }
}
