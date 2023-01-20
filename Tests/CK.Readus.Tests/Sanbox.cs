using System.Text;
using CK.Core;

namespace CK.Readus.Tests
{
    public class Sandbox
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
        public void CreateRelative_experiments( string sourceString, string targetString, string expectedString )
        {
            var source = new NormalizedPath( sourceString );
            var target = new NormalizedPath( targetString );
            var expected = new NormalizedPath( expectedString );

            // PathDifference( source, target, true ).Should().Be( expected );
            CreateRelative( source, target ).Should().Be( expected );
        }

        static NormalizedPath CreateRelative( NormalizedPath source, NormalizedPath target )
        {
            if( source.IsRooted || target.IsRooted ) throw new NotImplementedException();

            if( source.Equals( target ) ) return "";

            if( target.StartsWith( source ) ) return target.RemoveFirstPart( source.Parts.Count );
            if( source.StartsWith( target ) )
            {
                var moveUpBy = source.Parts.Count - target.Parts.Count;
                var result = "";
                for( var i = 0; i < moveUpBy; i++ ) result += "../";

                return result;
            }

            {
                var moveUpBy = source.Parts.Count;

                var result = "";
                for( var i = 0; i < moveUpBy; i++ ) result += "../";
                return new NormalizedPath( result ).Combine( target );
            }

            throw new NotImplementedException();

            return target;
        }

        [Test]
        [TestCase( "", "", "" )]
        [TestCase( "Project/A/B/C", "Project/A/", "Project/A" )]
        [TestCase( "Project/A/B/C", "Project/A/B/C/D", "Project/A/B/C" )]
        [TestCase( "Project/A/B/C/D", "Project/A/B/C/D", "Project/A/B/C/D" )]
        [TestCase( "A/B/C", "Project/A/B/C/D", "A/B/C" )]
        [TestCase( "A/B/C/E/F/G", "Project/A/B/C/D", "A/B/C" )]
        [TestCase( "C", "Project/A/B/C/D", "C" )]
        [TestCase( "B", "Project/A/B/C/D", "B" )]
        [TestCase( "A/B/C", "C/D", "C" )]
        [TestCase( "A/B/C", "C/D", "C" )]
        public void GetCommonParts_experiments( string sourceString, string targetString, string expectedString )
        {
            GetCommonParts( sourceString, targetString ).Should().Be( expectedString );
        }

        NormalizedPath GetCommonParts( NormalizedPath source, NormalizedPath target )
        {
            if( source.Equals( target ) ) return source;

            var sequence = new NormalizedPath();

            foreach( var sourcePart in source.Parts )
            {
                var match = target.Parts.FirstOrDefault( p => p.Equals( sourcePart ) );
                if( match is null ) continue;

                sequence = sequence.AppendPart( match );
            }

            return sequence;
        }
    }
}
