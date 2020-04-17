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
            Version version = opts.Version ?? VersionManager.GetLatestDarkRiftVersion();

            // Executes the command to download the version if it doesn't exist
            if (Pull(new PullOptions()
            {
                Version = version,
                Pro = opts.Pro,
                Platform = opts.Platform,
                Force = false
            }) != 0)
            {
                Console.Error.WriteLine(Output.Red("An error occured while trying to download the version required, exiting New"));
                return 2;
            }

            string targetDirectory = opts.TargetDirectory ?? Environment.CurrentDirectory;
            string templatePath = Path.Combine(TEMPLATES_PATH, opts.Type + ".zip");

            Directory.CreateDirectory(targetDirectory);

            if (Directory.GetFiles(targetDirectory).Length > 0 && !opts.Force)
            {
                Console.Error.WriteLine(Output.Red("Cannot create from template, the directory is not empty. Use -f to force creation."));
                Console.Error.WriteLine("\t" + Environment.GetCommandLineArgs()[0] + " " + Parser.Default.FormatCommandLine(new NewOptions { Type = opts.Type, TargetDirectory = opts.TargetDirectory, Force = true }));
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
                FileTemplater.TemplateFileAndPath(path, Path.GetFileName(targetDirectory), version, opts.Pro ? ServerTier.Pro : ServerTier.Free, opts.Platform);

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

            // Executes the command to download the version if it doesn't exist
            if (Pull(new PullOptions()
            {
                Version = project.Runtime.Version,
                Pro = project.Runtime.Tier == ServerTier.Pro,
                Platform = project.Runtime.Platform,
                Force = false
            }) != 0)
            {
                Console.Error.WriteLine(Output.Red("An error occured while trying to download the version required, exiting New"));
                return 2;
            }

            string path = VersionManager.GetInstallationPath(project.Runtime.Version, project.Runtime.Tier, project.Runtime.Platform);

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
            // If --list was specified, list installed versions and tell if documentation for that version is available locally
            if (opts.List)
            {
                VersionManager.ListInstalledVersions();
                return 0;
            }

            // if version provided is "latest", it is being replaced with currently most recent one
            if (opts.Latest)
            {
                opts.Version = VersionManager.GetLatestDarkRiftVersion();
            }

            if (opts.Version == null)
            {
                // if version info was omitted, overwrite any parameters with current project settings
                if (Project.IsCurrentDirectoryAProject())
                {
                    var project = Project.Load();

                    opts.Version = project.Runtime.Version;
                    opts.Platform = project.Runtime.Platform;
                    opts.Pro = project.Runtime.Tier == ServerTier.Pro;
                }
                else
                {
                    Console.Error.WriteLine(Output.Red($"Couldn't find a version to install. To download latest version use option --latest"));
                    return 2;
                }
            }

            ServerTier actualTier = opts.Pro ? ServerTier.Pro : ServerTier.Free;

            // If --docs was specified, download documentation instead
            bool success = false;
            if (opts.Docs)
            {
                bool docsInstalled = VersionManager.IsDocumentationInstalled(opts.Version);

                if (docsInstalled && !opts.Force)
                {
                    Console.WriteLine(Output.Green($"Documentation for DarkRift {opts.Version} - {actualTier} (.NET {opts.Platform}) already installed! To force a reinstall use the option -f or --force"));
                    success = true;
                }
                else
                    success = VersionManager.DownloadDocumentation(opts.Version);
            }
            else if (opts.Version != null)
            {
                bool versionInstalled = VersionManager.IsVersionInstalled(opts.Version, actualTier, opts.Platform);
                if (versionInstalled && !opts.Force)
                {
                    Console.WriteLine(Output.Green($"DarkRift {opts.Version} - {actualTier} (.NET {opts.Platform}) already installed! To force a reinstall use the option -f or --force"));
                    success = true;
                }
                else
                    success = VersionManager.DownloadVersion(opts.Version, actualTier, opts.Platform);
            }

            if (!success)
            {
                Console.Error.WriteLine(Output.Red("Invalid command"));
                Console.Error.WriteLine("\t" + Environment.GetCommandLineArgs()[0] + " " + Parser.Default.FormatCommandLine(new PullOptions()));
                return 1;
            }

            return 0;
        }

        private static int Docs(DocsOptions opts)
        {
            // If version provided is "latest", it is being replaced with currently most recent one
            if (opts.Latest)
            {
                opts.Version = VersionManager.GetLatestDarkRiftVersion();
            }

            if (opts.Version == null)
            {
                // If version info was omitted, overwrite any parameters with current project settings
                if (Project.IsCurrentDirectoryAProject())
                {
                    var project = Project.Load();

                    opts.Version = project.Runtime.Version;
                }
                else
                {
                    Console.Error.WriteLine(Output.Red($"Couldn't find a version to download documentation. To download latest version use option --latest"));
                    return 2;
                }
            }

            if (opts.Local)
            {
                if (VersionManager.IsDocumentationInstalled(opts.Version))
                    BrowserUtil.OpenTo("file://" + VersionManager.GetDocumentationPath(opts.Version) + "/index.html");
                else
                    Console.Error.WriteLine(Output.Red($"Documentation not installed, consider running \"darkrift pull --docs --version {opts.Version}\""));
            }
            else if (opts.Version != null)
            {
                BrowserUtil.OpenTo($"https://darkriftnetworking.com/DarkRift2/Docs/{opts.Version}");
            }

            return 0;
        }
    }
}
