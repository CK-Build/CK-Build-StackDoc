using System.Collections;

namespace CK.Readus.Tests;

internal class MdStackTests : TestBase
{
    [SetUp]
    public void SetUp()
    {
        var outputPath = OutFolder.AppendPart( "SimpleStack_Generated" );

        TestHelper.CleanupFolder( outputPath );
    }

    [Test]
    public void Generate_should_write_simple_stack()
    {
        var name = "foo-bar";

        var basePath = InFolder.AppendPart( "SimpleStack" );

        var repoPaths =
        new[]
        {
            "FooBarFakeRepo1",
            "FooBarFakeRepo2",
            "FooBarFakeRepo3",
            "FooBarFakeRepo4",
        }
        .Select( p => basePath.AppendPart( p ) )
        .ToArray();

        var repositories = new List<(NormalizedPath, NormalizedPath)>();
        foreach( var repoPath in repoPaths )
        {
            repositories.Add( (repoPath, string.Empty) );
        }

        var sut = MdStack.Load( Monitor, name, repositories, SimpleContext );

        var outputPath = OutFolder.AppendPart( "SimpleStack_Generated" );

        sut.Generate( Monitor, outputPath );
    }

    [Test]
    [Explicit( "Was using removed method" )]
    public void Generate_should_write_simple_stack_and_transform_to_html()
    {
        //TODO: This test could be used when it is possible to add a repository afterward
        // An other case could be using a stack that has been fed to the context.
        var name = "foo-bar";

        var basePath = InFolder.AppendPart( "SimpleStack" );

        var repoPaths =
        new[]
        {
            "FooBarFakeRepo1",
            "FooBarFakeRepo2",
            "FooBarFakeRepo3",
            "FooBarFakeRepo4",
        }
        .Select( p => basePath.AppendPart( p ) )
        .ToArray();

        var repositories = new List<(NormalizedPath, NormalizedPath)>();
        foreach( var repoPath in repoPaths )
        {
            repositories.Add( (repoPath, string.Empty) );
        }

        var sut = MdStack.Load( Monitor, name, repositories, SimpleContext );
        foreach( var (repositoryName, mdRepository) in sut.Repositories )
        {
            // mdRepository.EnsureLinks( Monitor );
            mdRepository.Apply( Monitor );
        }


        var outputPath = OutFolder.AppendPart( "SimpleStack_Generated" );

        sut.Generate( Monitor, outputPath );
    }

    [Test]
    [Explicit( "Uses an obsolete method" )]
    public void Generate_should_write_simple_stack_with_cross_links_and_transform_to_html()
    {
        var name = "foo-bar";

        var basePath = InFolder.AppendPart( "SimpleStackWithCrossRef" );

        var repoPaths =
        new[]
        {
            "FooBarFakeRepo1",
            "FooBarFakeRepo2",
            "FooBarFakeRepo3",
            "FooBarFakeRepo4",
        }
        .Select( p => basePath.AppendPart( p ) )
        .ToArray();

        var repositories = new List<(NormalizedPath, NormalizedPath)>();
        foreach( var repoPath in repoPaths )
        {
            repositories.Add( (repoPath, string.Empty) );
        }

        var sut = MdStack.Load( Monitor, name, repositories, default );
        foreach( var (repositoryName, mdRepository) in sut.Repositories )
        {
            // mdRepository.EnsureLinks( Monitor );
            mdRepository.Apply( Monitor );
        }


        var outputPath = OutFolder.AppendPart( "SimpleStackWithCrossRef_Generated" );

        sut.Generate( Monitor, outputPath );
    }

