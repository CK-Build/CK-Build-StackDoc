// See https://aka.ms/new-console-template for more information

using CK.Core;
using CK.Monitoring;
using CK.Monitoring.Handlers;
using CK.Readus;

public static class Program
{
    public static void Main( string[] args )
    {
        var grandOutputConfiguration = new GrandOutputConfiguration().AddHandler( new ConsoleConfiguration() );
        GrandOutput.EnsureActiveDefault( grandOutputConfiguration );

        var monitor = new ActivityMonitor();

        if( args.Length == 0 )
        {
            monitor.Error( "First parameter missing : Provide an input folder please." );
            return;
        }

        var inputFolder = args[0];
        string outputFolder;

        if( args.Length == 1 )
        {
            outputFolder = inputFolder + "_output";
            monitor.Info( $"Output folder not provided, using '${outputFolder}'." );
        }
        else outputFolder = args[1];

        if( Directory.Exists( outputFolder ) )
            outputFolder += "0";
        Directory.CreateDirectory( outputFolder );

        //TODO: cleanup output folder.

        var stackPath = new NormalizedPath( inputFolder );
        var ckCoreProjects = stackPath.AppendPart( "CK-Core-Projects" );

        var repositories = new List<(NormalizedPath local, NormalizedPath remote)>
        {
            (ckCoreProjects.AppendPart( "CK-ActivityMonitor" ), "https://github.com/Invenietis/CK-ActivityMonitor"),
            (ckCoreProjects.AppendPart( "CK-Auth-Abstractions" ), "https://github.com/Invenietis/CK-Auth-Abstractions"),
            (ckCoreProjects.AppendPart( "CK-Core" ), "https://github.com/Invenietis/CK-Core"),
            (ckCoreProjects.AppendPart( "CK-Globbing" ), "https://github.com/Invenietis/CK-Globbing"),
            (ckCoreProjects.AppendPart( "CK-Monitoring" ), "https://github.com/Invenietis/CK-Monitoring"),
            (ckCoreProjects.AppendPart( "CK-PerfectEvent" ), "https://github.com/Invenietis/CK-PerfectEvent"),
            (ckCoreProjects.AppendPart( "CK-Reflection" ), "https://github.com/Invenietis/CK-Reflection"),
            (ckCoreProjects.AppendPart( "CK-SqlServer" ), "https://github.com/Invenietis/CK-SqlServer"),
            (ckCoreProjects.AppendPart( "CK-SqlServer-Dapper" ), "https://github.com/Invenietis/CK-SqlServer-Dapper"),
            (ckCoreProjects.AppendPart( "CK-Testing" ), "https://github.com/Invenietis/CK-Testing"),
            (ckCoreProjects.AppendPart( "CK-UnitsOfMeasure" ), "https://github.com/Invenietis/CK-UnitsOfMeasure"),
            (ckCoreProjects.AppendPart( "CK-WeakAssemblyNameResolver" ), "https://github/com/Invenietis/CK-WeakAssemblyNameResolver"),
            (ckCoreProjects.AppendPart( "json-graph-serializer" ), "https://github/com/Invenietis/json-graph-serializer"),
        };


        var context = new MdContext( "CK", repositories );

        context.SetOutputPath( outputFolder );
        context.WriteHtml( monitor );
    }
}
