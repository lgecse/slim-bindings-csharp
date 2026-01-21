// Copyright AGNTCY Contributors (https://github.com/agntcy)
// SPDX-License-Identifier: Apache-2.0

using SlimBindings;
using Xunit;

namespace SlimBindings.Tests;

/// <summary>
/// Shared fixture that initializes SLIM once for all tests.
/// </summary>
public class SlimFixture : IDisposable
{
    public SlimFixture()
    {
        Slim.Initialize();
    }

    public void Dispose()
    {
        Slim.Shutdown();
    }
}

/// <summary>
/// Collection definition to share the fixture across test classes.
/// </summary>
[CollectionDefinition("Slim")]
public class SlimCollection : ICollectionFixture<SlimFixture> { }

/// <summary>
/// Smoke tests that verify bindings load correctly (no server required).
/// </summary>
[Collection("Slim")]
public class SmokeTests
{
    [Fact]
    public void Initialize_Works()
    {
        Assert.True(Slim.IsInitialized);
    }

    [Fact]
    public void Version_ReturnsValue()
    {
        Assert.NotEmpty(Slim.Version);
    }

    [Fact]
    public void BuildInfo_ReturnsValue()
    {
        var info = Slim.BuildInfo;
        Assert.NotEmpty(info.Version);
    }

    [Fact]
    public void SlimName_ParsesCorrectly()
    {
        using var name = SlimName.Parse("org/app/v1");
        Assert.StartsWith("org/app/v1", name.ToString());
    }
}

/// <summary>
/// Integration tests that require a running SLIM server.
/// Run server first: cd ../slim/data-plane && task data-plane:run:server
/// These tests run sequentially to avoid connection conflicts.
/// </summary>
[Collection("Slim")]
[Trait("Category", "Integration")]
public class IntegrationTests
{
    private const string ServerEndpoint = "http://localhost:46357";
    private const string SharedSecret = "test-shared-secret-minimum-32-chars!!";

    [Fact]
    public void CreateApp_Succeeds()
    {
        var service = Slim.GetGlobalService();
        var app = service.CreateApp("test-org", "create-app-test", "v1", SharedSecret);

        Assert.NotNull(app);
        Assert.True(app.Id > 0);

        app.Destroy();
    }

    [Fact]
    public void Connect_CreateApp_Subscribe_Succeeds()
    {
        var service = Slim.GetGlobalService();
        
        // Connect
        var connId = Slim.Connect(ServerEndpoint);
        Assert.True(connId >= 0);

        // Create app
        var app = service.CreateApp("test-org", "full-test", "v1", SharedSecret);
        Assert.NotNull(app);

        // Subscribe
        app.Subscribe(app.Name, connId);

        // Set route
        using var destination = SlimName.Parse("test-org/other-app/v1");
        app.SetRoute(destination, connId);

        // Cleanup
        app.RemoveRoute(destination, connId);
        app.Unsubscribe(app.Name, connId);
        app.Destroy();
        service.Disconnect(connId);
    }
}
