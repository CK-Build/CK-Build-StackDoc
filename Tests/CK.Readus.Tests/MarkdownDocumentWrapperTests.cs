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
}
