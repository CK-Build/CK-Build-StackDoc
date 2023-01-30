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

    /// <summary>
    /// I don't know about this
    /// If there is no error in a MdBoundLink, it means either :
    /// - The Current has not been set so the transformation has not been called
    /// - The Check has not been call
    /// - Both has been called and there are no error
    /// - Both has been called, or one of them, and there are error
    ///
    /// We can probably want to check on error when there is no point to.
    /// And we may want to skip transformation phase anyway.
    /// </summary>
    public bool IsError => MarkdownBoundLinks.Any( m => m.Errors.Count > 0 );

    public bool IsOk => IsError is false;
    public NormalizedPath Current { get; set; }

    internal MdDocument( MarkdownDocument markdownDocument, NormalizedPath path, MdRepository mdRepository )
    {
        MarkdownDocument = markdownDocument;
        OriginPath = Path.GetFullPath( path ); //TODO: Should enforce full path. Add tests on repo / stack level
        Parent = mdRepository;

        MarkdownBoundLinks = MarkdownDocument
                             .Descendants()
                             .OfType<LinkInline>()
                             .Select( linkInline => new MdBoundLink( this, linkInline ) )
                             .ToList();
    }

    public static MdDocument Load( NormalizedPath path, MdRepository mdRepository )
    {
        Debug.Assert( path.IsRooted, "path.IsRooted" );

        var text = File.ReadAllText( path );
        var md = Markdown.Parse( text );
        return new MdDocument( md, path, mdRepository );
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="monitor"></param>
    /// <param name="check">Takes a rooted path</param>
    public void CheckLinks
    (
        IActivityMonitor monitor,
        Action<IActivityMonitor, NormalizedPath> check
    )
    {
        foreach( var link in MarkdownBoundLinks )
        {
            // monitor.Info( $"Check '{link.OriginPath}'" );
            if( link.OriginPath.IsEmptyPath ) monitor.Info( "Is empty link" );

            check( monitor, link.RootedPath );
            //TODO: Here we may want to expose the LinkInline as it contains information about the link.
            // For example, we may need the text related to this link.
            // It also contains info like IsImage.
        }
    }

    public void TransformLinks
    (
        IActivityMonitor monitor,
        Func<IActivityMonitor, NormalizedPath, NormalizedPath> transform
    )
    {
        foreach( var link in MarkdownBoundLinks )
        {
            Debug.Assert( link.RootedPath.IsRooted, "link.RootedPath.IsRooted" );
            //TODO: I should transform current ? So each transformation can be chained instead of
            // overriding the preceding one.
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

            link.Current = transformed;

            monitor.Info
            (
                link.OriginPath.Equals( link.Current )
                ? $"Link '{link.OriginPath}' unchanged"
                : $"Transform '{link.OriginPath}' into '{link.Current}'"
            );
        }
    }

    public void Apply( IActivityMonitor monitor )
    {
        if( IsError ) throw new Exception( "I don't know" );
        foreach( var link in MarkdownBoundLinks )
        {
            // TODO: If link.Current is null, there is something wrong.
            // Handle it somehow.
            // The check right up secure this but this is not what i want.
            link.MarkdownReference.Url = link.Current;
        }
    }
}
