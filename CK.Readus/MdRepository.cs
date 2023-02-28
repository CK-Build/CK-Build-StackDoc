using System.Diagnostics;
using CK.Core;

namespace CK.Readus;

[DebuggerDisplay( "{Parent.StackName}::{RepositoryName}: {DocumentationFiles.Count} documents" )]
internal class MdRepository
{
    public NormalizedPath? GitBranch { get; }

    /// <summary>
    /// Usually, repository version found in a git tag.
    /// </summary>
    public NormalizedPath? GitRef =>  string.IsNullOrWhiteSpace(Info.Version) ? (NormalizedPath?)null: Info.Version;

    public RepositoryInfo Info { get; }

    public string RepositoryName => Info.Name;

    public NormalizedPath VirtualRoot => Parent.Parent.AttachToVirtualRoot( LocalPath );

    public NormalizedPath LocalPath => Info.Local;

    public NormalizedPath RemoteUrl => Info.Remote;

    public MdWorld Parent { get; }

    /// <summary>
    /// Key is full path.
    /// </summary>
    public Dictionary<NormalizedPath, MdDocument> DocumentationFiles { get; }

    public MdRepository
    (
        Dictionary<NormalizedPath, MdDocument> documentationFiles,
        MdWorld parent,
        NormalizedPath? gitBranch,
        RepositoryInfo repositoryInfo
    )
    {
        DocumentationFiles = documentationFiles;
        Parent = parent;
        GitBranch = gitBranch;
        Info = repositoryInfo;
    }

    public bool TryGetReadme( out NormalizedPath readme )
    {
        var readmeCandidates = DocumentationFiles
                               .Values
                               .Where
                               (
                                   d => d.DocumentNameWithoutExtension
                                         .Equals( "README", StringComparison.OrdinalIgnoreCase )
                               )
                               .ToArray();

        if( readmeCandidates.Length == 0 )
        {
            readme = default;
            return false;
        }

        readme = readmeCandidates.Min( r => r.Current);

        return true;
    }

    public void Apply( IActivityMonitor monitor )
    {
        foreach( var file in DocumentationFiles )
        {
            file.Value.Apply( monitor );
        }
    }

    /// <summary>
    /// Output the current state of the documentation as html.
    /// </summary>
    /// <param name="monitor"></param>
    public void Generate( IActivityMonitor monitor )
    {
        monitor.Info( $"Writing '{RepositoryName}' documentation to '{Parent.Parent.OutputPath}'" );

        foreach( var (_, mdDocument) in DocumentationFiles )
        {
            var toc = Parent.Parent.GenerateHtmlToc( monitor, mdDocument );
            HtmlWriter.WriteHtml( mdDocument, toc );
        }
    }

    public void CheckRepository( IActivityMonitor monitor, NormalizedPath link )
    {
        //TODO: check when the link has no attached text (so is useless).
    }
}
