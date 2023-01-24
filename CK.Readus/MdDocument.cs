using System.Diagnostics;
using CK.Core;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CK.Readus;

public class MdDocument
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

    public MdRepository Parent { get; }
    public IReadOnlyList<MdBoundLink> MarkdownBoundLinks { get; }

    internal MdDocument( MarkdownDocument markdownDocument, NormalizedPath path )
    {
        MarkdownDocument = markdownDocument;
        OriginPath = Path.GetFullPath( path ); //TODO: Should enforce full path. Add tests on repo / stack level

        MarkdownBoundLinks = MarkdownDocument
                             .Descendants()
                             .OfType<LinkInline>()
                             .Select( linkInline => new MdBoundLink( this, linkInline ) )
                             .ToList();
    }

    public static MdDocument Load( NormalizedPath path )
    {
        Debug.Assert( path.IsRooted, "path.IsRooted" );

        var text = File.ReadAllText( path );
        var md = Markdown.Parse( text );
        return new MdDocument( md, path );
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
            // I'm not sure this is true but I keep it for now.
            Debug.Assert( transformed.IsRooted, "transformed.IsRooted" );

            if( transformed.StartsWith( Directory ) )
            {
                // If the link get out of the scope with some ../../../ or else, we may be doomed.
                var relativeLinkPath = transformed.RemoveFirstPart( Directory.Parts.Count );
                transformed = relativeLinkPath;
            }
            else if( transformed.IsRelative() ) { }

            if( dryRun ) continue;

            link.MarkdownReference.Url = transformed;
        }
    }
}
