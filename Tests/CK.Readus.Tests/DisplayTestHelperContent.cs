namespace CK.Readus.Tests;

public class DisplayTestHelperContent : TestBase
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
    }
}
