// ReSharper disable MemberCanBeProtected.Global
// NUnit needs it public

using System.Text;

namespace CK.Readus.Tests;

internal class TestBase
{
    public IActivityMonitor Monitor => TestHelper.Monitor;
    public NormalizedPath ProjectFolder => TestHelper.TestProjectFolder;
    public NormalizedPath InFolder => ProjectFolder.AppendPart( "In" );
    public NormalizedPath OutFolder => ProjectFolder.AppendPart( "Out" );

    public TestBase()
    {
        GenerateContexts();
        GenerateRepositories();
        GenerateDocument();
    }

    #region Display helper content

    public static string GetContextContent( string title, MdContext context, string notes = null )
    {
        var builder = new StringBuilder();
        // indentation level
        var i = string.Empty;
        void IndentUp() => i = i + "  ";
        void IndentDown() => i = i.Remove( i.Length - 2, 2 );
        StringBuilder Add( string line ) => builder.AppendLine( i + line );

        var (stackName, mdStack) = context.Stacks.First();
        var repositoriesCount = mdStack.Repositories.Count;

        Add( $"<=> Property {title} <=>" );
        Add( $"Virtual root: {context.VirtualRoot}" );
        if( notes is not null ) Add( notes );
        Add( $"Stack {stackName} with {repositoriesCount} Repositories" );
        IndentUp();
        foreach( var (repositoryName, mdRepository) in mdStack.Repositories )
        {
            var documentationFilesCount = mdRepository.DocumentationFiles.Count;
            Add( $"Repository {repositoryName} contains {documentationFilesCount} md files" );
            IndentUp();

            foreach( var (fullPath, mdDocument) in mdRepository.DocumentationFiles )
            {
                var linkCount = mdDocument.MarkdownBoundLinks.Count;
                Add( $"'{fullPath}' with {linkCount} links" );
            }

            IndentDown();
            Add( "<=> Details <=>" );
            IndentUp();
            foreach( var (fullPath, mdDocument) in mdRepository.DocumentationFiles )
            {
                Add( $"'{fullPath}' links:" );

                IndentUp();

                foreach( var link in mdDocument.MarkdownBoundLinks )
                {
                    Add( link.OriginPath );
                }

                IndentDown();
            }

            IndentDown();
        }

        IndentDown();
        return builder.ToString();
    }

    public static string GetContextContent( string repositoryName, MdRepository repository )
    {
        var context = repository.Parent.Parent;
        var local = repository.RootPath;
        var remote = repository.RemoteUrl;
        var notes = $"Expose repository: {remote} on disk '{local}'";
        return GetContextContent( repositoryName, context, notes );
    }

    public static string GetContextContent( string repositoryName, MdDocument document )
    {
        var context = document.Parent.Parent.Parent;
        var remote = document.Parent.RemoteUrl;
        var path = document.OriginPath;
        var notes = $"Expose document '{path}' from repository: {remote}";
        return GetContextContent( repositoryName, context, notes );
    }

    #endregion

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
    /// 2 stacks with both 4 repositories.
    /// One stack has cross ref inside.
    /// </summary>
    public MdContext MultiStackContext { get; private set; }

    /// <summary>
    /// 2 stacks, having 1 that target the other stack.
    /// Cross refs inside a stack.
    /// </summary>
    public MdContext MultiStackWithCrossRefContext { get; private set; }

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

    /// <summary>
    /// 1 Document within a context that contains a single stack with a single repository.
    /// </summary>
    public MdDocument DummyDocument { get; private set; }

    /// <summary>
    /// 1 Document within a context that contains a single stack with 2 repositories with cross references.
    /// </summary>
    public MdDocument DocumentWithinMultiRepositoryStack { get; private set; }

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

        MultiStackContext = CreateContext
        (
            nameof( MultiStackContext ),
            new[]
            {
                new ValueTuple<string, IEnumerable<(string local, string remote)>>
                (
                    "SimpleStack",
                    new[]
                    {
                        ("FooBarFakeRepo1", "https://github.com/Invenietis/FooBarFakeRepo1"),
                        ("FooBarFakeRepo2", "https://github.com/Invenietis/FooBarFakeRepo2"),
                        ("FooBarFakeRepo3", "https://github.com/Invenietis/FooBarFakeRepo3"),
                        ("FooBarFakeRepo4", "https://github.com/Invenietis/FooBarFakeRepo4"),
                    }
                ),
                new ValueTuple<string, IEnumerable<(string local, string remote)>>
                (
                    "SimpleStackWithCrossRef",
                    new[]
                    {
                        ("FooBarFakeRepo1", "https://github.com/Invenietis/FooBarFakeRepo1"),
                        ("FooBarFakeRepo2", "https://github.com/Invenietis/FooBarFakeRepo2"),
                        ("FooBarFakeRepo3", "https://github.com/Invenietis/FooBarFakeRepo3"),
                        ("FooBarFakeRepo4", "https://github.com/Invenietis/FooBarFakeRepo4"),
                    }
                ),
            }
        );

