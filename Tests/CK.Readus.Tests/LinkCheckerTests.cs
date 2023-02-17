namespace CK.Readus.Tests;

internal class LinkCheckerTests : TestBase
{
    [Test]
    [TestCase("https://google.fr")]
    [TestCase("https://stackoverflow.com")]
    [TestCase("https://www.nuget.org/packages/CK.ActivityMonitor")]
    public async Task CheckLinkAvailabilityAsync_should_not_throwAsync(string link)
    {
        var checker = new LinkChecker();
        await checker.CheckLinkAvailabilityAsync( Monitor, link );
    }
}
