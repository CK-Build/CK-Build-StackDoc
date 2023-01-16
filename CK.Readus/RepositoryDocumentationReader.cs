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

        var documentationFiles = new Dictionary<NormalizedPath, MarkdownDocument>( filesPaths.Length );

        foreach( var file in filesPaths )
        {
            var text = File.ReadAllText( file );
            var md = Markdown.Parse( text );

            documentationFiles.Add( file, md );
        }

        monitor.Trace( $"Repository \"{repositoryName}\" at location {rootPath} contains {filesPaths.Length} md Files." );

        return new RepositoryDocumentationInfo( repositoryName, rootPath, remoteUrl, documentationFiles );
    }
}

public class RepositoryDocumentationInfo
{
    public string RepositoryName { get; }

    public NormalizedPath RootPath { get; }

    public NormalizedPath RemoteUrl { get; }

    public Dictionary<NormalizedPath, MarkdownDocument> DocumentationFiles { get; }

    public RepositoryDocumentationInfo
    (
        string repositoryName,
        NormalizedPath remoteUrl,
        NormalizedPath rootPath,
        Dictionary<NormalizedPath, MarkdownDocument> documentationFiles
    )
    {
        RepositoryName = repositoryName;
        RemoteUrl = remoteUrl;
        RootPath = rootPath;
        DocumentationFiles = documentationFiles;
    }
}
