namespace DIcontainer_study;

public static class ContainerExtensions
{
    public static T? Resolve<T>(this IContainer container)
        => (T?)container.Resolve(typeof(T));

    public static object ResolveRequired(this IContainer container, Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return container.Resolve(serviceType)
            ?? throw new InvalidOperationException(
                $"No registration found for '{serviceType.Name}'.");
    }

    public static T ResolveRequired<T>(this IContainer container)
        => (T)container.ResolveRequired(typeof(T));

    public static IEnumerable<T> ResolveAll<T>(this IContainer container)
        => (IEnumerable<T>?)container.Resolve(typeof(IEnumerable<T>)) ?? [];

    public static IContainerScope CreateScope(this IContainer container)
        => container.ResolveRequired<IScopeFactory>().CreateScope();
}
