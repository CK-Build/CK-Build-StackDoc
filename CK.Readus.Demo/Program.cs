// See https://aka.ms/new-console-template for more information

using CK.Core;
using CK.Monitoring;
using CK.Monitoring.Handlers;
using CK.Readus;

public static class Program
{
    public static async Task Main( string[] args )
    {
        var grandOutputConfiguration = new GrandOutputConfiguration()
        // { MinimalFilter = LogFilter.Release }
        .AddHandler( new ConsoleConfiguration() );
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
            Directory.Delete( outputFolder, true );
        Directory.CreateDirectory( outputFolder );

        var stackPath = new NormalizedPath( inputFolder );

        var ckCoreProjects = stackPath.AppendPart( "CK-Core-Projects" );
        var ckAspNetProjects = stackPath.AppendPart( "CK-AspNet-Projects" );
        var ckDatabaseProjects = stackPath.AppendPart( "CK-Database-Projects" );
        var yodiiProjects = stackPath.AppendPart( "Yodii-Projects" );
        var ckCrsProjects = stackPath.AppendPart( "CK-Crs-Projects" );
        var ckSample = stackPath.AppendPart( "CK-Sample" );
        var iot = stackPath.AppendPart( "IoT" );

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

            (ckAspNetProjects.AppendPart( "CK-AspNet" ), "https://github.com/Invenietis/CK-AspNet"),
            (ckAspNetProjects.AppendPart( "CK-AspNet-Auth" ), "https://github.com/Invenietis/CK-AspNet-Auth"),
            (ckAspNetProjects.AppendPart( "CK-AspNet-Tester" ), "https://github.com/Invenietis/CK-AspNet-Tester"),

            (ckDatabaseProjects.AppendPart( "CK-CodeGen" ), "https://github.com/Invenietis/CK-CodeGen"),
            (ckDatabaseProjects.AppendPart( "CK-StObj" ), "https://github.com/signature-opensource/CK-StObj"),
            (ckDatabaseProjects.AppendPart( "CK-Database" ), "https://gitlab.com/Signature-Code/CK-Database"),
            (ckDatabaseProjects.AppendPart( "CK-DB" ), "https://github.com/Invenietis/CK-DB"),
            (ckDatabaseProjects.AppendPart( "CK-DB-GitHub" ), "https://github.com/Invenietis/CK-DB-GitHub"),
            (ckDatabaseProjects.AppendPart( "CK-DB-GitLab" ), "https://github.com/signature-opensource/CK-DB-GitLab"),
            (ckDatabaseProjects.AppendPart( "CK-DB-Workspace" ), "https://github.com/signature-opensource/CK-DB-Workspace"),
            (ckDatabaseProjects.AppendPart( "CK-DB-Actor-ActorPhoneNumber" ), "https://github.com/signature-opensource/CK-DB-Actor-ActorPhoneNumber"),
            (ckDatabaseProjects.AppendPart( "CK-DB-Actor-ActorEMail" ), "https://github.com/Invenietis/CK-DB-Actor-ActorEMail"),
            (ckDatabaseProjects.AppendPart( "CK-DB-SqlCKTrait" ), "https://github.com/Invenietis/CK-DB-SqlCKTrait"),
            (ckDatabaseProjects.AppendPart( "CK-DB-TokenStore" ), "https://github.com/Invenietis/CK-DB-TokenStore"),
            (ckDatabaseProjects.AppendPart( "CK-DB-GuestActor" ), "https://github.com/Invenietis/CK-DB-GuestActor"),
            (ckDatabaseProjects.AppendPart( "CK-DB-GuestActor-Acl" ), "https://github.com/Invenietis/CK-DB-GuestActor-Acl"),
            (ckDatabaseProjects.AppendPart( "CK-DB-User-SimpleInvitation" ), "https://github.com/Invenietis/CK-DB-User-SimpleInvitation"),
            (ckDatabaseProjects.AppendPart( "CK-DB-User-UserPassword" ), "https://github.com/Invenietis/CK-DB-User-UserPassword"),
            (ckDatabaseProjects.AppendPart( "CK-DB-Facebook" ), "https://github.com/signature-opensource/CK-DB-Facebook"),
            (ckDatabaseProjects.AppendPart( "CK-DB-Twitter" ), "https://github.com/signature-opensource/CK-DB-Twitter"),
            (ckDatabaseProjects.AppendPart( "CK-Setup" ), "https://gitlab.com/Signature-Code/CK-Setup"),
            (ckDatabaseProjects.AppendPart( "CKSetupRemoteStore" ), "https://gitlab.com/Signature-Code/CKSetupRemoteStore"),
            (ckDatabaseProjects.AppendPart( "CK-Setup-Dependency" ), "https://github.com/Invenietis/CK-Setup-Dependency"),
            (ckDatabaseProjects.AppendPart( "CK-Sqlite" ), "https://github.com/Invenietis/CK-Sqlite"),
            (ckDatabaseProjects.AppendPart( "CK-SqlServer-Parser" ), "https://gitlab.com/Signature-Code/CK-SqlServer-Parser"),
            (ckDatabaseProjects.AppendPart( "CK-SqlServer-Parser-Model" ), "https://gitlab.com/Signature-Code/CK-SqlServer-Parser-Model"),

            (yodiiProjects.AppendPart( "Yodii-Script"),"https://github.com/Invenietis/yodii-script.git"),

            (ckCrsProjects.AppendPart( "CK-Cris" ), "https://gitlab.com/signature-code/CK-Cris"),
            (ckCrsProjects.AppendPart( "CK-StObj-TypeScript" ), "https://github.com/signature-opensource/CK-StObj-TypeScript"),
            (ckCrsProjects.AppendPart( "CK-AmbientValues" ), "https://github.com/Invenietis/CK-AmbientValues"),
            (ckCrsProjects.AppendPart( "CK-Crs" ), "https://github.com/Invenietis/crs"),

            (ckSample.AppendPart( "CK-Sample-MultiBinPath" ), "https://github.com/signature-opensource/CK-Sample-MultiBinPath"),
            (ckSample.AppendPart( "CK-Sample-Monitoring" ), "https://github.com/signature-opensource/CK-Sample-Monitoring"),
            (ckSample.AppendPart( "CK-Sample-WebFrontAuth" ), "https://github.com/Woinkk/CK-Sample-WebFrontAuth"),

            (iot.AppendPart( "CK-MQTT" ), "https://github.com/signature-opensource/CK-MQTT"),
            (iot.AppendPart( "CK-Monitoring-MQTT" ), "https://github.com/Invenietis/CK-Monitoring-MQTT"),
        };

        var context = new MdContext( "CK", repositories );

        context.SetOutputPath( outputFolder );
        await context.WriteHtmlAsync( monitor );
    }
}
