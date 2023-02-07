using CK.Core;
using Markdig.Syntax.Inlines;

namespace CK.Readus;

internal enum LinkType
{
    Unknown,
    External,
    InternalMd,
    InternalImg,
    InternalCode,
    InternalDirectory, // May be something that has to target a toc or a readme.md
}

internal class MdBoundLink
{
    public LinkType LinkType { get; }

    public MdDocument Parent { get; }

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
    public NormalizedPath Current { get; set; }

    public LinkInline MarkdownReference { get; }

    /// <summary>
    /// Can be some result about some actions (check, transform) applied on it
    /// </summary>
    public IReadOnlyCollection<string> Errors { get; }

    /// <summary>
    /// Can be some result about some actions (check, transform) applied on it
    /// </summary>
    public IReadOnlyCollection<string> Infos { get; }
    // IsError here does not seems fair in this context.

    public MdBoundLink( MdDocument mdDocument, LinkInline markdownReference )
    {
        Errors = new List<string>();
        Infos = new List<string>();

        MarkdownReference = markdownReference;
        Parent = mdDocument;

        OriginPath = new NormalizedPath( MarkdownReference.Url );

        Current = new NormalizedPath( OriginPath );

        //TODO: this should be a check. This should not throw here
        if( OriginPath.IsEmptyPath ) throw new NotImplementedException( "A null link could maybe be deleted" );

        // This is probably enough in order to test if the NormalizedPath is rooted.
        var originIsNotBoundToMdDocument = OriginPath.StartsWith( mdDocument.Directory ) is false;

        //TODO: RootedPath may be a bad idea.
        // Right now It is rooted by file system root.
        // I may want to create a virtual root to handle everything at the end.
        if( OriginPath.IsRooted )
            RootedPath = new NormalizedPath( OriginPath );
        else if( originIsNotBoundToMdDocument )
            RootedPath = mdDocument.Directory.Combine( OriginPath ).ResolveDots();
        else throw new NotImplementedException( $"Cannot determine a root for {OriginPath}" );

        // Repo level
        if( OriginPath.IsRelative() )
        {
            var extension = Path.GetExtension( OriginPath );
            if( MarkdownReference.IsImage )
                LinkType = LinkType.InternalImg;
            else
                LinkType = extension switch
                {
                ".md" => LinkType.InternalMd,
                ""    => LinkType.InternalDirectory,
                ".cs" => LinkType.InternalCode,
                _     => LinkType.Unknown
                };
        }
        else
        {
            LinkType = LinkType.External;
        }

        if( LinkType == LinkType.Unknown ) new ActivityMonitor().Warn( "LinkType could not be determined" );
    }
}

// some errors / log properties
