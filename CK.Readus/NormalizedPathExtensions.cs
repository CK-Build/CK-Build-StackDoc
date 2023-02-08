using CK.Core;

namespace CK.Readus;

internal static class NormalizedPathExtensions
{
    public static bool IsRelative( this NormalizedPath @this )
    {
        return @this.IsRooted is false;
    }

    public static NormalizedPath CreateRelative( this NormalizedPath source, NormalizedPath target )
    {
        return NormalizedPathHelpers.CreateRelative( source, target );
    }

    public static NormalizedPath LastPart( this NormalizedPath source )
    {
        return source.LastPart;
    }

    public static int Count( this NormalizedPath source )
    {
        return source.Parts.Count;
    }

    public static NormalizedPath ResolveDotsSmart( this NormalizedPath path )
    {
        var leadingDots = 0;
        foreach( var part in path.Parts )
        {
            if( part.Equals( ".." ) || part.Equals( "." ) )
                leadingDots++;
            else break;
        }

        return path.ResolveDots( leadingDots );
    }
}
