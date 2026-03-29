namespace A2V10.Xaml.LanguageServer.Application;

public sealed class TextDocumentStore
{
    private readonly Dictionary<string, TextDocumentState> _documents = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();

    public void Open(string documentUri, string text, string? projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentUri);

        var filePath = GetFilePath(documentUri);
        lock (_lock)
        {
            _documents[documentUri] = new TextDocumentState(documentUri, filePath, text, projectPath);
        }
    }

    public void Update(string documentUri, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentUri);

        lock (_lock)
        {
            if (_documents.TryGetValue(documentUri, out var state))
            {
                _documents[documentUri] = state with { Text = text };
            }
            else
            {
                _documents[documentUri] = new TextDocumentState(documentUri, GetFilePath(documentUri), text, null);
            }
        }
    }

    public bool TryGet(string documentUri, out TextDocumentState? state)
    {
        lock (_lock)
        {
            return _documents.TryGetValue(documentUri, out state);
        }
    }

    public void Close(string documentUri)
    {
        lock (_lock)
        {
            _documents.Remove(documentUri);
        }
    }

    private static string GetFilePath(string documentUri)
    {
        if (Uri.TryCreate(documentUri, UriKind.Absolute, out var uri) && uri.IsFile)
        {
            return uri.LocalPath;
        }

        return documentUri;
    }
}

public sealed record TextDocumentState(string DocumentUri, string FilePath, string Text, string? ProjectPath);
