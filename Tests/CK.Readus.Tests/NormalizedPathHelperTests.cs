using static CK.Readus.NormalizedPathHelpers;

namespace CK.Readus.Tests;

internal class NormalizedPathHelperTests : TestBase
{
    [Test]
    [TestCase( "", "", "" )]
    [TestCase( "Project/A/B/C", "Project/A/", "../.." )]
    [TestCase( "Project/A/B/C", "Project/A/B/C/D", "D" )]
    [TestCase( "Project/A/B/C/D", "Project/A/B/C/D", "" )]
    [TestCase( "A/B/C", "Project/A/B/C/D", "../../../Project/A/B/C/D" )]
    [TestCase( "A/B/C/E/F/G", "Project/A/B/C/D", "../../../../../../Project/A/B/C/D" )]
    [TestCase( "C", "Project/A/B/C/D", "../Project/A/B/C/D" )]
    [TestCase( "B", "Project/A/B/C/D", "../Project/A/B/C/D" )]
    [TestCase( "A/B/C", "C/D", "../../../C/D" )]
    [TestCase( "B", "Project/A/B/C/B/D", "../Project/A/B/C/B/D" )]
    [TestCase( "Project", "Project/../B", "../B" )]
    [TestCase( "Project", "../Project/../B", "../../B" )]
    [TestCase( "./~user/Pictures", "./~user/Documents/A", @"../Documents/A" )]
    public void CreateRelative_experiments_relative_relative
    (
        string sourceString,
        string targetString,
        string expectedString
    )
    {
        CreateRelativeTester( sourceString, targetString, expectedString );
    }

    // @formatter:off
    [Test]
    [TestCase( "/", "/", "" )]
    [TestCase( "/root/Project/A/B/C", "/root/Project/A/", "../.." )]
    [TestCase( "/root/Project/A/B/C", "/root/tcejorP/A/", "../../../../tcejorP/A" )]
    [TestCase( "//root/Project/A/B/C", "//root/tcejorP/A/", "../../../../tcejorP/A" )]
    [TestCase( @"\\root\Project\A\B\C", @"\\root\tcejorP\A\", @"..\..\..\..\tcejorP\A" )]
    [TestCase( @"\root\Project\A\B\C", @"\root\tcejorP\A\", @"..\..\..\..\tcejorP\A" )]
    [TestCase ( "https://github.com/Invenietis/CK-Core", "https://github.com/Invenietis/CK-Core/tree/develop/CK.Core", @"tree/develop/CK.Core" )]
    [TestCase( "~/Pictures", "~/Documents/A", @"../Documents/A" )]
    [TestCase( "~user/Pictures", "~user/Documents/A", @"../Documents/A" )]
    // @formatter:on
    public void CreateRelative_experiments_absolute_absolute
    (
        string sourceString,
        string targetString,
        string expectedString
    )
    {
        CreateRelativeTester( sourceString, targetString, expectedString );
    }

    // @formatter:off
    [Test]
    [TestCase( 0, "/", "", "/" )] //TODO: solve
    [TestCase( 1, "", "/", "/" )]
    [TestCase( 2, "~", "/", "/" )]
    [TestCase( 3, "/root/Project/A/B/C", "Project/A/", "/root/Project/A/B/C/Project/A" )] // TODO: solve
    [TestCase( 4, "Project/A/B/C", "/root/tcejorP/A/", "/root/tcejorP/A/" )] // TODO: this throws
    [TestCase ( 5, "https://github.com/Invenietis/CK-Core", "A/B", "https://github.com/Invenietis/CK-Core/A/B" )] // TODO: solve
    [TestCase ( 6, "A/B", "https://github.com/Invenietis/CK-Core", "https://github.com/Invenietis/CK-Core" )] // TODO: this throws
    [TestCase( 7, "~/Pictures", "/Documents/A", "/Documents/A" )] //TODO: this throws
    [TestCase( 8, "/Documents/A", "~/Pictures", "~/Pictures" )]
    // @formatter:on
    public void CreateRelative_experiments_different_rootKind
    (
        int i, // an index to get the same order in test explorer
        string sourceString,
        string targetString,
        string expectedString
    )
    {
        CreateRelativeTester( sourceString, targetString, expectedString );
    }


    private static void CreateRelativeTester( string sourceString, string targetString, string expectedString )
    {
        var source = new NormalizedPath( sourceString );
        var target = new NormalizedPath( targetString );
        var expected = new NormalizedPath( expectedString );

        CreateRelative( source, target ).Should().Be( expected );
        source.Path.Should().Be( new NormalizedPath( sourceString ) );
        target.Path.Should().Be( new NormalizedPath( targetString ) );
    }
}
