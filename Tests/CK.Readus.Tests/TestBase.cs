// ReSharper disable MemberCanBeProtected.Global
// NUnit needs it public

using System.Text;

namespace CK.Readus.Tests;

internal class TestBase
{
    public static IActivityMonitor Monitor => TestHelper.Monitor;
    public static NormalizedPath ProjectFolder => TestHelper.TestProjectFolder;
    public static NormalizedPath InFolder => ProjectFolder.AppendPart( "In" );
    public static NormalizedPath OutFolder => ProjectFolder.AppendPart( "Out" );

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

        var (stackName, mdStack) = context.Worlds.First();
        var repositoriesCount = mdStack.Repositories.Count;

        Add( $"<=> Property {title} <=>" );
        Add( $"Virtual root: {context.VirtualRoot}" );
        if( notes is not null ) Add( notes );
        Add( $"Stack {stackName} with {repositoriesCount} Repositories" );
        IndentUp();
        foreach( var (repositoryName, mdRepository) in mdStack.Repositories )
        {
            var documentationFilesCount = mdRepository.DocumentationFiles.Count;
            var url = mdRepository.RemoteUrl;
            var branch = mdRepository.GitBranch.HasValue ? $" (branch:{ mdRepository.GitBranch })" : string.Empty;
            Add( $"Repository {repositoryName}{branch} at `{url}` contains {documentationFilesCount} md files" );
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
        var local = repository.LocalPath;
        var remote = repository.RemoteUrl;
        var notes = $"Expose repository: {remote} on disk '{local}'";
        return GetContextContent( repositoryName, context, notes );
    }

    public static string GetContextContent( string repositoryName, MdDocument document )
    {
        var context = document.Parent.Parent.Parent;
        var remote = document.Parent.RemoteUrl;
        var path = document.LocalPath;
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
    /// 1 SimpleStack.
    /// 1 Repository => git with master develop testBranch.
    /// </summary>
    public MdContext GitContext { get; private set; }

    /// <summary>
    /// TODO summary
    /// </summary>
    public MdContext AdvancedGitContext { get; private set; }

    /// <summary>
    /// 1 Repository within a context that contains a single stack with this single repository.
    /// When building for example a MdDocument, use RootPath as base for the MdDocument path.
    /// </summary>
    public MdRepository DummyRepository { get; private set; }

    /// <summary>
    /// 1 Repository within a context that contains a single stack with this single repository.
    /// When building for example a MdDocument, use RootPath as base for the MdDocument path.
    /// This repository does not contains any file named README.md
    /// </summary>
    public MdRepository NoReadmeRepository { get; private set; }

    /// <summary>
    /// 1 Document within a context that contains a single stack with a single repository.
    /// </summary>
    public MdDocument DummyDocument { get; private set; }

    /// <summary>
    /// 1 Document within a context that contains a single stack with 2 repositories with cross references.
    /// </summary>
    public MdDocument DocumentWithinMultiRepositoryStack { get; private set; }

    public WorldInfo FakeWorldInfo(string name)
    {
        return new WorldInfo( name, Guid.NewGuid().ToString() );
    }

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

        GitContext = CreateContext
        (
            nameof( GitContext ),
            new[]
            {
                new ValueTuple<string, IEnumerable<(string local, string remote)>>
                (
                    "SimpleStackWithTrueGit",
                    new[]
                    {
                        ("FooBarFakeRepo1", "https://github.com/Invenietis/FooBarFakeRepo1"),
                    }
                ),
            },
            new MdContextConfiguration() { EnableLinkAvailabilityCheck = false, EnableGitSupport = true }
        );

        AdvancedGitContext = CreateContext
        (
            nameof( AdvancedGitContext ),
            new[]
            {
                new ValueTuple<string, IEnumerable<(string local, string remote)>>
                (
                    "AdvancedGitStack",
                    new[]
                    {
                        ("FooBarFakeRepo1", "https://github.com/Invenietis/FooBarFakeRepo1"),
                        ("FooBarFakeRepo1-featureBranch", "https://github.com/Invenietis/FooBarFakeRepo1"),
                        ("FooBarFakeRepo2", "https://gitlab.com/Invenietis/FooBarFakeRepo2"),
                    }
                ),
            },
            new MdContextConfiguration() { EnableLinkAvailabilityCheck = false, EnableGitSupport = true }
        );
    }

