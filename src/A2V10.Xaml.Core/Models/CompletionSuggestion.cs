namespace A2V10.Xaml.Core.Models;

public sealed record CompletionSuggestion(
    string Label,
    string InsertText,
    string? Detail,
    string? Documentation,
    XamlCompletionKind Kind,
    bool IsSnippet = false);
