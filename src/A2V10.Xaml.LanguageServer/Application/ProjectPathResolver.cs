namespace A2V10.Xaml.LanguageServer.Application;

public static class ProjectPathResolver
{
    public static string? FindProjectPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        var currentDirectory = File.Exists(filePath)
            ? Path.GetDirectoryName(filePath)
            : filePath;

        while (!string.IsNullOrWhiteSpace(currentDirectory))
        {
            var projectPath = Directory.EnumerateFiles(currentDirectory, "*.csproj", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (projectPath is not null)
            {
                return projectPath;
            }

            currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
        }

        return null;
    }
}
