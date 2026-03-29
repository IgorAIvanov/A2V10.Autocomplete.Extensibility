namespace A2V10.Xaml.Core.Models;

public sealed record XamlCompletionContext(
    XamlCompletionKind Kind,
    string Prefix,
    string? TagName,
    string? AttributeName,
    int Position,
    bool IsClosingTag = false);
