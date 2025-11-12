using System;

namespace InpcFieldGenerator.Generation;

internal static class DisplayStringUtilities
{
    private const string GlobalPrefix = "global::";

    internal static string TrimGlobalPrefix(string value)
    {
        return value.StartsWith(GlobalPrefix, StringComparison.Ordinal)
            ? value.Substring(GlobalPrefix.Length)
            : value;
    }
}
