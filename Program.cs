namespace DIcontainer_study;

// --- Demo service types ---

interface ILogger
{
    void Log(string message);
}

class ConsoleLogger : ILogger, IDisposable
{
    private readonly string _id = Guid.NewGuid().ToString("N")[..8];

    public void Log(string message) => Console.WriteLine($"[Logger {_id}] {message}");
    public void Dispose() => Console.WriteLine($"[Logger {_id}] Disposed.");
}

interface IMessageService
{
    void Send(string message);
}

class EmailMessageService : IMessageService
{
    private readonly ILogger _logger;

    [Inject]
    public ILogger SecondaryLogger { get; set; } = null!;

    public EmailMessageService(ILogger logger) => _logger = logger;

    public void Send(string message)
    {
        _logger.Log($"Primary: {message}");
        SecondaryLogger.Log($"Secondary: {message}");
    }
}

interface IRequestHandler
{
    void Handle(string request);
}

class RequestHandler : IRequestHandler, IDisposable
{
    private readonly IMessageService _messages;
    private readonly ILogger _logger;

    public RequestHandler(IMessageService messages, ILogger logger)
    {
        _messages = messages;
        _logger = logger;
    }

    public void Handle(string request) => _messages.Send(request);
    public void Dispose() => Console.WriteLine("RequestHandler disposed.");
}

class Circular
{
    public Circular(Circular self) { }
}

// --- Demo ---

class Program
{
    static void Main(string[] args)
    {
        var registry = new ServiceRegistry();
        registry
            .AddSingleton<ILogger, ConsoleLogger>()
            .AddTransient<IMessageService, EmailMessageService>()
            .AddScoped<IRequestHandler, RequestHandler>();

        using var container = registry.BuildContainer();

        // --- Singleton: same instance ---
        Console.WriteLine("=== Singleton ===");
        var log1 = container.ResolveRequired<ILogger>();
        var log2 = container.ResolveRequired<ILogger>();
        Console.WriteLine($"Same instance: {ReferenceEquals(log1, log2)}");

        // --- Transient: different instances ---
        Console.WriteLine("\n=== Transient ===");
        var msg1 = container.ResolveRequired<IMessageService>();
        var msg2 = container.ResolveRequired<IMessageService>();
        Console.WriteLine($"Different instances: {!ReferenceEquals(msg1, msg2)}");

        // --- Property injection ---
        Console.WriteLine("\n=== Property Injection ===");
        var msg = (EmailMessageService)container.ResolveRequired<IMessageService>();
        Console.WriteLine($"SecondaryLogger injected: {msg.SecondaryLogger is not null}");
        Console.WriteLine($"SecondaryLogger is singleton: {ReferenceEquals(msg.SecondaryLogger, log1)}");

        // --- Scoped: same within scope, different across scopes ---
        Console.WriteLine("\n=== Scoped ===");
        using var scope1 = container.CreateScope();
        using var scope2 = container.CreateScope();
        var h1a = scope1.Container.ResolveRequired<IRequestHandler>();
        var h1b = scope1.Container.ResolveRequired<IRequestHandler>();
        var h2  = scope2.Container.ResolveRequired<IRequestHandler>();
        Console.WriteLine($"Same within scope:       {ReferenceEquals(h1a, h1b)}");
        Console.WriteLine($"Different across scopes: {!ReferenceEquals(h1a, h2)}");

        // --- Captive dependency guard ---
        Console.WriteLine("\n=== Captive Dependency Guard ===");
        try
        {
            container.ResolveRequired<IRequestHandler>();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Threw: {ex.Message}");
        }

        // --- Circular dependency detection ---
        Console.WriteLine("\n=== Circular Dependency Detection ===");
        var circularRegistry = new ServiceRegistry();
        circularRegistry.AddTransient<Circular>();
        using (var circularContainer = circularRegistry.BuildContainer())
        {
            try
            {
                circularContainer.ResolveRequired<Circular>();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Threw: {ex.Message}");
            }
        }

        // --- IEnumerable<T> resolution ---
        Console.WriteLine("\n=== IEnumerable<T> ===");
        var multiRegistry = new ServiceRegistry();
        multiRegistry
            .AddSingleton<ILogger, ConsoleLogger>()
            .AddSingleton<ILogger, ConsoleLogger>();
        using (var multiContainer = multiRegistry.BuildContainer())
        {
            var loggers = multiContainer.ResolveAll<ILogger>();
            Console.WriteLine($"Logger count: {loggers.Count()}");
        }

        // --- IDisposable cleanup ---
        Console.WriteLine("\n=== IDisposable Cleanup ===");
        Console.WriteLine("Disposing scope1 (scoped RequestHandler is IDisposable):");
        scope1.Dispose();
        Console.WriteLine("Disposing root container (singleton ConsoleLogger is IDisposable):");
    }
    // scope2 and container dispose here via 'using var' (reverse declaration order)
}
