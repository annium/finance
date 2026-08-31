using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Annium.Finance.Providers.Core.Internal.User;

/// <summary>
/// Default <see cref="IUserConnectorFactory"/> implementation. Builds a standalone user connector in its own DI
/// scope, resolving the provider-specific instance factory registered for the settings' provider key.
/// </summary>
/// <param name="sp">The root service provider used to create the connector's own DI scope.</param>
/// <param name="logger">The logger instance.</param>
internal class UserConnectorFactory(IServiceProvider sp, ILogger logger) : IUserConnectorFactory, ILogSubject
{
    /// <summary>Gets the logger instance.</summary>
    public ILogger Logger { get; } = logger;

    /// <summary>
    /// Creates a user connector configured with the given settings.
    /// </summary>
    /// <param name="settings">The user settings identifying the provider and account to connect to.</param>
    /// <returns>A new user connector instance.</returns>
    public IUserConnector Create(UserSettings settings)
    {
        var providerKey = settings.GetProviderKey();

        this.Trace("{key} - create disposable box for {settings}", providerKey, settings);
        var disposable = Disposable.AsyncBox(Logger);

        this.Trace("{key} - create new scope for {settings}", providerKey, settings);
        var scope = sp.CreateAsyncScope();

        this.Trace("{key} - resolve factory for {settings}", providerKey, settings);
        var factory = scope.ServiceProvider.ResolveKeyed<IUserConnectorInstanceFactory>(settings.Provider);

        this.Trace<ProviderKey, UserSettings, string>(
            "{key} - create connector for {settings} with {factory}",
            providerKey,
            settings,
            factory.GetFullId()
        );
        var connector = factory.Create(settings, disposable);

        // the scope is not put in the connector's own box: that box drains its asynchronous entries
        // concurrently, so the scope would tear down alongside the executor rather than after it, and
        // the executor may still be draining a sync cycle that is using what the scope owns
        return new ScopedUserConnector(connector, scope);
    }
}
