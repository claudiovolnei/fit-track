using FitTrack.Application.Dtos;

namespace FitTrack.Adapters.In.Api.Composition;

/// <summary>
/// Reads the search/pagination query string shared by every collection endpoint. The compatibility
/// rule lives here: without <c>page</c>/<c>pageSize</c> the endpoint answers the plain array it always
/// answered; with either one it answers the paginated envelope.
/// </summary>
internal static class ListRequest
{
    public static bool IsPaged(int? page, int? pageSize) =>
        page is not null || pageSize is not null;

    public static IResult? TryRead(
        string? search,
        int? page,
        int? pageSize,
        out ListQuery query)
    {
        query = ListQuery.All;

        if (search?.Trim().Length > ListQuery.MaxSearchLength)
        {
            return Results.Problem(
                title: "search.too_long",
                detail: $"A busca deve ter no máximo {ListQuery.MaxSearchLength} caracteres.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (page is < 1 || pageSize is < 1 or > ListQuery.MaxPageSize)
        {
            var errors = new Dictionary<string, string[]>();

            if (page is < 1)
            {
                errors["page"] = ["A página deve ser maior ou igual a 1."];
            }

            if (pageSize is < 1 or > ListQuery.MaxPageSize)
            {
                errors["pageSize"] = [$"O tamanho da página deve ficar entre 1 e {ListQuery.MaxPageSize}."];
            }

            return Results.ValidationProblem(
                errors,
                title: "pagination.invalid",
                statusCode: StatusCodes.Status400BadRequest);
        }

        query = IsPaged(page, pageSize)
            ? ListQuery.Paged(search, page ?? 1, pageSize ?? ListQuery.DefaultPageSize)
            : ListQuery.Filtered(search);

        return null;
    }

    public static IResult Ok<T>(PagedResult<T> result, bool paged) =>
        paged ? Results.Ok(result) : Results.Ok(result.Items);

    public static IResult Ok(WorkoutHistoryDto result, bool paged) =>
        paged ? Results.Ok(result) : Results.Ok(result.Items);
}
