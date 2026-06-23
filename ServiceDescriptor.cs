namespace DIcontainer_study;

public sealed class ServiceDescriptor
{
    public Type ServiceType { get; }
    public Type? ImplementationType { get; }
    public ServiceLifetime Lifetime { get; }
    public Func<IContainer, object>? Factory { get; }
    public object? Instance { get; }

    private ServiceDescriptor(
        Type serviceType,
        Type? implementationType,
        ServiceLifetime lifetime,
        Func<IContainer, object>? factory,
        object? instance)
    {
        var defined = (implementationType != null ? 1 : 0)
                    + (factory != null ? 1 : 0)
                    + (instance != null ? 1 : 0);

        if (defined != 1)
            throw new ArgumentException(
                "Exactly one of ImplementationType, Factory, or Instance must be provided.");

        if (instance != null && lifetime != ServiceLifetime.Singleton)
            throw new ArgumentException(
                "A pre-built Instance implies Singleton lifetime.");

        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        ImplementationType = implementationType;
        Lifetime = lifetime;
        Factory = factory;
        Instance = instance;
    }

    // --- Singleton factories ---

    public static ServiceDescriptor Singleton<TService, TImpl>()
        where TImpl : TService
        => new(typeof(TService), typeof(TImpl), ServiceLifetime.Singleton, null, null);

    public static ServiceDescriptor Singleton<TService>(TService instance)
        where TService : notnull
    {
        ArgumentNullException.ThrowIfNull(instance);
        return new(typeof(TService), null, ServiceLifetime.Singleton, null, instance);
    }

    public static ServiceDescriptor Singleton<TService>(Func<IContainer, TService> factory)
        where TService : notnull
        => new(typeof(TService), null, ServiceLifetime.Singleton,
               c => factory(c)!, null);

    // --- Transient factories ---

    public static ServiceDescriptor Transient<TService, TImpl>()
        where TImpl : TService
        => new(typeof(TService), typeof(TImpl), ServiceLifetime.Transient, null, null);

    public static ServiceDescriptor Transient<TService>(Func<IContainer, TService> factory)
        where TService : notnull
        => new(typeof(TService), null, ServiceLifetime.Transient,
               c => factory(c)!, null);

    // --- Scoped factories ---

    public static ServiceDescriptor Scoped<TService, TImpl>()
        where TImpl : TService
        => new(typeof(TService), typeof(TImpl), ServiceLifetime.Scoped, null, null);

    public static ServiceDescriptor Scoped<TService>(Func<IContainer, TService> factory)
        where TService : notnull
        => new(typeof(TService), null, ServiceLifetime.Scoped,
               c => factory(c)!, null);
}
