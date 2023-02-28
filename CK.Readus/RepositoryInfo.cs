using CK.Core;

namespace CK.Readus;

public class RepositoryInfo
{
    /// <summary>
    /// By default, set with <see cref="Local"/> last part. Mostly used for logging.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Path to repository root on local disk.
    /// </summary>
    public NormalizedPath Local { get; }

    /// <summary>
    /// Remote repository used to resolve urls.
    /// </summary>
    public NormalizedPath Remote { get; }

    /// <summary>
    /// Repository version, used as a git reference.
    /// </summary>
    public string? Version { get; }

    /// <summary>
    /// A package is formed with its provider and full name: Provider:Full.Name.
    /// </summary>
    public IReadOnlyCollection<string> PublishedPackages { get; } = Array.Empty<string>();

    /// <summary>
    /// A dependency is formed with its provider, full name and version: Provider:Full.Name/version.
    /// </summary>
    public IReadOnlyCollection<string> ExternalDependencies { get; } = Array.Empty<string>();

    /// <summary>
    /// Dependency from the same world, formed with its provider and full name: Provider:Full.Name.
    /// </summary>
    public IReadOnlyCollection<string> InternalDependencies { get; } = Array.Empty<string>();

    public RepositoryInfo
    (
        NormalizedPath local,
        NormalizedPath remote,
        string? version,
        IReadOnlyCollection<string> publishedPackages,
        IReadOnlyCollection<string> externalDependencies,
        IReadOnlyCollection<string> internalDependencies,
        string? name = null
    )
    {
        Local = local;
        Remote = remote;
        Version = version;
        PublishedPackages = publishedPackages;
        ExternalDependencies = externalDependencies;
        InternalDependencies = internalDependencies;
        Name = name ?? local.LastPart();
    }

    public RepositoryInfo( NormalizedPath local, NormalizedPath remote, string? name = null )
    {
        Local = local;
        Remote = remote;
        Name = name ?? local.LastPart();
    }

    public bool IsValid => !Local.IsEmptyPath && !Remote.IsEmptyPath;
    //TODO: Validate packages and dependencies
}
