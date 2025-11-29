using System.Diagnostics.CodeAnalysis;

namespace Annium.Finance.Providers.Abstractions.Domain.Operations;

public interface IBaseResult<out T>
{
    bool IsNetworkError { get; }

    bool IsAborted { get; }

    [MemberNotNullWhen(true, nameof(Data))]
    bool IsSuccess { get; }

    bool IsFailure { get; }

    T? Data { get; }

    string Message { get; }
}

public interface IBaseResult
{
    bool IsNetworkError { get; }

    bool IsAborted { get; }

    bool IsSuccess { get; }

    bool IsFailure { get; }

    string Message { get; }
}
