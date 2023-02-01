namespace CK.Readus.Tests;

public class MdContextTests : TestBase
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
}
