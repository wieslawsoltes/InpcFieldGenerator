using FluentAssertions;
using InpcFieldGenerator.Abstractions;
using Xunit;

namespace InpcFieldGenerator.Tests.Attributes;

public static class ReactiveViewModelAttributeTests
{
    [Fact]
    public static void Constructor_Sets_Defaults()
    {
        var attribute = new ReactiveViewModelAttribute();

        attribute.ViewModelMember.Should().BeNull();
        attribute.NotifyOnChanging.Should().BeFalse();
        attribute.GenerateEqualityCheck.Should().BeFalse();
    }

    [Fact]
    public static void Properties_Are_Assignable()
    {
        var attribute = new ReactiveViewModelAttribute
        {
            ViewModelMember = "Raise",
            NotifyOnChanging = true,
            GenerateEqualityCheck = true,
        };

        attribute.ViewModelMember.Should().Be("Raise");
        attribute.NotifyOnChanging.Should().BeTrue();
        attribute.GenerateEqualityCheck.Should().BeTrue();
    }
}
