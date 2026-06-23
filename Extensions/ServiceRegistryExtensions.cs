namespace DIcontainer_study;

public static class ServiceRegistryExtensions
{
    // --- Singleton ---

    public static IServiceRegistry AddSingleton<TService, TImpl>(this IServiceRegistry registry)
        where TImpl : TService
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.Add(ServiceDescriptor.Singleton<TService, TImpl>());
        return registry;
    }

    public static IServiceRegistry AddSingleton<TService>(this IServiceRegistry registry)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.Add(ServiceDescriptor.Singleton<TService, TService>());
        return registry;
    }

    public static IServiceRegistry AddSingleton<TService>(this IServiceRegistry registry, TService instance)
        where TService : notnull
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.Add(ServiceDescriptor.Singleton(instance));
        return registry;
    }

    public static IServiceRegistry AddSingleton<TService>(this IServiceRegistry registry, Func<IContainer, TService> factory)
        where TService : notnull
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(factory);
        registry.Add(ServiceDescriptor.Singleton(factory));
        return registry;
    }

    // --- Transient ---

    public static IServiceRegistry AddTransient<TService, TImpl>(this IServiceRegistry registry)
        where TImpl : TService
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.Add(ServiceDescriptor.Transient<TService, TImpl>());
        return registry;
    }

    public static IServiceRegistry AddTransient<TService>(this IServiceRegistry registry)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.Add(ServiceDescriptor.Transient<TService, TService>());
        return registry;
    }

    public static IServiceRegistry AddTransient<TService>(this IServiceRegistry registry, Func<IContainer, TService> factory)
        where TService : notnull
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(factory);
        registry.Add(ServiceDescriptor.Transient(factory));
        return registry;
    }

    // --- Scoped ---

    public static IServiceRegistry AddScoped<TService, TImpl>(this IServiceRegistry registry)
        where TImpl : TService
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.Add(ServiceDescriptor.Scoped<TService, TImpl>());
        return registry;
    }

    public static IServiceRegistry AddScoped<TService>(this IServiceRegistry registry)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.Add(ServiceDescriptor.Scoped<TService, TService>());
        return registry;
    }

    public static IServiceRegistry AddScoped<TService>(this IServiceRegistry registry, Func<IContainer, TService> factory)
        where TService : notnull
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(factory);
        registry.Add(ServiceDescriptor.Scoped(factory));
        return registry;
    }

    // --- Builder (completed in Section 5 once Container exists) ---

    public static IContainer BuildContainer(this IServiceRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return new Container(registry);
    }
}
