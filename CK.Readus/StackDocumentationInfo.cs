using CK.Core;

namespace CK.Readus;

public class StackDocumentationInfo
{
    public IEnumerable<RepositoryDocumentationInfo> Repositories { get; }
    public string StackName { get; }

    public StackDocumentationInfo( IEnumerable<RepositoryDocumentationInfo> repositories, string stackName )
    {
        Repositories = repositories;
        StackName = stackName;
    }

    public void Generate( IActivityMonitor monitor, NormalizedPath outputPath )
    {
        monitor.Trace( $"Writing stack {StackName} to {outputPath}" );

        foreach( var repository in Repositories )
        {
            repository.Generate( monitor, outputPath );
        }
    }
}
