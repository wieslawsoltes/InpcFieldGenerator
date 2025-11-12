using FluentAssertions;
using InpcFieldGenerator.Generation;
using Xunit;

namespace InpcFieldGenerator.Tests.Generation;

public static class IndentedStringBuilderTests
{
    [Fact]
    public static void AppendsLinesWithIndentation()
    {
        var builder = new IndentedStringBuilder();
        builder.AppendLine("class Foo");
        builder.AppendLine("{");
        builder.IncreaseIndent();
        builder.AppendLine("void Bar() {");
        builder.IncreaseIndent();
        builder.AppendLine("return;");
        builder.DecreaseIndent();
        builder.AppendLine("}");
        builder.DecreaseIndent();
        builder.AppendLine("}");

        var text = builder.ToString();

        text.Should().Contain("class Foo");
        text.Should().Contain("    void Bar() {");
        text.Should().Contain("        return;");
    }

    [Fact]
    public static void Append_WritesOnSingleLine()
    {
        var builder = new IndentedStringBuilder();
        builder.IncreaseIndent();
        builder.Append("partial ");
        builder.Append("class");

        builder.ToString().Should().Contain("partial     class");
    }
}
