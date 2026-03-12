using System.Text;
using Drift.Cli;

Console.OutputEncoding = Encoding.UTF8;
return await DriftCli.InvokeAsync( args, toConsole: true );