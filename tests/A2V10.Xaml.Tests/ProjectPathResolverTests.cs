using A2V10.Xaml.LanguageServer.Application;

namespace A2V10.Xaml.Tests;

public sealed class ProjectPathResolverTests
{
    [Fact]
    public void FindProjectPath_ReturnsNearestCsproj()
    {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var projectDirectory = Path.Combine(root, "src", "Sample");
        var nestedDirectory = Path.Combine(projectDirectory, "Views");
        Directory.CreateDirectory(nestedDirectory);

        var projectPath = Path.Combine(projectDirectory, "Sample.csproj");
        var filePath = Path.Combine(nestedDirectory, "Index.xaml");
        File.WriteAllText(projectPath, "<Project />");
        File.WriteAllText(filePath, "<Page />");

        try
        {
            Assert.Equal(projectPath, ProjectPathResolver.FindProjectPath(filePath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
