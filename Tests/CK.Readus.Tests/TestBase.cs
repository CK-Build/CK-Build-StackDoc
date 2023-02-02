// ReSharper disable MemberCanBeProtected.Global
// NUnit needs it public

namespace CK.Readus.Tests;

public class TestBase
{
    public IActivityMonitor Monitor => TestHelper.Monitor;
    public NormalizedPath ProjectFolder => TestHelper.TestProjectFolder;
    public NormalizedPath InFolder => ProjectFolder.AppendPart( "In" );
    public NormalizedPath OutFolder => ProjectFolder.AppendPart( "Out" );

    public TestBase()
    {
        GenerateContexts();
        GenerateRepositories();
    }

    /// <summary>
    /// 1 SimpleStack.
    /// 4 Repositories.
    /// </summary>
    public MdContext SimpleContext { get; private set; }

    /// <summary>
    /// 1 SimpleStackWithCrossRef.
    /// 4 Repositories.
    /// Some documents can reference other repositories.
    /// </summary>
    public MdContext CrossRefContext { get; private set; }

    /// <summary>
    /// 1 SimpleStack.
    /// 1 Repository.
    /// </summary>
    public MdContext SingleRepositoryContext { get; private set; }

    /// <summary>
    /// 1 Repository within a context that contains a single stack with this single repository.
    /// When building for example a MdDocument, use RootPath as base for the MdDocument path.
    /// </summary>
    public MdRepository DummyRepository { get; private set; }

    private void GenerateContexts()
    {
        SimpleContext = CreateContext
        (
            "SimpleStack",
            new[]
            {
                ("FooBarFakeRepo1", "https://github.com/Invenietis/FooBarFakeRepo1"),
                ("FooBarFakeRepo2", "https://github.com/Invenietis/FooBarFakeRepo2"),
                ("FooBarFakeRepo3", "https://github.com/Invenietis/FooBarFakeRepo3"),
                ("FooBarFakeRepo4", "https://github.com/Invenietis/FooBarFakeRepo4"),
            }
        );

        CrossRefContext = CreateContext
        (
            "SimpleStackWithCrossRef",
            new[]
            {
                ("FooBarFakeRepo1", "https://github.com/Invenietis/FooBarFakeRepo1"),
                ("FooBarFakeRepo2", "https://github.com/Invenietis/FooBarFakeRepo2"),
                ("FooBarFakeRepo3", "https://github.com/Invenietis/FooBarFakeRepo3"),
                ("FooBarFakeRepo4", "https://github.com/Invenietis/FooBarFakeRepo4"),
            }
        );

        SingleRepositoryContext = CreateContext
        (
            "SimpleStack",
            new[]
            {
                ("FooBarFakeRepo1", "https://github.com/Invenietis/FooBarFakeRepo1"),
            }
        );
    }

    private void GenerateRepositories()
    {
        var context = CreateContext
        (
            "SimpleStack",
            new[]
            {
                ("FooBarFakeRepo1", "https://github.com/Invenietis/FooBarFakeRepo1"),
            }
        );

        DummyRepository = context.Stacks.First().Value.Repositories.First().Value;
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
