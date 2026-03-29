using A2V10.Xaml.Core.Models;
using A2V10.Xaml.Core.Services;

namespace A2V10.Xaml.Tests;

public sealed class CompletionServiceTests
{
    private readonly CompletionService _service = new();

    [Fact]
    public void GetSuggestions_ReturnsMatchingTags()
    {
        var metadata = CreateMetadata();

        var suggestions = _service.GetSuggestions(
            new XamlCompletionContext(XamlCompletionKind.TagName, "Gr", null, null, 0),
            metadata);

        Assert.Contains(suggestions, item => item.Label == "Grid");
    }

    [Fact]
    public void GetSuggestions_ReturnsMatchingAttributes()
    {
        var metadata = CreateMetadata();

        var suggestions = _service.GetSuggestions(
            new XamlCompletionContext(XamlCompletionKind.AttributeName, "Vis", "Grid", null, 0),
            metadata);

        Assert.Contains(suggestions, item => item.Label == "Visibility");
    }

    [Fact]
    public void GetSuggestions_ReturnsDialogRootSnippet()
    {
        var metadata = CreateMetadata();

        var suggestion = Assert.Single(_service.GetSuggestions(
            new XamlCompletionContext(XamlCompletionKind.TagName, "Dia", null, null, 0),
            metadata));

        Assert.Equal("Dialog", suggestion.Label);
        Assert.Equal("Dialog xmlns=\"clr-namespace:A2v10.Xaml;assembly=A2v10.Xaml\"$0></Dialog>", suggestion.InsertText);
        Assert.True(suggestion.IsSnippet);
    }

    [Fact]
    public void GetSuggestions_ReturnsPlainDialogName_ForClosingTag()
    {
        var metadata = CreateMetadata();

        var suggestion = Assert.Single(_service.GetSuggestions(
            new XamlCompletionContext(XamlCompletionKind.TagName, "Dia", null, null, 0, true),
            metadata));

        Assert.Equal("Dialog", suggestion.InsertText);
        Assert.False(suggestion.IsSnippet);
    }

    [Fact]
    public void GetSuggestions_ReturnsAttributeValues()
    {
        var metadata = CreateMetadata();

        var suggestions = _service.GetSuggestions(
            new XamlCompletionContext(XamlCompletionKind.AttributeValue, "Co", "Grid", "Visibility", 0),
            metadata);

        Assert.Contains(suggestions, item => item.Label == "Collapsed");
    }

    private static MetadataRegistry CreateMetadata()
    {
        return new MetadataRegistry(
        [
            new TagDescriptor(
                "Grid",
                "Layout container",
                [
                    new AttributeDescriptor("Id"),
                    new AttributeDescriptor("Visibility", allowedValues: ["Visible", "Collapsed"])
                ]),
            new TagDescriptor("Dialog", "Dialog root"),
            new TagDescriptor("Group", "Reusable group")
        ]);
    }
}
