using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using CommandLine;
using Crayon;

namespace darkrift_cli
{
    class Program
    {
        /// <summary>
        /// The location of the project template archive.
        /// <summary>
        private static readonly string PROJECT_TEMPLATE_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "template-project.zip");

        [Verb("new", HelpText = "Create a new DarkRift project.")]
        class NewOptions
        {
            [Option('f', Default = false, HelpText = "Force creation overwriting any files that already exist in the directory.")]
            public bool Force { get; set; }

            [Value(0, HelpText = "The directory to create the project in.")]
            public string TargetDirectory { get; set; }
        }

        [Verb("run", HelpText = "Run a DarkRift project.")]
        class RunOptions
        {

        }

        [Verb("exec", HelpText = "Runs a command on a remote DarkRift server.")]
        class ExecOptions
        {

        }

        [Verb("get", HelpText = "Downloads a plugin package into this server.")]
        class GetOptions
        {
            [Value(1)]
            public string Url { get; set; }
        }

        public static int Main(string[] args)
        {
            return CommandLine.Parser.Default.ParseArguments<NewOptions, RunOptions, ExecOptions, GetOptions>(args)
                .MapResult(
                    (NewOptions opts) => New(opts),
                    (RunOptions opts) => Run(opts),
                    (ExecOptions opts) => Exec(opts),
                    (GetOptions opts) => Get(opts),
                    _ => 1);

            
        }

        private static int New(NewOptions opts)
        {
            string targetDirectory = opts.TargetDirectory ?? Environment.CurrentDirectory;

            Directory.CreateDirectory(targetDirectory);

            if (Directory.GetFiles(targetDirectory).Length > 0 && !opts.Force)
            {
                Console.Error.WriteLine(Output.Red("Cannot create new project, the directory is not empty. Use -f to force creation."));
                Console.Error.WriteLine("\t" + Environment.GetCommandLineArgs()[0] + " " + CommandLine.Parser.Default.FormatCommandLine(new NewOptions { TargetDirectory = opts.TargetDirectory, Force = true }));
                return 1;
            }

            Console.WriteLine($"Creating new project '${Path.GetFileName(targetDirectory)}'...");

            ZipFile.ExtractToDirectory(PROJECT_TEMPLATE_PATH, targetDirectory, true);

            Console.WriteLine(Output.Green($"Created new project '{Path.GetFileName(targetDirectory)}'"));

            return 0;
        }

        private static int Run(RunOptions opts)
        {
            throw new NotImplementedException();
        }

        private static int Exec(ExecOptions opts)
        {
            throw new NotImplementedException();
        }

        private static int Get(GetOptions opts)
        {
            if (!Uri.TryCreate(opts.Url, UriKind.RelativeOrAbsolute, out Uri uri))
            {
                Console.Error.WriteLine(Output.Red("Invalid URL passed."));
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

            Console.WriteLine(Output.Green($"Downloaded package into plugins directory."));

            return 0;
        }
    }
}
