using CK.Core;

namespace CK.Readus;

public class MdContextFactory
{
    /// <summary>
    /// Create context with default configuration.
    /// </summary>
    /// <returns></returns>
    public MdContext CreateContext( IActivityMonitor monitor )
    {
        return CreateContext( monitor, MdContextConfiguration.DefaultConfiguration() );
    }

    /// <summary>
    /// Create context with specific configuration.
    /// </summary>
    /// <param name="monitor"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public MdContext CreateContext( IActivityMonitor monitor, MdContextConfiguration configuration )
    {
        var context = new MdContext( configuration );
        return context;
    }
}
