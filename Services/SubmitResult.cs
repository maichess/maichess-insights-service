using MaichessInsightsService.Domain;

namespace MaichessInsightsService.Services;

// Outcome of a submit: the created job, a 400 reason, or a 404 (analysis over an
// unknown corpus).
internal abstract record SubmitResult
{
    internal sealed record Success(JobRecord Job) : SubmitResult;

    internal sealed record InvalidInput(string Message) : SubmitResult;

    internal sealed record NotFound(string Message) : SubmitResult;
}
