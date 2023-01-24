using CK.Core;
using Markdig;
using Markdig.Syntax;

namespace CK.Readus;

/// <summary>
/// Factory for <see cref="MdRepository"/>.
/// </summary>
public class MdRepositoryReader
{
    public MdRepository ReadPath
    (
        IActivityMonitor monitor,
        NormalizedPath rootPath,
        NormalizedPath remoteUrl
    )
    {
        Throw.CheckArgument( !rootPath.IsEmptyPath );

        monitor.Info( $"Read repository {rootPath}" );

        var repositoryName = rootPath.LastPart;
        var filesPaths = Directory.GetFiles
        (
            rootPath.Path,
            "*.md",
            new EnumerationOptions { RecurseSubdirectories = true }
        );
        //TODO: what about built things. Like under node_modules.

        var documentationFiles = new Dictionary<NormalizedPath, MdDocument>( filesPaths.Length );

        foreach( var file in filesPaths )
        {
            monitor.Trace( $"Add {file}" );
            documentationFiles.Add( file, MdDocument.Load( file ) );
        }

        monitor.Info( $"Repository \"{repositoryName}\" at location {rootPath} contains {filesPaths.Length} md Files." );

        return new MdRepository( repositoryName, remoteUrl, rootPath, documentationFiles );
    }
}
