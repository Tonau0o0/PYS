using Microsoft.AspNetCore.Http.HttpResults;
using PYS.Service.Common;

namespace PYS.API.Common;

internal static class ResultMapper
{
    public static IResult ToHttp<T>(this ServiceResult<T> result) => result.Status switch
    {
        ResultStatus.Success => Results.Ok(result.Data),
        ResultStatus.NotFound => Results.NotFound(new { error = result.Error }),
        ResultStatus.ValidationError => Results.BadRequest(new { error = result.Error, details = result.ValidationErrors }),
        ResultStatus.Conflict => Results.Conflict(new { error = result.Error }),
        ResultStatus.Unauthorized => Results.Unauthorized(),
        _ => Results.Problem(result.Error ?? "Unexpected error.")
    };

    public static IResult ToHttp(this ServiceResult result) => result.Status switch
    {
        ResultStatus.Success => Results.NoContent(),
        ResultStatus.NotFound => Results.NotFound(new { error = result.Error }),
        ResultStatus.ValidationError => Results.BadRequest(new { error = result.Error, details = result.ValidationErrors }),
        ResultStatus.Conflict => Results.Conflict(new { error = result.Error }),
        ResultStatus.Unauthorized => Results.Unauthorized(),
        _ => Results.Problem(result.Error ?? "Unexpected error.")
    };

    public static IResult ToCreated<T>(this ServiceResult<T> result, string locationPattern)
    {
        if (!result.IsSuccess) return result.ToHttp();
        var idProp = typeof(T).GetProperty("Id");
        var id = idProp?.GetValue(result.Data);
        var location = id is null ? locationPattern : $"{locationPattern}/{id}";
        return Results.Created(location, result.Data);
    }
}
