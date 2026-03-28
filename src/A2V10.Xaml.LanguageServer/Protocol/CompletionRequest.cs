namespace A2V10.Xaml.LanguageServer.Protocol;

public sealed record CompletionRequest(string FilePath, int Position, string? ProjectPath = null);
