using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace DarkRift.Cli
{
    [Verb("new", HelpText = "Create a new DarkRift project.")]
    public class NewOptions
    {
        [Option('f', Default = false, HelpText = "Force creation overwriting any files that already exist in the directory.")]
        public bool Force { get; set; }

        [Value(0, HelpText = "The name of the template to unpack.", Required = true)]
        public string Type { get; set; }

        [Value(1, HelpText = "The directory to unpack the template in.")]
        public string TargetDirectory { get; set; }

        [Option("version", HelpText = "Specify the DarkRift version to use.")]
        public string Version { get; set; }

        [Option('p', "pro", Default = false, HelpText = "Use the pro version.")]
        public bool Pro { get; set; }

        [Option('s', "platform", Default = ServerPlatform.Framework, HelpText = "Specify the .NET platform of the server to use.")]
        public ServerPlatform Platform { get; set; }
    }

    [Verb("run", HelpText = "Run a DarkRift project.")]
    public class RunOptions
    {
        [Value(0)]
        public IEnumerable<string> Values { get; set; }
    }

    [Verb("get", HelpText = "Downloads a plugin package into this server.")]
    public class GetOptions
    {
        [Value(0, Required = true)]
        public string Url { get; set; }
    }

    [Verb("pull", HelpText = "Pulls the specified version of DarkRift locally.")]
    public class PullOptions
    {
        [Value(0, Required = false, HelpText = "The version of DarkRift")]
        public string Version { get; set; }

        [Option('p', "pro", Default = false, HelpText = "Use the pro version.")]
        public bool Pro { get; set; }

        [Option('s', "platform", Default = ServerPlatform.Framework, HelpText = "Use the .NET platform of the server to use.")]
        public ServerPlatform Platform { get; set; }

        [Option('d', "docs", Default = false, HelpText = "Download the documentation for this version instead.")]
        public bool Docs { get; set; }

        [Option('l', "list", Default = false, HelpText = "List installed versions of DarkRift")]
        public bool List { get; set; }

        [Option('f', "force", Default = false, HelpText = "Forces downloading of a DarkRift version or documentation")]
        public bool Force { get; set; }

        [Option("latest", Default = false, HelpText = "Specifies latest version")]
        public bool Latest { get; set; }
    }

    [Verb("docs", HelpText = "Opens the documentation for DarkRift.")]
    public class DocsOptions
    {
        [Value(0, Required = false, HelpText = "The version of DarkRift")]
        public string Version { get; set; }

        [Option('l', "local", Default = false, HelpText = "Opens a local copy of the documentation.")]
        public bool Local { get; set; }

        [Option("latest", Default = false, HelpText = "Specifies latest version")]
        public bool Latest { get; set; }
    }
}
