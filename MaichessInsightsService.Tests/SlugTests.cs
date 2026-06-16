using MaichessInsightsService.Domain;
using Xunit;

namespace MaichessInsightsService.Tests;

public class SlugTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("blitz", "blitz")]
    [InlineData("1600-1999", "1600-1999")]
    [InlineData("UPPER", "upper")]
    [InlineData("Hello World!", "hello-world")]
    [InlineData("--a__b--", "a-b")]
    [InlineData("....", "")]
    [InlineData("a", "a")]
    public void MakeProducesRfc1123Slug(string input, string expected) =>
        Assert.Equal(expected, Slug.Make(input));
}
