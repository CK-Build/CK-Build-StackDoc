using System.Diagnostics;
using System.Text;
using CK.Core;
using Markdig;

namespace CK.Readus;

// If the name can me Md[..]Context with [..] being after "Stack" alphabetically,
// the solution explorer will display all the components in the right order.

[DebuggerDisplay( "{Worlds.Count} stacks" )]
public class MdContext
{
    internal LinkChecker LinkChecker { get; }

    internal static MarkdownPipeline Pipeline => new MarkdownPipelineBuilder()
                                                 .UsePipeTables()
                                                 .UseGenericAttributes()
                                                 .Build();

    internal NormalizedPath VirtualRoot { get; private set; }

    /// <summary>
    /// It will throw if the link cannot be processed.
    /// Only links that can match the virtual root can be processed
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    internal NormalizedPath AttachToVirtualRoot( NormalizedPath path )
    {
        // TODO: if the path is relative, that wont work
        // if the path is an url that won't work too
        // actually that smells shit

        if( path.StartsWith( VirtualRoot, false ) is false )
            throw new InvalidOperationException
            (
                $"VirtualRoot is probably inconsistent."
              + $" Path is {path} and VirtualRoot is {VirtualRoot}"
            );

        var virtualPath = new NormalizedPath( "~" ).Combine( path.RemoveFirstPart( VirtualRoot.Count() ) );
        Debug.Assert
        (
            virtualPath.RootKind == NormalizedPathRootKind.RootedByFirstPart,
            "virtualPath.RootKind == NormalizedPathRootKind.RootedByFirstPart"
        );

        return virtualPath;
    }

    /// <summary>
    /// Key is world name.
    /// </summary>
    internal IDictionary<WorldInfo, MdWorld> Worlds { get; init; }

    public NormalizedPath OutputPath { get; private set; }

    //TODO: Make it IEnumerable<MdDocument>
    internal MdDocument[] AllDocuments => Worlds.Values
                                                .SelectMany( s => s.Repositories.Values )
                                                .SelectMany( repository => repository.DocumentationFiles.Values )
                                                .ToArray();

    internal bool IsOk { get; private set; }
    internal bool IsError => !IsOk;

    public MdContextConfiguration Configuration { get; }

    [Obsolete( "Use factory" )]
    public MdContext
    (
        IEnumerable<(WorldInfo, IEnumerable<RepositoryInfo> repositories)> stacks,
        MdContextConfiguration? configuration = null
    ) : this( configuration )
    {
        var task = InitAsync( stacks.ToArray() ).ConfigureAwait( false );
        task.GetAwaiter().GetResult();
    }

    [Obsolete( "Use factory" )]
    public MdContext
    (
        WorldInfo worldInfo,
        IEnumerable<RepositoryInfo> repositories,
        MdContextConfiguration? configuration = null
    )
    : this
    (
        new (WorldInfo worldInfo, IEnumerable<RepositoryInfo> repositories)[]
        {
            new( worldInfo, repositories ),
        },
        configuration
    ) { }

    /// <summary>
    /// Configure a new context.
    /// Call <see cref="RegisterRepositories"/> to work on it.
    /// </summary>
    /// <param name="configuration"></param>
    internal MdContext( MdContextConfiguration? configuration )
    {
        Worlds = new Dictionary<WorldInfo, MdWorld>();
        LinkChecker = new LinkChecker();
        Configuration = configuration ?? MdContextConfiguration.DefaultConfiguration();
        _linkProcessor = new LinkProcessor();
    }

    private async Task InitAsync
    (
        (WorldInfo, IEnumerable<RepositoryInfo> repositories)[] stacks
    )
    {
        var monitor = new ActivityMonitor();

        // I could probably take only one repo per stack => only when multi stack
        var repositoriesPaths = stacks
                                .SelectMany( s => s.repositories )
                                .Select( r => r.Local )
                                .ToArray();

        var commonRoot = repositoriesPaths[0];
        foreach( var repositoryPath in repositoriesPaths.Skip( 1 ) )
        {
            commonRoot = repositoryPath.GetCommonLeadingParts( commonRoot );
        }

        VirtualRoot = commonRoot;

        foreach( var (worldInfo, repositories) in stacks )
        {
            var mdStack = MdWorld.Load( monitor, worldInfo, repositories.ToArray(), this );
            if( Worlds.TryAdd( worldInfo, mdStack ) is false )
                throw new ArgumentException( "This stack is already registered: ", worldInfo.Name );
        }

        IsOk = true;

        foreach( var (name, mdStack) in Worlds )
        {
            var processingOk = await EnsureProcessingAsync( monitor, mdStack );
            IsOk = IsOk && processingOk;
        }

        //TODO: Could add the possibility to add a stack or a repo afterward and process it directly.
        // if the apply has been called, it should not be possible to add more elements
    }

