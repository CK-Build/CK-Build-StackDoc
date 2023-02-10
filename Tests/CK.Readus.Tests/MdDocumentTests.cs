using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CK.Readus.Tests;

internal class MdDocumentTests : TestBase
{
    [Test]
    public void CheckLinks_should_run_action_on_every_link()
    {
        var sut = DummyDocument;

        var hasBeenCalled = false;

        void Do( IActivityMonitor monitor, NormalizedPath path )
        {
            hasBeenCalled = true;
        }

        sut.CheckLinks( Monitor, Do );

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

        // var mdRepository = SingleRepositoryContext.Stacks.First().Value.Repositories.First().Value;
        var mdRepository = DummyRepository;

        var sut = new MdDocument
        (
            md,
            "C:/Dev/Signature/CK.Readus/Tests/CK.Readus.Tests/In/SimpleStack/FooBarFakeRepo1/VirtualPath",
            mdRepository
        );

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
        // var mdPath = ProjectFolder
        //              .AppendPart( "In" )
        //              .AppendPart( "FooBarFakeRepo" )
        //              .AppendPart( "README.md" );
        //
        // var sut = MdDocument.Load( mdPath, default );

        var sut = DummyDocument;

        var calls = 0;

        void Do( IActivityMonitor monitor, NormalizedPath path )
        {
            calls++;
        }

        sut.CheckLinks( Monitor, Do );

        calls.Should().Be( 3 );
    }

    [Test]
    public void TransformLinks_FooBarFakeRepo()
    {
        var sut = DummyDocument;

        var calls = 0;

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

        transformedLinks[0].Url.Should().Be( new NormalizedPath( "https://google.fr/AddedPart" ) );
        transformedLinks[1].Url.Should().Be( new NormalizedPath( "./Project/README.md/AddedPart" ) );
        transformedLinks[2].Url.Should().Be( new NormalizedPath( "./Project/Code.cs/AddedPart" ) );
    }

    [Test]
    public void TransformLinks_should_handle_links_with_dots()
    {
        // We expect the text not to be modified because if the link is already relative, there is
        // no point to change it. It is already well defined in our scope.
        // Indeed, that does not make much sense to work on a single file.

        var expected = new NormalizedPath
        (
            @"..\ServiceContainer\SimpleServiceContainer.cs"
        );
        var text = @"
[click](../ServiceContainer/SimpleServiceContainer.cs)
";
        var md = Markdown.Parse( text );

        var virtualFile = $"{DummyRepository.RootPath}/CK.Core/AutomaticDI/README.md";
        var sut = new MdDocument( md, virtualFile, DummyRepository );

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

     // @formatter:off
    [Test]
    [TestCase( @"C:\Dev\Signature\CK.Readus\Tests\CK.Readus.Tests\In\SimpleStack\FooBarFakeRepo1\Project\README.md" )]
    [TestCase( @"C:\Dev\Signature\CK.Readus\Tests\CK.Readus.Tests\In\SimpleStack\FooBarFakeRepo1\Project\Code.cs" )]
    [TestCase( @"C:\Dev\Signature\CK.Readus\Tests\CK.Readus.Tests\In\SimpleStack\FooBarFakeRepo1\Project\SomeDocumentation.md" )]
    [TestCase( @"C:\Dev\Signature\CK.Readus\Tests\CK.Readus.Tests\In\SimpleStack\FooBarFakeRepo1\someFile" )]
    [TestCase( @"./Project/Code.cs" )]
    [TestCase( @"./Project/README.md" )]
    // @formatter:on
    public void TransformTargetDirectory_should_return_same_link_when_target_a_file( string link )
    {
        TransformAndAssert( link, DummyDocument, link );
    }

    [Test]
    [TestCase( @"./Project" )]
    public void TransformTargetDirectory_should_return_readme_when_target_a_directory_that_contains_a_readme
    ( string linkString )
    {
        var link = new NormalizedPath( linkString );
        var expected = link.AppendPart( "README.md" );

        TransformAndAssert( link, DummyDocument, expected );
    }

    [Test]
    [TestCase( @"C:\Dev\Signature\CK.Readus\Tests\CK.Readus.Tests\In\SimpleStack\FooBarFakeRepo1\AnotherProject" )]
    public void TransformTargetDirectory_should_return_same_link_when_target_a_directory_that_does_not_contains_a_readme
    ( string link )
    {
        TransformAndAssert( link, DummyDocument, link );
    }

    [Test]
    [TestCase( @"C:\Dev\Signature\CK.Readus\Tests\CK.Readus.Tests\In\SimpleStack\FooBarFakeRepo1\Project" )]
    [TestCase( @"C:\Dev\Signature\CK.Readus\Tests\CK.Readus.Tests\In\SimpleStack\FooBarFakeRepo1\" )]
    [TestCase( @"https://github.com/Invenietis/FooBarFakeRepo2" )]
    public void TransformTargetDirectory_should_return_same_link_when_target_out_of_scope( string link )
    {
        TransformAndAssert( link, DummyDocument, link );
    }

    [Test]
    [TestCase( @"../../FooBarFakeRepo2" )]
    public void TransformTargetDirectory_should_return_same_link_when_target_out_of_virtual_root( string link )
    {
        TransformAndAssert( link, DummyDocument, link );
    }

    [Test]
    [TestCase( @"../FooBarFakeRepo2" )]
    public void TransformTargetDirectory_should_return_same_link_when_target_does_not_exist( string link )
    {
        TransformAndAssert( link, DummyDocument, link );
    }

    [Test]
    [TestCase( @"./Project" )]
    public void TransformTargetDirectory_should_return_readme_when_target_a_directory_that_contains_a_readme_cross_repo
    ( string linkString )
    {
        var link = new NormalizedPath( linkString );
        var expected = link.AppendPart( "README.md" );

        TransformAndAssert( link, DocumentWithinMultiRepositoryStack, expected );
    }


    private void TransformAndAssert( string link, MdDocument document, string expected )
    {
        var sut = document.TransformTargetDirectory( Monitor, link );
        sut.Should().Be( expected );
    }
}
