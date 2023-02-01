﻿using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CK.Readus.Tests;

public class MdDocumentTests : TestBase
{
    [Test]
    public void CheckLinks_should_run_action_on_every_link()
    {
        var text = @"
# Title

hello [link](linkToSomething).
";
        var md = Markdown.Parse( text );

        var sut = new MdDocument( md, "virtualPath", default );

        var hasBeenCalled = false;

        void Do( IActivityMonitor monitor, NormalizedPath path )
        {
            hasBeenCalled = true;
        }

        sut.CheckLinks( Monitor, Do );

        hasBeenCalled.Should().BeTrue();
    }

    [Test]
    [Ignore( "Solve TODO" )]
    public void TransformLinks_should_run_action_on_every_link()
    {
        var text = @"
# Title

hello [link](linkToSomething).
";
        var md = Markdown.Parse( text );

        //TODO: mdRepository has to be set since it is coupled to MdDocument.
        var sut = new MdDocument( md, "VirtualPath", default );

        NormalizedPath Do( IActivityMonitor monitor, NormalizedPath path )
        {
            var transformed = path.AppendPart( "AddedPart" );

            return transformed;
        }

        sut.TransformLinks( Monitor, Do );
        sut.Apply( Monitor );
        sut.MarkdownDocument.Descendants().OfType<LinkInline>().Single().Url.Should().Be( "linkToSomething/AddedPart" );
    }

    [Test]
    public void CheckLinks_FooBarFakeRepo()
    {
        var mdPath = ProjectFolder
                               .AppendPart( "In" )
                               .AppendPart( "FooBarFakeRepo" )
                               .AppendPart( "README.md" );

        var sut = MdDocument.Load( mdPath, default );

        var calls = 0;

        void Do( IActivityMonitor monitor, NormalizedPath path )
        {
            calls++;
        }

        sut.CheckLinks( Monitor, Do );

        calls.Should().Be( 3 );
    }

    [Test]
    [Ignore("Solve TODO")]
    public void TransformLinks_FooBarFakeRepo()
    {
        var mdPath = ProjectFolder
                               .AppendPart( "In" )
                               .AppendPart( "FooBarFakeRepo" )
                               .AppendPart( "README.md" );

        var calls = 0;

        //TODO: mdRepository has to be set since it is coupled to MdDocument.
        var sut = MdDocument.Load( mdPath, default );

        NormalizedPath Do( IActivityMonitor monitor, NormalizedPath path )
        {
            calls++;
            var transformed = path.AppendPart( "AddedPart" );

            return transformed;
        }

        sut.TransformLinks( Monitor, Do );
        sut.Apply( Monitor );
        calls.Should().Be( 3 );

        var transformedLinks = sut.MarkdownDocument.Descendants().OfType<LinkInline>().ToArray();
        transformedLinks.Should().HaveCount( calls );

        transformedLinks[0].Url.Should().Be( new NormalizedPath( "https://google.fr/AddedPart" ).ResolveDots() );
        transformedLinks[1].Url.Should().Be( new NormalizedPath( "./Project/README.md/AddedPart" ).ResolveDots() );
        transformedLinks[2].Url.Should().Be( new NormalizedPath( "./Project/Code.cs/AddedPart" ).ResolveDots() );
    }

    [Test]
    [Ignore("Solve TODO")]
    public void TransformLinks_should_handle_links_with_dots()
    {
        // Here we expect a full path because this is the only possible resolution when the scope is 1 file.
        // If we were working in a repository, the expected result would be relative,
        // if both file are in the repository indeed.
        var expected = new NormalizedPath
        (
            @"C:\Users\Aymeric.Richard\Downloads\CK-Core-develop\CK.Core\ServiceContainer\SimpleServiceContainer.cs"
        );
//TODO: We expect the text not to be modified because if the link is already relative, there is
// no point to change it. It is already well defined in our scope.
        var text = @"
[click](../ServiceContainer/SimpleServiceContainer.cs)
";
        var md = Markdown.Parse( text );

        var virtualFile = "C:/Users/Aymeric.Richard/Downloads/CK-Core-develop/CK.Core/AutomaticDI/README.md";
        var sut = new MdDocument( md, virtualFile, default );

        NormalizedPath Do( IActivityMonitor monitor, NormalizedPath path )
        {
            var transformed = path;

            transformed.Should().Be( expected );

            return transformed;
        }

        sut.TransformLinks( Monitor, Do );
        sut.Apply( Monitor );
        sut.MarkdownDocument.Descendants().OfType<LinkInline>().Single().Url.Should().Be( expected );
    }
}