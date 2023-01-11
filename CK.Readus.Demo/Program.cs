// See https://aka.ms/new-console-template for more information

using CK.Core;
using CK.Monitoring;
using CK.Monitoring.Handlers;
using CK.Readus;
using Console = System.Console;

var grandOutputConfiguration = new GrandOutputConfiguration().AddHandler( new ConsoleConfiguration() );
GrandOutput.EnsureActiveDefault( grandOutputConfiguration );

var monitor = new ActivityMonitor();

var path = @"C:\Users\Aymeric.Richard\Downloads\CK-Core-develop";

var crawler = new DocumentationCrawler();
var files = crawler.GetMarkdownFiles( monitor, path ).ToArray();

Console.WriteLine( $"{files.Length} md files :" );
foreach( var file in files )
{
    Console.WriteLine( file );
}

Console.ReadKey();
