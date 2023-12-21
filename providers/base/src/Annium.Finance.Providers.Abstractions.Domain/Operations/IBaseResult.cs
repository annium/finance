using System.Diagnostics.CodeAnalysis;

namespace Annium.Finance.Providers.Abstractions.Domain.Operations;

public interface IBaseResult<out T>
{
    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsSuccess { get; }

    [MemberNotNullWhen(false, nameof(Data))]
    public bool IsFailure { get; }

    public T? Data { get; }
    public string Message { get; }
}

public interface IBaseResult
{
    public bool IsSuccess { get; }
    public bool IsFailure { get; }
    public string Message { get; }
}
