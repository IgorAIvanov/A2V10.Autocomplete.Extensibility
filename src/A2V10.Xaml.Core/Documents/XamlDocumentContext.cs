namespace A2V10.Xaml.Core.Documents;

public sealed record XamlDocumentContext(
    Uri DocumentUri,
    string Text,
    string? ProjectPath = null,
    string? RootNamespace = null);