    private void EnsureVirtualRoot()
    {
        //todo: virtual has to be computed based on not registered yet repositories
        // either add a parameter that take future repo into account.
        // Or delay usage of virtual root after creation. Meaning that MdDocument.Current for example,
        // is computed afterward. May be complex.

        // virtual per world ?
        var repositoriesPaths = Worlds.Values
                                      .SelectMany( w => w.Repositories )
                                      .Select( r => r.Value.LocalPath )
                                      .ToArray();

        var commonRoot = repositoriesPaths[0];
        foreach( var repositoryPath in repositoriesPaths.Skip( 1 ) )
        {
            commonRoot = repositoryPath.GetCommonLeadingParts( commonRoot );
        }

        VirtualRoot = commonRoot;
    }

    /// <summary>
    /// Add repositories to the world, or create it if not existing yet.
    /// World is created even if no repository is provided. Note that an empty world won't affect any behavior.
    /// </summary>
    /// <param name="monitor"></param>
    /// <param name="worldInfo">Used as key to determine world uniqueness</param>
    /// <param name="repositoriesInfo"></param>
    public async Task RegisterRepositoriesAsync
    (
        IActivityMonitor monitor,
        WorldInfo worldInfo,
        params RepositoryInfo[] repositoriesInfo
    )
    {
        Throw.CheckNotNullArgument( nameof( worldInfo ) );

        if( Worlds.ContainsKey( worldInfo ) )
        {
            Worlds[worldInfo].Load( monitor, repositoriesInfo );
        }
        else
        {
            var mdWorld = MdWorld.Load( monitor, worldInfo, repositoriesInfo, this );
            Worlds.Add( worldInfo, mdWorld );
        }

        EnsureVirtualRoot();//TODO: move it up with support of candidate
        IsOk = true;
        foreach( var mdWorld in Worlds )
        {
            //TODO: reset current ?
            var processingOk = await EnsureProcessingAsync( monitor, mdWorld.Value );
            IsOk = IsOk && processingOk;
        }
    }

    public void SetOutputPath( NormalizedPath outputPath )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( outputPath );
        Throw.CheckArgument( Directory.Exists( outputPath ) );

