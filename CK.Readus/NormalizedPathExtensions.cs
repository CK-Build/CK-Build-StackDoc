using CK.Core;

namespace CK.Readus;

internal static class NormalizedPathExtensions
{
    public static bool IsRelative( this NormalizedPath @this )
    {
        return @this.IsRooted is false;
    }

    /// <summary>
    /// You probably want to pass only rooted paths
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <returns></returns>
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

    public static NormalizedPath GetCommonLeadingParts( this NormalizedPath source, NormalizedPath target )
    {
        if( source.Equals( target ) ) return source;

        var sequence = new NormalizedPath();

        var sourceCount = source.Count();
        var targetCount = target.Count();
        var range = sourceCount > targetCount ? targetCount : sourceCount;

        for( var index = 0; index < range; index++ )
        {
            var sourcePart = source.Parts[index];
            var targetPart = target.Parts[index];

            if( sourcePart != targetPart ) return sequence;

            sequence = sequence.AppendPart( sourcePart );
        }

        return sequence;
    }
}
