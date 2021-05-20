using System.Runtime.CompilerServices;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using CommandLine;
using Crayon;
using System.Collections.Generic;
using DarkRift.Cli.Templating;
using DarkRift.Cli.Utility;
using DarkRift.Cli.NuGet;

[assembly: InternalsVisibleTo("darkrift-cli-test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace DarkRift.Cli
{
    internal class Program
    {
        /// <summary>
        /// The location of the template archives.
        /// <summary>
        private static readonly string TEMPLATES_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates");

        /// <summary>
        /// The DarkRift settings directory path.
        /// </summary>
        private static readonly string USER_DR_DIRECTORY = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".darkrift");

        /// <summary>
        /// The DarkRift project directory path.
        /// </summary>
        private static readonly string PROJECT_DR_DIRECTORY = Path.Combine(Directory.GetCurrentDirectory(), ".darkrift", ".cli");

        /// <summary>
        /// The URL of the DarkRift website.
        /// </summary>
        private static readonly string BASE_URL = "https://www.darkriftnetworking.com";

        /// <summary>
        /// The installation manager to use.
        /// </summary>
        private readonly InstallationManager installationManager;

        /// <summary>
        /// The documentation manager to use.
        /// </summary>
        private readonly DocumentationManager documentationManager;

        /// <summary>
        /// The templater to use.
        /// </summary>
        private readonly Templater templater;

        /// <summary>
        /// Helper for NuGet operations.
        /// </summary>
        private readonly NuGetHelper nuGetHelper;

        /// <summary>
        /// Helper for editing the server configuration file.
        /// </summary>
        private readonly ServerConfigurationHelper serverConfigurationHelper;

        /// <summary>
        /// The application's context.
        /// </summary>
        private readonly Context context;

        public Program(InstallationManager installationManager, DocumentationManager documentationManager, Templater templater, NuGetHelper nuGetHelper,
            ServerConfigurationHelper serverConfigurationHelper, Context context)
        {
            this.installationManager = installationManager;
            this.documentationManager = documentationManager;
            this.templater = templater;
            this.nuGetHelper = nuGetHelper;
            this.serverConfigurationHelper = serverConfigurationHelper;
            this.context = context;
        }

        public static int Main(string[] args)
        {
            Context context = Context.Load(Path.Combine(USER_DR_DIRECTORY, "profile.xml"), "Project.xml");

            using WebClient webClient = new WebClient
            {
                BaseAddress = BASE_URL
            };
            RemoteRepository remoteRepository = new RemoteRepository(new InvoiceManager(context), context, new WebClientUtility(webClient), new FileUtility());

            InstallationManager installationManager = new InstallationManager(remoteRepository, new FileUtility(), Path.Combine(USER_DR_DIRECTORY, "installed"), context);
            DocumentationManager documentationManager = new DocumentationManager(remoteRepository, new FileUtility(), Path.Combine(USER_DR_DIRECTORY, "documetation"));

            Templater templater = new Templater(TEMPLATES_PATH);

            NuGetHelper nuGetHelper = new NuGetHelper(Path.Combine(USER_DR_DIRECTORY, "packages"), Path.Combine(PROJECT_DR_DIRECTORY, "packages"), context);
            ServerConfigurationHelper serverConfigurationHelper = new ServerConfigurationHelper(
                Path.Combine(Directory.GetCurrentDirectory(), "Server.config"),
                Path.Combine(PROJECT_DR_DIRECTORY, "Server.config.backup")
            );

            Program program = new Program(installationManager, documentationManager, templater, nuGetHelper, serverConfigurationHelper, context);

            return new Parser(SetupParser).ParseArguments<NewOptions, RunOptions, GetOptions, PullOptions, DocsOptions, AddOptions>(args)
                .MapResult(
                    (NewOptions opts) => program.New(opts),
                    (RunOptions opts) => program.Run(opts),
                    (GetOptions opts) => program.Get(opts),
                    (PullOptions opts) => program.Pull(opts),
                    (DocsOptions opts) => program.Docs(opts),
                    (AddOptions opts) => program.Add(opts),
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

        private int New(NewOptions opts)
        {
            string version = opts.Version ?? installationManager.GetLatestDarkRiftVersion();
            ServerTier tier = opts.Pro ? ServerTier.Pro : ServerTier.Free;
            string targetDirectory = opts.TargetDirectory ?? Environment.CurrentDirectory;

            installationManager.Install(version, tier, opts.Platform, false);

            try
            {
                templater.Template(opts.Type, targetDirectory, version, tier, opts.Platform, opts.Force);
            }
            catch (DirectoryNotEmptyException)
            {
                Console.Error.WriteLine(Output.Red("Cannot create from template, the directory is not empty. Use -f to force creation."));
                Console.Error.WriteLine("\t" + Environment.GetCommandLineArgs()[0] + " " + Parser.Default.FormatCommandLine(new NewOptions { Type = opts.Type, TargetDirectory = opts.TargetDirectory, Force = true }));
                return 1;
            }
            catch (ArgumentException e)
            {
                Console.Error.WriteLine(Output.Red(e.Message));
                return 1;
            }

            return 0;
        }

        private int Run(RunOptions opts)
        {
            // If we're not in a project then try our best to guess settings
            if (context.Project?.Runtime == null)
            {
                Console.Error.WriteLine(Output.Red("Unable to get desired runtime from project file, are you sure this is a DarkRift project?"));
                return 1;
            }

            DarkRiftInstallation installation = installationManager.Install(context.Project.Runtime.Version, context.Project.Runtime.Tier, context.Project.Runtime.Platform, false);
            if (installation == null)
            {
                Console.Error.WriteLine(Output.Red("Unable to find correct version of DarkRift locally and unable to download it."));
                return 1;
            }

            return installation.Run(opts.Values);
        }

        private int Get(GetOptions opts)
        {
            if (!Uri.TryCreate(opts.Url, UriKind.RelativeOrAbsolute, out Uri uri))
            {
                Console.Error.WriteLine(Output.Red("Invalid URL passed."));
                Console.Error.WriteLine("\t" + Environment.GetCommandLineArgs()[0] + " " + CommandLine.Parser.Default.FormatCommandLine(new GetOptions { Url = "https://your-download-url/file.zip" }));
                return 1;
            }

            string stagingDirectory = Path.Combine(PROJECT_DR_DIRECTORY, "temp");
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

        private int Pull(PullOptions opts)
        {
            // If --list was specified, list installed versions and tell if documentation for that version is available locally
            if (opts.List)
            {
                PrintInstalledVersions();
                return 0;
            }

            string version;
            ServerTier tier;
            ServerPlatform platform;
            if (opts.Version == null)
            {
                // If version info was omitted, set parameters to current project settings
                if (context.Project?.Runtime != null)
                {
                    version = context.Project.Runtime.Version;
                    platform = context.Project.Runtime.Platform;
                    tier = context.Project.Runtime.Tier;
                }
                else
                {
                    Console.Error.WriteLine(Output.Red($"Couldn't find a version to install. To download latest version use 'latest'"));
                    return 1;
                }
            }
            else
            {
                version = opts.Version == "latest" ? installationManager.GetLatestDarkRiftVersion() : opts.Version;
                tier = opts.Pro ? ServerTier.Pro : ServerTier.Free;
                platform = opts.Platform;
            }

            // If --docs was specified, download documentation instead
            if (opts.Docs)
            {
                bool docsInstalled = documentationManager.GetInstallation(version) != null;
                if (docsInstalled && !opts.Force)
                {
                    Console.WriteLine(Output.Green($"Documentation for DarkRift {version} - {tier} (.NET {platform}) already installed! To force a reinstall use the option -f or --force"));
                }
                else
                {
                    if (documentationManager.Install(version, opts.Force) == null)
                    {
                        Console.Error.WriteLine(Output.Red($"Could not install the requested documentation."));
                        return 1;
                    }
                }
            }
            else
            {
                bool versionInstalled = installationManager.GetInstallation(version, tier, platform) != null;
                if (versionInstalled && !opts.Force)
                {
                    Console.WriteLine(Output.Green($"DarkRift {version} - {tier} (.NET {platform}) already installed! To force a reinstall use the option -f or --force"));
                }
                else
                {
                    if (installationManager.Install(version, tier, platform, opts.Force) == null)
                    {
                        Console.Error.WriteLine(Output.Red($"Could not install the requested version."));
                        return 1;
                    }
                }
            }

            return 0;
        }

        private int Docs(DocsOptions opts)
        {
            // If version provided is "latest", it is being replaced with currently most recent one
            if (opts.Version == "latest")
            {
                opts.Version = installationManager.GetLatestDarkRiftVersion();
            }

            if (opts.Version == null)
            {
                // If version info was omitted, overwrite any parameters with current project settings
                if (context.Project?.Runtime != null)
                {
                    opts.Version = context.Project.Runtime.Version;
                }
                else
                {
                    Console.Error.WriteLine(Output.Red($"Couldn't find a version to download documentation. To download latest version 'latest'"));
                    return 1;
                }
            }

            if (opts.Local)
            {
                DocumentationInstallation installation = documentationManager.GetInstallation(opts.Version);
                if (installation != null)
                    installation.Open();
                else
                    Console.Error.WriteLine(Output.Red($"Documentation not installed, consider running \"darkrift pull {opts.Version} --docs\""));
            }
            else if (opts.Version != null)
            {
                BrowserUtil.OpenTo($"https://darkriftnetworking.com/DarkRift2/Docs/{opts.Version}");
            }

            return 0;
        }

        private int Add(AddOptions opts)
        {
            nuGetHelper.AddPackage(opts.Package, opts.Version, opts.Prerelease);

            serverConfigurationHelper.AddPluginSearchPathIfNotPresent(Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.Combine(PROJECT_DR_DIRECTORY, "packages")));

            return 0;
        }

        /// <summary>
        /// Lists installed DarkRift versions on the console along with the documentation
        /// </summary>
        public void PrintInstalledVersions()
        {
            // Since the free version only supports .Net Framework I'm not adding support here
            List<DarkRiftInstallation> freeInstallations = installationManager.GetVersions(ServerTier.Free, ServerPlatform.Framework);

            List<DarkRiftInstallation> proFrameworkInstallations = installationManager.GetVersions(ServerTier.Pro, ServerPlatform.Framework);
            List<DarkRiftInstallation> proCoreInstallations = installationManager.GetVersions(ServerTier.Pro, ServerPlatform.Core);

            if (freeInstallations.Count == 0 && proFrameworkInstallations.Count == 0 && proCoreInstallations.Count == 0)
            {
                Console.Error.WriteLine(Output.Red($"You don't have any versions of DarkRift installed"));
                return;
            }

            foreach (DarkRiftInstallation installation in freeInstallations)
                PrintVersion(installation, documentationManager.GetInstallation(installation.Version) != null);
            foreach (DarkRiftInstallation installation in proFrameworkInstallations)
                PrintVersion(installation, documentationManager.GetInstallation(installation.Version) != null);
            foreach (DarkRiftInstallation installation in proCoreInstallations)
                PrintVersion(installation, documentationManager.GetInstallation(installation.Version) != null);
        }

        /// <summary>
        /// Prints version information on the console
        /// </summary>
        /// <param name="installation">The installation to be printed.</param>
        /// <param name="isDocumentationInstalled">Whether the respective documentation for this version is installed.</param>
        private void PrintVersion(DarkRiftInstallation installation, bool isDocumentationInstalled)
        {
            string output = "";

            // There's no free or pro in documentation

            output += $"DarkRift {installation.Version} - {installation.Tier} (.NET {installation.Platform})";

            if (isDocumentationInstalled)
                output += " and its documentation are";
            else
                output += " is";

            output += " installed";

            Console.WriteLine(output);
        }
    }
}