    [Test]
    public void TransformCrossRepositoryUrl_should_return_same_link_when_not_uri()
    {
        var stackName = "foo-bar";

        var basePath = InFolder.AppendPart( "SimpleStackWithCrossRef" );

        var repoPaths =
        new[]
        {
            "FooBarFakeRepo1",
            "FooBarFakeRepo2",
            "FooBarFakeRepo3",
            "FooBarFakeRepo4",
        }
        .Select( p => basePath.AppendPart( p ) )
        .ToArray();


        var repoRemotes = new[]
        {
            "https://github.com/Invenietis/FooBarFakeRepo1",
            "https://github.com/Invenietis/FooBarFakeRepo2",
            "https://github.com/Invenietis/FooBarFakeRepo3",
            "https://github.com/Invenietis/FooBarFakeRepo4"
        };

        var repositoriesInfo = new List<(NormalizedPath, NormalizedPath)>();
        for( var index = 0; index < repoPaths.Length; index++ )
        {
            var repoPath = repoPaths[index];
            repositoriesInfo.Add( (repoPath, repoRemotes[index]) );
        }

        var mdStack = MdStack.Load( Monitor, stackName, repositoriesInfo, CrossRefContext );

        var urlsUnderTest = new[]
        {
            "A/B/C",
            "",
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

        foreach( var urlUnderTest in urlsUnderTest )
        {
            var sut = mdStack.TransformCrossRepositoryUrl( Monitor, urlUnderTest );
            sut.Should().Be( urlUnderTest );
        }
    }

    [Test]
    public void TransformCrossRepositoryUrl_should_return_same_link_when_not_cross_ref()
    {
        var stackName = "foo-bar";

        var basePath = InFolder.AppendPart( "SimpleStackWithCrossRef" );

        var repoPaths =
        new[]
        {
            "FooBarFakeRepo1",
            "FooBarFakeRepo2",
            "FooBarFakeRepo3",
            "FooBarFakeRepo4",
        }
        .Select( p => basePath.AppendPart( p ) )
        .ToArray();


        var repoRemotes = new[]
        {
            "https://github.com/Invenietis/FooBarFakeRepo1",
            "https://github.com/Invenietis/FooBarFakeRepo2",
            "https://github.com/Invenietis/FooBarFakeRepo3",
            "https://github.com/Invenietis/FooBarFakeRepo4"
        };

        var repositoriesInfo = new List<(NormalizedPath, NormalizedPath)>();
        for( var index = 0; index < repoPaths.Length; index++ )
        {
            var repoPath = repoPaths[index];
            repositoriesInfo.Add( (repoPath, repoRemotes[index]) );
        }

        var mdStack = MdStack.Load( Monitor, stackName, repositoriesInfo, CrossRefContext );

        var urlsUnderTest = new[]
        {
            "https://github.com/Invenietis/CK-Core",
            "http://whatever.com",
            "https://google.com",
            "https://github.com/Invenietis/CK-Core/blob/develop/CK.Core/NormalizedPath.cs",
            "https://github.com/Invenietis/CK-Core/search?q=concatenate",
            "https://www.greenbird.com/news/railway-oriented-programming-in-kotlin#:~:text=Railway%20Oriented%20Programming%20(ROP)%20is,can%20be%20of%20any%20type.",
            "https://github.com/CK-Build/CK-Build-StackDoc",
            "https://gitmoji.dev/related-tools"
        };
        foreach( var urlUnderTest in urlsUnderTest )
        {
            var sut = mdStack.TransformCrossRepositoryUrl( Monitor, urlUnderTest );
            sut.Should().Be( urlUnderTest );
        }
    }

    [Test]
    public void TransformCrossRepositoryUrl_should_return_local_path_when_cross_ref()
    {
        var stackName = "foo-bar";

        var basePath = InFolder.AppendPart( "SimpleStackWithCrossRef" );

        var repoPaths =
        new[]
        {
            "FooBarFakeRepo1",
            "FooBarFakeRepo2",
            "FooBarFakeRepo3",
            "FooBarFakeRepo4",
        }
        .Select( p => basePath.AppendPart( p ) )
        .ToArray();

        var repoRemotes = new[]
        {
            "https://github.com/Invenietis/FooBarFakeRepo1",
            "https://github.com/Invenietis/FooBarFakeRepo2",
            "https://github.com/Invenietis/FooBarFakeRepo3",
            "https://github.com/Invenietis/FooBarFakeRepo4",
        };

        var repositoriesInfo = new List<(NormalizedPath, NormalizedPath)>();
        for( var index = 0; index < repoPaths.Length; index++ )
        {
            var repoPath = repoPaths[index];
            repositoriesInfo.Add( (repoPath, repoRemotes[index]) );
        }

        var mdStack = MdStack.Load( Monitor, stackName, repositoriesInfo, CrossRefContext );

        var urlUnderTest = repoRemotes[1];
        var expected = "~/FooBarFakeRepo2";
        var sut = mdStack.TransformCrossRepositoryUrl( Monitor, urlUnderTest );

        sut.Should().NotBe( urlUnderTest );
        sut.Should().Be( expected );

        urlUnderTest = "https://github.com/Invenietis/FooBarFakeRepo2/README.md";
        expected = new NormalizedPath( "~" ).AppendPart( repoPaths[1].LastPart ).AppendPart( "README.md" );
        sut = mdStack.TransformCrossRepositoryUrl( Monitor, urlUnderTest );

        sut.Should().NotBe( urlUnderTest );
        sut.Should().Be( expected );
    }

    public static IEnumerable TransformCrossRepositoryUrlShouldBeIdempotentData
    {
        get
        {
            yield return new TestCaseData( "README.md", "README.md" );
            yield return new TestCaseData( "", "" );
            yield return new TestCaseData( ".", "." );
            yield return new TestCaseData( "A/README.md", "A/README.md" );
            yield return new TestCaseData( "../README.md", "../README.md" );
            yield return new TestCaseData
            (
                "https://github.com/Invenietis/FooBarFakeRepo2/README.md",
                "~/FooBarFakeRepo2/README.md"
            );
            yield return new TestCaseData
            (
                "https://github.com/Invenietis/UnknownRepo/README.md",
                "https://github.com/Invenietis/UnknownRepo/README.md"
            );
            yield return new TestCaseData
            (
                InFolder.Combine( @"SimpleStackWithCrossRef\FooBarFakeRepo1\README.md" ).Path,
                @"~/FooBarFakeRepo1\README.md"
            );
        }
    }

    [Test]
    [TestCaseSource( nameof( TransformCrossRepositoryUrlShouldBeIdempotentData ) )]
    public void TransformCrossRepositoryUrl_should_be_idempotent( string link, string expected )
    {
        var stack = CrossRefContext.Stacks.First().Value;

        var sut = stack.TransformCrossRepositoryUrl( Monitor, link );
        sut.Should().Be( expected );
        sut = stack.TransformCrossRepositoryUrl( Monitor, sut );
        sut.Should().Be( expected );
    }

    [Test]
    [TestCase( "https://github.com/Invenietis/FooBarFakeRepo1/blob/master/Project/README.md", "~/Project/README.md" )]
    [TestCase( "https://github.com/Invenietis/FooBarFakeRepo1/tree/master/Project", "~/Project" )]
    [TestCase( "https://github.com/Invenietis/FooBarFakeRepo1/blob/develop/Project/README.md", "https://github.com/Invenietis/FooBarFakeRepo1/blob/develop/Project/README.md" )]
    [TestCase( "https://github.com/Invenietis/FooBarFakeRepo1/README.md", "~/README.md" )]
    [TestCase( "https://github.com/Invenietis/FooBarFakeRepo2/blob/master/Project/README.md", "https://github.com/Invenietis/FooBarFakeRepo2/blob/master/Project/README.md" )]
    public void TransformCrossRepositoryUrl_should_handle_github_branches( string link, string expected )
    {
        var stack = GitContext.Stacks.First().Value;

        var sut = stack.TransformCrossRepositoryUrl( Monitor, link );
        sut.Should().Be( expected );
    }

    [Test]
    [TestCase( "https://gitlab.com/Invenietis/FooBarFakeRepo1/-/blob/master/Project/README.md", "https://gitlab.com/Invenietis/FooBarFakeRepo1/-/blob/master/Project/README.md" )]
    [TestCase( "https://gitlab.com/Invenietis/FooBarFakeRepo1/-/tree/master/Project", "https://gitlab.com/Invenietis/FooBarFakeRepo1/-/tree/master/Project" )]
    [TestCase( "https://gitlab.com/Invenietis/FooBarFakeRepo1/-/blob/develop/Project/README.md", "https://gitlab.com/Invenietis/FooBarFakeRepo1/-/blob/develop/Project/README.md" )]
    [TestCase( "https://gitlab.com/Invenietis/FooBarFakeRepo1/-/README.md", "https://gitlab.com/Invenietis/FooBarFakeRepo1/-/README.md" )]
    [TestCase( "https://gitlab.com/Invenietis/FooBarFakeRepo2/-/blob/master/Project/README.md", "https://gitlab.com/Invenietis/FooBarFakeRepo2/-/blob/master/Project/README.md" )]
    [TestCase( "https://gitlab.com/Invenietis/FooBarFakeRepo2/-/blob/develop/Project/README.md", "~/FooBarFakeRepo2/Project/README.md" )]
    public void TransformCrossRepositoryUrl_should_handle_gitlab_branches( string link, string expected )
    {
        var stack = AdvancedGitContext.Stacks.First().Value;

        var sut = stack.TransformCrossRepositoryUrl( Monitor, link );
        sut.Should().Be( expected );
    }

    [Test]
    [TestCase( "https://github.com/Invenietis/FooBarFakeRepo1/blob/master/Project/README.md", "~/FooBarFakeRepo1/Project/README.md" )]
    [TestCase( "https://github.com/Invenietis/FooBarFakeRepo1/blob/feature/doSmth/Project/README.md", "~/FooBarFakeRepo1-featureBranch/Project/README.md" )]
    [TestCase( "https://github.com/Invenietis/FooBarFakeRepo1/blob/develop/Project/README.md", "https://github.com/Invenietis/FooBarFakeRepo1/blob/develop/Project/README.md" )]
    public void TransformCrossRepositoryUrl_should_handle_multiple_branches( string link, string expected )
    {
        var stack = AdvancedGitContext.Stacks.First().Value;

        var sut = stack.TransformCrossRepositoryUrl( Monitor, link );
        sut.Should().Be( expected );
    }
}
