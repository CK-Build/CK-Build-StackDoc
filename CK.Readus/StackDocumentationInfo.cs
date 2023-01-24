using System.Collections;
using CK.Core;

namespace CK.Readus;

public class StackDocumentationInfo
{
    public MdContext Parent { get; }

    /// <summary>
    /// Key is repository name
    /// </summary>
    public IDictionary<string, RepositoryDocumentationInfo> Repositories { get; }
    public string StackName { get; }

    public StackDocumentationInfo( IDictionary<string, RepositoryDocumentationInfo> repositories, string stackName )
    {
        Repositories = repositories;
        StackName = stackName;
    }

    public void Generate( IActivityMonitor monitor, NormalizedPath outputPath )
    {
        monitor.Trace( $"Writing stack {StackName} to {outputPath}" );

        foreach( var repository in Repositories )
        {
            repository.Value.Generate( monitor, outputPath );
        }
    }
}
