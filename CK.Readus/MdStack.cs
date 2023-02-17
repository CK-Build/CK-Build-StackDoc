﻿using System.Diagnostics;
using System.Text;
using CK.Core;

namespace CK.Readus;

[DebuggerDisplay( "{StackName}: {Repositories.Count} repositories" )]
internal class MdStack
{
    public MdContext Parent { get; }

    /// <summary>
    /// Key is repository name
    /// </summary>
    public IDictionary<string, MdRepository> Repositories { get; }

    public string StackName { get; }

    // public MdStack( IDictionary<string, MdRepository> repositories, string stackName )
    // {
    //     Repositories = repositories;
    //     StackName = stackName;
    // }

    public MdStack( string stackName, MdContext parent )
    {
        StackName = stackName;
        Repositories = new Dictionary<string, MdRepository>();
        Parent = parent;
    }

    public static MdStack Load
    (
        IActivityMonitor monitor,
        string stackName,
        IEnumerable<(NormalizedPath localPath, NormalizedPath remoteUrl)> repositoriesInfo, //TODO dico. Path is uniq
        MdContext parent
    )
    {
        using var info = monitor.OpenInfo( $"Loading stack '{stackName}'" );

        var mdStack = new MdStack( stackName, parent );

        var repositoryFactory = new MdRepositoryReader();

        foreach( var repoPath in repositoriesInfo )
        {
            var repository = repositoryFactory.ReadPath( monitor, repoPath.localPath, repoPath.remoteUrl, mdStack );
            mdStack.Repositories.Add( repository.RepositoryName, repository );
        }

        return mdStack;
    }

    public void Generate( IActivityMonitor monitor, NormalizedPath outputPath )
    {
        using( monitor.OpenInfo( $"Writing stack '{StackName}' to '{outputPath}'" ) )
        {
            foreach( var repository in Repositories )
            {
                repository.Value.Generate( monitor );
            }
        }
    }

    /// <summary>
    /// If the link target is in any of the stacks, return its path virtually rooted.
    /// </summary>
    /// <param name="monitor"></param>
    /// <param name="link"></param>
    /// <returns></returns>
    public NormalizedPath TransformCrossRepositoryUrl( IActivityMonitor monitor, NormalizedPath link )
    {
        if( link.IsRelative() ) return link; // Should not happen

        var isUri = link.RootKind == NormalizedPathRootKind.RootedByURIScheme;
//TODO: rooted by first part with ~ can shortcut return link
        return ResolveScope();

        NormalizedPath ResolveScope()
        {
            var repositories = Parent.Stacks.Values.SelectMany( s => s.Repositories );
            // We want repositories but it can be done at context level with all repo from all stacks.
            foreach( var (_, mdRepository) in repositories )
            {
                var target = isUri ? mdRepository.RemoteUrl : mdRepository.RootPath;

                Debug.Assert( target.IsEmptyPath is false, "target.IsEmptyPath is false" );
                // Strict has to be false because by default when both path are equals it return false.
                // This is not a behavior that I would except to be the default.
                // I may want to have a way to return true when both are equal but not when other is empty.
                var linkIsInScope = link.StartsWith( target, false );
                if( linkIsInScope is false ) continue;

                var linkRelativeToItsRepository = link.RemoveFirstPart( target.Parts.Count );
                var virtualLink = mdRepository.VirtualRoot.Combine( linkRelativeToItsRepository );

                return virtualLink;
            }

            return link;
        }
    }

    public void CheckStack( IActivityMonitor monitor, NormalizedPath link ) { }
}

public static class StringBuilderExtensions
{
    public static StringBuilder AppendLine( this StringBuilder @this, int lineCount )
    {
        for( var i = 0; i < lineCount; i++ ) @this.AppendLine();

        return @this;
    }
}
