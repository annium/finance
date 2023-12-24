namespace Annium.Finance.Providers.Abstractions.Domain.Dto;

public sealed record ResourceDto
{
    public string Code { get; }
    public byte Precision { get; private set; }

    public ResourceDto(string code, byte precision)
    {
        Code = code;
        Precision = precision;
    }

    public void Update(byte precision)
    {
        Precision = precision;
    }
}
