using System.Collections.Concurrent;

namespace Jellyfin.Plugin.PremiumUI.Diagnostics;

/// <summary>
/// Thread-safe runtime state shared by the startup filter, the middleware and the admin diagnostics endpoint.
/// </summary>
public sealed class IntegrationStatus
{
    private const int MaxErrors = 20;
    private readonly ConcurrentQueue<ErrorEntry> _errors = new();
    private string? _lastErrorSignature;
    private long _indexRequests;
    private long _assetRequests;
    private long _injections;
    private long _bootstrapRequests;

    /// <summary>
    /// Gets or sets the running Jellyfin server version.
    /// </summary>
    public string ServerVersion { get; set; } = "unknown";

    /// <summary>
    /// Gets or sets the bundled frontend version.
    /// </summary>
    public string FrontendVersion { get; set; } = "0.24.2";

    /// <summary>
    /// Gets or sets the number of embedded frontend assets.
    /// </summary>
    public int AssetCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the server major version is supported.
    /// </summary>
    public bool Supported { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the startup filter was registered.
    /// </summary>
    public bool MiddlewareRegistered { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether index injection is active.
    /// </summary>
    public bool InjectionActive { get; set; }

    /// <summary>
    /// Gets or sets the last integration message.
    /// </summary>
    public string LastMessage { get; set; } = "Sin inicializar";

    /// <summary>
    /// Gets or sets the UTC timestamp of the last successful injection.
    /// </summary>
    public DateTimeOffset? LastInjectionUtc { get; set; }

    /// <summary>
    /// Gets the number of intercepted index requests.
    /// </summary>
    public long IndexRequests => Interlocked.Read(ref _indexRequests);

    /// <summary>
    /// Gets the number of served asset requests.
    /// </summary>
    public long AssetRequests => Interlocked.Read(ref _assetRequests);

    /// <summary>
    /// Gets the number of successful injections.
    /// </summary>
    public long Injections => Interlocked.Read(ref _injections);

    /// <summary>
    /// Gets the number of authenticated bootstrap requests made by the frontend.
    /// </summary>
    public long BootstrapRequests => Interlocked.Read(ref _bootstrapRequests);

    /// <summary>
    /// Records a startup event.
    /// </summary>
    /// <param name="serverVersion">Server version.</param>
    /// <param name="supported">Whether the version is supported.</param>
    /// <param name="message">Human readable message.</param>
    public void MarkStartup(string serverVersion, bool supported, string message)
    {
        ServerVersion = serverVersion;
        Supported = supported;
        MiddlewareRegistered = true;
        LastMessage = message;
    }

    /// <summary>
    /// Records an intercepted index request.
    /// </summary>
    public void RecordIndexRequest() => Interlocked.Increment(ref _indexRequests);

    /// <summary>
    /// Records a served asset request.
    /// </summary>
    public void RecordAssetRequest() => Interlocked.Increment(ref _assetRequests);

    /// <summary>
    /// Records a frontend bootstrap request.
    /// </summary>
    public void RecordBootstrap() => Interlocked.Increment(ref _bootstrapRequests);

    /// <summary>
    /// Records a successful injection.
    /// </summary>
    public void RecordInjection()
    {
        Interlocked.Increment(ref _injections);
        LastInjectionUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Adds an error to the bounded diagnostic buffer.
    /// Consecutive identical errors are collapsed to avoid flooding.
    /// </summary>
    /// <param name="source">Error source.</param>
    /// <param name="message">Error message.</param>
    public void RecordError(string source, string message)
    {
        var signature = source + "|" + message;

        lock (_errors)
        {
            if (string.Equals(_lastErrorSignature, signature, StringComparison.Ordinal))
            {
                return;
            }

            _lastErrorSignature = signature;
            _errors.Enqueue(new ErrorEntry(DateTimeOffset.UtcNow, source, message));

            while (_errors.Count > MaxErrors && _errors.TryDequeue(out _))
            {
                // Keep the buffer bounded.
            }
        }
    }

    /// <summary>
    /// Returns a snapshot of the recent errors.
    /// </summary>
    /// <returns>Recent errors, newest first.</returns>
    public IReadOnlyList<ErrorEntry> GetErrors()
    {
        return _errors.Reverse().ToArray();
    }

    /// <summary>
    /// A single diagnostic error entry.
    /// </summary>
    /// <param name="Timestamp">UTC timestamp.</param>
    /// <param name="Source">Error source.</param>
    /// <param name="Message">Error message.</param>
    public sealed record ErrorEntry(DateTimeOffset Timestamp, string Source, string Message);
}
