using System.Diagnostics;
using CK.Core;
using Markdig;

namespace CK.Readus;

// If the name can me Md[..]Context with [..] being after "Stack" alphabetically,
// the solution explorer will display all the components in the right order.

[DebuggerDisplay( "{Stacks.Count} stacks" )]
public class MdContext
{
    internal static MarkdownPipeline Pipeline => new MarkdownPipelineBuilder()
                                                 .UsePipeTables()
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
        Init( stacks.ToArray() );
        // Parameters could be stacks at least. Or a list of repositories from where we create stacks.
        // Probably here needs to run all the things to make it ready to report or act.
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

        // _transformers = new List<Func<IActivityMonitor, NormalizedPath, NormalizedPath>>();
        // _checkers = new List<Action<IActivityMonitor, NormalizedPath>>();
    }

    private void Init
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
            var processingOk = EnsureProcessing( monitor, mdStack );
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

    public void WriteHtml( IActivityMonitor monitor, NormalizedPath outputPath )
    {
        SetOutputPath( outputPath );
        WriteHtml( monitor );
    }

    public void WriteHtml( IActivityMonitor monitor )
    {
        Throw.CheckArgument( OutputPath.HasParts );
        var postProcessingResult = EnsurePostProcessing( monitor );
        if( postProcessingResult is false )
        {
            monitor.Error( "Post processing failed" );
            return;
        }

        Apply( monitor );
    }

    private bool EnsureProcessing( IActivityMonitor monitor, MdStack mdStack )
    {
        var isOk = true;

        var processor = new LinkProcessor();
        foreach( var (path, mdRepository) in mdStack.Repositories )
        {
            var mdDocuments = mdRepository.DocumentationFiles.Values.ToArray();
            var processingResult = processor.Process
            (
                monitor,
                mdDocuments,
                GetChecks,
                GetTransforms
            );
            isOk = isOk && processingResult;
        }

        return isOk;
        // Configure and run the processing
        // If any error is raised, return false.
    }

    private bool EnsurePostProcessing( IActivityMonitor monitor )
    {
        var processor = new LinkProcessor();

        var processingResult = processor.Process( monitor, AllDocuments, default, GetPostProcessTransforms );

        foreach( var mdDocument in AllDocuments )
        {
            mdDocument.Current = Path.ChangeExtension( mdDocument.Current, "html" );
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

        foreach( var (name, mdStack) in Stacks )
        {
            mdStack.Generate( monitor, OutputPath );
        }
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

    // ResolveLinks() // Apply all transformations

    // path ? to output.
    // Output type, like tree, one directory etc.
}
