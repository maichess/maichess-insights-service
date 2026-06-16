using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using MaichessInsightsService.Domain;
using MaichessInsightsService.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaichessInsightsService.Rest;

// REST surface for the insights control plane (rest/insights.md). Excluded from
// coverage: thin HTTP adapter over the tested JobService + the MinIO upload seam.
[ExcludeFromCodeCoverage]
internal static class InsightsEndpoints
{
    internal static IEndpointRouteBuilder MapInsightsEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/insights").RequireAuthorization();
        group.MapPost("/ingestions", SubmitIngestion);
        group.MapPost("/uploads", Upload).DisableAntiforgery();
        group.MapPost("/analyses", SubmitAnalysis);
        group.MapGet("/jobs", ListJobs);
        group.MapGet("/jobs/{id}", GetJob);
        group.MapGet("/corpora", ListCorpora);
        group.MapGet("/corpora/{id}/summary", GetCorpusSummary);
        group.MapGet("/corpora/{id}/openings", GetTopOpenings);
        group.MapGet("/corpora/{id}/endgames", GetCommonEndgames);
        group.MapGet("/corpora/{id}/positions", GetCommonPositions);
        group.MapGet("/corpora/{id}/tricky", GetTrickyPositions);
        return routes;
    }

    private static async Task<IResult> SubmitIngestion(
        [FromBody] IngestionRequestDto body, ClaimsPrincipal principal, JobService jobs, CancellationToken ct)
    {
        if (!TryGetUserId(principal, out string userId))
        {
            return Results.Unauthorized();
        }

        IngestionInput input = new(
            body.LichessMonth is null ? null : new LichessMonthInput(body.LichessMonth.YearMonth ?? string.Empty),
            body.Upload is null ? null : new UploadInput(body.Upload.ObjectKey ?? string.Empty, body.Upload.Label ?? string.Empty),
            ToFilter(body.Filter));

        SubmitResult result = await jobs.SubmitIngestionAsync(input, userId, ct);
        return Accepted(result);
    }

    private static async Task<IResult> SubmitAnalysis(
        [FromBody] AnalysisRequestDto body, ClaimsPrincipal principal, JobService jobs, CancellationToken ct)
    {
        if (!TryGetUserId(principal, out string userId))
        {
            return Results.Unauthorized();
        }

        AnalysisInput input = new(body.CorpusId ?? string.Empty, body.Kinds ?? []);
        SubmitResult result = await jobs.SubmitAnalysisAsync(input, userId, ct);
        return Accepted(result);
    }

    private static async Task<IResult> Upload(
        HttpRequest request, ClaimsPrincipal principal, IObjectStore store, CancellationToken ct)
    {
        if (!TryGetUserId(principal, out _))
        {
            return Results.Unauthorized();
        }

        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new ErrorResponse("multipart/form-data with a file part is required"));
        }

        IFormCollection form = await request.ReadFormAsync(ct);
        IFormFile? file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new ErrorResponse("a non-empty file part is required"));
        }

        string label = form.TryGetValue("label", out Microsoft.Extensions.Primitives.StringValues value)
            ? value.ToString()
            : string.Empty;

        await using Stream content = file.OpenReadStream();
        string objectKey = await store.StorePgnAsync(content, file.FileName, ct);
        return Results.Created($"/insights/uploads/{objectKey}", new UploadResponseDto(objectKey, label));
    }

    private static async Task<IResult> ListJobs(
        [FromQuery] string? status,
        JobService jobs,
        CancellationToken ct,
        [FromQuery] int limit = 0,
        [FromQuery] int offset = 0)
    {
        JobStatus? filter = !string.IsNullOrEmpty(status) && JobStatusNames.TryParse(status, out JobStatus parsed)
            ? parsed
            : null;
        IReadOnlyList<JobRecord> result = await jobs.ListJobsAsync(filter, limit, offset, ct);
        return Results.Ok(new JobListResponse([.. result.Select(InsightsViews.ToView)]));
    }

    private static async Task<IResult> GetJob(string id, JobService jobs, CancellationToken ct)
    {
        JobRecord? job = await jobs.GetJobAsync(id, ct);
        return job is null ? Results.NotFound() : Results.Ok(InsightsViews.ToView(job));
    }

    private static async Task<IResult> ListCorpora(
        JobService jobs, CancellationToken ct, [FromQuery] int limit = 0, [FromQuery] int offset = 0)
    {
        IReadOnlyList<CorpusRecord> result = await jobs.ListCorporaAsync(limit, offset, ct);
        return Results.Ok(new CorpusListResponse([.. result.Select(InsightsViews.ToView)]));
    }

    private static async Task<IResult> GetCorpusSummary(string id, InsightsQueryService queries, CancellationToken ct)
    {
        CorpusSummaryMetric? summary = await queries.GetCorpusSummaryAsync(id, ct);
        return summary is null ? Results.NotFound() : Results.Ok(new SummaryResponse(summary));
    }

    private static async Task<IResult> GetTopOpenings(
        string id,
        InsightsQueryService queries,
        CancellationToken ct,
        [FromQuery] string? color = null,
        [FromQuery(Name = "rating_band")] string? ratingBand = null,
        [FromQuery(Name = "time_control")] string? timeControl = null,
        [FromQuery] int limit = 0,
        [FromQuery] int offset = 0)
    {
        OpeningsQuery query = new(
            id, color ?? string.Empty, ratingBand ?? string.Empty, timeControl ?? string.Empty, limit, offset);
        IReadOnlyList<OpeningMetric>? rows = await queries.GetTopOpeningsAsync(query, ct);
        return rows is null ? Results.NotFound() : Results.Ok(new OpeningsResponse(rows));
    }

    private static async Task<IResult> GetCommonEndgames(
        string id,
        InsightsQueryService queries,
        CancellationToken ct,
        [FromQuery] int limit = 0,
        [FromQuery] int offset = 0)
    {
        IReadOnlyList<EndgameMetric>? rows = await queries.GetCommonEndgamesAsync(
            new PagedQuery(id, limit, offset), ct);
        return rows is null ? Results.NotFound() : Results.Ok(new EndgamesResponse(rows));
    }

    private static async Task<IResult> GetCommonPositions(
        string id,
        InsightsQueryService queries,
        CancellationToken ct,
        [FromQuery(Name = "exclude_book")] bool excludeBook = false,
        [FromQuery] int limit = 0,
        [FromQuery] int offset = 0)
    {
        IReadOnlyList<PositionMetric>? rows = await queries.GetCommonPositionsAsync(
            new PositionsQuery(id, excludeBook, limit, offset), ct);
        return rows is null ? Results.NotFound() : Results.Ok(new PositionsResponse(rows));
    }

    private static async Task<IResult> GetTrickyPositions(
        string id,
        InsightsQueryService queries,
        CancellationToken ct,
        [FromQuery] int limit = 0,
        [FromQuery] int offset = 0)
    {
        IReadOnlyList<TrickyMetric>? rows = await queries.GetTrickyPositionsAsync(
            new PagedQuery(id, limit, offset), ct);
        return rows is null ? Results.NotFound() : Results.Ok(new TrickyResponse(rows));
    }

    private static IResult Accepted(SubmitResult result) =>
        result switch
        {
            SubmitResult.Success ok => Results.Json(InsightsViews.ToView(ok.Job), statusCode: StatusCodes.Status202Accepted),
            SubmitResult.NotFound missing => Results.NotFound(new ErrorResponse(missing.Message)),
            SubmitResult.InvalidInput bad => Results.BadRequest(new ErrorResponse(bad.Message)),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
        };

    private static CorpusFilterSpec ToFilter(FilterDto? f) =>
        f is null
            ? CorpusFilterSpec.Empty
            : new CorpusFilterSpec(
                f.RatingBand ?? string.Empty, f.TimeControl ?? string.Empty, f.DateFromMs, f.DateToMs, f.SampleRate);

    private static bool TryGetUserId(ClaimsPrincipal principal, out string userId)
    {
        string? value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        userId = value ?? string.Empty;
        return value is not null;
    }
}
