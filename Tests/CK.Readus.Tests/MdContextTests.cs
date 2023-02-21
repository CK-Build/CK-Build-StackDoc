namespace CK.Readus.Tests;

internal class MdContextTests : TestBase
{
    [Test]
    public async Task WriteHtml_simple_stack_should_ensure_calls_to_all_checks_and_transformsAsync()
    {
        var context = SimpleContext;

        await context.WriteHtmlAsync( Monitor );
    }

    [Test]
    public async Task WriteHtml_simple_stack_with_cross_ref_should_ensure_calls_to_all_checks_and_transformsAsync()
    {
        var context = CrossRefContext;

        await context.WriteHtmlAsync( Monitor );
    }
    [Test]
    public async Task WriteHtml_multi_stack_with_cross_stack_ref_should_ensure_calls_to_all_checks_and_transformsAsync()
    {
        var context = MultiStackWithCrossRefContext;

        await context.WriteHtmlAsync( Monitor );
    }

    [Test]
    public void should_have_common_virtual_root()
    {
        MultiStackContext.VirtualRoot.Should().Be( InFolder );
        MultiStackWithCrossRefContext.VirtualRoot.Should().Be( InFolder );
        SimpleContext.VirtualRoot.Should().Be( InFolder.AppendPart( "SimpleStack" ) );
        SingleRepositoryContext.VirtualRoot.Should().Be( InFolder.AppendPart( "SimpleStack" ).AppendPart( "FooBarFakeRepo1" ) );
        CrossRefContext.VirtualRoot.Should().Be( InFolder.AppendPart( "SimpleStackWithCrossRef" ) );
    }

    [Test]
    public void AttachToVirtualRoot_should_attach_single_repository_context()
    {
        var context = SingleRepositoryContext;
        var stack = context.Stacks.First().Value;
        var repository = stack.Repositories.First().Value;

        var virtualPath = context.AttachToVirtualRoot( repository.RootPath );

        virtualPath.Should().Be( "~" );
    }

    [Test]
    public void AttachToVirtualRoot_should_attach_multi_repository_context()
    {
        var context = SimpleContext;
        var stack = context.Stacks.First().Value;
        var repository = stack.Repositories.First().Value;

        var virtualPath = context.AttachToVirtualRoot( repository.RootPath );

        var expected = new NormalizedPath( "~" ).AppendPart( repository.RepositoryName );

        virtualPath.Should().Be( expected );
    }
}
