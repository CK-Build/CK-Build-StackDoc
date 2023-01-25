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
        IEnumerable<(NormalizedPath localPath, NormalizedPath remoteUrl)> repositoriesInfo
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
}
