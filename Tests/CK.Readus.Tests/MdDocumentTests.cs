using System.Collections;
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
        var mdRepository = DummyRepository;

        var sut = new MdDocument
        (
            text,
            InFolder.Combine( "SimpleStack/FooBarFakeRepo1/VirtualPath.md" ),
            mdRepository
        );

        NormalizedPath Do( IActivityMonitor monitor, NormalizedPath path )
        {
            var transformed = path.AppendPart( "AddedPart" );

            return transformed;
        }

        sut.TransformLinks( Monitor, Do );
        sut.MarkdownBoundLinks.Single().Current.Should().Be( "~/linkToSomething/AddedPart" );
        sut.Apply( Monitor );
        sut.MarkdownDocument.Descendants()
           .OfType<LinkInline>()
           .Single()
           .Url.Should()
           .Be( "~/linkToSomething/AddedPart" );
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
        transformedLinks[1].Url.Should().Be( new NormalizedPath( "~/./Project/README.md/AddedPart" ) );
        transformedLinks[2].Url.Should().Be( new NormalizedPath( "~/./Project/Code.cs/AddedPart" ) );
    }

    [Test]
    public void TransformLinks_should_handle_links_with_dots()
    {
        // We expect the text not to be modified because if the link is already relative, there is
        // no point to change it. It is already well defined in our scope.
        // Indeed, that does not make much sense to work on a single file.
        var text = @"
[click](../ServiceContainer/SimpleServiceContainer.cs)
";
        var virtualFile = $"{DummyRepository.RootPath}/CK.Core/AutomaticDI/README.md";
        var sut = new MdDocument( text, virtualFile, DummyRepository );

        var expected = new NormalizedPath
        (
            $@"{sut.VirtualLocation}/..\ServiceContainer\SimpleServiceContainer.cs"
        );

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

    public static IEnumerable TargetAFile
    {
        get
        {
            yield return new TestCaseData( InFolder.Combine( @"SimpleStack\FooBarFakeRepo1\Project\README.md" ) );
            yield return new TestCaseData( InFolder.Combine( @"SimpleStack\FooBarFakeRepo1\Project\Code.cs" ) );
            yield return new TestCaseData( InFolder.Combine( @"SimpleStack\FooBarFakeRepo1\Project\SomeDocumentation.md" ) );
            yield return new TestCaseData( InFolder.Combine( @"SimpleStack\FooBarFakeRepo1\someFile" ) );
            yield return new TestCaseData( new NormalizedPath(@"~/./Project/Code.cs" ) );
            yield return new TestCaseData( new NormalizedPath(@"~/./Project/README.md)" ) );
        }
    }

    [Test]
    [TestCaseSource( nameof( TargetAFile ) )]
    public void TransformTargetDirectory_should_return_same_link_when_target_a_file( NormalizedPath link )
    {
        var document = DummyDocument;
        TransformAndAssert( link, document, link );
    }

    [Test]
    [TestCase( @"./Project" )]
    public void TransformTargetDirectory_should_return_readme_when_target_a_directory_that_contains_a_readme
    ( string linkString )
    {
        var document = DummyDocument;
        var link = document.VirtualLocation.Combine( linkString );
        var expected = link.AppendPart( "README.md" );

        TransformAndAssert( link, document, expected );
    }

    public static IEnumerable DirectoryThatDoesNotContainsAReadme
    {
        get
        {
            yield return new TestCaseData
            (
                InFolder.Combine( @"SimpleStack\FooBarFakeRepo1\AnotherProject" )
            );
        }
    }

    [Test]
    [TestCaseSource( nameof( DirectoryThatDoesNotContainsAReadme ) )]
    public void TransformTargetDirectory_should_return_same_link_when_target_a_directory_that_does_not_contains_a_readme
    ( NormalizedPath link )
    {
        TransformAndAssert( link, DummyDocument, link );
    }

    public static IEnumerable TargetOutOfScope
    {
        get
        {
            yield return new TestCaseData( InFolder.Combine( @"SimpleStack\FooBarFakeRepo1\Project" ) );
            yield return new TestCaseData( InFolder.Combine( @"SimpleStack\FooBarFakeRepo1\" ) );
            yield return new TestCaseData( new NormalizedPath( @"https://github.com/Invenietis/FooBarFakeRepo2" ) );
        }
    }

    [Test]
    [TestCaseSource( nameof( TargetOutOfScope ) )]
    public void TransformTargetDirectory_should_return_same_link_when_target_out_of_scope( NormalizedPath link )
    {
        TransformAndAssert( link, DummyDocument, link );
    }

    [Test]
    [TestCase( @"FooBarFakeRepo2" )]
    public void TransformTargetDirectory_should_return_same_link_when_target_does_not_exist( string link )
    {
        var document = DummyDocument;
        link = document.VirtualLocation.Combine( link );
        TransformAndAssert( link, document, link );
    }

    [Test]
    [TestCase( @"../FooBarFakeRepo2" )]
    [TestCase( @"../../FooBarFakeRepo2" )]
    public void TransformTargetDirectory_should_throw_when_above_virtual_root( string link )
    {
        var document = DummyDocument;
        link = document.VirtualLocation.Combine( link );
        var action = () => TransformAndAssert( link, document, link );
        action.Should().Throw<InvalidOperationException>();
    }

    [Test]
    [TestCase( @"./Project" )]
    public void TransformTargetDirectory_should_return_readme_when_target_a_directory_that_contains_a_readme_cross_repo
    ( string linkString )
    {
        var document = DocumentWithinMultiRepositoryStack;
        var link = document.VirtualLocation.Combine( linkString );
        var expected = link.AppendPart( "README.md" );

        TransformAndAssert( link, document, expected );
    }

    private void TransformAndAssert( string link, MdDocument document, string expected )
    {
        var sut = document.TransformTargetDirectory( Monitor, link );
        sut.Should().Be( expected );
    }

    [Test]
    [TestCase( "~/README.md", "README.md" )]
    [TestCase( "~/AnotherProject/WhySoSerious.md", "AnotherProject/WhySoSerious.md" )]
    [TestCase( "~/Project/README.md", "Project/README.md" )]
    [TestCase( "~/Project/SomeDocumentation.md", "Project/SomeDocumentation.md" )]
    public void TransformResolveVirtualRootAsConcretePath_resolve_virtual_root( string link, string expected )
    {
        var document = DummyDocument;
        var result = document.TransformResolveVirtualRootAsConcretePath( Monitor, link );

        result.Should().Be( expected );
    }
}
