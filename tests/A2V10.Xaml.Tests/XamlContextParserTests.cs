using A2V10.Xaml.Core.Models;
using A2V10.Xaml.Core.Services;

namespace A2V10.Xaml.Tests;

public sealed class XamlContextParserTests
{
    private readonly XamlContextParser _parser = new();

    [Fact]
    public void Parse_ReturnsTagNameContext()
    {
        var context = _parser.Parse("<Gr", 3);

        Assert.Equal(XamlCompletionKind.TagName, context.Kind);
        Assert.Equal("Gr", context.Prefix);
        Assert.Null(context.TagName);
    }

    [Fact]
    public void Parse_ReturnsAttributeNameContext()
    {
        var context = _parser.Parse("<Grid It", 8);

        Assert.Equal(XamlCompletionKind.AttributeName, context.Kind);
        Assert.Equal("Grid", context.TagName);
        Assert.Equal("It", context.Prefix);
    }

    [Fact]
    public void Parse_ReturnsAttributeValueContext()
    {
        var context = _parser.Parse("<Grid Visibility=\"Co", 20);

        Assert.Equal(XamlCompletionKind.AttributeValue, context.Kind);
        Assert.Equal("Grid", context.TagName);
        Assert.Equal("Visibility", context.AttributeName);
        Assert.Equal("Co", context.Prefix);
    }
}
