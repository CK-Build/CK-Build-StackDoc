using CK.Core;
using Markdig;
using Markdig.Syntax;

namespace CK.Readus;

/// <summary>
/// Factory for <see cref="RepositoryDocumentationInfo"/>.
/// </summary>
public class RepositoryDocumentationReader
{
    public RepositoryDocumentationInfo ReadPath
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

        var documentationFiles = new Dictionary<NormalizedPath, MarkdownDocumentWrapper>( filesPaths.Length );

        foreach( var file in filesPaths )
        {
            monitor.Trace( $"Add {file}" );
            documentationFiles.Add( file, MarkdownDocumentWrapper.Load( file ) );
        }

        monitor.Info( $"Repository \"{repositoryName}\" at location {rootPath} contains {filesPaths.Length} md Files." );

        return new RepositoryDocumentationInfo( repositoryName, remoteUrl, rootPath, documentationFiles );
    }
}
