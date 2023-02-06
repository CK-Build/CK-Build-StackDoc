using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CK.Readus.Tests;

public class MdRepositoryTests : TestBase
{
    [SetUp]
    public void SetUp()
    {
        // To explore output folder after tests, we want to delete its content before tests.
        // This is only for convenience.

        var outputFolder = ProjectFolder.AppendPart( "Out" );

        if( Directory.Exists( outputFolder ) )
            Directory.Delete( outputFolder, true );
    }

    [Test]
    public void Generate_does_not_throw()
    {
        var factory = new MdRepositoryReader();
        var remoteUrl = string.Empty;
        var repositoryName = "FooBarFakeRepo";
        var rootPath = InFolder.AppendPart( repositoryName );

        var sut = factory.ReadPath( Monitor, rootPath, remoteUrl, default );


        sut.RepositoryName.Should().Be( repositoryName );
        sut.EnsureLinks( Monitor );

        var outputFolder = ProjectFolder
                           .AppendPart( "Out" )
                           .AppendPart( repositoryName + "_generated" );
        Directory.CreateDirectory( outputFolder );
        sut.Generate( Monitor, outputFolder );
    }

    [Test]
    public void Generate_should_output_html()
    {
        var repositoryName = "TheMightyProject";
        var tempPath = InFolder.AppendPart( "Temp" )
                               .AppendPart( repositoryName );

        Directory.CreateDirectory( tempPath );
        File.WriteAllText( tempPath.AppendPart( "README.md" ), "# Nothing" );

        var factory = new MdRepositoryReader();
        var remoteUrl = string.Empty;
        var rootPath = tempPath;

        var sut = factory.ReadPath( Monitor, rootPath, remoteUrl, default );

        var outputFolder = ProjectFolder
                           .AppendPart( "Out" )
                           .AppendPart( repositoryName + "_generated" );
        TestHelper.CleanupFolder( outputFolder );
        sut.Generate( Monitor, outputFolder );

        var expectedPath = outputFolder
                           .AppendPart( repositoryName )
                           .AppendPart( "README.html" );
        File.Exists( expectedPath ).Should().BeTrue();

        var content = File.ReadAllText( expectedPath );
        content.Trim().Should().Be( "<h1>Nothing</h1>" );
    }

    [Test]
    public void EnsureLinks_transform_links_to_inner_md_file_to_html_equivalent()
    {
        var mdText = @"
# This is a clear documentation

Let's see how this link behave : [Click me](clickMe.md)
";

        var mdTextClickMe = @"
Thanks for the click !
";

        var repositoryName = "TheMightyProject";

        var tempPath = InFolder.AppendPart( "Temp" )
                               .AppendPart( repositoryName );

        Directory.CreateDirectory( tempPath );
        File.WriteAllText( tempPath.AppendPart( "README.md" ), mdText );
        File.WriteAllText( tempPath.AppendPart( "clickMe.md" ), mdTextClickMe );

        var factory = new MdRepositoryReader();
        var remoteUrl = string.Empty;
        var rootPath = tempPath;

        var sut = factory.ReadPath( Monitor, rootPath, remoteUrl, default );

        var md = sut.DocumentationFiles[tempPath.AppendPart( "README.md" )].MarkdownDocument;
        var theLink = md.Descendants().OfType<LinkInline>().First();

        theLink.Url.Should().Be( "clickMe.md" );
        sut.EnsureLinks( Monitor );
        sut.Apply( Monitor );
        theLink.Url.Should().Be( "clickMe.html" );
    }

    // // @formatter:off
    // [Test]
    // [TestCase( @"C:\Dev\Signature\CK.Readus\Tests\CK.Readus.Tests\In\SimpleStack\FooBarFakeRepo1\Project\README.md" )]
    // [TestCase( @"C:\Dev\Signature\CK.Readus\Tests\CK.Readus.Tests\In\SimpleStack\FooBarFakeRepo1\Project\Code.cs" )]
    // [TestCase( @"C:\Dev\Signature\CK.Readus\Tests\CK.Readus.Tests\In\SimpleStack\FooBarFakeRepo1\Project\SomeDocumentation.md" )]
    // [TestCase( @"C:\Dev\Signature\CK.Readus\Tests\CK.Readus.Tests\In\SimpleStack\FooBarFakeRepo1\someFile" )]
    // [TestCase( @"./Project/Code.cs" )]
    // [TestCase( @"./Project/README.md" )]
    // // @formatter:on
    // public void TransformTargetDirectory_should_return_same_link_when_target_a_file( string link )
    // {
    //     TransformAndAssert( link, DummyRepository, link );
    // }
    //
    // [Test]
    // [TestCase( @"C:\Dev\Signature\CK.Readus\Tests\CK.Readus.Tests\In\SimpleStack\FooBarFakeRepo1\Project" )]
    // [TestCase( @"C:\Dev\Signature\CK.Readus\Tests\CK.Readus.Tests\In\SimpleStack\FooBarFakeRepo1\" )]
    // [TestCase( @"./Project" )]
    // [TestCase( @"../../FooBarFakeRepo2" )]
    // public void TransformTargetDirectory_should_return_readme_when_target_a_directory_that_contains_a_readme
    // ( string linkString )
    // {
    //     var link = new NormalizedPath( linkString );
    //     var expected = link.AppendPart( "README.md" );
    //
    //     TransformAndAssert( link, DummyRepository, expected );
    // }
    //
    // [Test]
    // [TestCase( @"C:\Dev\Signature\CK.Readus\Tests\CK.Readus.Tests\In\SimpleStack\FooBarFakeRepo1\AnotherProject" )]
    // [TestCase( @"https://github.com/Invenietis/FooBarFakeRepo2" )] // This can't be resolved at this level
    // public void TransformTargetDirectory_should_return_same_link_when_target_a_directory_that_does_not_contains_a_readme( string link )
    // {
    //     TransformAndAssert( link, DummyRepository, link );
    // }

    private void TransformAndAssert( string link, MdRepository repository, string expected )
    {
        var sut = repository.TransformTargetDirectory( Monitor, link );
        sut.Should().Be( expected );
    }

}
