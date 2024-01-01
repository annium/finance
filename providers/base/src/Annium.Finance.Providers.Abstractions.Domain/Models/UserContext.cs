using System.Collections.Generic;

namespace Annium.Finance.Providers.Abstractions.Domain.Models;

public sealed record UserContext(IReadOnlyCollection<AssetModel> Assets, IReadOnlyCollection<PositionModel> Positions);
