using System.Diagnostics;
using CK.Core;
using LibGit2Sharp;

namespace CK.Readus;

/// <summary>
/// Factory for <see cref="MdRepository"/>.
/// </summary>
internal class MdRepositoryReader
{
    public MdRepository ReadPath
    (
        IActivityMonitor monitor,
        RepositoryInfo repositoryInfo,
        MdWorld mdWorld
    )
    {
        Throw.CheckArgument( repositoryInfo.IsValid );

        using( monitor.OpenInfo( $"Reading repository '{repositoryInfo.Local}'" ) )
        {
            var filesPaths = Directory.GetFiles
                                      (
                                          repositoryInfo.Local.Path,
                                          "*.md",
                                          new EnumerationOptions { RecurseSubdirectories = true }
                                      )
                                      .Select( f => new NormalizedPath( f ) )
                                      .ToArray();

            //TODO: what about built things. Like under node_modules.

            var documentationFiles = new Dictionary<NormalizedPath, MdDocument>( filesPaths.Length );

            NormalizedPath? gitBranch = null;
            if( mdWorld.Parent.Configuration.EnableGitSupport )
            {
                using var gitRepository = new Repository( repositoryInfo.Local );
                gitBranch = gitRepository.Head.FriendlyName;
            }

            var mdRepository = new MdRepository
            (
                documentationFiles,
                mdWorld,
                gitBranch,
                repositoryInfo
            );

            foreach( var file in filesPaths )
            {
                Debug.Assert( file.IsRooted, "file.IsRooted" );
                monitor.Info( $"Add '{file}'" );
                documentationFiles.Add( file, MdDocument.Load( file, mdRepository ) );
            }

            monitor.Info
            (
                $"Repository '{repositoryInfo.Name}' contains a total of "
              + $"{documentationFiles.Values.Select( v => v.MarkdownBoundLinks.Count ).Sum()} links"
              + $" within {filesPaths.Length} md files."
            );

            return mdRepository;
        }
    }
}
