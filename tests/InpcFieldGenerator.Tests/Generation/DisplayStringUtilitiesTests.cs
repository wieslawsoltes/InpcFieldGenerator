using FluentAssertions;
using InpcFieldGenerator.Generation;
using Xunit;

namespace InpcFieldGenerator.Tests.Generation;

public static class DisplayStringUtilitiesTests
{
    [Theory]
    [InlineData("global::Sample.Namespace", "Sample.Namespace")]
    [InlineData("Sample.Namespace", "Sample.Namespace")]
    public static void TrimGlobalPrefix_ReturnsExpectedValue(string input, string expected)
    {
        DisplayStringUtilities.TrimGlobalPrefix(input).Should().Be(expected);
    }
}
