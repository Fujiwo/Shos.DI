using Shos.DI;

{
    Console.WriteLine("■ Case 1.");

    var container = new DIContainer();

    container.Register<Boo1>();
    container.Register<Boo2>();
    container.Register<Boo3>();
    container.Register<Foo>();

    var foo = container.GetInstance<Foo>();
    Console.WriteLine(foo ?? "Error");
}
{
    Console.WriteLine("■ Case 2.");

    var container = new DIContainer();

    container.Register(nameof(Boo1));
    container.Register(nameof(Boo2));
    container.Register(nameof(Boo3));
    container.Register(nameof(Foo));

    var foo = container.GetInstance(nameof(Foo));
    Console.WriteLine(foo ?? "Error");
}
{
    Console.WriteLine("■ Case 3.");

    var container = new DIContainer();

    container.Register<Foo>();

    var foo = container.GetInstance<Foo>(new Boo2(new Coo()), new Boo3(new Boo1()));
    Console.WriteLine(foo ?? "Error");
}
{
    Console.WriteLine("■ Case 4.");

    var container = new DIContainer();

    container.Register(nameof(Foo));

    var foo = container.GetInstance(nameof(Foo), new Boo2(new Coo()), new Boo3(new Boo1()));
    Console.WriteLine(foo ?? "Error");
}

public class Coo
{
    public Coo() => Console.WriteLine(nameof(Coo));
}

public class Boo1
{
    public Boo1() => Console.WriteLine(nameof(Boo1));
}

public class Boo2
{
    public Boo2(Coo coo) => Console.WriteLine($"{nameof(Boo2)}({nameof(Coo)})");
}

public class Boo3
{
    public Boo3(Boo1 boo1) => Console.WriteLine($"{nameof(Boo3)}({nameof(Boo1)})");
}

public class Foo
{
    public Foo(Boo2 boo2, Boo3 boo3) => Console.WriteLine($"{nameof(Foo)}({nameof(Boo2)}, {nameof(Boo3)})");

    public override string ToString() => nameof(Foo);
}
