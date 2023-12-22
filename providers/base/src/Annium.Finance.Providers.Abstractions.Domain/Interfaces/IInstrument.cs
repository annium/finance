namespace Annium.Finance.Providers.Abstractions.Domain.Interfaces;

public interface IInstrument
{
    decimal LotSize { get; }
    decimal TickSize { get; }
    decimal MinQty { get; }
    decimal MaxQty { get; }
    decimal MinSum { get; }
    decimal MaxSum { get; }
}
