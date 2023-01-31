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

    /// <summary>
    /// TODO: Set the relative path and handle extension
    /// See CK.Readus.MdRepository.Generate
    /// </summary>
    public NormalizedPath Current { get; set; }

    /// <summary>
    /// Path relative to the the repository root.
    /// </summary>
    /// <returns></returns>
    public NormalizedPath RelativePath => Directory.RemoveFirstPart( Parent.RootPath.Parts.Count - 1 );

    internal MdDocument( MarkdownDocument markdownDocument, NormalizedPath path, MdRepository mdRepository )
    {
        MarkdownDocument = markdownDocument;
        OriginPath = Path.GetFullPath( path ); //TODO: Should enforce full path. Add tests on repo / stack level
        Parent = mdRepository;
        Current = OriginPath;

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
            //TODO: I should transform current ? So each transformation can be chained instead of
            // overriding the preceding one.
            var transformed = transform( monitor, link.Current );
            // I'm not sure this is true but I keep it for now.
            // Debug.Assert( transformed.IsRooted, "transformed.IsRooted" );

            if( transformed.StartsWith( Directory ) )
            {
                // If the link get out of the scope with some ../../../ or else, we may be doomed.
                var relativeLinkPath = transformed.RemoveFirstPart( Directory.Parts.Count );
                transformed = relativeLinkPath;
            }
            else if( transformed.IsRelative() )
            {
                // TODO: I may have forgot that we can target our own repo link. Fuck
                // I can try to use my full method GetRelative()

                // maybe it's wrong but for now I handle the case the link is relative, so may come from
                // an other repo.
                // In the future i may use a specific virtual root like ~

                // target repo2/readme.md
                // source repo1/project/
                // i go to ../../../repo2/readme.md
                var source = RelativePath;
                var target = transformed;
                //TODO: here source and/or target are probably wrong
                // mostly target
                if( link.LinkType.Equals( LinkType.External ) )
                    transformed = CreateRelative( source, target );
                //  var moveUpBy = source.Parts.Count;
                //
                //  var result = "";
                //  for( var i = 0; i < moveUpBy; i++ ) result += "../";
                // transformed = new NormalizedPath( result ).Combine( target );
            }

            monitor.Info
            (
                transformed.Equals( link.Current )
                ? $"Link '{link.Current}' unchanged"
                : $"Transform '{link.Current}' into '{transformed}'"
            );

            link.Current = transformed;
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

    static NormalizedPath CreateRelative( NormalizedPath source, NormalizedPath target )
    {
        NormalizedPath ReturnProxy( NormalizedPath toReturn )
        {
            Debug.Assert( toReturn.IsRelative(), "toReturn.IsRelative()" );
            return toReturn;
        }
        //TODO: We may consider a fact. Probably hard to enforce but the method could be :
        // source is a path that we assume we start from. Kind of a current position.
        // target is a way to go from source to target.
        // This way we can get a relative path with a real logic.

        if( source.RootKind != target.RootKind ) // No proxy
        {
            if( target.IsRooted ) return new NormalizedPath( target );
            if( source.IsRooted ) return source.Combine( target ).ResolveDots( source.Parts.Any() ? 1 : 0 );
        }
        // if( source.IsRooted || target.IsRooted ) throw new NotImplementedException();

        if( source.Equals( target ) ) return ReturnProxy( "" );

        if( target.StartsWith( source ) ) return ReturnProxy( target.RemoveFirstPart( source.Parts.Count ) );
        if( source.StartsWith( target ) )
        {
            var moveUpBy = source.Parts.Count - target.Parts.Count;
            var result = "";
            for( var i = 0; i < moveUpBy; i++ ) result += "../";

            return ReturnProxy( result );
        }

        {
            // If rooted, stop move up on common root
            var commonStartPartCount = 0;
            while( source.Parts[commonStartPartCount] == target.Parts[commonStartPartCount] )
            {
                commonStartPartCount++;
            }

            if( source.IsRelative() && target.IsRelative() )
            {
                // it is handled up there
                // Debug.Assert( commonStartPartCount.Equals( 0 ), "commonStartPartCount.Equals( 0 )" );
            }

            var moveUpBy = source.Parts.Count - commonStartPartCount;

            var result = "";
            for( var i = 0; i < moveUpBy; i++ ) result += "../";

            // if target start with dots, needs to block
            var rootPartCount = moveUpBy;
            foreach( var targetPart in target.Parts )
            {
                if( targetPart.Equals( ".." ) ) rootPartCount++;
                else break;
            }

            // The result has to be a relative path. Knowing this :
            // Here we remove the common part from both path to create the suffix of the path.
            // It removes a root (that become unneeded) if any.
            var suffix = target.RemovePrefix( target.RemoveLastPart( target.Parts.Count - commonStartPartCount ) );

            return ReturnProxy
            (
                new NormalizedPath( result )
                .Combine( suffix )
                .ResolveDots( rootPartCount )
            );
        }
    }
}
