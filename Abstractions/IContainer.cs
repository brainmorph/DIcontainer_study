namespace DIcontainer_study;

public interface IContainer
{
    object? Resolve(Type serviceType);
}
