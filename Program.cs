using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using CommandLine;
using Crayon;
using DarkRift.Server;

namespace darkrift_cli
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

            [Value(0, HelpText = "The name of the template to unpack, currently only 'project'.", Required = true)]
            public string Type { get; set; }

            [Value(1, HelpText = "The directory to unpack the template in.")]
            public string TargetDirectory { get; set; }
        }

        [Verb("run", HelpText = "Run a DarkRift project.")]
        class RunOptions
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
            return CommandLine.Parser.Default.ParseArguments<NewOptions, RunOptions, GetOptions>(args)
                .MapResult(
                    (NewOptions opts) => New(opts),
                    (RunOptions opts) => Run(opts),
                    (GetOptions opts) => Get(opts),
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

            Console.WriteLine($"Creating new project '{Path.GetFileName(targetDirectory)}'...");

            ZipFile.ExtractToDirectory(templatePath, targetDirectory, true);

            Console.WriteLine(Output.Green($"Created new project '{Path.GetFileName(targetDirectory)}'"));

            return 0;
        }

        private static int Run(RunOptions opts)
        {
            DarkRiftServer server;

            //TODO find a way to integrate with commandline rather than reparsing arguments with DR
            string[] rawArguments = CommandEngine.ParseArguments(string.Join(" ", Environment.GetCommandLineArgs().Skip(2).ToArray()));
            string[] arguments = CommandEngine.GetArguments(rawArguments);
            NameValueCollection variables = CommandEngine.GetFlags(rawArguments);

            string configFile;
            if (arguments.Length == 0)
            {
                // TODO Some people might prefer to use .config still, allow?
                configFile = "Server.xml";
            }
            else if (arguments.Length == 1)
            {
                configFile = arguments[0];
            }
            else
            {
                System.Console.Error.WriteLine("Invalid comand line arguments.");
                System.Console.WriteLine("Press any key to exit...");
                System.Console.ReadKey();
                return 1;
            }

            ServerSpawnData spawnData;

            try
            {
                spawnData = ServerSpawnData.CreateFromXml(configFile, variables);
            }
            catch (IOException e)
            {
                System.Console.Error.WriteLine("Could not load the config file needed to start (" + e.Message + "). Are you sure it's present and accessible?");
                System.Console.WriteLine("Press any key to exit...");
                System.Console.ReadKey();
                return 1;
            }
            catch (XmlConfigurationException e)
            {
                System.Console.Error.WriteLine(e.Message);
                System.Console.WriteLine("Press any key to exit...");
                System.Console.ReadKey();
                return 1;
            }
            catch (KeyNotFoundException e)
            {
                System.Console.Error.WriteLine(e.Message);
                System.Console.WriteLine("Press any key to exit...");
                System.Console.ReadKey();
                return 1;
            }

            // Set this thread as the one executing dispatcher tasks
            spawnData.DispatcherExecutorThreadID = Thread.CurrentThread.ManagedThreadId;

            server = new DarkRiftServer(spawnData);

            server.Start();

            new Thread(new ThreadStart(() =>
            {
                while (!server.Disposed)
                {
                    string input = System.Console.ReadLine();

                    server.ExecuteCommand(input);
                }
            })).Start();

            while (!server.Disposed)
            {
                server.DispatcherWaitHandle.WaitOne();
                server.ExecuteDispatcherTasks();
            }

            return 0;
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