        OutputPath = outputPath;
    }

    public async Task WriteHtmlAsync( IActivityMonitor monitor, NormalizedPath outputPath )
    {
        SetOutputPath( outputPath );
        await WriteHtmlAsync( monitor );
    }

    public async Task WriteHtmlAsync( IActivityMonitor monitor )
    {
        Throw.CheckArgument( OutputPath.HasParts );
        var postProcessingResult = await EnsurePostProcessingAsync( monitor );
        if( postProcessingResult is false )
        {
            monitor.Error( "Post processing failed" );
            return;
        }

        Apply( monitor );
    }

    private async Task<bool> EnsureProcessingAsync( IActivityMonitor monitor, MdWorld mdWorld )
    {
        var isOk = true;

        foreach( var (_, mdRepository) in mdWorld.Repositories )
        {
            var mdDocuments = mdRepository.DocumentationFiles.Values.ToArray();
            var processingResult = await _linkProcessor.ProcessAsync
            (
                monitor,
                mdDocuments,
                GetChecks,
                GetAsyncChecks,
                GetTransforms
            );
            isOk = isOk && processingResult;
        }

        return isOk;
        // Configure and run the processing
        // If any error is raised, return false.
    }

    /// <summary>
    /// Key is location. Value is output.
    /// </summary>
    // private Dictionary<NormalizedPath, NormalizedPath> _codeFiles;

    private readonly LinkProcessor _linkProcessor;

    private async Task<bool> EnsurePostProcessingAsync( IActivityMonitor monitor )
    {
        //TODO: Having a Current and a Resolved or smth would help here
        // For example, it is necessary to resolve post process on links before documents.
        // If the order is inverted, the post process on links will be unable to resolve virtual links to concrete links.
        // It is only performance issue tbh.

        // var codeLinks = AllDocuments.SelectMany( d => d.GetLinkedCodeFiles( monitor ) );
        // _codeFiles = new Dictionary<NormalizedPath, NormalizedPath>();
        // foreach( var (location, virtuallyRooted) in codeLinks )
        // {
        //     // prepare for output
        //     var output = OutputPath.Combine( virtuallyRooted.RemovePrefix( "~" ) );
        //     _codeFiles.TryAdd( location, output );
        // }

        var processingResult = await _linkProcessor
        .ProcessAsync( monitor, AllDocuments, default, default, GetPostProcessTransforms );

        foreach( var mdDocument in AllDocuments )
        {
            mdDocument.Current = Path.ChangeExtension( mdDocument.Current, "html" );
            mdDocument.Current = OutputPath.Combine( mdDocument.Current.RemovePrefix( "~" ) );
        }

        return processingResult;
    }

    private void Apply( IActivityMonitor monitor )
    {
        if( IsError )
        {
            monitor.Error( "Cannot apply when context is in error" );
            return;
        }

        foreach( var mdDocument in AllDocuments )
        {
            mdDocument.Apply( monitor );
        }

        // Html
        foreach( var (name, mdStack) in Worlds )
        {
            mdStack.Generate( monitor, OutputPath );
        }

        // Generate ToC
        var html = GenerateHtmlToc( monitor );

        File.WriteAllText( OutputPath.AppendPart( "ToC.html" ), html );

        // Css
        HtmlWriter.WriteCss( this );

        // // Code files
        // foreach( var (location, output) in _codeFiles )
        // {
        //     Directory.CreateDirectory( output.RemoveLastPart() );
        //     try
        //     {
        //         File.Copy( location, output );
        //     }
        //     catch( Exception e )
        //     {
        //         monitor.Fatal( e );
        //     }
        // }
    }

    private Action<IActivityMonitor, NormalizedPath>[] GetChecks( MdDocument mdDocument )
    {
        var mdRepository = mdDocument.Parent;
        var mdStack = mdRepository.Parent;

        var repoCheck = mdRepository.CheckRepository;
        var stackCheck = mdStack.CheckStack;

        var checks = new[] { repoCheck, stackCheck };

        return checks;
    }

    private Func<IActivityMonitor, NormalizedPath, Task>[] GetAsyncChecks( MdDocument mdDocument )
    {
        if( !Configuration.EnableLinkAvailabilityCheck )
            return Array.Empty<Func<IActivityMonitor, NormalizedPath, Task>>();

        var onlineCheck = LinkChecker.CheckLinkAvailabilityAsync;
        var checks = new[] { onlineCheck };
        return checks;
    }

    private Func<IActivityMonitor, NormalizedPath, NormalizedPath>[] GetTransforms( MdDocument mdDocument )
    {
        var mdRepository = mdDocument.Parent;
        var mdStack = mdRepository.Parent;

        var transformCrossRepositoryUrl = mdStack.TransformCrossRepositoryUrl;
        var transformTargetDirectory = mdDocument.TransformTargetDirectory;

        var transforms = new[]
        {
            transformCrossRepositoryUrl,
            transformTargetDirectory,
        };

        return transforms;
    }

    private Func<IActivityMonitor, NormalizedPath, NormalizedPath>[] GetPostProcessTransforms( MdDocument mdDocument )
    {
        var mdRepository = mdDocument.Parent;
        var mdStack = mdRepository.Parent;

        var transformToHtml = mdDocument.TransformToHtml;
        var resolveVirtualRoot = mdDocument.TransformResolveVirtualRootAsConcretePath;
        var transforms = new[]
        {
            transformToHtml,
            resolveVirtualRoot,
        };

        return transforms;
    }

    internal string GenerateHtmlToc( IActivityMonitor monitor, MdDocument? mdDocument = default )
    {
        var builder = new StringBuilder();
        foreach( var (_, mdStack) in Worlds )
        {
            builder.AppendLine( @$"<a class=""pure-menu-heading"" href="""">{mdStack.StackName}</a>" );
            builder.AppendLine( @$"<ul class=""pure-menu-list"">" );

            foreach( var (name, mdRepository) in mdStack.Repositories )
            {
                var hasReadme = mdRepository.TryGetReadme( out var readme );
                if( !hasReadme ) continue;

                if( mdDocument is not null ) // else, build from this.
                    readme = mdDocument.VirtualLocation.CreateRelative( readme );

                var menuSelected = string.Empty;
                if( readme.Equals( "README.html" ) ) menuSelected = " menu-item-divided pure-menu-selected";
                builder.AppendLine
                (
                    $@"<li class=""pure-menu-item{menuSelected}""><a href=""{readme}"" class=""pure-menu-link"">{name}</a></li>"
                );
            }

            builder.AppendLine( @$"</ul>" );
        }

        return builder.ToString();
    }
}
