using System.Threading.Tasks.Dataflow;
using CK.Core;
using Markdig;

namespace CK.Readus;

public class RepositoryDocumentationInfo
{
    public string RepositoryName { get; }

    public NormalizedPath RootPath { get; }

    public NormalizedPath RemoteUrl { get; }

    public StackDocumentationInfo Parent { get; }

    // TODO: this could be readonly dictionary ?
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

    public void EnsureLinks( IActivityMonitor monitor, bool dryRun = false )
    {
        foreach( var file in DocumentationFiles )
        {
            monitor.Trace( $"Check links in file {file.Key}" );
            file.Value.CheckLinks( monitor, Check );
            monitor.Trace( $"Transform links in file {file.Key}" );
            file.Value.TransformLinks( monitor, Transform, dryRun );
        }
    }

    /// <summary>
    /// Output the current state of the documentation as html.
    /// </summary>
    /// <param name="monitor"></param>
    /// <param name="outputPath"></param>
    public void Generate( IActivityMonitor monitor, NormalizedPath outputPath )
    {
        monitor.Trace( $"Writing {RepositoryName} documentation to {outputPath}" );

        NormalizedPath ResolvePath( NormalizedPath file )
        {
            var parts = file.Parts;
            var relativePath = parts.SkipWhile( p => p.Equals( RepositoryName ) is false );
            var path = outputPath;
            foreach( var part in relativePath ) path = path.AppendPart( part );

            var lastPart = path.LastPart.Replace( ".md", ".html" );
            path = path.RemoveLastPart().AppendPart( lastPart );

            return path;
        }

        //TODO: Maybe it should check existence only.
        Directory.CreateDirectory( outputPath );

        foreach( var (sourcePath, markdown) in DocumentationFiles )
        {
            var html = markdown.MarkdownDocument.ToHtml();
            var path = ResolvePath( sourcePath );

            Directory.CreateDirectory( path.RemoveLastPart() );
            File.WriteAllText( path, html );
        }
    }

    private void Check( IActivityMonitor monitor, NormalizedPath link )
    {
        monitor.Trace( $"Check {link}" );
        if( link.IsEmptyPath ) monitor.Trace( "Is empty link" );
        monitor.Trace( link.IsRooted ? "Is rooted link" : "Is not rooted link" );
        //TODO: check when the link has no attached text (so is useless).
    }

    private NormalizedPath Transform( IActivityMonitor monitor, NormalizedPath link )
    {
        var transformed = link;

        // TODO: a link to a directory should try look for a README.md file.
        // if( Directory.Exists( link.LastPart ) ) ;

        if( IsOur( transformed ) ) //TODO: in case of a stack, this has to be changed
        {
            var extension = Path.GetExtension( transformed.LastPart );
            if( extension.Equals( ".md" ) )
            {
                transformed = Path.ChangeExtension( transformed, ".html" );
            }

            #region WIP

            if( FeatureFlag.TransformAlwaysReturnAbsolutePath is false )
            {
                // Make the link relative to the repository root.
                transformed = transformed.RemoveFirstPart( RootPath.Parts.Count );
            }

            #endregion
        }

        monitor.Trace( $"Transform {link} into {transformed}" );
        return transformed;
        //TODO: A link to a directory should be mapped to README.md in this directory
    }

    /// <summary>
    /// Determine if the link target is a file from our system.
    /// </summary>
    /// <param name="link">Absolute link to a file.</param>
    /// <exception cref="ArgumentException"></exception>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private bool IsOur( NormalizedPath link )
    {
        if( link.IsRelative() ) throw new ArgumentException( "Expects absolute path", nameof( link ) );

        return File.Exists( link )
            && link.StartsWith( RootPath );
    }
}
