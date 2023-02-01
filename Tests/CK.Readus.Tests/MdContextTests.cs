namespace CK.Readus.Tests;

public class MdContextTests : TestBase
{
    [Test]
    public void WriteHtml_should_ensure_calls_to_all_checks_and_transforms()
    {
        var stackName = "foo-bar";

        var basePath = TestHelper.TestProjectFolder
        .AppendPart( "IN" )
        .AppendPart( "SimpleStackWithCrossRef" );

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


        var repoRemotes = new[]
        {
        "https://github.com/Invenietis/FooBarFakeRepo1",
        "https://github.com/Invenietis/FooBarFakeRepo2",
        "https://github.com/Invenietis/FooBarFakeRepo3",
        "https://github.com/Invenietis/FooBarFakeRepo4"
        };

        var repositoriesInfo = new List<(NormalizedPath, NormalizedPath)>();
        for( var index = 0; index < repoPaths.Length; index++ )
        {
            var repoPath = repoPaths[index];
            repositoriesInfo.Add( (repoPath, repoRemotes[index]) );
        }

        var mdContext = new MdContext( stackName, repositoriesInfo );

        var outputPath = TestHelper.TestProjectFolder
        .AppendPart( "OUT" )
        .AppendPart( "SimpleStackWithCrossRef_GeneratedWithContext" );

        Directory.CreateDirectory( outputPath );

        mdContext.WriteHtml( Monitor, outputPath );
    }
}
