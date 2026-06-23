namespace DIcontainer_study;

internal sealed class ScopedContainer : IContainer, IDisposable
{
    private readonly Container _root;
    private readonly Dictionary<Type, object> _scopedCache = [];
    private readonly List<IDisposable> _disposables = [];
    private bool _disposed;

    internal ScopedContainer(Container root) => _root = root;

    public object? Resolve(Type serviceType)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(serviceType);
        return ResolveCore(serviceType, []);
    }

    internal object? ResolveCore(Type serviceType, HashSet<Type> stack)
    {
        if (serviceType == typeof(IContainer)) return this;
        if (serviceType == typeof(IScopeFactory)) return _root;

        if (serviceType.IsGenericType &&
            serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return ResolveAll(serviceType.GetGenericArguments()[0], stack);
        }

        var descriptor = _root.GetLastDescriptor(serviceType);
        return descriptor is null ? null : ResolveDescriptor(descriptor, stack);
    }

    private object ResolveDescriptor(ServiceDescriptor descriptor, HashSet<Type> stack)
        => descriptor.Lifetime switch
        {
            ServiceLifetime.Singleton => _root.ResolveCore(descriptor.ServiceType, stack)!,
            ServiceLifetime.Transient => Instantiate(descriptor, stack),
            ServiceLifetime.Scoped    => ResolveScoped(descriptor, stack),
            _ => throw new InvalidOperationException($"Unknown lifetime: {descriptor.Lifetime}")
        };

    private object ResolveScoped(ServiceDescriptor descriptor, HashSet<Type> stack)
    {
        if (_scopedCache.TryGetValue(descriptor.ServiceType, out var cached))
            return cached;

        var instance = Instantiate(descriptor, stack);
        _scopedCache[descriptor.ServiceType] = instance;
        return instance;
    }

    private object Instantiate(ServiceDescriptor descriptor, HashSet<Type> stack)
    {
        if (descriptor.Instance is not null)
            return descriptor.Instance;

        object instance;

        if (descriptor.Factory is not null)
            instance = descriptor.Factory(this);
        else
            instance = Injector.ConstructorInject(descriptor.ImplementationType!, stack, ResolveCore);

        Injector.PropertyInject(instance, stack, ResolveCore);

        if (instance is IDisposable disposable)
            _disposables.Add(disposable);

        return instance;
    }

    private object ResolveAll(Type elementType, HashSet<Type> stack)
    {
        var allDescriptors = _root.GetAllDescriptors(elementType);
        if (allDescriptors is null)
            return Array.CreateInstance(elementType, 0);

        var array = Array.CreateInstance(elementType, allDescriptors.Count);
        for (var i = 0; i < allDescriptors.Count; i++)
            array.SetValue(ResolveDescriptor(allDescriptors[i], stack), i);

        return array;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        for (var i = _disposables.Count - 1; i >= 0; i--)
            _disposables[i].Dispose();
        _disposables.Clear();
        _scopedCache.Clear();
    }
}
