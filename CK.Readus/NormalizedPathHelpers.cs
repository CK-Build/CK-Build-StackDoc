using System.Diagnostics;
using CK.Core;

namespace CK.Readus;

internal static class NormalizedPathHelpers
{
    /// <summary>
    /// You probably want to pass only rooted paths
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static NormalizedPath CreateRelative( NormalizedPath source, NormalizedPath target )
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
            // if( source.IsRooted ) return source.Combine( target ).ResolveDots( source.Parts.Any() ? 1 : 0 );
            if( source.IsRooted ) return source.Combine( target ).ResolveDotsSmart();
        }
        // if( source.IsRooted || target.IsRooted ) throw new NotImplementedException();

        if( source.Equals( target ) ) return ReturnProxy( "" );

        if( target.StartsWith( source ) )
            return ReturnProxy( target.RemoveFirstPart( source.Parts.Count ).ResolveDotsSmart() );
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
                // .ResolveDots( rootPartCount )
                .ResolveDotsSmart()
            );
        }
    }
}
