namespace A2V10.Xaml.LanguageServer.Protocol;

public sealed record HoverResponse(string Contents, string SourceText, int Start, int End);
