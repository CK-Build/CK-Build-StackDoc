namespace CK.Readus.Tests;

internal class DisplayTestHelperContent : TestBase
{
    [Test]
    public void DisplayContent()
    {
        Monitor.Info( Environment.NewLine );
        Monitor.Info( GetContextContent( nameof( SimpleContext ), SimpleContext ) );
        Monitor.Info( Environment.NewLine );
        Monitor.Info( GetContextContent( nameof( CrossRefContext ), CrossRefContext ) );
        Monitor.Info( Environment.NewLine );
        Monitor.Info( GetContextContent( nameof( SingleRepositoryContext ), SingleRepositoryContext ) );
        Monitor.Info( Environment.NewLine );
        Monitor.Info( GetContextContent( nameof( DummyRepository ), DummyRepository ) );
        Monitor.Info( Environment.NewLine );
        Monitor.Info( GetContextContent( nameof( DummyDocument ), DummyDocument ) );
        Monitor.Info( Environment.NewLine );
        Monitor.Info( GetContextContent( nameof( DocumentWithinMultiRepositoryStack ), DocumentWithinMultiRepositoryStack ) );
        Monitor.Info( Environment.NewLine );
        Monitor.Info( GetContextContent( nameof( MultiStackContext ), MultiStackContext ) );
        Monitor.Info( Environment.NewLine );
        Monitor.Info( GetContextContent( nameof( MultiStackWithCrossRefContext ), MultiStackWithCrossRefContext ) );
    }
}
