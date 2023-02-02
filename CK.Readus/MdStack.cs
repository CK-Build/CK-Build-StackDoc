using System.Diagnostics;
using System.Text;
using CK.Core;
using Markdig;
using Markdig.Syntax;

namespace CK.Readus;

public class MdStack
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

    public MarkdownDocument GenerateToc( IActivityMonitor monitor )
    {
        var builder = new StringBuilder();
        builder.AppendLine( $"# {StackName}" );
        builder.AppendLine();

        foreach( var (name, mdRepository) in Repositories )
        {
            builder.AppendLine( $"## {name}" ).AppendLine();
            var pathToRepo = new NormalizedPath( mdRepository.RepositoryName ).AppendPart( "README.md" );
            builder.AppendLine( $"[README]({pathToRepo})" ).AppendLine();
            foreach( var (path, mdDocument) in mdRepository.DocumentationFiles )
            {
                builder.AppendLine( $"[{mdDocument.DocumentName}]({mdDocument.Current})" );
            }
        }

        return Markdown.Parse( builder.ToString() );
    }

    public void Generate( IActivityMonitor monitor, NormalizedPath outputPath )
    {
        using( monitor.OpenInfo( $"Writing stack '{StackName}' to '{outputPath}'" ) )
        {
            foreach( var repository in Repositories )
            {
                repository.Value.Generate( monitor, outputPath );
            }
        }
    }

    //TODO: Where to put the transformations methods ?
    // They can be in a Md* class, like here.
    // But it could be handled differently, anywhere actually.
    // It depend how it is called, but the whole stack of Md* has access to everything.
    /// <summary>
    /// If the link target is in the stack, return its path relative to the stack virtual root.
    /// </summary>
    /// <param name="monitor"></param>
    /// <param name="link"></param>
    /// <returns></returns>
    public NormalizedPath TransformCrossRepositoryUrl( IActivityMonitor monitor, NormalizedPath link )
    {
        if( link.IsRelative() ) return link;

        var isUri = link.RootKind == NormalizedPathRootKind.RootedByURIScheme;

        return ResolveScope();

        NormalizedPath ResolveScope()
        {
            foreach( var (name, mdRepository) in Repositories )
            {
                var target = isUri ? mdRepository.RemoteUrl : mdRepository.RootPath;

                Debug.Assert( target.IsEmptyPath is false, "target.IsEmptyPath is false" );
                // Strict has to be false because by default when both path are equals it return false.
                // This is not a behavior that I would except to be the default.
                // I may want to have a way to return true when both are equal but not when other is empty.
                var linkIsInScope = link.StartsWith( target, false );
                if( linkIsInScope is false ) continue;

                var linkRelativeToItsRepository = link.RemoveFirstPart( target.Parts.Count );
                var rootlessLink = new NormalizedPath( mdRepository.RepositoryName )
                .Combine( linkRelativeToItsRepository );

                return rootlessLink;
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
