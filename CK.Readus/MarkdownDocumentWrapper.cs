using CK.Core;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CK.Readus;

public class MarkdownDocumentWrapper
{
    public MarkdownDocument MarkdownDocument { get; }
    public NormalizedPath OriginPath { get; }

    public MarkdownDocumentWrapper( MarkdownDocument markdownDocument, NormalizedPath path )
    {
        MarkdownDocument = markdownDocument;
        OriginPath = path;
    }

    public static MarkdownDocumentWrapper Load( NormalizedPath path )
    {
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
        foreach( var link in FindLinks() )
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
        foreach( var link in FindLinks() )
        {
            var transformed = transform( monitor, link.Url );
            if (dryRun) continue;

            link.MarkdownReference.Url = transformed;
        }
    }

    private IEnumerable<MarkdownBoundLink> FindLinks()
    {
        var links = new List<MarkdownBoundLink>();

        foreach( var markdownObject in MarkdownDocument.Descendants() )
        {
            if( markdownObject is not LinkInline linkInline ) continue;

            var url = linkInline.Url;

            if( url is null ) throw new NotImplementedException( "A null link could maybe be deleted" );

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
