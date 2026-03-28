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
            new TagDescriptor("Group", "Reusable group")
        ]);
    }
}
