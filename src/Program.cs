using System.Text.RegularExpressions;
using System.Reflection.Metadata;
using System.Reflection;
using System.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using CommandLine;
using Crayon;
using System.Collections.Generic;

namespace DarkRift.Cli
{
    internal class Program
    {
        /// <summary>
        /// The location of the template archives.
        /// <summary>
        private static readonly string TEMPLATES_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates");

        [Verb("new", HelpText = "Create a new DarkRift project.")]
        private class NewOptions
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
        private class RunOptions
        {
            [Value(0)]
            public IEnumerable<string> Values { get; set; }
        }

        [Verb("get", HelpText = "Downloads a plugin package into this server.")]
        private class GetOptions
        {
            [Value(0, Required = true)]
            public string Url { get; set; }
        }

        [Verb("pull", HelpText = "Pulls the specified version of DarkRift locally.")]
        private class PullOptions
        {
            [Value(0, Required = false)]
            public string Version { get; set; }

            [Option('p', "pro", Default = false, HelpText = "Use the pro version.")]
            public bool Tier { get; set; }

            [Option('s', "platform", Default = ServerPlatform.Framework, HelpText = "Use the .NET platform of the server to use.")]
            public ServerPlatform Platform { get; set; }

            [Option('d', "docs", Default = false, HelpText = "Download the documentation for this version instead.")]
            public bool Docs { get; set; }
        }

        [Verb("docs", HelpText = "Opens the documentation for DarkRift.")]
        private class DocsOptions
        {
            [Value(0, Required = false)]
            public string Version { get; set; }

            [Option('l', "local", Default = false, HelpText = "Opens a local copy of the documentation.")]
            public bool Local { get; set; }
        }

        public static int Main(string[] args)
        {
            return new Parser(SetupParser).ParseArguments<NewOptions, RunOptions, GetOptions, PullOptions, DocsOptions>(args)
                .MapResult(
                    (NewOptions opts) => New(opts),
                    (RunOptions opts) => Run(opts),
                    (GetOptions opts) => Get(opts),
                    (PullOptions opts) => Pull(opts),
                    (DocsOptions opts) => Docs(opts),
                    _ => 1);
        }

        /// <summary>
        /// Setup the parser for our application.
        /// </summary>
        /// <param name="settings">The settings for the parser.</param>
        private static void SetupParser(ParserSettings settings)
        {
            // Default
            settings.HelpWriter = Console.Error;

            // Added for 'run' command
            settings.EnableDashDash = true;
        }

        private static int New(NewOptions opts)
        {
            string targetDirectory = opts.TargetDirectory ?? Environment.CurrentDirectory;
            string templatePath = Path.Combine(TEMPLATES_PATH, opts.Type + ".zip");

            Directory.CreateDirectory(targetDirectory);

            if (Directory.GetFiles(targetDirectory).Length > 0 && !opts.Force)
            {
                Console.Error.WriteLine(Output.Red("Cannot create from template, the directory is not empty. Use -f to force creation."));
                Console.Error.WriteLine("\t" + Environment.GetCommandLineArgs()[0] + " " + CommandLine.Parser.Default.FormatCommandLine(new NewOptions { Type = opts.Type, TargetDirectory = opts.TargetDirectory, Force = true }));
                return 1;
            }

            if (!File.Exists(templatePath))
            {
                Console.Error.WriteLine(Output.Red("Cannot create from template, no template with that name exists."));
                return 1;
            }

            Console.WriteLine($"Creating new {opts.Type} '{Path.GetFileName(targetDirectory)}' from template...");

            ZipFile.ExtractToDirectory(templatePath, targetDirectory, true);

            Console.WriteLine($"Cleaning up extracted artifacts...");

            string version = opts.Version ?? VersionManager.GetLatestDarkRiftVersion();

            foreach (string path in Directory.GetFiles(targetDirectory, "*.*", SearchOption.AllDirectories))
                FileTemplater.TemplateFileAndPath(path, Path.GetFileName(targetDirectory), version, opts.Pro ? ServerTier.Pro : ServerTier.Free, opts.Platform);

            Console.WriteLine(Output.Green($"Created '{Path.GetFileName(targetDirectory)}'"));

            // Make sure that the given DarkRift version is actually downloaded.
            VersionManager.GetInstallationPath(version, opts.Pro ? ServerTier.Pro : ServerTier.Free, opts.Platform);

            return 0;
        }

