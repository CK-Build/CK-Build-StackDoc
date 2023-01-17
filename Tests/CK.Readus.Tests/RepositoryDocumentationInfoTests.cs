namespace CK.Readus.Tests;

public class RepositoryDocumentationInfoTests
{
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
        // Directory.CreateDirectory( outputFolder );
        TestHelper.CleanupFolder( outputFolder );
        sut.Generate( TestHelper.Monitor, outputFolder );

        var expectedPath = outputFolder
                           .AppendPart( repositoryName )
                           .AppendPart( "README.html" );
        File.Exists( expectedPath ).Should().BeTrue();

        var content = File.ReadAllText( expectedPath );
        content.Trim().Should().Be( "<h1>Nothing</h1>" );
    }
}
