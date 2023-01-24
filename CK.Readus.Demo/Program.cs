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

        if (args.Length == 1)
        {
            outputFolder = inputFolder + "_output";
            monitor.Info( $"Output folder not provided, using ${outputFolder}." );
        }
        else outputFolder = args[1];

        //TODO: cleanup output folder.

        var factory = new MdRepositoryReader();
        var info = factory.ReadPath( monitor, inputFolder, string.Empty );

        info.EnsureLinks( monitor );
        info.Generate( monitor, outputFolder );
    }
}
