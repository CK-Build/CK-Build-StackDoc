namespace CK.Readus.Tests;

[TestFixtureSource( nameof( FlipFlags ) )]
public class StackDocumentationInfoTests : TestBase
{
    /// <inheritdoc />
    public StackDocumentationInfoTests( bool flag ) : base( flag ) { }

    [SetUp]
    public void SetUp()
    {
        var outputPath = TestHelper.TestProjectFolder
                                   .AppendPart( "OUT" )
                                   .AppendPart( "SimpleStack_Generated" );

        // Directory.Delete( outputPath, true );
        // Seems that does it better :
        TestHelper.CleanupFolder( outputPath );
    }

    [Test]
    public void Generate_should_write_simple_stack()
    {
        var name = "foo-bar";

        var basePath = TestHelper.TestProjectFolder
                                 .AppendPart( "IN" )
                                 .AppendPart( "SimpleStack" );

        var repoPaths =
            new[]
                {
                    "FooBarFakeRepo1",
                    "FooBarFakeRepo2",
                    "FooBarFakeRepo3",
                    "FooBarFakeRepo4",
                }
                .Select( p => basePath.AppendPart( p ) )
                .ToArray();

        var repositoryFactory = new RepositoryDocumentationReader();
        var repositories = new List<RepositoryDocumentationInfo>();

        foreach( var repoPath in repoPaths )
        {
            var repo = repositoryFactory.ReadPath( TestHelper.Monitor, repoPath, string.Empty );
            repositories.Add( repo );
        }

        var sut = new StackDocumentationInfo( repositories, name );

        var outputPath = TestHelper.TestProjectFolder
                                   .AppendPart( "OUT" )
                                   .AppendPart( "SimpleStack_Generated" );

        sut.Generate( TestHelper.Monitor, outputPath );
    }
}
