using CK.Core;

namespace CK.Readus;

public static class NormalizedPathExtensions
{
    public static bool IsRelative( this NormalizedPath @this )
    {
        return @this.IsRooted is false;
    }
}
