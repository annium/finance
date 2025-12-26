using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Annium.Finance.Providers.Core.Internal.User;

internal class UserConnectorFactory(IServiceProvider sp, ILogger logger) : IUserConnectorFactory, ILogSubject
{
    public ILogger Logger { get; } = logger;

    public IUserConnector Create(UserSettings settings)
    {
        var providerKey = settings.GetProviderKey();

        this.Trace("{key} - create new scope for {settings}", providerKey, settings);
        var scope = sp.CreateAsyncScope();

        this.Trace("{key} - resolve factory for {settings}", providerKey, settings);
        var factory = scope.ServiceProvider.ResolveKeyed<IUserConnectorFactory>(settings.Provider);

        this.Trace<ProviderKey, UserSettings, string>(
            "{key} - create connector for {settings} with {factory}",
            providerKey,
            settings,
            factory.GetFullId()
        );
        var connector = factory.Create(settings);

        return new UserConnectorContainer(scope, connector, Logger);
    }
}
