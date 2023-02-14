namespace CK.Readus.Tests;

internal class MdContextTests : TestBase
{
    [Test]
    public void WriteHtml_simple_stack_should_ensure_calls_to_all_checks_and_transforms()
    {
        var context = SimpleContext;

        context.WriteHtml( Monitor );
    }

    [Test]
    public void WriteHtml_simple_stack_with_cross_ref_should_ensure_calls_to_all_checks_and_transforms()
    {
        var context = CrossRefContext;

        context.WriteHtml( Monitor );
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
}
