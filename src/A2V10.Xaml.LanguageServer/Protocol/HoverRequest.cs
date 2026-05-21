namespace A2V10.Xaml.LanguageServer.Protocol;

public sealed record HoverRequest(
    string FilePath,
    int Position,
    string? ProjectPath = null,
    string? Text = null);
