using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Abstractions.Connectors.Sync;

public interface IUserSynchronizer
{
    Task ExecuteAsync(UserSettings settings, IUserProvider provider);
}
