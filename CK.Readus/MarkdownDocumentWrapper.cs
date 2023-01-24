using System.Diagnostics;
using CK.Core;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CK.Readus;

public class MarkdownDocumentWrapper // MarkdownDocumentHolder
{
    public MarkdownDocument MarkdownDocument { get; }
    public NormalizedPath OriginPath { get; }

    /// <summary>
    /// Get the full path of the directory that contains this file
    /// </summary>
    /// <returns></returns>
    public NormalizedPath Directory => OriginPath.RemoveLastPart();

    /// <summary>
    /// File name without extension
    /// </summary>
    public string DocumentName => Path.GetFileNameWithoutExtension( OriginPath.LastPart );

    public IReadOnlyList<MarkdownBoundLink> MarkdownBoundLinks { get; }

    internal MarkdownDocumentWrapper( MarkdownDocument markdownDocument, NormalizedPath path )
    {
        MarkdownDocument = markdownDocument;
        OriginPath = Path.GetFullPath( path ); //TODO: Should enforce full path. Add tests on repo / stack level

        MarkdownBoundLinks = MarkdownDocument
                             .Descendants()
                             .OfType<LinkInline>()
                             .Select( linkInline => new MarkdownBoundLink( this, linkInline ) )
                             .ToList();
    }

    public static MarkdownDocumentWrapper Load( NormalizedPath path )
    {
        Debug.Assert( path.IsRooted , "path.IsRooted");

        var text = File.ReadAllText( path );
        var md = Markdown.Parse( text );
        return new MarkdownDocumentWrapper( md, path );
    }

    public void CheckLinks
    (
        IActivityMonitor monitor,
        Action<IActivityMonitor, NormalizedPath> check
    )
    {
        foreach( var link in MarkdownBoundLinks )
        {
            check( monitor, link.RootedPath );
            //TODO: Here we may want to expose the LinkInline as it contains information about the link.
            // For example, we may need the text related to this link.
            // It also contains info like IsImage.
        }
    }

    public void TransformLinks
    (
        IActivityMonitor monitor,
        Func<IActivityMonitor, NormalizedPath, NormalizedPath> transform,
        bool dryRun = false
    )
    {
        foreach( var link in MarkdownBoundLinks )
        {
            Debug.Assert( link.RootedPath.IsRooted, "link.RootedPath.IsRooted" );
            var transformed = transform( monitor, link.RootedPath );

            #region WIP

            if( FeatureFlag.TransformAlwaysReturnAbsolutePath )
            {
                if( transformed.StartsWith( Directory ) )
                {
                    // If the link get out of the scope with some ../../../ or else, we may be doomed.
                    var relativeLinkPath = transformed.RemoveFirstPart( Directory.Parts.Count );
                    transformed = relativeLinkPath;
                }
            }
            else
            {
                if( transformed.IsRelative() )
                {
                    for( var i = 1; i <= Directory.Parts.Count; i++ )
                    {
                        var endOfDirectory = Directory.Parts.TakeLast( i );
                        var startOfTransformed = transformed.Parts.Take( i );

                        if( endOfDirectory.SequenceEqual( startOfTransformed ) is false ) continue;
                        // both represent the file path relative from the repo
                        var relativeLinkPath = transformed.RemoveFirstPart( i );
                        transformed = relativeLinkPath;
                        break;
                    }
                }
            }

            #endregion

            if( dryRun ) continue;

            link.MarkdownReference.Url = transformed;
        }
    }
}
