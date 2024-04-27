using System.Diagnostics.CodeAnalysis;

namespace Annium.Finance.Providers.Abstractions.Domain.Operations;

public interface IBaseResult<out T>
{
    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsSuccess { get; }
    public bool IsAborted { get; }
    public bool IsFailure { get; }

    [MemberNotNullWhen(false, nameof(Data))]
    public bool IsFailureOrAborted { get; }

    public T? Data { get; }
    public string Message { get; }
}

public interface IBaseResult
{
    public bool IsSuccess { get; }
    public bool IsAborted { get; }
    public bool IsFailure { get; }
    public bool IsFailureOrAborted { get; }
    public string Message { get; }
}
