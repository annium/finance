using System.Collections.Generic;

namespace Annium.Finance.Providers.Abstractions.Domain.User;

public sealed record UserContext(IReadOnlyCollection<AssetModel> Assets, IReadOnlyCollection<PositionModel> Positions);
