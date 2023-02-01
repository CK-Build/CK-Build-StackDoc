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
}
