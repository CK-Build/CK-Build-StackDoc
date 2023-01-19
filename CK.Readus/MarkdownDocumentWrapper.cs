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

    public MarkdownDocumentWrapper( MarkdownDocument markdownDocument, NormalizedPath path )
    {
        MarkdownDocument = markdownDocument;
        OriginPath = Path.GetFullPath( path ); //TODO: Should enforce full path. Add tests on repo / stack level
        // This ctor may be internal (visible to tests) to ensure the MarkdownDocument is correctly parsed
    }

    public static MarkdownDocumentWrapper Load( NormalizedPath path )
    {
        Debug.Assert( path.IsAbsolute() );

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
        foreach( var link in FindLinks( false ) ) // TODO: do i want an absolute link here or the original link ?
        {
            check( monitor, link.Url );
            //TODO: Here we may want to expose the LinkInline as it contains informations about the link.
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
        foreach( var link in FindLinks( true ) )
        {
            Debug.Assert( link.Url.IsAbsolute(), "link.Url.IsAbsolute()" );
            var transformed = transform( monitor, link.Url );

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

                        if( endOfDirectory.Equals( startOfTransformed ) is false ) continue;
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

    private IEnumerable<MarkdownBoundLink> FindLinks( bool makeAbsolute )
    {
        var links = new List<MarkdownBoundLink>();

        foreach( var markdownObject in MarkdownDocument.Descendants() )
        {
            if( markdownObject is not LinkInline linkInline ) continue;

            var url = new NormalizedPath( linkInline.Url );
            if( url.IsEmptyPath ) throw new NotImplementedException( "A null link could maybe be deleted" );

            if( makeAbsolute )
            {
                // This is probably enough in order to test if the NormalizedPath is rooted.
                var isRelativeToThisFile = url.StartsWith( Directory ) is false;
                if( url.IsRelative() && isRelativeToThisFile )
                    url = Directory.Combine( url.ResolveDots() );
            }

            var link = new MarkdownBoundLink( url, linkInline );
            links.Add( link );
        }

        return links;
    }

    private class MarkdownBoundLink
    {
        public MarkdownBoundLink( NormalizedPath url, LinkInline markdownReference )
        {
            Url = url;
            MarkdownReference = markdownReference;
        }

        public NormalizedPath Url { get; }
        public LinkInline MarkdownReference { get; }
    }
}
