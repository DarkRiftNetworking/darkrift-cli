using System;
using System.IO;
using System.IO.Compression;
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

        [Verb("add", HelpText = "Runs a command on a remote DarkRift server.")]
        class ExecOptions
        {

        }

        public static int Main(string[] args)
        {
            return CommandLine.Parser.Default.ParseArguments<NewOptions, RunOptions, ExecOptions>(args)
                .MapResult(
                    (NewOptions opts) => New(opts),
                    (RunOptions opts) => Run(opts),
                    (ExecOptions opts) => Exec(opts),
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

            Console.WriteLine($"Creating new project '${Path.GetFileName(targetDirectory)}'");

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
    }
}
