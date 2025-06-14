using System.Diagnostics.CodeAnalysis;

namespace Annium.Finance.Providers.Abstractions.Domain.Operations;

public interface IBaseResult<out T>
{
    [MemberNotNullWhen(true, nameof(Data))]
    bool IsSuccess { get; }
    bool IsAborted { get; }
    bool IsFailure { get; }

    [MemberNotNullWhen(false, nameof(Data))]
    bool IsFailureOrAborted { get; }

    T? Data { get; }
    string Message { get; }
}

public interface IBaseResult
{
    bool IsSuccess { get; }
    bool IsAborted { get; }
    bool IsFailure { get; }
    bool IsFailureOrAborted { get; }
    string Message { get; }
}
