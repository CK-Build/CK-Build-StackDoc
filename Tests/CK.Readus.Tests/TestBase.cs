namespace CK.Readus.Tests;

public class TestBase
{
    public IActivityMonitor Monitor => TestHelper.Monitor;
    public NormalizedPath ProjectFolder => TestHelper.TestProjectFolder;
    public NormalizedPath InFolder => ProjectFolder.AppendPart( "In" );
    public NormalizedPath OutFolder => ProjectFolder.AppendPart( "Out" );

    // ReSharper disable once MemberCanBeProtected.Global
    public TestBase() { }

    public MdContext SimpleContext => CreateSimpleStackContext();
    public MdContext CrossRefContext => CreateSimpleStackWithCrossRefContext();
    public MdContext SingleRepositoryContext => CreateStackWithOneRepositoryContext();

    private MdContext CreateSimpleStackContext()
    {
        var stackName = "SimpleStack";

        var repositories = new[]
        {
            ("FooBarFakeRepo1", "https://github.com/Invenietis/FooBarFakeRepo1"),
            ("FooBarFakeRepo2", "https://github.com/Invenietis/FooBarFakeRepo2"),
            ("FooBarFakeRepo3", "https://github.com/Invenietis/FooBarFakeRepo3"),
            ("FooBarFakeRepo4", "https://github.com/Invenietis/FooBarFakeRepo4"),
        };

        return CreateContext( stackName, repositories );
    }

    private MdContext CreateSimpleStackWithCrossRefContext()
    {
        var stackName = "SimpleStackWithCrossRef";

        var repositories = new[]
        {
            ("FooBarFakeRepo1", "https://github.com/Invenietis/FooBarFakeRepo1"),
            ("FooBarFakeRepo2", "https://github.com/Invenietis/FooBarFakeRepo2"),
            ("FooBarFakeRepo3", "https://github.com/Invenietis/FooBarFakeRepo3"),
            ("FooBarFakeRepo4", "https://github.com/Invenietis/FooBarFakeRepo4"),
        };

        return CreateContext( stackName, repositories );
    }

    private MdContext CreateStackWithOneRepositoryContext()
    {
        var stackName = "SimpleStack";

        var repositories = new[]
        {
            ("FooBarFakeRepo1", "https://github.com/Invenietis/FooBarFakeRepo1"),
        };

        return CreateContext( stackName, repositories );
    }

    private MdContext CreateContext( (string stackName, (string local, string remote)[] repositories )[] stacks )
    {
        throw new NotImplementedException( "Until it is proven useful, I'm not going to implement this" );
    }

    private MdContext CreateContext( string stackName, (string local, string remote)[] repositories )
    {
        stackName.Should().NotBeNullOrWhiteSpace();
        repositories.Should().NotBeEmpty();

        var basePath = InFolder.AppendPart( stackName );

        var repositoriesInfo = new List<(NormalizedPath local, NormalizedPath remote)>( repositories.Length );
        foreach( var (local, remote) in repositories )
            repositoriesInfo.Add( (basePath.AppendPart( local ), remote) );

        var mdContext = new MdContext( stackName, repositoriesInfo );

        #region Assertions

        mdContext.Stacks.Should().ContainKey( stackName );
        var mdStack = mdContext.Stacks[stackName];
        mdStack.StackName.Should().Be( stackName );
        mdStack.Parent.Should().Be( mdContext );
        var mdRepositories = mdStack.Repositories;
        mdRepositories.Count.Should().Be( repositories.Length );
        foreach( var (name, mdRepository) in mdRepositories )
        {
            mdRepository.Parent.Should().Be( mdStack );
            mdRepository.RepositoryName.Should().Be( name );
            foreach( var (path, mdDocument) in mdRepository.DocumentationFiles )
            {
                mdDocument.Parent.Should().Be( mdRepository );
                Directory.Exists( mdDocument.Directory ).Should().BeTrue();
                File.Exists( path ).Should().BeTrue();
                foreach( var link in mdDocument.MarkdownBoundLinks )
                {
                    link.Parent.Should().Be( mdDocument );
                }
            }
        }

        #endregion

        var outputPath = OutFolder.AppendPart( $"_{stackName}" );
        TestHelper.CleanupFolder( outputPath );
        mdContext.SetOutputPath( outputPath );

        return mdContext;
    }
}
