using System;
using Annium.Core.DependencyInjection;
using Annium.Core.Mapper;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Logging;

namespace Annium.Finance.Providers.Shared.Internal.Connectors;

internal sealed class UserConnectorCacheProvider : ConnectorCacheProvider<UserSettings, IUserConnector>
{
    private readonly IMapper _mapper;

    public UserConnectorCacheProvider(IServiceProvider sp, IMapper mapper, ILogger logger)
        : base(sp, logger)
    {
        _mapper = mapper;
    }

    protected override void Inject(IServiceProvider scopeProvider, UserSettings settings)
    {
        scopeProvider.Resolve<Injected<MarketSettings>>().Init(_mapper.Map<MarketSettings>(settings));
        scopeProvider.Resolve<Injected<UserSettings>>().Init(settings);
    }
}