        private static int Run(RunOptions opts)
        {
            Project project = Project.Load();

            if (project.Runtime == null)
            {
                project.Runtime = new Runtime(VersionManager.GetLatestDarkRiftVersion(), ServerTier.Free, ServerPlatform.Framework);
                project.Save();
            }

            string path = VersionManager.GetInstallationPath(project.Runtime.Version, project.Runtime.Tier, project.Runtime.Platform);
            if (path == null)
                return 1;

            // Calculate the executable file to run
            string fullPath;
            IEnumerable<string> args;
            if (project.Runtime.Platform == ServerPlatform.Framework)
            {
                fullPath = Path.Combine(path, "DarkRift.Server.Console.exe");
                args = opts.Values;
            }
            else
            {
                fullPath = "dotnet";
                args = opts.Values.Prepend(Path.Combine(path, "Lib", "DarkRift.Server.Console.dll"));
            }

            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo(fullPath, string.Join(" ", args));
                process.Start();

                process.WaitForExit();

                return process.ExitCode;
            }
        }

        private static int Get(GetOptions opts)
        {
            if (!Uri.TryCreate(opts.Url, UriKind.RelativeOrAbsolute, out Uri uri))
            {
                Console.Error.WriteLine(Output.Red("Invalid URL passed."));
                Console.Error.WriteLine("\t" + Environment.GetCommandLineArgs()[0] + " " + CommandLine.Parser.Default.FormatCommandLine(new GetOptions { Url = "https://your-download-url/file.zip" }));
                return 1;
            }

            string stagingDirectory = Path.Combine(".", ".darkrift", "temp");
            string stagingPath = Path.Combine(stagingDirectory, "Download.zip");
            Directory.CreateDirectory(stagingDirectory);

            Console.WriteLine($"Downloading package...");

            using (WebClient myWebClient = new WebClient())
            {
                myWebClient.DownloadFile(uri, stagingPath);
            }

            Console.WriteLine($"Extracting package...");

            // TODO find a better place for this
            string targetDirectory = Path.Combine(".", "plugins");
            Directory.CreateDirectory(targetDirectory);

            ZipFile.ExtractToDirectory(stagingPath, targetDirectory, true);

            Console.WriteLine(Output.Green($"Sucessfully downloaded package into plugins directory."));

            return 0;
        }

        private static int Pull(PullOptions opts)
        {
            if (string.IsNullOrEmpty(opts.Version))
            {
                // if version info was omitted, overwrite any parameters with current project settings
                if (Project.IsCurrentDirectoryAProject())
                {
                    var project = Project.Load();

                    opts.Version = project.Runtime.Version;
                    opts.Platform = project.Runtime.Platform;
                    opts.Tier = project.Runtime.Tier == ServerTier.Pro;
                }
                else
                {
                    Console.Error.WriteLine(Output.Red($"You can perform this command only in a project directory."));
                    return 2;
                }
            }

            // if version provided is "latest", it is being replaced with currently most recent one
            if (opts.Version == "latest")
            {
                opts.Version = VersionManager.GetLatestDarkRiftVersion();
            }

            // If --docs was specified, download documentation instead
            string path;
            if (opts.Docs)
            {
                path = VersionManager.GetDocumentationPath(opts.Version);
            }
            else
            {
                path = VersionManager.GetInstallationPath(opts.Version, opts.Tier ? ServerTier.Pro : ServerTier.Free, opts.Platform);
            }

            if (path == null)
                return 1;

            return 0;
        }

        private static int Docs(DocsOptions opts)
        {
            if (string.IsNullOrEmpty(opts.Version))
            {
                // If version info was omitted, overwrite any parameters with current project settings
                if (Project.IsCurrentDirectoryAProject())
                {
                    var project = Project.Load();

                    opts.Version = project.Runtime.Version;
                }
                else
                {
                    Console.Error.WriteLine(Output.Red($"You can perform this command only in a project directory."));
                    return 2;
                }
            }

            // If version provided is "latest", it is being replaced with currently most recent one
            if (opts.Version == "latest")
            {
                opts.Version = VersionManager.GetLatestDarkRiftVersion();
            }

            if (opts.Local)
            {
                BrowserUtil.OpenTo("file://" + VersionManager.GetDocumentationPath(opts.Version) + "/index.html");
            }
            else
            {
                BrowserUtil.OpenTo($"https://darkriftnetworking.com/DarkRift2/Docs/{opts.Version}");
            }

            return 0;
        }
    }
}
