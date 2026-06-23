# DIcontainer_study

A dependency injection (DI) container built from scratch in .NET 8.0. No third-party libraries.

---

## What is a DI container?

A DI container is a tool that builds objects for you. Instead of writing:

```csharp
var logger = new ConsoleLogger();
var service = new EmailMessageService(logger);
```

You tell the container *how* to build things once, then ask for them by interface:

```csharp
var service = container.ResolveRequired<IEmailMessageService>();
```

The container figures out the full chain of dependencies automatically.

---

## Quick start

```csharp
// 1. Create a registry and describe your services
var registry = new ServiceRegistry();
registry
    .AddSingleton<ILogger, ConsoleLogger>()
    .AddTransient<IMessageService, EmailMessageService>()
    .AddScoped<IRequestHandler, RequestHandler>();

// 2. Build the container (do this once at startup)
using var container = registry.BuildContainer();

// 3. Resolve services
var handler = container.ResolveRequired<IRequestHandler>();
```

---

## Step 1 — Registration

Registration is the act of telling the container what to build and how long to keep it.
All registration happens on `ServiceRegistry` through extension methods on `IServiceRegistry`.

### Lifetimes

| Lifetime    | How long the instance lives                              | Typical use case          |
|-------------|----------------------------------------------------------|---------------------------|
| `Singleton` | Forever — one instance for the life of the container     | Logging, configuration    |
| `Transient` | New instance every time it is resolved                   | Stateless utility classes |
| `Scoped`    | One instance per scope (e.g. one per HTTP request)       | Database contexts         |

### Registration overloads

**Interface + implementation (most common)**
```csharp
registry.AddSingleton<ILogger, ConsoleLogger>();
registry.AddTransient<IMessageService, EmailMessageService>();
registry.AddScoped<IRequestHandler, RequestHandler>();
```

**Self-registration (no separate interface)**
```csharp
registry.AddSingleton<ConsoleLogger>();
registry.AddTransient<EmailSender>();
```

**Pre-built instance (you create it, container stores it)**
```csharp
var config = new AppConfig { ConnectionString = "..." };
registry.AddSingleton<IAppConfig>(config);
```

**Factory delegate (you control construction)**
```csharp
registry.AddSingleton<ILogger>(container =>
{
    var config = container.ResolveRequired<IAppConfig>();
    return new FileLogger(config.LogPath);
});
```

All `Add*` methods return `IServiceRegistry` so calls can be chained fluently.

---

## Step 2 — Building the container

Call `BuildContainer()` once after all registrations are done. After this point the registry is frozen — adding more descriptors to it has no effect on the container.

```csharp
using var container = registry.BuildContainer();
```

Use `using var` so the container is disposed (and singletons cleaned up) when it goes out of scope.

---

## Step 3 — Resolution

Ask the container for a service by its registered type. The container builds the full
dependency graph for you.

### Extension methods on `IContainer`

| Method | Returns | Throws if not found? |
|---|---|---|
| `Resolve<T>()` | `T?` | No — returns `null` |
| `ResolveRequired<T>()` | `T` | Yes — `InvalidOperationException` |
| `ResolveRequired(Type)` | `object` | Yes — `InvalidOperationException` |
| `ResolveAll<T>()` | `IEnumerable<T>` | No — returns empty |

```csharp
// Nullable — safe when registration is optional
ILogger? logger = container.Resolve<ILogger>();

// Non-nullable — throws if ILogger was never registered
ILogger logger = container.ResolveRequired<ILogger>();

// Resolve all registrations for a type (see multiple registrations below)
IEnumerable<ILogger> loggers = container.ResolveAll<ILogger>();
```

---

## Constructor injection

This is the primary injection mechanism. The container inspects the constructor parameters
of your implementation class and resolves each one automatically.

```csharp
class EmailMessageService : IMessageService
{
    private readonly ILogger _logger;

    // The container sees ILogger is needed and resolves it before calling this
    public EmailMessageService(ILogger logger)
    {
        _logger = logger;
    }
}
```

