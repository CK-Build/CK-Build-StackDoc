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


    public void SetOutputPath( NormalizedPath outputPath )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( outputPath );
        Throw.CheckArgument( Directory.Exists( outputPath ) );

        OutputPath = outputPath;
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
