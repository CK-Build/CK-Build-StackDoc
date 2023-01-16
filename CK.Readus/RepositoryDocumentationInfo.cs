using CK.Core;
using Markdig;
using Markdig.Syntax;

namespace CK.Readus;

public class RepositoryDocumentationInfo
{
    public string RepositoryName { get; }

    public NormalizedPath RootPath { get; }

    public NormalizedPath RemoteUrl { get; }

    public Dictionary<NormalizedPath, MarkdownDocumentWrapper> DocumentationFiles { get; }

    public RepositoryDocumentationInfo
    (
        string repositoryName,
        NormalizedPath remoteUrl,
        NormalizedPath rootPath,
        Dictionary<NormalizedPath, MarkdownDocumentWrapper> documentationFiles
    )
    {
        RepositoryName = repositoryName;
        RemoteUrl = remoteUrl;
        RootPath = rootPath;
        DocumentationFiles = documentationFiles;
    }

    public void EnsureLinks( IActivityMonitor monitor )
    {
        foreach( var file in DocumentationFiles )
        {
            file.Value.CheckLinks( monitor, Check );
            file.Value.TransformLinks( monitor, Transform );
        }
    }

    /// <summary>
    /// Output the current state of the documentation.
    /// </summary>
    /// <param name="monitor"></param>
    /// <param name="outputPath"></param>
    public void Generate( IActivityMonitor monitor, NormalizedPath outputPath )
    {
        NormalizedPath ResolvePath( NormalizedPath file )
        {
            var parts = file.Parts;
            var relativePath = parts.SkipWhile( p => p.Equals( RepositoryName ) is false );
            var path = outputPath;
            foreach( var part in relativePath ) path = path.AppendPart( part );

            return path;
        }

        //TODO: Extension should be html
        //TODO: Maybe it should check existence only.
        Directory.CreateDirectory( outputPath );

        foreach( var file in DocumentationFiles )
        {
            var html = file.Value.MarkdownDocument.ToHtml();

            var path = ResolvePath( file.Key );

            Directory.CreateDirectory( path.RemoveLastPart() );
            File.WriteAllText( path, html );
        }
    }

    private void Check( IActivityMonitor monitor, NormalizedPath link )
    {
        monitor.Trace( $"Check {link}" );
        if( link.IsEmptyPath ) monitor.Trace( "Empty link" );
        if( link.IsRooted ) monitor.Trace( "Rooted" );
        //TODO: check when the link has no attached text (so is useless).
    }

    private NormalizedPath Transform( IActivityMonitor monitor, NormalizedPath link )
    {
        var transformed = link;
        monitor.Trace( $"Transform {link} into {transformed}" );
        return transformed;
        //TODO: A link to a directory should be mapped to README.md in this directory
    }
}