**Greedy constructor selection:** if a class has multiple constructors, the one with the
most parameters wins.

Every parameter type must be registered in the container or resolution will throw.

---

## Property injection

For cases where constructor injection isn't practical, mark a public property with
`[Inject]`. The container sets it after construction.

```csharp
using DIcontainer_study; // for [Inject]

class EmailMessageService : IMessageService
{
    public EmailMessageService(ILogger primary) { ... }

    [Inject]
    public ILogger SecondaryLogger { get; set; } = null!;
}
```

Rules:
- The property must have a **public setter**.
- The property type must be registered in the container.
- Injection happens after the constructor runs, regardless of whether a factory or
  reflection was used to build the object.

---

## Scopes

A scope represents a unit of work with its own lifetime — the canonical example is a
single HTTP request. Scoped services are created once per scope and disposed when the
scope ends.

```csharp
// Create a scope from the root container
using var scope = container.CreateScope();

// Resolve through the scope's container
var handler = scope.Container.ResolveRequired<IRequestHandler>();

// Same instance within the same scope
var handler2 = scope.Container.ResolveRequired<IRequestHandler>();
Console.WriteLine(ReferenceEquals(handler, handler2)); // true

// Different scopes → different instances
using var scope2 = container.CreateScope();
var handler3 = scope2.Container.ResolveRequired<IRequestHandler>();
Console.WriteLine(ReferenceEquals(handler, handler3)); // false

// scope and scope2 dispose their scoped services here
```

**Important:** resolving a Scoped service directly from the root container throws an
`InvalidOperationException`. This is intentional — it protects you from a bug called a
*captive dependency* where a short-lived service accidentally lives forever.

```csharp
// This throws — do not do this
container.ResolveRequired<IRequestHandler>();
```

---

## Multiple registrations

You can register the same interface more than once. The last registration wins when using
`Resolve` or `ResolveRequired`. Use `ResolveAll<T>()` to get every registration.

```csharp
registry
    .AddSingleton<ILogger, ConsoleLogger>()
    .AddSingleton<ILogger, FileLogger>();

// Returns only FileLogger (last-wins)
var logger = container.ResolveRequired<ILogger>();

// Returns both
var loggers = container.ResolveAll<ILogger>(); // count: 2
```

---

## IDisposable cleanup

The container automatically tracks any resolved instance that implements `IDisposable`.

- **Root container** — disposes singletons (and any transients it created directly) in
  reverse creation order when `container.Dispose()` is called.
- **Scoped container** — disposes scoped and transient instances it created in reverse
  creation order when `scope.Dispose()` is called.

```csharp
class ConsoleLogger : ILogger, IDisposable
{
    public void Dispose() => Console.WriteLine("Logger cleaned up.");
}

using var container = registry.BuildContainer();
var logger = container.ResolveRequired<ILogger>();
// ... use logger ...
// "Logger cleaned up." prints here when 'using' block ends
```

Always wrap containers and scopes in `using` statements or call `Dispose()` explicitly.

---

## Error reference

| Situation | Exception |
|---|---|
| `ResolveRequired` finds no registration | `InvalidOperationException` |
| Circular dependency detected | `InvalidOperationException` |
| Scoped service resolved from root container | `InvalidOperationException` |
| Constructor or `[Inject]` parameter cannot be resolved | `InvalidOperationException` |
| `[Inject]` property has no public setter | `InvalidOperationException` |
| Implementation class has no public constructor | `InvalidOperationException` |
| `Resolve` called after container is disposed | `ObjectDisposedException` |
| `null` passed where a type is required | `ArgumentNullException` |

---

## What this framework does NOT support

The following are out of scope by design:

- Open generic registrations (`IRepository<T>`)
- Named / keyed services
- Method injection
- Thread safety — single-threaded use only
- Conditional registrations (`TryAdd` semantics)
- Decorator pattern
- `Lazy<T>` resolution
