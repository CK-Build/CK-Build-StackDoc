global using NUnit.Framework;
global using FluentAssertions;
global using static CK.Testing.MonitorTestHelper;
using System.Diagnostics;
using CK.Core;

[assembly: DebuggerDisplay( "{Path}", Target = typeof( NormalizedPath ) )]
