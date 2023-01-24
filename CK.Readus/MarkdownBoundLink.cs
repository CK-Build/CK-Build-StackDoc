using CK.Core;
using Markdig.Syntax.Inlines;

namespace CK.Readus;

public class MarkdownBoundLink
{
    public MarkdownDocumentWrapper Parent { get; }

    /// <summary>
    /// Raw path found in markdown
    /// </summary>
    public NormalizedPath OriginPath { get; }

    /// <summary>
    /// Base path made absolute in our scope
    /// </summary>
    public NormalizedPath RootedPath { get; }

    /// <summary>
    /// Current result.
    /// Transformations applied.
    /// </summary>
    [Obsolete("Not Implemented")]
    public NormalizedPath Current { get; set; }

    public LinkInline MarkdownReference { get; }

    public MarkdownBoundLink( MarkdownDocumentWrapper markdownDocumentWrapper, LinkInline markdownReference )
    {
        MarkdownReference = markdownReference;
        Parent = markdownDocumentWrapper;

        OriginPath = new NormalizedPath( MarkdownReference.Url );

        if( OriginPath.IsEmptyPath ) throw new NotImplementedException( "A null link could maybe be deleted" );

        // This is probably enough in order to test if the NormalizedPath is rooted.
        var originIsNotBoundToMdDocument = OriginPath.StartsWith( markdownDocumentWrapper.Directory ) is false;

        if( OriginPath.IsRooted )
            RootedPath = new NormalizedPath( OriginPath );
        else if( originIsNotBoundToMdDocument )
            RootedPath = markdownDocumentWrapper.Directory.Combine( OriginPath ).ResolveDots();
        else throw new NotImplementedException( $"Cannot determine a root for {OriginPath}" );
    }
}

// some errors / log properties

/*
 * MdContext
 * MdStack
 * MdRepository
 * MdDocument
 * MdLink
 *
 * All have Property Parent {get;}
 *
 */
