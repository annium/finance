using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Connectors.Sync;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Shared.Internal.Sync;

internal class NoopUserSynchronizer : IUserSynchronizer
{
    public Task ExecuteAsync(UserSettings settings, IUserProvider provider)
    {
        return Task.CompletedTask;
    }
}
