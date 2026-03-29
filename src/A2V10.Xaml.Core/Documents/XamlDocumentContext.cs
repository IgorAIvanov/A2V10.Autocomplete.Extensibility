namespace A2V10.Xaml.Core.Documents;

public sealed record XamlDocumentContext(
    Uri DocumentUri,
    string Text,
    string? ProjectPath = null,
    string? RootNamespace = null);

public static partial class XamlAssemblyReferenceDetector
{
    public static bool ContainsAssemblyReference(string? xamlText, string assemblyName)
    {
        if (string.IsNullOrWhiteSpace(xamlText) || string.IsNullOrWhiteSpace(assemblyName))
        {
            return false;
        }

        var pattern = $@"assembly\s*=\s*{System.Text.RegularExpressions.Regex.Escape(assemblyName)}(?=$|[\s;""'])";
        return System.Text.RegularExpressions.Regex.IsMatch(
            xamlText,
            pattern,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    }
}
