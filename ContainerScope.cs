namespace DIcontainer_study;

internal sealed class ContainerScope : IContainerScope
{
    private readonly ScopedContainer _inner;
    private bool _disposed;

    public IContainer Container { get; }

    internal ContainerScope(Container root)
    {
        _inner = new ScopedContainer(root);
        Container = _inner;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _inner.Dispose();
    }
}
