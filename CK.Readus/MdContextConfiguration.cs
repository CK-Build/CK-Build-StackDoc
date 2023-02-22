namespace CK.Readus;

public class MdContextConfiguration
{
    public bool EnableLinkAvailabilityCheck { get; set; }
    public bool EnableGitSupport { get; set; }


    public static MdContextConfiguration DefaultConfiguration()
    {
        return new MdContextConfiguration()
        {
            EnableLinkAvailabilityCheck = false,
            EnableGitSupport = false,
        };
    }
}
