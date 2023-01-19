using CK.Core;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CK.Readus.Tests;

[TestFixtureSource( nameof( FlipFlags ) )]
public class RepositoryDocumentationInfoTests : TestBase
{
    /// <inheritdoc />
    public RepositoryDocumentationInfoTests( bool flag ) : base( flag ) { }

    [SetUp]
    public void SetUp()
    {
        // To explore output folder after tests, we want to delete its content before tests.
        // This is only for convenience.

        var outputFolder = TestHelper.TestProjectFolder
                                     .AppendPart( "OUT" );

        if( Directory.Exists( outputFolder ) )
            Directory.Delete( outputFolder, true );
    }

    [Test]
    public void Generate_does_not_throw()
    {
        var factory = new RepositoryDocumentationReader();
        var remoteUrl = string.Empty;
        var repositoryName = "FooBarFakeRepo";
        var rootPath = TestHelper.TestProjectFolder
                                 .AppendPart( "IN" )
                                 .AppendPart( repositoryName );

        var sut = factory.ReadPath( TestHelper.Monitor, rootPath, remoteUrl );


        sut.RepositoryName.Should().Be( repositoryName );
        sut.EnsureLinks( TestHelper.Monitor );

        var outputFolder = TestHelper.TestProjectFolder
                                     .AppendPart( "OUT" )
                                     .AppendPart( repositoryName + "_generated" );
        Directory.CreateDirectory( outputFolder );
        sut.Generate( TestHelper.Monitor, outputFolder );
    }

    [Test]
    public void Generate_should_output_html()
    {
        var repositoryName = "TheMightyProject";
        var tempPath = TestHelper.TestProjectFolder
                                 .AppendPart( "In" )
                                 .AppendPart( "Temp" )
                                 .AppendPart( repositoryName );

        Directory.CreateDirectory( tempPath );
        File.WriteAllText( tempPath.AppendPart( "README.md" ), "# Nothing" );

        var factory = new RepositoryDocumentationReader();
        var remoteUrl = string.Empty;
        var rootPath = tempPath;

        var sut = factory.ReadPath( TestHelper.Monitor, rootPath, remoteUrl );

        var outputFolder = TestHelper.TestProjectFolder
                                     .AppendPart( "OUT" )
                                     .AppendPart( repositoryName + "_generated" );
        TestHelper.CleanupFolder( outputFolder );
        sut.Generate( TestHelper.Monitor, outputFolder );

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

        var tempPath = TestHelper.TestProjectFolder
                                 .AppendPart( "In" )
                                 .AppendPart( "Temp" )
                                 .AppendPart( repositoryName );

        Directory.CreateDirectory( tempPath );
        File.WriteAllText( tempPath.AppendPart( "README.md" ), mdText );
        File.WriteAllText( tempPath.AppendPart( "clickMe.md" ), mdTextClickMe );

        var factory = new RepositoryDocumentationReader();
        var remoteUrl = string.Empty;
        var rootPath = tempPath;

        var sut = factory.ReadPath( TestHelper.Monitor, rootPath, remoteUrl );

        var md = sut.DocumentationFiles[tempPath.AppendPart( "README.md" )].MarkdownDocument;
        var theLink = md.Descendants().OfType<LinkInline>().First();

        theLink.Url.Should().Be( "clickMe.md" );
        sut.EnsureLinks( TestHelper.Monitor );
        theLink.Url.Should().Be( "clickMe.html" );
    }
}
