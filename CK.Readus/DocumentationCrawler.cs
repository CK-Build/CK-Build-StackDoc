using CK.Core;

namespace CK.Readus;

public class DocumentationCrawler
{
    public IEnumerable<string> GetMarkdownFiles( IActivityMonitor monitor, NormalizedPath path )
    {
        Throw.CheckArgument( !path.IsEmptyPath );
        var normalizedPath = new NormalizedPath( path );

        var repositoryName = normalizedPath.LastPart;
        var files = Directory.GetFiles( path.Path, "*.md", new EnumerationOptions() { RecurseSubdirectories = true } );

        monitor.Trace( $"Repository \"{repositoryName}\" at location {path} contains {files.Length} md files." );

        return files;
    }
}
