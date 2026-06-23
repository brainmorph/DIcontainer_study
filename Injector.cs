namespace DIcontainer_study;

internal static class Injector
{
    internal static object ConstructorInject(
        Type implType,
        HashSet<Type> stack,
        Func<Type, HashSet<Type>, object?> resolve)
    {
        var constructors = implType.GetConstructors();
        if (constructors.Length == 0)
            throw new InvalidOperationException(
                $"No public constructor found on '{implType.Name}'.");

        var ctor = constructors.OrderByDescending(c => c.GetParameters().Length).First();

        if (stack.Contains(implType))
            throw new InvalidOperationException(
                $"Circular dependency detected while resolving '{implType.Name}'.");

        stack.Add(implType);

        var parameters = ctor.GetParameters();
        var args = new object[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var resolved = resolve(parameters[i].ParameterType, stack);
            if (resolved is null)
                throw new InvalidOperationException(
                    $"Cannot resolve constructor parameter '{parameters[i].Name}' of type " +
                    $"'{parameters[i].ParameterType.Name}' for '{implType.Name}'.");
            args[i] = resolved;
        }

        stack.Remove(implType);

        return Activator.CreateInstance(implType, args)!;
    }

    internal static void PropertyInject(
        object instance,
        HashSet<Type> stack,
        Func<Type, HashSet<Type>, object?> resolve)
    {
        foreach (var prop in instance.GetType().GetProperties())
        {
            if (prop.GetCustomAttributes(typeof(InjectAttribute), inherit: true).Length == 0)
                continue;

            if (prop.SetMethod is null || !prop.SetMethod.IsPublic)
                throw new InvalidOperationException(
                    $"[Inject] property '{prop.Name}' on '{instance.GetType().Name}' " +
                    "must have a public setter.");

            var resolved = resolve(prop.PropertyType, stack);
            if (resolved is null)
                throw new InvalidOperationException(
                    $"Cannot resolve [Inject] property '{prop.Name}' of type " +
                    $"'{prop.PropertyType.Name}' on '{instance.GetType().Name}'.");

            prop.SetValue(instance, resolved);
        }
    }
}
