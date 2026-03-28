namespace A2V10.Xaml.Core.Models;

public sealed record CompletionSuggestion(
    string Label,
    string InsertText,
    string? Detail,
    XamlCompletionKind Kind);
