using System.Diagnostics;
using CK.Core;
using Markdig;

namespace CK.Readus;

[DebuggerDisplay( "{Parent.StackName}::{RepositoryName}: {DocumentationFiles.Count} documents" )]
internal class MdRepository
{
    public string RepositoryName { get; }

    public NormalizedPath VirtualRoot => Parent.Parent.AttachToVirtualRoot( RootPath );

    public NormalizedPath RootPath { get; }

    public NormalizedPath RemoteUrl { get; }

    public MdStack Parent { get; }

    // TODO: this could be readonly dictionary ?
    /// <summary>
    /// Key is full path.
    /// </summary>
    public Dictionary<NormalizedPath, MdDocument> DocumentationFiles { get; }

    public MdRepository
    (
        string repositoryName,
        NormalizedPath remoteUrl,
        NormalizedPath rootPath,
        Dictionary<NormalizedPath, MdDocument> documentationFiles,
        MdStack parent
    )
    {
        RepositoryName = repositoryName;
        RemoteUrl = remoteUrl;
        RootPath = rootPath;
        DocumentationFiles = documentationFiles;
        Parent = parent;
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
            var html = mdDocument.MarkdownDocument.ToHtml( MdContext.Pipeline );

            Directory.CreateDirectory( mdDocument.Current.RemoveLastPart() );
            File.WriteAllText( mdDocument.Current, html );
        }
    }

    public void CheckRepository( IActivityMonitor monitor, NormalizedPath link )
    {
        //TODO: check when the link has no attached text (so is useless).
    }
}
