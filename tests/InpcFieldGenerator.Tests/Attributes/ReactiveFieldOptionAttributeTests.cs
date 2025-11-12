using FluentAssertions;
using InpcFieldGenerator.Abstractions;
using Xunit;

namespace InpcFieldGenerator.Tests.Attributes;

public static class ReactiveFieldOptionAttributeTests
{
    [Fact]
    public static void Constructor_Sets_Defaults()
    {
        var attribute = new ReactiveFieldOptionAttribute();

        attribute.ViewModelMember.Should().BeNull();
        attribute.NotifyOnChanging.Should().BeFalse();
        attribute.GenerateEqualityCheck.Should().BeFalse();
        attribute.AlsoNotify.Should().BeEmpty();
    }

    [Fact]
    public static void Properties_Are_Assignable()
    {
        var attribute = new ReactiveFieldOptionAttribute
        {
            ViewModelMember = "Raise",
            NotifyOnChanging = true,
            GenerateEqualityCheck = true,
            AlsoNotify = new[] { "FullName" },
        };

        attribute.ViewModelMember.Should().Be("Raise");
        attribute.NotifyOnChanging.Should().BeTrue();
        attribute.GenerateEqualityCheck.Should().BeTrue();
        attribute.AlsoNotify.Should().ContainSingle().Which.Should().Be("FullName");
    }

    [Fact]
    public static void AlsoNotify_Rejects_Null()
    {
        var attribute = new ReactiveFieldOptionAttribute
        {
            AlsoNotify = null!,
        };

        attribute.AlsoNotify.Should().BeEmpty();
    }
}
