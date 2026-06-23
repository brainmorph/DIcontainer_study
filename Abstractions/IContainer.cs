namespace DIcontainer_study;

public interface IContainer : IDisposable
{
    object? Resolve(Type serviceType);
}
