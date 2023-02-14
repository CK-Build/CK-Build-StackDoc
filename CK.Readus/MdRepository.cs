﻿using System.Diagnostics;
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
    /// <param name="outputPath"></param>
    public void Generate( IActivityMonitor monitor, NormalizedPath outputPath )
    {
        monitor.Info( $"Writing '{RepositoryName}' documentation to '{outputPath}'" );

        NormalizedPath ResolvePath( NormalizedPath file )
        {
            var path = outputPath.Combine( file.RemovePrefix( "~" ) );
            return path;
        }

        foreach( var (_, mdDocument) in DocumentationFiles )
        {
            var html = mdDocument.MarkdownDocument.ToHtml();
            var path = ResolvePath( mdDocument.Current );

            Directory.CreateDirectory( path.RemoveLastPart() );
            File.WriteAllText( path, html );
        }
    }

    public void CheckRepository( IActivityMonitor monitor, NormalizedPath link )
    {
        //TODO: check when the link has no attached text (so is useless).
    }
}
