using Shos.DI;

var container = new DIContainer();

container.Register<Boo1>();
container.Register<Boo2>();
container.Register<Boo3>();
container.Register<Foo>();

var foo = container.GetInstance<Foo>();
Console.WriteLine(foo ?? "Error");

class Coo
{
    public Coo() => Console.WriteLine(nameof(Coo));
}

class Boo1
{
    public Boo1() => Console.WriteLine(nameof(Boo1));
}

class Boo2
{
    public Boo2(Coo coo) => Console.WriteLine($"{nameof(Boo2)}({nameof(Coo)})");
}

class Boo3
{
    public Boo3(Boo1 boo1) => Console.WriteLine($"{nameof(Boo3)}({nameof(Boo1)})");
}

class Foo
{
    public Foo(Boo2 boo2, Boo3 boo3) => Console.WriteLine($"{nameof(Foo)}({nameof(Boo2)}, {nameof(Boo3)})");

    public override string ToString() => $"{nameof(Foo)}";
}
