using System.Text;

namespace InpcFieldGenerator.Generation;

internal sealed class IndentedStringBuilder
{
    private const int IndentWidth = 4;
    private readonly StringBuilder _builder = new();
    private int _indentLevel;

    public void IncreaseIndent() => _indentLevel++;

    public void DecreaseIndent()
    {
        if (_indentLevel > 0)
        {
            _indentLevel--;
        }
    }

    public void AppendLine() => _builder.AppendLine();

    public void AppendLine(string value)
    {
        if (_indentLevel > 0)
        {
            _builder.Append(' ', _indentLevel * IndentWidth);
        }

        _builder.AppendLine(value);
    }

    public void Append(string value)
    {
        if (_indentLevel > 0)
        {
            _builder.Append(' ', _indentLevel * IndentWidth);
        }

        _builder.Append(value);
    }

    public override string ToString() => _builder.ToString();
}
