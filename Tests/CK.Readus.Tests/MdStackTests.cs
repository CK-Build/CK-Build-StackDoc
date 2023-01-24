namespace CK.Readus.Tests;

public class MdStackTests : TestBase
{
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

        var repositoryFactory = new MdRepositoryReader();
        var repositories = new Dictionary<string, MdRepository>();

        foreach( var repoPath in repoPaths )
        {
            var repo = repositoryFactory.ReadPath( TestHelper.Monitor, repoPath, string.Empty );
            repositories.Add( repo.RepositoryName ,repo );
        }

        var sut = new MdStack( repositories, name );

        var outputPath = TestHelper.TestProjectFolder
                                   .AppendPart( "OUT" )
                                   .AppendPart( "SimpleStack_Generated" );

        sut.Generate( TestHelper.Monitor, outputPath );
    }
}
