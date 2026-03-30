using A2V10.Xaml.LanguageServer.Application;
using A2V10.Xaml.LanguageServer.Protocol;

try
{
    await Console.Error.WriteLineAsync($"[A2V10.Xaml.LanguageServer] Starting. Args: {string.Join(' ', args)}");

    var composition = LanguageServerComposition.CreateDefault();
    var host = new LspServerHost(composition.CompletionHandler, new TextDocumentStore());

    if (args.Length >= 2 && string.Equals(args[0], "--complete", StringComparison.OrdinalIgnoreCase))
    {
        await Console.Error.WriteLineAsync("[A2V10.Xaml.LanguageServer] Running in completion smoke test mode.");

        var filePath = Path.GetFullPath(args[1]);
        var position = args.Length >= 3 && int.TryParse(args[2], out var parsedPosition)
            ? parsedPosition
            : (await File.ReadAllTextAsync(filePath)).Length;
        var projectPath = args.Length >= 4 ? args[3] : null;

        var response = await composition.CompletionHandler.HandleAsync(new CompletionRequest(filePath, position, projectPath));
        foreach (var item in response.Items)
        {
            Console.WriteLine($"{item.Kind}: {item.Label}");
        }

        return 0;
    }

    if (args.Length >= 1 && string.Equals(args[0], "--stdio", StringComparison.OrdinalIgnoreCase))
    {
        await Console.Error.WriteLineAsync("[A2V10.Xaml.LanguageServer] Running in stdio mode.");
        await host.RunAsync(Console.OpenStandardInput(), Console.OpenStandardOutput());
        return 0;
    }

    Console.WriteLine("A2V10 XAML language server skeleton is ready.");
    Console.WriteLine("Use '--complete <filePath> [position] [projectPath]' for a local completion smoke test.");
    Console.WriteLine("Use '--stdio' to run the minimal LSP server.");
    return 0;
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync($"[A2V10.Xaml.LanguageServer] Fatal error: {ex}");
    return 1;
}
