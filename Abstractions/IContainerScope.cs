namespace DIcontainer_study;

public interface IContainerScope : IDisposable
{
    IContainer Container { get; }
}
