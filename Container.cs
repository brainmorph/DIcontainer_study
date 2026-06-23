namespace DIcontainer_study;

// Stub — fully implemented in Section 5.
public class Container : IContainer, IScopeFactory
{
    public Container(IEnumerable<ServiceDescriptor> descriptors)
        => throw new NotImplementedException("Container is not yet implemented.");

    public object? Resolve(Type serviceType)
        => throw new NotImplementedException();

    public IContainerScope CreateScope()
        => throw new NotImplementedException();
}
