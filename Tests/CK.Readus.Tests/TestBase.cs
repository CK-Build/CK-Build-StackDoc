namespace CK.Readus.Tests;

public class TestBase
{
    // ReSharper disable once MemberCanBeProtected.Global
    public TestBase( bool flag )
    {
        FeatureFlag.TransformAlwaysReturnAbsolutePath = flag;
    }

    public static bool[] FlipFlags() => new[] { true, false };
}
