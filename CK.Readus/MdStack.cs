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

    public void Generate( IActivityMonitor monitor, NormalizedPath outputPath )
    {
        monitor.Trace( $"Writing stack {StackName} to {outputPath}" );

        foreach( var repository in Repositories )
        {
            repository.Value.Generate( monitor, outputPath );
        }
    }
}