    private void GenerateRepositories()
    {
        DummyRepository = CreateContext
        (
            "SimpleStack",
            new[]
            {
                ("FooBarFakeRepo1", "https://github.com/Invenietis/FooBarFakeRepo1"),
            }
        ).Worlds.First().Value.Repositories.First().Value;

        NoReadmeRepository = CreateContext
        (
            "StackNoReadme",
            new[]
            {
                ("FooBarFakeRepoNoREADME", "https://github.com/Invenietis/FooBarFakeRepoNoREADME"),
            }
        ).Worlds.First().Value.Repositories.First().Value;


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

            var repository = context.Worlds.First().Value.Repositories.First().Value;
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

            var repository = context.Worlds.First().Value.Repositories.First().Value;
            DocumentWithinMultiRepositoryStack = repository.DocumentationFiles.First().Value;
        }
    }

    private readonly List<NormalizedPath> _gitFolders = new();

    [OneTimeTearDown]
    public void TearDown()
    {
        foreach( var gitFolder in _gitFolders )
        {
            var lastPart = gitFolder.LastPart;
            lastPart.Should().Be( ".git" );
            var gitFolderOff = gitFolder.RemoveLastPart().AppendPart( ".gitOFF" );
            Directory.Move( gitFolder, gitFolderOff );
        }
    }

    private MdContext CreateContext
    (
        string contextName,
        (string stackName, IEnumerable<(string local, string remote)> repositories )[] stacks,
        MdContextConfiguration configuration = null
    )
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

        return CreateContext( contextName, stacksInfo.ToArray(), configuration );
    }

    private MdContext CreateContext
    (
        string contextName,
        (string stackName, IEnumerable<(NormalizedPath local, NormalizedPath remote)> repositories )[] stacks,
        MdContextConfiguration configuration = null
    )
    {
        if( configuration is { EnableGitSupport: true } )
            foreach( var (local, _) in stacks.SelectMany( s => s.repositories ) )
            {
                // Make the git folder usable for this run.
                // It will be reverted in the tear down.
                var gitFolderOff = local.AppendPart( ".gitOFF" );
                var gitFolderOn = local.AppendPart( ".git" );
                _gitFolders.Add( gitFolderOn );
                if( Directory.Exists( gitFolderOff ) )
                    Directory.Move( gitFolderOff, gitFolderOn );
                else if( Directory.Exists( gitFolderOn ) )
                    Monitor.Error
                    (
                        $"Inconsistency detected, `{gitFolderOn}` should end with `OFF`. "
                      + $"It will be fixed in tear down."
                    );
                else
                    Monitor.Fatal( $"Expected .git to exist here `{gitFolderOn}`." );
            }

        var mdContext = new MdContextFactory().CreateContext
        (
            Monitor,
            configuration ?? MdContextConfiguration.DefaultConfiguration()
        );
        var worldInfos = new List<WorldInfo>();
        foreach( var (stackName, repositories) in stacks )
        {
            var worldInfo = FakeWorldInfo( stackName );
            worldInfos.Add( worldInfo );
            mdContext.RegisterRepositoriesAsync
                     (
                         Monitor,
                         worldInfo,
                         repositories.Select( r => new RepositoryInfo( r.local, r.remote ) ).ToArray()
                     )
                     .ConfigureAwait( false )
                     .GetAwaiter()
                     .GetResult();
        }

        var outputPath = OutFolder.AppendPart( $"_{contextName}" );
        TestHelper.CleanupFolder( outputPath );
        mdContext.SetOutputPath( outputPath );

        for( var i = 0; i < stacks.Length; i++ )
        {
            var (stackName, repositories) = stacks[i];
            ContextGenericAssertions( worldInfos[i], repositories.ToArray(), mdContext );
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


        var mdContext = new MdContextFactory().CreateContext
        (
            Monitor
        );
        var worldInfo = FakeWorldInfo( stackName );
        mdContext.RegisterRepositoriesAsync
                 (
                     Monitor,
                     worldInfo,
                     repositoriesInfo.Select( r => new RepositoryInfo( r.local, r.remote ) ).ToArray()
                 )
                 .ConfigureAwait( false )
                 .GetAwaiter()
                 .GetResult();

        #region Assertions

        ContextGenericAssertions( worldInfo, repositoriesInfo.ToArray(), mdContext );

        #endregion

        var outputPath = OutFolder.AppendPart( $"_{stackName}" );
        TestHelper.CleanupFolder( outputPath );
        mdContext.SetOutputPath( outputPath );

        return mdContext;
    }

    private static void ContextGenericAssertions
    (
        WorldInfo worldInfo,
        (NormalizedPath local, NormalizedPath remote)[] repositories,
        MdContext mdContext
    )
    {
        mdContext.Worlds.Should().ContainKey( worldInfo );
        var mdStack = mdContext.Worlds[worldInfo];
        mdStack.StackName.Should().Be( worldInfo.Name );
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
