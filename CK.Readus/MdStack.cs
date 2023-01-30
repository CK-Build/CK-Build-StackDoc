using System.Collections;
using CK.Core;

namespace CK.Readus;

public class MdStack
{
    public MdContext Parent { get; }

    /// <summary>
    /// Key is repository name
    /// </summary>
    public IDictionary<string, MdRepository> Repositories { get; }

    public string StackName { get; }

    public MdStack( IDictionary<string, MdRepository> repositories, string stackName )
    {
        Repositories = repositories;
        StackName = stackName;
    }

    public static MdStack Load
    (
        IActivityMonitor monitor,
        string stackName,
        IEnumerable<(NormalizedPath localPath, NormalizedPath remoteUrl)> repositoriesInfo //TODO dico. Path is uniq
    )
    {
        using( monitor.OpenInfo( $"Loading stack '{stackName}'" ) )
        {
            var repositoryFactory = new MdRepositoryReader();
            var repositories = new Dictionary<string, MdRepository>();

            foreach( var repoPath in repositoriesInfo )
            {
                var repository = repositoryFactory.ReadPath( monitor, repoPath.localPath, repoPath.remoteUrl );
                repositories.Add( repository.RepositoryName, repository );
            }

            return new MdStack( repositories, stackName );
        }
    }

    public void Generate( IActivityMonitor monitor, NormalizedPath outputPath )
    {
        using( monitor.OpenInfo( $"Writing stack '{StackName}' to '{outputPath}'" ) )
        {
            foreach( var repository in Repositories )
            {
                repository.Value.Generate( monitor, outputPath );
            }
        }
    }

    //TODO: Where to put the transformations methods ?
    // They can be in a Md* class, like here.
    // But it could be handled differently, anywhere actually.
    // It depend how it is called, but the whole stack of Md* has access to everything.
    public NormalizedPath TransformCrossRepositoryUrl( IActivityMonitor monitor, NormalizedPath link )
    {
        var isUri = link.RootKind == NormalizedPathRootKind.RootedByURIScheme;
        if( isUri is false ) return link;

        foreach( var (name, mdRepository) in Repositories )
        {
            var url = mdRepository.RemoteUrl;

            Debug.Assert( url.IsEmptyPath is false, "url.IsEmptyPath is false" );
            // Strict has to be false because by default when both path are equals it return false.
            // This is not a behavior that I would except to be the default.
            // I may want to have a way to return true when both are equal but not when other is empty.
            var linkIsInScope = link.StartsWith( url, false );
            if( linkIsInScope is false ) continue;

            var newRoot = mdRepository.RootPath;


            var transformed = newRoot.Combine( link.RemoveFirstPart( url.Parts.Count ) );
            return transformed;
        }

        return link;
    }

}
