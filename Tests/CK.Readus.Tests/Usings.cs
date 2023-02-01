global using NUnit.Framework;
global using FluentAssertions;
global using CK.Core;
global using static CK.Testing.MonitorTestHelper;
using System.Diagnostics;

[assembly: DebuggerDisplay( "{Path}", Target = typeof( NormalizedPath ) )]
