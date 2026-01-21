// Copyright AGNTCY Contributors (https://github.com/agntcy)
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using Internal = uniffi.slim_bindings;

namespace SlimBindings;

/// <summary>
/// Main entry point for SLIM bindings. Provides initialization and global service access.
/// </summary>
public static class Slim
{
    private static readonly object _lock = new();
    private static bool _initialized;

    /// <summary>
    /// Initialize SLIM with default configuration.
    /// Safe to call multiple times - subsequent calls are ignored.
    /// </summary>
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized) return;
            Internal.SlimBindingsMethods.InitializeWithDefaults();
            _initialized = true;
        }
    }

    /// <summary>
    /// Initialize SLIM from a YAML configuration file.
    /// </summary>
    /// <param name="configPath">Path to the YAML configuration file.</param>
    public static void InitializeFromConfig(string configPath)
    {
        lock (_lock)
        {
            if (_initialized) return;
            Internal.SlimBindingsMethods.InitializeFromConfig(configPath);
            _initialized = true;
        }
    }

    /// <summary>
    /// Check if SLIM has been initialized.
    /// </summary>
    public static bool IsInitialized => Internal.SlimBindingsMethods.IsInitialized();

    /// <summary>
    /// Get the SLIM version string.
    /// </summary>
    public static string Version => Internal.SlimBindingsMethods.GetVersion();

    /// <summary>
    /// Get detailed build information.
    /// </summary>
    public static SlimBuildInfo BuildInfo
    {
        get
        {
            var info = Internal.SlimBindingsMethods.GetBuildInfo();
            return new SlimBuildInfo(info.version, info.gitSha);
        }
    }

    /// <summary>
    /// Get the global service instance.
    /// </summary>
    public static SlimService GetGlobalService()
    {
        return new SlimService(Internal.SlimBindingsMethods.GetGlobalService());
    }

    /// <summary>
    /// Create a new insecure client configuration (no TLS).
    /// </summary>
    /// <param name="endpoint">Server endpoint URL (e.g., "http://localhost:46357").</param>
    public static SlimClientConfig NewInsecureClientConfig(string endpoint)
    {
        return new SlimClientConfig(Internal.SlimBindingsMethods.NewInsecureClientConfig(endpoint));
    }

    /// <summary>
    /// Connect to a SLIM server using insecure (no TLS) configuration.
    /// Convenience method that creates config and connects in one call.
    /// </summary>
    /// <param name="endpoint">Server endpoint URL (e.g., "http://localhost:46357").</param>
    /// <returns>Connection ID.</returns>
    public static ulong Connect(string endpoint)
    {
        var config = NewInsecureClientConfig(endpoint);
        return GetGlobalService().Connect(config);
    }

    /// <summary>
    /// Connect to a SLIM server asynchronously using insecure (no TLS) configuration.
    /// </summary>
    /// <param name="endpoint">Server endpoint URL (e.g., "http://localhost:46357").</param>
    /// <returns>Connection ID.</returns>
    public static async Task<ulong> ConnectAsync(string endpoint)
    {
        var config = NewInsecureClientConfig(endpoint);
        return await GetGlobalService().ConnectAsync(config);
    }

    /// <summary>
    /// Shutdown SLIM and release all resources. Blocks until complete.
    /// </summary>
    public static void Shutdown()
    {
        Internal.SlimBindingsMethods.ShutdownBlocking();
        lock (_lock)
        {
            _initialized = false;
        }
    }

    /// <summary>
    /// Shutdown SLIM asynchronously.
    /// </summary>
    public static async Task ShutdownAsync()
    {
        var service = Internal.SlimBindingsMethods.GetGlobalService();
        await service.ShutdownAsync();
        lock (_lock)
        {
            _initialized = false;
        }
    }
}

/// <summary>
/// Build information for SLIM.
/// </summary>
public readonly record struct SlimBuildInfo(string Version, string GitSha);

/// <summary>
/// Represents a SLIM application name in org/namespace/app format.
/// </summary>
public sealed class SlimName : IDisposable
{
    internal readonly Internal.Name _inner;
    private bool _disposed;

