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

        var repositoryName = rootPath.LastPart;
        var filesPaths = Directory.GetFiles
        (
            rootPath.Path,
            "*.md",
            new EnumerationOptions { RecurseSubdirectories = true }
        );

        var documentationFiles = new Dictionary<NormalizedPath, MarkdownDocumentWrapper>( filesPaths.Length );

        foreach( var file in filesPaths )
        {
            documentationFiles.Add( file, MarkdownDocumentWrapper.Load( file ) );
        }

        monitor.Trace( $"Repository \"{repositoryName}\" at location {rootPath} contains {filesPaths.Length} md Files." );

        return new RepositoryDocumentationInfo( repositoryName, rootPath, remoteUrl, documentationFiles );
    }
}
