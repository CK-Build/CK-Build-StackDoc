using System.Diagnostics;
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
        [TestCase( "Project", "Project/../B", "../B" )]
        [TestCase( "Project", "../Project/../B", "../../B" )]
        public void CreateRelative_experiments_relative_relative
        (
            string sourceString,
            string targetString,
            string expectedString
        )
        {
            var source = new NormalizedPath( sourceString );
            var target = new NormalizedPath( targetString );
            var expected = new NormalizedPath( expectedString );

            CreateRelative( source, target ).Should().Be( expected );
            source.Path.Should().Be( new NormalizedPath( sourceString ) );
            target.Path.Should().Be( new NormalizedPath( targetString ) );
        }

        [Test]
        [TestCase( "/", "/", "" )]
        [TestCase( "/root/Project/A/B/C", "/root/Project/A/", "../.." )]
        [TestCase( "/root/Project/A/B/C", "/root/tcejorP/A/", "../../../../tcejorP/A" )]
        public void CreateRelative_experiments_absolute_absolute
        (
            string sourceString,
            string targetString,
            string expectedString
        )
        {
            var source = new NormalizedPath( sourceString );
            var target = new NormalizedPath( targetString );
            var expected = new NormalizedPath( expectedString );

            CreateRelative( source, target ).Should().Be( expected );
            source.Path.Should().Be( new NormalizedPath( sourceString ) );
            target.Path.Should().Be( new NormalizedPath( targetString ) );
        }

        static NormalizedPath CreateRelative( NormalizedPath source, NormalizedPath target )
        {
            // if( source.IsRooted || target.IsRooted ) throw new NotImplementedException();

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
                // If rooted, stop move up on common root
                var commonStartPartCount = 0;
                while( source.Parts[commonStartPartCount] == target.Parts[commonStartPartCount] )
                {
                    commonStartPartCount++;
                }

                if( source.IsRelative() && target.IsRelative() )
                {
                    // it is handled up there
                    Debug.Assert( commonStartPartCount.Equals( 0 ), "commonStartPartCount.Equals( 0 )" );
                }

                var moveUpBy = source.Parts.Count - commonStartPartCount;

                var result = "";
                for( var i = 0; i < moveUpBy; i++ ) result += "../";

                // if target start with dots, needs to block
                var rootPartCount = moveUpBy;
                foreach( var targetPart in target.Parts )
                {
                    if( targetPart.Equals( ".." ) ) rootPartCount++;
                    else break;
                }

                // The result has to be a relative path. Knowing this :
                // Here we remove the common part from both path to create the suffix of the path.
                // It removes a root (that become unneeded) if any.
                var suffix = target.RemovePrefix( target.RemoveLastPart( target.Parts.Count - commonStartPartCount ) );

                return new NormalizedPath( result )
                       .Combine( suffix )
                       .ResolveDots( rootPartCount );
            }

            throw new NotImplementedException();
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
            // I thought it was needed, but is not. It is not finished even if all tests pass.
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
