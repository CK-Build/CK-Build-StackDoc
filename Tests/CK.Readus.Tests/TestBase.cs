using CK.Core;

namespace CK.Readus.Tests;

public class TestBase
{
    public IActivityMonitor Monitor => TestHelper.Monitor;

    // ReSharper disable once MemberCanBeProtected.Global
    public TestBase(  )
    {
    }

    public static bool[] FlipFlags() => new[] { true, false };
}
