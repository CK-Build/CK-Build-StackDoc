namespace CK.Readus.Tests;

internal class MdRepositoryTests : TestBase
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
        MdRepository sut = DummyRepository;

        sut.Generate( Monitor );
    }

    [Test]
    [Explicit( "I may do rework that make this test adaptable" )]
    public void Generate_should_output_html()
    {
        // If the MdRepository get public (with the factory), I may change the behavior to this :
        // The client instantiate a context, simple.
        // Then it create some repositories thanks to the factory
        // Those repositories can be added to the context.

        var repositoryName = "TheMightyProject";
        var tempPath = InFolder.AppendPart( "Temp" )
                               .AppendPart( repositoryName );

        Directory.CreateDirectory( tempPath );
        File.WriteAllText( tempPath.AppendPart( "README.md" ), "# Nothing" );

        var factory = new MdRepositoryReader();
        var remoteUrl = string.Empty;
        var rootPath = tempPath;
        var repositoryInfo = new RepositoryInfo( rootPath, remoteUrl );

        var sut = factory.ReadPath( Monitor, repositoryInfo, default );

        var outputFolder = ProjectFolder
                           .AppendPart( "Out" )
                           .AppendPart( repositoryName + "_generated" );
        TestHelper.CleanupFolder( outputFolder );
        sut.Generate( Monitor );

        var expectedPath = outputFolder
                           .AppendPart( repositoryName )
                           .AppendPart( "README.html" );
        File.Exists( expectedPath ).Should().BeTrue();

        var content = File.ReadAllText( expectedPath );
        content.Trim().Should().Be( "<h1>Nothing</h1>" );
    }

    [Test]
    public void TryGetReadme_should_get_main_readme()
    {
        var repo = DummyRepository;

        var found = repo.TryGetReadme( out var readme );

        found.Should().BeTrue();
        readme.Should().Be( "~/README.md" );
    }

    [Test]
    public void TryGetReadme_should_return_false_when_no_readme()
    {
        var repo = NoReadmeRepository;

        var found = repo.TryGetReadme( out var readme );

        found.Should().BeFalse();
        readme.Should().Be( default );
    }
}
