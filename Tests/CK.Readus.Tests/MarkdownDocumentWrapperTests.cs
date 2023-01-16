using CK.Core;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CK.Readus.Tests;

public class MarkdownDocumentWrapperTests
{
    [Test]
    public void CheckLinks_should_run_action_on_every_link()
    {
        var text = @"
# Title

hello [link](linkToSomething).
";
        var md = Markdown.Parse( text );

        var sut = new MarkdownDocumentWrapper( md, default );

        var hasBeenCalled = false;

        void Do(IActivityMonitor monitor, NormalizedPath path)
        {
            hasBeenCalled = true;
            monitor.Trace( $"Check {path}" );
        }

        sut.CheckLinks( TestHelper.Monitor, Do );

        hasBeenCalled.Should().BeTrue();
    }

    [Test]
    public void TransformLinks_should_run_action_on_every_link()
    {
        var text = @"
# Title

hello [link](linkToSomething).
";
        var md = Markdown.Parse( text );

        var sut = new MarkdownDocumentWrapper( md, default );

        NormalizedPath Do(IActivityMonitor monitor, NormalizedPath path)
        {
            var transformed = path.AppendPart( "AddedPart" );
            monitor.Trace( $"Transform {path} into {transformed}" );
            return transformed;
        }

        sut.TransformLinks( TestHelper.Monitor, Do );

        sut.MarkdownDocument.Descendants().OfType<LinkInline>().Single().Url.Should().Be( "linkToSomething/AddedPart" );
    }

    [Test]
    public void CheckLinks_FooBarFakeRepo()
    {
        var text = File.ReadAllText( TestHelper.TestProjectFolder.AppendPart( "IN" ).AppendPart( "FooBarFakeRepo" ).AppendPart( "README.md" ) );

        var md = Markdown.Parse( text );

        var sut = new MarkdownDocumentWrapper( md, default );

        var calls = 0;

        void Do( IActivityMonitor monitor, NormalizedPath path )
        {
            calls++;
            monitor.Trace( $"Check {path}" );
        }

        sut.CheckLinks( TestHelper.Monitor, Do );

        calls.Should().Be( 3 );
    }

    [Test]
    public void TransformLinks_FooBarFakeRepo()
    {
        var text = File.ReadAllText( TestHelper.TestProjectFolder.AppendPart( "IN" ).AppendPart( "FooBarFakeRepo" ).AppendPart( "README.md" ) );

        var md = Markdown.Parse( text );

        var calls = 0;

        var sut = new MarkdownDocumentWrapper( md, default );

        NormalizedPath Do( IActivityMonitor monitor, NormalizedPath path )
        {
            calls++;
            var transformed = path.AppendPart( "AddedPart" );
            monitor.Trace( $"Transform {path} into {transformed}" );
            return transformed;
        }

        sut.TransformLinks( TestHelper.Monitor, Do );

        calls.Should().Be( 3 );

        var transformedLinks = sut.MarkdownDocument.Descendants().OfType<LinkInline>().ToArray();
        transformedLinks.Should().HaveCount( calls );

        transformedLinks[0].Url.Should().Be( "https://google.fr/AddedPart" );
        transformedLinks[1].Url.Should().Be( "./Project/README.md/AddedPart" );
        transformedLinks[2].Url.Should().Be( "./Project/Code.cs/AddedPart" );
    }
}
