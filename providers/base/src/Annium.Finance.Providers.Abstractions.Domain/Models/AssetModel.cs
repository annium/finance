namespace Annium.Finance.Providers.Abstractions.Domain.Models;

public sealed record AssetModel
{
    public string Resource { get; }
    public decimal Free { get; private set; }
    public decimal Locked { get; private set; }

    public AssetModel(string resource, decimal free, decimal locked)
    {
        Resource = resource;
        Free = free;
        Locked = locked;
    }

    public void Update(decimal free, decimal locked)
    {
        Free = free;
        Locked = locked;
    }
}
