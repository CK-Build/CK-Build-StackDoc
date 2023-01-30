using CK.Core;

namespace CK.Readus;

// If the name can me Md[..]Context with [..] being after "Stack" alphabetically,
// the solution explorer will display all the components in the right order.

public class MdContext
{
    private NormalizedPath _virtualRoot = new NormalizedPath( "~" );

    /// <summary>
    /// Key is stack name.
    /// </summary>
    private IDictionary<string, MdStack> Stacks { get; init; }

    // TODO: Remove this or change than as dictionary to link them to a document
    // This can be initialized in the ctor with the help of GetTransforms and GetChecks
    // And could even be customized before/after
    // Without customization there is not much point.
    // private IList<Func<IActivityMonitor, NormalizedPath, NormalizedPath>> _transformers;
    // private IList<Action<IActivityMonitor, NormalizedPath>> _checkers;
    public NormalizedPath OutputPath { get; private set; }

    public MdContext
    (
        IEnumerable<(string stackName, IEnumerable<(NormalizedPath local, NormalizedPath remote)> repositories)> stacks
    ) : this()
    {
        foreach( var (stackName, repositories) in stacks ) Init( stackName, repositories );
        // Parameters could be stacks at least. Or a list of repositories from where we create stacks.
        // Probably here needs to run all the things to make it ready to report or act.
    }

    public MdContext( string stackName, IEnumerable<(NormalizedPath local, NormalizedPath remote)> repositories )
    : this() =>
    Init( stackName, repositories );

    private MdContext()
    {
        Stacks = new Dictionary<string, MdStack>();

        // _transformers = new List<Func<IActivityMonitor, NormalizedPath, NormalizedPath>>();
        // _checkers = new List<Action<IActivityMonitor, NormalizedPath>>();
    }

    private void Init( string stackName, IEnumerable<(NormalizedPath local, NormalizedPath remote)> repositories )
    {
        var monitor = new ActivityMonitor(); // I don't know if I should just pass it as ctor arg.

        var mdStack = MdStack.Load( monitor, stackName, repositories );
        if( Stacks.TryAdd( stackName, mdStack ) is false )
            throw new ArgumentException( "This stack is already registered: ", nameof( stackName ) );
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

        foreach( var (name, mdStack) in Stacks )
        {
            if( EnsureProcessing( monitor, mdStack ) )
                mdStack.Generate( monitor, OutputPath );
        }
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
        throw new NotImplementedException();
    }

    private Action<IActivityMonitor, NormalizedPath>[] GetChecks(MdDocument mdDocument)
    {
        var mdRepository = mdDocument.Parent;
        var mdStack = mdRepository.Parent;

        var repoCheck = mdRepository.CheckRepository;
        var stackCheck = mdStack.CheckStack;

        var checks = new[] { repoCheck, stackCheck};

        return checks;
    }

    private Func<IActivityMonitor, NormalizedPath, NormalizedPath>[] GetTransforms(MdDocument mdDocument)
    {
        var mdRepository = mdDocument.Parent;
        var mdStack = mdRepository.Parent;

        var repoCheck = mdRepository.TransformRepository;
        var stackCheck = mdStack.TransformCrossRepositoryUrl;

        var checks = new[] { repoCheck, stackCheck};

        return checks;
    }

    // ResolveLinks() // Apply all transformations

    // path ? to output.
    // Output type, like tree, one directory etc.
}