        MultiStackWithCrossRefContext = CreateContext
        (
            nameof( MultiStackWithCrossRefContext ),
            new[]
            {
                new ValueTuple<string, IEnumerable<(string local, string remote)>>
                (
                    "SimpleStack",
                    new[]
                    {
                        ("FooBarFakeRepo1", "https://github.com/Invenietis/FooBarFakeRepo1"),
                        ("FooBarFakeRepo2", "https://github.com/Invenietis/FooBarFakeRepoTwo"),
                        ("FooBarFakeRepo3", "https://github.com/Invenietis/FooBarFakeRepo3"),
                        ("FooBarFakeRepo4", "https://github.com/Invenietis/FooBarFakeRepo4"),
                    }
                ),
                new ValueTuple<string, IEnumerable<(string local, string remote)>>
                (
                    "StackWithRefsToOtherStacks",
                    new[]
                    {
                        // @formatter:off
                        ("FooBarFakeRepo2", "https://github.com/Invenietis/FooBarFakeRepo2"),
                        ("RepoWithCrossRefsWithRefsToSimpleStack", "https://github.com/Invenietis/RepoWithCrossRefsWithRefsToSimpleStack"),
                        ("RepoWithRefsToSimpleStack", "https://github.com/Invenietis/RepoWithRefsToSimpleStack"),
                        // @formatter:on
                    }
                ),
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

    private void GenerateDocument()
    {
        {
            var context = CreateContext
            (
                "SimpleStack",
                new[]
                {
                    ("FooBarFakeRepo1", "https://github.com/Invenietis/FooBarFakeRepo1"),
                }
            );

            var repository = context.Stacks.First().Value.Repositories.First().Value;
            DummyDocument = repository.DocumentationFiles.First().Value;
        }

        {
            var context = CreateContext
            (
                "SimpleStackWithCrossRef",
                new[]
                {
                    ("FooBarFakeRepo1", "https://github.com/Invenietis/FooBarFakeRepo1"),
                    ("FooBarFakeRepo2", "https://github.com/Invenietis/FooBarFakeRepo2"),
                }
            );

            var repository = context.Stacks.First().Value.Repositories.First().Value;
            DocumentWithinMultiRepositoryStack = repository.DocumentationFiles.First().Value;
        }
    }

    private MdContext CreateContext
    ( string contextName, (string stackName, IEnumerable<(string local, string remote)> repositories )[] stacks )
    {
        var stacksInfo =
        new List<(string stackName, IEnumerable<(NormalizedPath local, NormalizedPath remote)> repositories )>
        ( stacks.Length );

        foreach( var (stackName, repositoriesQuery) in stacks )
        {
            stackName.Should().NotBeNullOrWhiteSpace();
            var basePath = InFolder.AppendPart( stackName );
            var repositories = repositoriesQuery.ToArray();
            repositories.Should().NotBeEmpty();
            var repositoriesInfo = new List<(NormalizedPath local, NormalizedPath remote)>( repositories.Length );

            foreach( var (local, remote) in repositories )
            {
                repositoriesInfo.Add( (basePath.AppendPart( local ), remote) );
            }

            stacksInfo.Add( (stackName, repositoriesInfo) );
        }

        return CreateContext( contextName, stacksInfo.ToArray() );
    }

    private MdContext CreateContext
    (
        string contextName,
        (string stackName, IEnumerable<(NormalizedPath local, NormalizedPath remote)> repositories )[] stacks
    )
    {
        var mdContext = new MdContext( stacks );

        var outputPath = OutFolder.AppendPart( $"_{contextName}" );
        TestHelper.CleanupFolder( outputPath );
        mdContext.SetOutputPath( outputPath );

        foreach( var (stackName, repositories) in stacks )
        {
            ContextGenericAssertions( stackName, repositories.ToArray(), mdContext );
        }

        return mdContext;
    }

    public MdContext CreateContext( string stackName, (string local, string remote)[] repositories )
    {
        stackName.Should().NotBeNullOrWhiteSpace();
        repositories.Should().NotBeEmpty();

        var basePath = InFolder.AppendPart( stackName );

        var repositoriesInfo = new List<(NormalizedPath local, NormalizedPath remote)>( repositories.Length );
        foreach( var (local, remote) in repositories )
            repositoriesInfo.Add( (basePath.AppendPart( local ), remote) );

        var mdContext = new MdContext( stackName, repositoriesInfo );

        #region Assertions

        ContextGenericAssertions( stackName, repositoriesInfo.ToArray(), mdContext );

        #endregion

        var outputPath = OutFolder.AppendPart( $"_{stackName}" );
        TestHelper.CleanupFolder( outputPath );
        mdContext.SetOutputPath( outputPath );

        return mdContext;
    }

    private static void ContextGenericAssertions
    (
        string stackName,
        (NormalizedPath local, NormalizedPath remote)[] repositories,
        MdContext mdContext
    )
    {
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
    }
}
