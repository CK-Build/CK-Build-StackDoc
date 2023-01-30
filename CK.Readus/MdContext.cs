using CK.Core;

namespace CK.Readus;

// If the name can me Md[..]Context with [..] being after "Stack" alphabetically,
// the solution explorer will display all the components in the right order.

public class MdContext
{
    private IDictionary<string, MdStack> Stacks { get; }
    private NormalizedPath _virtualRoot = new NormalizedPath( "~" );

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
