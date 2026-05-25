using System.ComponentModel.DataAnnotations;

namespace PYS.API.Common;

internal static class DataAnnotationsValidator
{
    public static IResult? Validate<T>(T model) where T : class
    {
        var ctx = new ValidationContext(model);
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(model, ctx, results, validateAllProperties: true))
        {
            return null;
        }

        var errors = results.Select(r => r.ErrorMessage ?? "Invalid").ToArray();
        return Results.BadRequest(new { error = "Validation failed", details = errors });
    }
}
