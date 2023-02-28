using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CK.Readus.Tests;

internal class MdBoundLinkTests : TestBase
{
    [Test]
    public void should_resolve_relative_code_links_to_remote()
    {
        var document = DummyDocument;

        var mdText = @"
[link](Project/Code.cs)
";
        var markdownDocument = Markdown.Parse( mdText );
        var linkInline = markdownDocument.Descendants().OfType<LinkInline>().First();
        var link = new MdBoundLink( document, linkInline );

        link.Current.Should().Be( "https://github.com/Invenietis/FooBarFakeRepo1/blob/master/Project/Code.cs" );
    }
}