    internal SlimName(Internal.Name inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// Create a new SLIM name.
    /// </summary>
    /// <param name="organization">Organization identifier.</param>
    /// <param name="namespace">Namespace identifier.</param>
    /// <param name="application">Application identifier.</param>
    public SlimName(string organization, string @namespace, string application)
    {
        _inner = new Internal.Name(organization, @namespace, application);
    }

    /// <summary>
    /// Parse a name from "org/namespace/app" format.
    /// </summary>
    public static SlimName Parse(string id)
    {
        var parts = id.Split('/');
        if (parts.Length != 3)
        {
            throw new ArgumentException(
                $"Name must be in 'organization/namespace/app' format, got: {id}",
                nameof(id));
        }
        return new SlimName(parts[0], parts[1], parts[2]);
    }

    /// <summary>
    /// Get the string representation in "org/namespace/app" format.
    /// </summary>
    public override string ToString() => _inner.AsString();

    public void Dispose()
    {
        if (_disposed) return;
        _inner.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Session type for SLIM communication.
/// </summary>
public enum SlimSessionType
{
    /// <summary>Point-to-point session between two endpoints.</summary>
    PointToPoint,
    /// <summary>Group session with multiple participants.</summary>
    Group
}

/// <summary>
/// Configuration for creating a SLIM session.
/// </summary>
public sealed class SlimSessionConfig
{
    /// <summary>Session type (PointToPoint or Group).</summary>
    public SlimSessionType SessionType { get; init; } = SlimSessionType.PointToPoint;

    /// <summary>Whether to enable MLS encryption.</summary>
    public bool EnableMls { get; init; }

    /// <summary>Maximum retry attempts for session establishment.</summary>
    public uint MaxRetries { get; init; } = 5;

    /// <summary>Retry interval between attempts.</summary>
    public TimeSpan RetryInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Optional metadata key-value pairs.</summary>
    public Dictionary<string, string>? Metadata { get; init; }

    internal Internal.SessionConfig ToInternal()
    {
        return new Internal.SessionConfig(
            sessionType: SessionType == SlimSessionType.PointToPoint
                ? Internal.SessionType.PointToPoint
                : Internal.SessionType.Group,
            enableMls: EnableMls,
            maxRetries: MaxRetries,
            interval: RetryInterval,
            metadata: Metadata ?? new Dictionary<string, string>()
        );
    }
}

/// <summary>
/// Client configuration for connecting to a SLIM server.
/// </summary>
public sealed class SlimClientConfig
{
    internal readonly Internal.ClientConfig _inner;

    internal SlimClientConfig(Internal.ClientConfig inner)
    {
        _inner = inner;
    }
}

/// <summary>
/// A received message from a SLIM session.
/// </summary>
public sealed class SlimMessage
{
    internal readonly Internal.MessageContext _context;

    internal SlimMessage(Internal.ReceivedMessage msg)
    {
        _context = msg.context;
        Payload = msg.payload;
    }

    /// <summary>Raw payload bytes.</summary>
    public byte[] Payload { get; }

    /// <summary>Payload decoded as UTF-8 string.</summary>
    public string Text => Encoding.UTF8.GetString(Payload);
}

/// <summary>
/// SLIM service for managing connections and applications.
/// </summary>
public sealed class SlimService : IDisposable
{
    internal readonly Internal.Service _inner;
    private bool _disposed;

    internal SlimService(Internal.Service inner)
    {
        _inner = inner;
    }

    /// <summary>Get the service name.</summary>
    public string Name => _inner.GetName();

    /// <summary>
    /// Create an application with shared secret authentication.
    /// </summary>
    /// <param name="name">Application name.</param>
    /// <param name="secret">Shared secret (minimum 32 characters).</param>
    public SlimApp CreateApp(SlimName name, string secret)
    {
        var app = _inner.CreateAppWithSecret(name._inner, secret);
        return new SlimApp(app);
    }

    /// <summary>
    /// Create an application with shared secret authentication.
    /// </summary>
    /// <param name="organization">Organization identifier.</param>
    /// <param name="namespace">Namespace identifier.</param>
    /// <param name="application">Application identifier.</param>
    /// <param name="secret">Shared secret (minimum 32 characters).</param>
    public SlimApp CreateApp(string organization, string @namespace, string application, string secret)
    {
        using var name = new SlimName(organization, @namespace, application);
        var app = _inner.CreateAppWithSecret(name._inner, secret);
        return new SlimApp(app);
    }

    /// <summary>
    /// Connect to a remote SLIM server.
    /// </summary>
    /// <param name="config">Client configuration.</param>
    /// <returns>Connection ID.</returns>
    public ulong Connect(SlimClientConfig config)
    {
        return _inner.Connect(config._inner);
    }

    /// <summary>
    /// Connect to a remote SLIM server asynchronously.
    /// </summary>
    public async Task<ulong> ConnectAsync(SlimClientConfig config)
    {
        return await _inner.ConnectAsync(config._inner);
    }

    /// <summary>
    /// Disconnect a client connection.
    /// </summary>
    /// <param name="connectionId">Connection ID to disconnect.</param>
    public void Disconnect(ulong connectionId)
    {
        _inner.Disconnect(connectionId);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _inner.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// SLIM application for managing sessions and communication.
/// </summary>
public sealed class SlimApp : IDisposable
{
    internal readonly Internal.App _inner;
    private bool _disposed;

    internal SlimApp(Internal.App inner)
    {
        _inner = inner;
    }

    /// <summary>Get the application ID.</summary>
    public ulong Id => _inner.Id();

    /// <summary>Get the application name.</summary>
    public SlimName Name => new(_inner.Name());

    /// <summary>
    /// Subscribe to receive messages for a name.
    /// </summary>
    /// <param name="name">Name to subscribe to.</param>
    /// <param name="connectionId">Optional connection ID to forward subscription.</param>
    public void Subscribe(SlimName name, ulong? connectionId = null)
    {
        _inner.Subscribe(name._inner, connectionId);
    }

    /// <summary>
    /// Unsubscribe from a name.
    /// </summary>
    public void Unsubscribe(SlimName name, ulong? connectionId = null)
    {
        _inner.Unsubscribe(name._inner, connectionId);
    }

    /// <summary>
    /// Set a route to a destination via a specific connection.
    /// </summary>
    /// <param name="destination">Destination name.</param>
    /// <param name="connectionId">Connection ID to route through.</param>
    public void SetRoute(SlimName destination, ulong connectionId)
    {
        _inner.SetRoute(destination._inner, connectionId);
    }

    /// <summary>
    /// Remove a route.
    /// </summary>
    public void RemoveRoute(SlimName destination, ulong connectionId)
    {
        _inner.RemoveRoute(destination._inner, connectionId);
    }

    /// <summary>
    /// Create a session to a destination and wait for establishment.
    /// </summary>
    /// <param name="destination">Destination name.</param>
    /// <param name="config">Optional session configuration.</param>
    public SlimSession CreateSession(SlimName destination, SlimSessionConfig? config = null)
    {
        config ??= new SlimSessionConfig();
        var session = _inner.CreateSessionAndWait(config.ToInternal(), destination._inner);
        return new SlimSession(session);
    }

    /// <summary>
    /// Create a session asynchronously.
    /// </summary>
    public async Task<SlimSession> CreateSessionAsync(SlimName destination, SlimSessionConfig? config = null)
    {
        config ??= new SlimSessionConfig();
        var session = await _inner.CreateSessionAndWaitAsync(config.ToInternal(), destination._inner);
        return new SlimSession(session);
    }

    /// <summary>
    /// Listen for incoming sessions.
    /// </summary>
    /// <param name="timeout">Optional timeout duration.</param>
    public SlimSession ListenForSession(TimeSpan? timeout = null)
    {
        var session = _inner.ListenForSession(timeout);
        return new SlimSession(session);
    }

    /// <summary>
    /// Listen for incoming sessions asynchronously.
    /// </summary>
    public async Task<SlimSession> ListenForSessionAsync(TimeSpan? timeout = null)
    {
        var session = await _inner.ListenForSessionAsync(timeout);
        return new SlimSession(session);
    }

    /// <summary>
    /// Delete a session and wait for completion.
    /// </summary>
    public void DeleteSession(SlimSession session)
    {
        _inner.DeleteSessionAndWait(session._inner);
    }

    /// <summary>
    /// Delete a session asynchronously.
    /// </summary>
    public async Task DeleteSessionAsync(SlimSession session)
    {
        await _inner.DeleteSessionAndWaitAsync(session._inner);
    }

    /// <summary>
    /// Destroy the application and release resources.
    /// </summary>
    public void Destroy()
    {
        _inner.Destroy();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _inner.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// SLIM session for sending and receiving messages.
/// </summary>
public sealed class SlimSession : IDisposable
{
    internal readonly Internal.Session _inner;
    private bool _disposed;

    internal SlimSession(Internal.Session inner)
    {
        _inner = inner;
    }

    /// <summary>Get the session ID.</summary>
    public uint SessionId => _inner.SessionId();

    /// <summary>Get the session type.</summary>
    public SlimSessionType SessionType => _inner.SessionType() == Internal.SessionType.PointToPoint
        ? SlimSessionType.PointToPoint
        : SlimSessionType.Group;

    /// <summary>Check if this session is the initiator.</summary>
    public bool IsInitiator => _inner.IsInitiator();

    /// <summary>Get the source name.</summary>
    public SlimName Source => new(_inner.Source());

    /// <summary>Get the destination name.</summary>
    public SlimName Destination => new(_inner.Destination());

    /// <summary>
    /// Publish a message to the session destination.
    /// </summary>
    /// <param name="data">Message payload.</param>
    /// <param name="payloadType">Optional content type.</param>
    /// <param name="metadata">Optional metadata.</param>
    public void Publish(byte[] data, string? payloadType = null, Dictionary<string, string>? metadata = null)
    {
        _inner.PublishAndWait(data, payloadType, metadata);
    }

    /// <summary>
    /// Publish a string message to the session destination.
    /// </summary>
    public void Publish(string message, string? payloadType = null, Dictionary<string, string>? metadata = null)
    {
        Publish(Encoding.UTF8.GetBytes(message), payloadType, metadata);
    }

    /// <summary>
    /// Publish a message asynchronously.
    /// </summary>
    public async Task PublishAsync(byte[] data, string? payloadType = null, Dictionary<string, string>? metadata = null)
    {
        await _inner.PublishAndWaitAsync(data, payloadType, metadata);
    }

    /// <summary>
    /// Publish a string message asynchronously.
    /// </summary>
    public Task PublishAsync(string message, string? payloadType = null, Dictionary<string, string>? metadata = null)
    {
        return PublishAsync(Encoding.UTF8.GetBytes(message), payloadType, metadata);
    }

    /// <summary>
    /// Receive a message from the session.
    /// </summary>
    /// <param name="timeout">Optional timeout duration.</param>
    public SlimMessage GetMessage(TimeSpan? timeout = null)
    {
        var msg = _inner.GetMessage(timeout);
        return new SlimMessage(msg);
    }

    /// <summary>
    /// Receive a message asynchronously.
    /// </summary>
    public async Task<SlimMessage> GetMessageAsync(TimeSpan? timeout = null)
    {
        var msg = await _inner.GetMessageAsync(timeout);
        return new SlimMessage(msg);
    }

    /// <summary>
    /// Reply to a received message.
    /// </summary>
    /// <param name="originalMessage">The message to reply to.</param>
    /// <param name="data">Reply payload.</param>
    /// <param name="payloadType">Optional content type.</param>
    /// <param name="metadata">Optional metadata.</param>
    public void Reply(SlimMessage originalMessage, byte[] data, string? payloadType = null, Dictionary<string, string>? metadata = null)
    {
        _inner.PublishToAndWait(originalMessage._context, data, payloadType, metadata);
    }

    /// <summary>
    /// Reply to a received message with a string.
    /// </summary>
    public void Reply(SlimMessage originalMessage, string message, string? payloadType = null, Dictionary<string, string>? metadata = null)
    {
        Reply(originalMessage, Encoding.UTF8.GetBytes(message), payloadType, metadata);
    }

    /// <summary>
    /// Reply to a received message asynchronously.
    /// </summary>
    public async Task ReplyAsync(SlimMessage originalMessage, byte[] data, string? payloadType = null, Dictionary<string, string>? metadata = null)
    {
        await _inner.PublishToAndWaitAsync(originalMessage._context, data, payloadType, metadata);
    }

    /// <summary>
    /// Reply to a received message asynchronously with a string.
    /// </summary>
    public Task ReplyAsync(SlimMessage originalMessage, string message, string? payloadType = null, Dictionary<string, string>? metadata = null)
    {
        return ReplyAsync(originalMessage, Encoding.UTF8.GetBytes(message), payloadType, metadata);
    }

    /// <summary>
    /// Get list of participants in the session.
    /// </summary>
    public IReadOnlyList<SlimName> GetParticipants()
    {
        return _inner.ParticipantsList().Select(n => new SlimName(n)).ToList();
    }

    /// <summary>
    /// Invite a participant to the session.
    /// </summary>
    public void Invite(SlimName participant)
    {
        _inner.InviteAndWait(participant._inner);
    }

    /// <summary>
    /// Remove a participant from the session.
    /// </summary>
    public void Remove(SlimName participant)
    {
        _inner.RemoveAndWait(participant._inner);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _inner.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Exception thrown by SLIM operations.
/// </summary>
public class SlimException : Exception
{
    internal SlimException(Exception inner) : base(inner.Message, inner) { }
    
    /// <summary>
    /// Check if this is a timeout error.
    /// </summary>
    public bool IsTimeout => Message.Contains("timeout", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Check if this is a closed connection/session error.
    /// </summary>
    public bool IsClosed => Message.Contains("closed", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Check if this is a transient error that may succeed on retry.
    /// </summary>
    public bool IsTransient => IsTimeout || 
        Message.Contains("temporarily", StringComparison.OrdinalIgnoreCase) ||
        Message.Contains("retry", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Extension methods for classifying SLIM-related exceptions.
/// Works with any exception type, including internal FFI exceptions.
/// </summary>
public static class SlimExceptionExtensions
{
    /// <summary>
    /// Check if the exception indicates a timeout error.
    /// </summary>
    public static bool IsTimeoutError(this Exception ex)
    {
        return ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if the exception indicates a closed connection/session.
    /// </summary>
    public static bool IsClosedError(this Exception ex)
    {
        return ex.Message.Contains("closed", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if the exception is a transient error that may succeed on retry.
    /// </summary>
    public static bool IsTransientError(this Exception ex)
    {
        return ex.IsTimeoutError() || 
               ex.Message.Contains("temporarily", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("retry", StringComparison.OrdinalIgnoreCase);
    }
}
