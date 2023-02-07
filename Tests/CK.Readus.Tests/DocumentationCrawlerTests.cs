namespace CK.Readus.Tests;

internal class DocumentationCrawlerTests : TestBase
{
    private readonly NormalizedPath _inFolder;
    private readonly NormalizedPath _fooBarSolutionFolder;
    private int _fooBarMdFileCount;

    public DocumentationCrawlerTests()
    {
        _inFolder = ProjectFolder.AppendPart( "In" );
        var tempFolder = _inFolder.AppendPart( "Temp" );
        _fooBarSolutionFolder = tempFolder.AppendPart( "FooBar" );

        Directory.CreateDirectory( _inFolder );
    }

    [OneTimeSetUp]
    public void Setup()
    {
        if( Directory.Exists( _fooBarSolutionFolder ) )
            Directory.Delete( _fooBarSolutionFolder, true );

        // Setup FooBar Solution.
        var filesNames = new[]
        {
            "Readme.md",
            "code.cs",
            "FooBar.sln",
            ".hiddenFile",
            "muffin-recipe.docx",
            "SubFolder1/Readme.md",
            "SubFolder1/someCode.cs",
            "SubFolder1/SubFolder.csproj",
            "SubFolder1/bin/Debug/library.dll",
            "SubFolder2/Readme.md",
            "SubFolder2/Hello/There/This/Is/General/Kenobi.md",
        };

        foreach( var fileName in filesNames )
        {
            var file = _fooBarSolutionFolder.Combine( new NormalizedPath( fileName ) );

            Directory.CreateDirectory( file.RemoveLastPart() );
            File.Create( file );
        }

        _fooBarMdFileCount = 4;
    }
}
