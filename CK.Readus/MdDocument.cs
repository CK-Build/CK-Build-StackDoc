using System.Diagnostics;
using CK.Core;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CK.Readus;

[DebuggerDisplay( "{Parent.RepositoryName}::{DocumentName}" )]
internal class MdDocument
{
    public MarkdownDocument MarkdownDocument { get; }
    public NormalizedPath LocalPath { get; }

    /// <summary>
    /// Get the full path of the directory that contains this file
    /// </summary>
    /// <returns></returns>
    public NormalizedPath Directory => LocalPath.RemoveLastPart();

    /// <summary>
    /// File name with extension
    /// </summary>
    public string DocumentName => LocalPath.LastPart;

    /// <summary>
    /// File name without extension
    /// </summary>
    public string DocumentNameWithoutExtension => Path.GetFileNameWithoutExtension( DocumentName );

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

    private NormalizedPath? _current;

    /// <summary>
    /// Virtually rooted path
    /// </summary>
    public NormalizedPath Current
    {
        get
        {
            _current ??= Parent.Parent.Parent.AttachToVirtualRoot( LocalPath );
            return _current.Value;
        }
        set => _current = value;
    }

    /// <summary>
    /// Directory containing the file with a virtual root.
    /// </summary>
    public NormalizedPath VirtualLocation => Current.RemoveLastPart();

    internal MdDocument( string markdownText, NormalizedPath path, MdRepository mdRepository )
    {
        if( path.IsRelative() )
            throw new ArgumentException( $"{nameof( Path )} should be absolute" );
        LocalPath = path;
        Parent = mdRepository;
        // Current = Parent.Parent.Parent.AttachToVirtualRoot( LocalPath );

        MarkdownDocument = Markdown.Parse( markdownText, MdContext.Pipeline );
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
        return new MdDocument( text, path, mdRepository );
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="monitor"></param>
    /// <param name="check"></param>
    public void CheckLinks
    (
        IActivityMonitor monitor,
        Action<IActivityMonitor, NormalizedPath> check
    )
    {
        foreach( var link in MarkdownBoundLinks )
        {
            if( link.OriginPath.IsEmptyPath ) monitor.Info( "Is empty link" );

            check( monitor, link.RootedPath );
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="monitor"></param>
    /// <param name="check"></param>
    public async Task CheckLinksAsync
    (
        IActivityMonitor monitor,
        Func<IActivityMonitor, NormalizedPath, Task> check
    )
    {
        foreach( var link in MarkdownBoundLinks )
        {
            if( link.OriginPath.IsEmptyPath ) monitor.Info( "Is empty link" );

            await check( monitor, link.RootedPath );
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
            if( link.LinkType == LinkType.InternalCode ) continue;

            var transformed = transform( monitor, link.Current );

            monitor.Info
            (
                transformed.Equals( link.Current )
                ? $"Link '{link.Current}' unchanged"
                : $"Transform '{link.Current}' into '{transformed}'"
            );

            link.Current = transformed;
        }
    }

    /// <summary>
    /// Final transformation that resolve virtual root by creating a relative link equivalent to what could be the origin
    /// path. Only virtually rooted paths are changed.
    /// </summary>
    /// <param name="monitor"></param>
    /// <param name="link"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public NormalizedPath TransformResolveVirtualRootAsConcretePath( IActivityMonitor monitor, NormalizedPath link )
    {
        if( link.IsRelative() ) throw new ArgumentException( "Expected a rooted link." );

        var relative = VirtualLocation.CreateRelative( link );

        return relative;
    }

    /// <summary>
    /// Whenever the link target a directory, try target file README.md instead.
    /// </summary>
    /// <returns>If the link is not a directory, return the link.
    /// If the link is a directory, return README.md if exists, else return the link.</returns>
    public NormalizedPath TransformTargetDirectory( IActivityMonitor monitor, NormalizedPath link )
    {
        // I think this is true. May be wrong.
        if
        (
            link.RootKind
            is NormalizedPathRootKind.RootedBySeparator
            or NormalizedPathRootKind.RootedByDoubleSeparator
            or NormalizedPathRootKind.RootedByURIScheme
        )
            return link;

        if( link.IsRelative() ) throw new ArgumentException( "Link should have a virtual root" );

        var mdDocuments = Parent.Parent.Parent.AllDocuments;

        var potentialMatch = link.AppendPart( "README.md" );

        NormalizedPath potentialMatchDotResolved;
        // This could be asserted elsewhere
        try
        {
            potentialMatchDotResolved = potentialMatch.ResolveDots();
        }
        catch( InvalidOperationException e )
        {
            monitor.Fatal
            (
                $"Invalid link: `{link}`(virtual representation) in document: `{LocalPath}`. Are you missing a target ?",
                e
            );
            throw;
        }

        foreach( var mdDocument in mdDocuments )
        {
            if( potentialMatchDotResolved.Equals( mdDocument.Current.ResolveDots() ) )
                return link.AppendPart( "README.md" );

            if( link.Equals( mdDocument.Current ) ) return link;
        }

        return link;
    }

    public NormalizedPath TransformToHtml( IActivityMonitor monitor, NormalizedPath link )
    {
        var transformed = new NormalizedPath( link );

        // for now true
        // 99% cases we want to transform .md to .html
        // But then we need to determine which links are to transform
        // A link has to correspond to a file.
        // At the very end I may resolve this :
        // The Current link will have a resolved target.
        // We can lookup in all the files and if the target match, we change extensions of both
        // file and link.
        // Update :
        // If the link target this file, then act
        var isMatch = Current.Equals( link );

        if( true )
        {
            var extension = Path.GetExtension( transformed.LastPart );
            if( extension.Equals( ".md" ) )
            {
                transformed = Path.ChangeExtension( transformed, ".html" );
                // Current = Path.ChangeExtension( Current, ".html" );
            }
        }

        return transformed;
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

    /// <summary>
    /// For each link that target a code file, return it's path on disk, alongside with a virtual path.
    /// </summary>
    /// <param name="monitor"></param>
    /// <returns>location file on disk and virtuallyRooted than can be resolved on output.</returns>
    [Obsolete]
    public IEnumerable<(NormalizedPath location, NormalizedPath virtuallyRooted)> GetLinkedCodeFiles( IActivityMonitor monitor )
    {
        foreach( var markdownBoundLink in MarkdownBoundLinks )
        {
            if( markdownBoundLink.LinkType is not LinkType.InternalCode ) continue;
            var link = markdownBoundLink.Current;
            var isVirtual = link.RootKind is NormalizedPathRootKind.RootedByFirstPart
                         && link.FirstPart.Equals( "~" );
            if( isVirtual is false ) continue;

            // link target a code file that we have to copy.
            var context = Parent.Parent.Parent;
            var root = context.VirtualRoot;
            var relativeLink = link.RemoveFirstPart();
            Debug.Assert( relativeLink.IsRelative(), "relativeLink.IsRelative()" );

            var targetLocation = root.Combine( relativeLink ).ResolveDots();
            var targetDestination = context.AttachToVirtualRoot( targetLocation );
            monitor.Info( $"`{targetLocation}` attached to `{targetDestination}`" );

            yield return (targetLocation, targetDestination);
            // We found our target, this has to be registered for copy to the output.
        }
    }
}
