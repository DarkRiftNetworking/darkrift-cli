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

namespace DarkRift.Cli
{
    class Program
    {
        /// <summary>
        /// The location of the template archives.
        /// <summary>
        private static readonly string TEMPLATES_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates");

        [Verb("new", HelpText = "Create a new DarkRift project.")]
        class NewOptions
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
            public bool Tier { get; set; }
            
            [Option('s', "platform", Default = ServerPlatform.Framework, HelpText = "Specify the .NET platform of the server to use.")]
            public ServerPlatform Platform { get; set; }
        }

        [Verb("run", HelpText = "Run a DarkRift project.")]
        class RunOptions
        {

        }

        [Verb("get", HelpText = "Downloads a plugin package into this server.")]
        class GetOptions
        {
            [Value(0, Required = true)]
            public string Url { get; set; }
        }

        [Verb("pull", HelpText = "Pulls the specified version of DarkRift locally.")]
        class PullOptions
        {
            [Value(0, Required = true)]
            public String Version { get; set; }

            [Option('p', "pro", Default = false, HelpText = "Use the pro version.")]
            public bool Tier { get; set; }

            [Option('s', "platform", Default = ServerPlatform.Framework, HelpText = "Use the .NET platform of the server to use.")]
            public ServerPlatform Platform { get; set; }
        }

        public static int Main(string[] args)
        {
            return CommandLine.Parser.Default.ParseArguments<NewOptions, RunOptions, GetOptions, PullOptions>(args)
                .MapResult(
                    (NewOptions opts) => New(opts),
                    (RunOptions opts) => Run(opts),
                    (GetOptions opts) => Get(opts),
                    (PullOptions opts) => Pull(opts),
                    _ => 1);
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

            foreach (string path in Directory.GetFiles(targetDirectory, "*.*", SearchOption.AllDirectories))
                FileTemplater.TemplateFileAndPath(path, Path.GetFileName(targetDirectory), opts.Version ?? VersionManager.GetLatestDarkRiftVersion(), opts.Tier ? ServerTier.Pro : ServerTier.Free, opts.Platform);

            Console.WriteLine(Output.Green($"Created '{Path.GetFileName(targetDirectory)}'"));

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

            string path = VersionManager.GetInstallationPath(Version.Parse(project.Runtime.Version), project.Runtime.Tier, project.Runtime.Platform);
            if (path == null)
                return 1;

            string fullPath = Path.Combine(path, "DarkRift.Server.Console.exe"); //TODO handle standard having a different filename/executable

            Match match = Regex.Match(Environment.CommandLine, @"(?<=(darkrift-cli\.dll|darkrift(\.exe|\.sh)?) run)");
            if (match == null)
                throw new ArgumentException("Failed to start server. Cannot find start of server arguments.");

            string args = Environment.CommandLine.Substring(match.Index);

            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo(fullPath, args);
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
            string path = VersionManager.GetInstallationPath(Version.Parse(opts.Version), opts.Tier ? ServerTier.Pro : ServerTier.Free, opts.Platform);

            if (path == null)
                return 1;

            Console.WriteLine(path);
            return 0;
        }
    }
}
