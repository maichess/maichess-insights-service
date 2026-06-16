using MaichessInsightsService.Domain;
using Reqnroll;
using Xunit;

namespace MaichessInsightsService.Tests.StepDefinitions;

[Binding]
internal sealed class StatusMappingSteps
{
    private JobStatus current;
    private JobStatus mapped;

    [Given(@"a job currently ""([^""]*)""")]
    public void GivenAJobCurrently(string status) => current = JobStatusNames.FromName(status);

    [When(@"the SparkApplication reports state ""([^""]*)""")]
    public void WhenTheSparkApplicationReportsState(string state) =>
        mapped = SparkApplicationState.ToJobStatus(state, current);

    [Then(@"the mapped status is ""([^""]*)""")]
    public void ThenTheMappedStatusIs(string status) =>
        Assert.Equal(JobStatusNames.FromName(status), mapped);
}
