using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using CK.Core;
using Markdig;

namespace CK.Readus;

// If the name can me Md[..]Context with [..] being after "Stack" alphabetically,
// the solution explorer will display all the components in the right order.

[DebuggerDisplay( "{Stacks.Count} stacks" )]
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

        if( path.StartsWith( VirtualRoot ) is false )
            throw new InvalidOperationException
            (
                $"Either this method should not have been called or there is a bug in VirtualRoot computing."
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
    /// Key is stack name.
    /// </summary>
    internal IDictionary<string, MdStack> Stacks { get; init; }

    // TODO: Remove this or change than as dictionary to link them to a document
    // This can be initialized in the ctor with the help of GetTransforms and GetChecks
    // And could even be customized before/after
    // Without customization there is not much point.
    // private IList<Func<IActivityMonitor, NormalizedPath, NormalizedPath>> _transformers;
    // private IList<Action<IActivityMonitor, NormalizedPath>> _checkers;
    public NormalizedPath OutputPath { get; private set; }

    //TODO: Make it IEnumerable<MdDocument>
    internal MdDocument[] AllDocuments => Stacks.Values
                                                .SelectMany( s => s.Repositories.Values )
                                                .SelectMany( repository => repository.DocumentationFiles.Values )
                                                .ToArray();

    internal bool IsOk { get; private set; }
    internal bool IsError => !IsOk;

    public MdContext
    (
        IEnumerable<(string stackName, IEnumerable<(NormalizedPath local, NormalizedPath remote)> repositories)> stacks
    ) : this()
    {
        var task = InitAsync( stacks.ToArray() ).ConfigureAwait( false );
        task.GetAwaiter().GetResult();
    }

    public MdContext( string stackName, IEnumerable<(NormalizedPath local, NormalizedPath remote)> repositories )
    : this
    (
        new (string stackName, IEnumerable<(NormalizedPath local, NormalizedPath remote)> repositories)[]
        {
            new( stackName, repositories ),
        }
    ) { }

    private MdContext()
    {
        Stacks = new Dictionary<string, MdStack>();
        LinkChecker = new LinkChecker();
    }

    private async Task InitAsync
    (
        (string stackName, IEnumerable<(NormalizedPath local, NormalizedPath remote)> repositories)[] stacks
    )
    {
        var monitor = new ActivityMonitor(); // I don't know if I should just pass it as ctor arg.

        // I could probably take only one repo per stack => only when multi stack
        var repositoriesPaths = stacks
                                .SelectMany( s => s.repositories )
                                .Select( r => r.local )
                                .ToArray();

        var commonRoot = repositoriesPaths[0];
        foreach( var repositoryPath in repositoriesPaths.Skip( 1 ) )
        {
            commonRoot = repositoryPath.GetCommonLeadingParts( commonRoot );
        }

        VirtualRoot = commonRoot;

        foreach( var (stackName, repositories) in stacks )
        {
            var mdStack = MdStack.Load( monitor, stackName, repositories, this );
            if( Stacks.TryAdd( stackName, mdStack ) is false )
                throw new ArgumentException( "This stack is already registered: ", stackName );
        }

        IsOk = true;

        foreach( var (name, mdStack) in Stacks )
        {
            var processingOk = await EnsureProcessingAsync( monitor, mdStack );
            IsOk = IsOk && processingOk;
        }

        //TODO: This is then initialized with everything ready :
        // the next step is a method that apply output changes like .md to .html
        //TODO: Could add the possibility to add a stack or a repo afterward and process it directly.
        // if the apply has been called, it should not be possible to add more elements
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

    private async Task<bool> EnsureProcessingAsync( IActivityMonitor monitor, MdStack mdStack )
    {
        var isOk = true;

        var processor = new LinkProcessor();
        foreach( var (path, mdRepository) in mdStack.Repositories )
        {
            var mdDocuments = mdRepository.DocumentationFiles.Values.ToArray();
            var processingResult = await processor.ProcessAsync
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

    private async Task<bool> EnsurePostProcessingAsync( IActivityMonitor monitor )
    {
        var processor = new LinkProcessor();

        var processingResult = await processor.ProcessAsync
        ( monitor, AllDocuments, default, default, GetPostProcessTransforms );

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
        foreach( var (name, mdStack) in Stacks )
        {
            mdStack.Generate( monitor, OutputPath );
        }

        // Generate ToC
        var html = GenerateHtmlToc( monitor );

        File.WriteAllText( OutputPath.AppendPart( "ToC.html" ), html );

        // Css
        HtmlWriter.WriteCss( this );
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
        foreach( var (_, mdStack) in Stacks )
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
