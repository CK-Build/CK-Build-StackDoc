namespace CK.Readus.Tests;

public class MdStackTests : TestBase
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

        var sut = MdStack.Load( Monitor, name, repositories, default );

        var outputPath = OutFolder.AppendPart( "SimpleStack_Generated" );

        sut.Generate( Monitor, outputPath );
    }

    [Test]
    public void Generate_should_write_simple_stack_and_transform_to_html()
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

        var sut = MdStack.Load( Monitor, name, repositories, default );
        foreach( var (repositoryName, mdRepository) in sut.Repositories )
        {
            mdRepository.EnsureLinks( Monitor );
            mdRepository.Apply( Monitor );
        }


        var outputPath = OutFolder.AppendPart( "SimpleStack_Generated" );

        sut.Generate( Monitor, outputPath );
    }

    [Test]
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
            mdRepository.EnsureLinks( Monitor );
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

        var mdStack = MdStack.Load( Monitor, stackName, repositoriesInfo, default );

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

        var mdStack = MdStack.Load( Monitor, stackName, repositoriesInfo, default );

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
    [Ignore( "Solve TODO" )]
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
            "https://github.com/Invenietis/FooBarFakeRepo4"
        };

        var repositoriesInfo = new List<(NormalizedPath, NormalizedPath)>();
        for( var index = 0; index < repoPaths.Length; index++ )
        {
            var repoPath = repoPaths[index];
            repositoriesInfo.Add( (repoPath, repoRemotes[index]) );
        }

        var mdStack = MdStack.Load( Monitor, stackName, repositoriesInfo, default );

        //TODO: Since the behavior of transformations has changed :
        // I do not want to manipulate absolute path on the local disk
        // I want to manipulate path that can be rooted to a virtual root.
        // Here the path that contains FooBarFakeRepo2 (and others) is the virtual root
        // While it's virtual, the output can be anything, we just ensure this root kind.
        // The expected has to be changed.

        var urlUnderTest = repoRemotes[1];
        var expected = repoPaths[1];
        var sut = mdStack.TransformCrossRepositoryUrl( Monitor, urlUnderTest );

        sut.Should().NotBe( urlUnderTest );
        sut.Should().Be( expected );

        urlUnderTest = "https://github.com/Invenietis/FooBarFakeRepo2/README.md";
        expected = repoPaths[1].AppendPart( "README.md" );
        sut = mdStack.TransformCrossRepositoryUrl( Monitor, urlUnderTest );

        sut.Should().NotBe( urlUnderTest );
        sut.Should().Be( expected );
    }
}
