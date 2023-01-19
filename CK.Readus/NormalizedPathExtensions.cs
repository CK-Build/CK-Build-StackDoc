using CK.Core;

namespace CK.Readus;

public static class NormalizedPathExtensions
{
    public static bool IsAbsolute( this NormalizedPath @this )
    {
        return @this.IsRooted;
    }

    public static bool IsRelative( this NormalizedPath @this )
    {
        return IsAbsolute( @this ) is false;
    }
}
