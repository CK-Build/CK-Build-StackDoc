﻿using CK.Core;
using Markdig;

namespace CK.Readus;

public class MdRepository
{
    public string RepositoryName { get; }

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

    [Obsolete( "Will be removed for clarity. Use MdContext instead." )]
    public void EnsureLinks( IActivityMonitor monitor )
    {
        foreach( var file in DocumentationFiles )
        {
            using( monitor.OpenInfo( $"Check links in file '{file.Value.DocumentName}'" ) )
            {
                file.Value.CheckLinks( monitor, CheckRepository );
            }

            using( monitor.OpenInfo( $"Transform links in file '{file.Value.DocumentName}'" ) )
            {
                file.Value.TransformLinks( monitor, TransformRepository );
            }
        }
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
            var parts = file.Parts;
            var relativePath = parts.SkipWhile( p => p.Equals( RepositoryName ) is false );
            var path = outputPath;
            foreach( var part in relativePath ) path = path.AppendPart( part );

            var lastPart = path.LastPart.Replace( ".md", ".html" );
            path = path.RemoveLastPart().AppendPart( lastPart );

            return path;
        }

        //TODO: Maybe it should check existence only.
        Directory.CreateDirectory( outputPath );

        foreach( var (sourcePath, markdown) in DocumentationFiles )
        {
            var html = markdown.MarkdownDocument.ToHtml();
            var path = ResolvePath( sourcePath );

            Directory.CreateDirectory( path.RemoveLastPart() );
            File.WriteAllText( path, html );
        }
    }

    public void CheckRepository( IActivityMonitor monitor, NormalizedPath link )
    {
        //TODO: check when the link has no attached text (so is useless).
    }

    public NormalizedPath TransformRepository( IActivityMonitor monitor, NormalizedPath link )
    {
        var transformed = new NormalizedPath( link );

        // TODO: a link to a directory should try look for a README.md file.
        // if( Directory.Exists( link.LastPart ) ) ;

        // for now true
        // 99% cases we want to transform .md to .html
        // But then we need to determine which links are to transform
        // A link has to correspond to a file.
        // At the very end I may resolve this :
        // The Current link will have a resolved target.
        // We can lookup in all the files and if the target match, we change extensions of both
        // file and link.
        if( true ) //TODO: in case of a stack, this has to be changed
        {
            var extension = Path.GetExtension( transformed.LastPart );
            if( extension.Equals( ".md" ) )
            {
                transformed = Path.ChangeExtension( transformed, ".html" );
            }
        }

        return transformed;
    }
}
