using A2V10.Xaml.LanguageServer.Application;
using A2V10.Xaml.LanguageServer.Protocol;

var composition = LanguageServerComposition.CreateDefault();

if (args.Length >= 2 && string.Equals(args[0], "--complete", StringComparison.OrdinalIgnoreCase))
{
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

    return;
}

Console.WriteLine("A2V10 XAML language server skeleton is ready.");
Console.WriteLine("Use '--complete <filePath> [position] [projectPath]' for a local completion smoke test.");
