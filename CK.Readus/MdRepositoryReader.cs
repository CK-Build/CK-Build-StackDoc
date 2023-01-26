using System.Diagnostics;
using CK.Core;
using Markdig;
using Markdig.Syntax;

namespace CK.Readus;

/// <summary>
/// Factory for <see cref="MdRepository"/>.
/// </summary>
public class MdRepositoryReader
{
    public MdRepository ReadPath
    (
        IActivityMonitor monitor,
        NormalizedPath rootPath,
        NormalizedPath remoteUrl
    )
    {
        Throw.CheckArgument( !rootPath.IsEmptyPath );

        using( monitor.OpenInfo( $"Reading repository '{rootPath}'" ) )
        {
            var repositoryName = rootPath.LastPart;
            var filesPaths = Directory.GetFiles
            (
                rootPath.Path,
                "*.md",
                new EnumerationOptions { RecurseSubdirectories = true }
            )
            .Select( f => new NormalizedPath( f ) )
            .ToArray();

            //TODO: what about built things. Like under node_modules.

            var documentationFiles = new Dictionary<NormalizedPath, MdDocument>( filesPaths.Length );

            foreach( var file in filesPaths )
            {
                Debug.Assert( file.IsRooted, "file.IsRooted" );
                monitor.Info( $"Add '{file}'" );
                documentationFiles.Add( file, MdDocument.Load( file ) );
            }

            monitor.Info( $"Repository '{repositoryName}' contains a total of "
                        + $"{documentationFiles.Values.Select( v => v.MarkdownBoundLinks.Count ).Sum()} links"
                        + $" within {filesPaths.Length} md files.");

            return new MdRepository( repositoryName, remoteUrl, rootPath, documentationFiles );
        }
    }
}
