using MaichessInsightsService.Domain;
using MaichessInsightsService.Services;
using MaichessInsightsService.Tests.Support;
using Reqnroll;
using Xunit;

namespace MaichessInsightsService.Tests.StepDefinitions;

[Binding]
internal sealed class JobDispatchSteps(JobDispatchContext context)
{
    [Given(@"an ingested corpus ""([^""]*)""")]
    public async Task GivenAnIngestedCorpus(string corpusId) =>
        await context.Store.InsertCorpusAsync(
            new CorpusRecord { Id = corpusId, Source = IngestionSourceSpec.Lichess("2024-12"), Filter = CorpusFilterSpec.Empty },
            CancellationToken.None);

    [When(@"an ingestion is submitted for Lichess month ""([^""]*)""")]
    public async Task WhenAnIngestionForLichessMonth(string month) =>
        context.Result = await context.Service.SubmitIngestionAsync(
            new IngestionInput(new LichessMonthInput(month), null, CorpusFilterSpec.Empty), "user-1", CancellationToken.None);

    [When(@"an ingestion is submitted for uploaded key ""([^""]*)""")]
    public async Task WhenAnIngestionForUpload(string key) =>
        context.Result = await context.Service.SubmitIngestionAsync(
            new IngestionInput(null, new UploadInput(key, "label"), CorpusFilterSpec.Empty), "user-1", CancellationToken.None);

    [When(@"an analysis is submitted for corpus ""([^""]*)"" with kinds ""([^""]*)""")]
    public async Task WhenAnAnalysisIsSubmitted(string corpusId, string kinds)
    {
        IReadOnlyList<string> parsed = kinds.Length == 0 ? [] : [.. kinds.Split(',')];
        context.Result = await context.Service.SubmitAnalysisAsync(
            new AnalysisInput(corpusId, parsed), "user-1", CancellationToken.None);
    }

    [Then("the submit succeeds")]
    public void ThenTheSubmitSucceeds() => Assert.IsType<SubmitResult.Success>(context.Result);

    [Then("the submit is not found")]
    public void ThenTheSubmitIsNotFound() => Assert.IsType<SubmitResult.NotFound>(context.Result);

    [Then(@"the job is an ingestion for corpus ""([^""]*)""")]
    public void ThenTheJobIsAnIngestionForCorpus(string corpusId)
    {
        JobRecord job = Assert.IsType<SubmitResult.Success>(context.Result).Job;
        Assert.Equal(JobType.Ingestion, job.Type);
        Assert.Equal(corpusId, job.CorpusId);
    }

    [Then(@"a SparkApplication ""([^""]*)"" of class ""([^""]*)"" was launched")]
    public void ThenASparkApplicationNamedWasLaunched(string name, string mainClass)
    {
        Assert.Equal(name, context.Launcher.Last.ApplicationName);
        Assert.Equal(mainClass, context.Launcher.Last.MainClass);
    }

    [Then(@"a SparkApplication of class ""([^""]*)"" was launched")]
    public void ThenASparkApplicationOfClassWasLaunched(string mainClass) =>
        Assert.Equal(mainClass, context.Launcher.Last.MainClass);

    [Then("no SparkApplication was launched")]
    public void ThenNoSparkApplicationWasLaunched() => Assert.Empty(context.Launcher.Launched);

    [Then(@"the launch argument ""([^""]*)"" is ""([^""]*)""")]
    public void ThenTheLaunchArgumentIs(string flag, string value)
    {
        IReadOnlyList<string> args = context.Launcher.Last.Arguments;
        int index = -1;
        for (int i = 0; i < args.Count - 1; i++)
        {
            if (args[i] == flag)
            {
                index = i;
                break;
            }
        }

        Assert.True(index >= 0, $"argument {flag} not present");
        Assert.Equal(value, args[index + 1]);
    }

    [Then("a submitted event was emitted for the job")]
    public void ThenASubmittedEventWasEmitted()
    {
        JobRecord job = Assert.IsType<SubmitResult.Success>(context.Result).Job;
        Assert.Contains(("submitted", job.Id), context.Events.Events);
    }
}
</content>
