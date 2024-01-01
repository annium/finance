namespace Annium.Finance.Providers.Abstractions.Domain.Models;

public sealed record ResourceModel
{
    public string Code { get; }
    public byte Precision { get; private set; }

    public ResourceModel(string code, byte precision)
    {
        Code = code;
        Precision = precision;
    }

    public void Update(byte precision)
    {
        Precision = precision;
    }
}
