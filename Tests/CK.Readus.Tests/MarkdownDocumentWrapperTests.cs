﻿using CK.Core;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CK.Readus.Tests;

[TestFixtureSource( nameof( FlipFlags ) )]
public class MarkdownDocumentWrapperTests : TestBase
{
    /// <inheritdoc />
    public MarkdownDocumentWrapperTests( bool flag ) : base( flag ) { }

    [Test]
    public void CheckLinks_should_run_action_on_every_link()
    {
        var text = @"
# Title

hello [link](linkToSomething).
";
        var md = Markdown.Parse( text );

        var sut = new MarkdownDocumentWrapper( md, "virtualPath" );

        var hasBeenCalled = false;

        void Do( IActivityMonitor monitor, NormalizedPath path )
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

        var sut = new MarkdownDocumentWrapper( md, "VirtualPath" );

        NormalizedPath Do( IActivityMonitor monitor, NormalizedPath path )
        {
            var transformed = path.AppendPart( "AddedPart" );

            if( FeatureFlag.TransformAlwaysReturnAbsolutePath is false )
            {
                transformed = transformed.RemoveFirstPart( new NormalizedPath( Path.GetFullPath( "." ) ).Parts.Count );
            }

            monitor.Trace( $"Transform {path} into {transformed}" );
            return transformed;
        }

        sut.TransformLinks( TestHelper.Monitor, Do );

        sut.MarkdownDocument.Descendants().OfType<LinkInline>().Single().Url.Should().Be( "linkToSomething/AddedPart" );
    }

    [Test]
    public void CheckLinks_FooBarFakeRepo()
    {
        var mdPath = TestHelper.TestProjectFolder
                               .AppendPart( "IN" )
                               .AppendPart( "FooBarFakeRepo" )
                               .AppendPart( "README.md" );

        var sut = MarkdownDocumentWrapper.Load( mdPath );

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
        var mdPath = TestHelper.TestProjectFolder
                               .AppendPart( "IN" )
                               .AppendPart( "FooBarFakeRepo" )
                               .AppendPart( "README.md" );

        var calls = 0;

        var sut = MarkdownDocumentWrapper.Load( mdPath );

        NormalizedPath Do( IActivityMonitor monitor, NormalizedPath path )
        {
            calls++;
            var transformed = path.AppendPart( "AddedPart" );
            if( File.Exists( transformed.RemoveLastPart() ) && transformed.StartsWith( mdPath.RemoveLastPart() ) )
            {
                if( FeatureFlag.TransformAlwaysReturnAbsolutePath is false )
                {
                    // Make the link relative to the repository root.
                    transformed = transformed.RemoveFirstPart( mdPath.RemoveLastPart().Parts.Count );
                }
            }

            monitor.Trace( $"Transform {path} into {transformed}" );
            return transformed;
        }

        sut.TransformLinks( TestHelper.Monitor, Do );

        calls.Should().Be( 3 );

        var transformedLinks = sut.MarkdownDocument.Descendants().OfType<LinkInline>().ToArray();
        transformedLinks.Should().HaveCount( calls );

        transformedLinks[0].Url.Should().Be( new NormalizedPath( "https://google.fr/AddedPart" ).ResolveDots() );
        transformedLinks[1].Url.Should().Be( new NormalizedPath( "./Project/README.md/AddedPart" ).ResolveDots() );
        transformedLinks[2].Url.Should().Be( new NormalizedPath( "./Project/Code.cs/AddedPart" ).ResolveDots() );
    }

    [Test]
    public void TransformLinks_should_handle_links_with_dots()
    {
        // Here we expect a full path because this is the only possible resolution when the scope is 1 file.
        // If we were working in a repository, the expected result would be relative,
        // if both file are in the repository indeed.
        var expected = new NormalizedPath
        (
            @"C:\Users\Aymeric.Richard\Downloads\CK-Core-develop\CK.Core\ServiceContainer\SimpleServiceContainer.cs"
        );

        var text = @"
[click](../ServiceContainer/SimpleServiceContainer.cs)
";
        var md = Markdown.Parse( text );

        var virtualFile = "C:/Users/Aymeric.Richard/Downloads/CK-Core-develop/CK.Core/AutomaticDI/README.md";
        var sut = new MarkdownDocumentWrapper( md, virtualFile );

        NormalizedPath Do( IActivityMonitor monitor, NormalizedPath path )
        {
            var transformed = path;

            transformed.Should().Be( expected );

            monitor.Trace( $"Transform {path} into {transformed}" );
            return transformed;
        }

        sut.TransformLinks( TestHelper.Monitor, Do );

        sut.MarkdownDocument.Descendants().OfType<LinkInline>().Single().Url.Should().Be( expected );
    }
}
