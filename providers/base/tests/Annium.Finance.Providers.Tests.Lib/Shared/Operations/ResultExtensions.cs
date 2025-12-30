using Annium.Data.Operations;

namespace Annium.Finance.Providers.Tests.Lib.Shared.Operations;

public static class ResultExtensions
{
    public static IResult<T> AsResult<T>(this T value)
    {
        return Result.New(value);
    }
}
