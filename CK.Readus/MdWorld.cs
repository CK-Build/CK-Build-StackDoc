using System.Diagnostics;
using CK.Core;

namespace CK.Readus;

[DebuggerDisplay( "{StackName}: {Repositories.Count} repositories" )]
internal class MdWorld
{
    private readonly MdRepositoryReader _repositoryReader = new();

    public WorldInfo Info { get; }
    public MdContext Parent { get; }

    /// <summary>
    /// Key is repository name
    /// </summary>
    public IDictionary<string, MdRepository> Repositories { get; }

    public string StackName => Info.Name;
    public string StackVersion => Info.Version;

    private MdWorld( WorldInfo worldInfo, MdContext parent )
    {
        Repositories = new Dictionary<string, MdRepository>();
        Info = worldInfo;
        Parent = parent;
    }


    // WIP: Load can add or update ?
    /// <summary>
    /// A World represent a stack of repositories designed for a specific purpose, at a specified version for each of them. It represent a coherent
    /// whole, whom each part can be associated internally.
    /// </summary>
    /// <param name="monitor"></param>
    /// <param name="worldInfo"></param>
    /// <param name="repositoriesInfo"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public static MdWorld Load
    (
        IActivityMonitor monitor,
        WorldInfo worldInfo,
        RepositoryInfo[] repositoriesInfo,
        MdContext parent
    )
    {
        using var info = monitor.OpenInfo( $"Loading stack '{worldInfo.Name}'" );

        var mdStack = new MdWorld( worldInfo, parent );

        mdStack.Load( monitor, repositoriesInfo );

        return mdStack;
    }

    public void Load( IActivityMonitor monitor, params RepositoryInfo[] repositoriesInfo )
    {
        foreach( var repositoryInfo in repositoriesInfo )
        {
            var repository = _repositoryReader.ReadPath( monitor, repositoryInfo, this );
            Repositories.Add( repository.RepositoryName, repository );
        }
    }

    public void Generate( IActivityMonitor monitor, NormalizedPath outputPath )
    {
        using( monitor.OpenInfo( $"Writing stack '{StackName}' to '{outputPath}'" ) )
        {
            foreach( var repository in Repositories )
            {
                repository.Value.Generate( monitor );
            }
        }
    }

    /// <summary>
    /// If the link target is in any of the stacks, return its path virtually rooted.
    /// </summary>
    /// <param name="monitor"></param>
    /// <param name="link"></param>
    /// <returns></returns>
    public NormalizedPath TransformCrossRepositoryUrl( IActivityMonitor monitor, NormalizedPath link )
    {
        if( link.IsRelative() ) return link; // Should not happen
        if // virtual ~ already
        (
            link.RootKind is NormalizedPathRootKind.RootedByFirstPart
         && link.FirstPart.Equals( "~" )
        )
            return link;

        var isUri = link.RootKind == NormalizedPathRootKind.RootedByURIScheme;

        var repositories = Parent.Worlds.Values.SelectMany( s => s.Repositories );

        foreach( var (_, mdRepository) in repositories )
        {
            var target = isUri ? mdRepository.RemoteUrl : mdRepository.LocalPath;

            Debug.Assert( target.IsEmptyPath is false, "target.IsEmptyPath is false" );
            // Strict has to be false because by default when both path are equals it return false.
            // This is not a behavior that I would except to be the default.
            // I may want to have a way to return true when both are equal but not when other is empty.
            var linkIsInScope = link.StartsWith( target, false );
            if( linkIsInScope is false ) continue;

            var linkRelativeToItsRepository = link.RemoveFirstPart( target.Parts.Count );

            if( isUri )
            {
                var uri = new Uri( link, UriKind.Absolute );
                var host = uri.Host;
                var branch = mdRepository.GitBranch;
/* default branch or even any branch can be here.
 * Considerations:
 * Source repo which contains the link (in doc) has a branch that can be known.
 * The link exist as is, so we can assume it is valid user wise. (can human error but we don't care)
 * Destination repo is targeted. We can read all of its local (and remote ?) branches.
 * We can compare all of the branches to the link to find the matching one.
 *
 * At the end, if the link target a specific branch, we may want to link it only if the branch is loaded, like it is
 * done right now...
 */
                switch( host )
                {
                    // branches
                    case "github.com" when linkRelativeToItsRepository.StartsWith( "tree" )
                                        || linkRelativeToItsRepository.StartsWith( "blob" ):
                    {
                        if( Parent.Configuration.EnableGitSupport is false ) return link;

                        if( branch is null )
                            throw new InvalidOperationException( "Cannot operate a Github link on a nonexistent git." );

                        var leadCount = 1;
                        if( linkRelativeToItsRepository.RemoveFirstPart( leadCount ).StartsWith( branch ) )
                        {
                            // matched branch
                            leadCount += branch.Value.Count();
                            linkRelativeToItsRepository = linkRelativeToItsRepository.RemoveFirstPart( leadCount );
                        }
                        else
                            continue;
                        // return link;

                        break;
                    }
                    // If main page, can be linked by default
                    case "github.com":
                        // Default behavior
                        break;
                    case "gitlab.com" when linkRelativeToItsRepository.StartsWith( "-/tree" )
                                        || linkRelativeToItsRepository.StartsWith( "-/blob" ):
                    {
                        if( Parent.Configuration.EnableGitSupport is false ) return link;

                        if( branch is null )
                            throw new InvalidOperationException( "Cannot operate a Github link on a nonexistent git." );

                        var leadCount = 2;
                        if( linkRelativeToItsRepository.RemoveFirstPart( leadCount ).StartsWith( branch ) )
                        {
                            // matched branch
                            leadCount += branch.Value.Count();
                            linkRelativeToItsRepository = linkRelativeToItsRepository.RemoveFirstPart( leadCount );
                        }
                        else
                            continue;
                        // return link;

                        break;
                    }
                    case "gitlab.com":
                        // Default behavior
                        break;
                    default:
                        // default uri behavior
                        break;
                }

                var virtualLink = mdRepository.VirtualRoot.Combine( linkRelativeToItsRepository );
                return virtualLink;
            }
            else // Local absolute path transformation
            {
                var virtualLink = mdRepository.VirtualRoot.Combine( linkRelativeToItsRepository );
                return virtualLink;
            }
        }

        return link;
    }

    public void CheckStack( IActivityMonitor monitor, NormalizedPath link ) { }
}
