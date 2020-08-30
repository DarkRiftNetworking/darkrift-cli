using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DarkRift.Cli
{
    /// <summary>
    /// An installation of a DarkRift build.
    /// </summary>
    internal class DarkRiftInstallation
    {
        /// <summary>
        /// The version of the installation.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// The tier of the installation.
        /// </summary>
        public ServerTier Tier { get; }

        /// <summary>
        /// The platform of the installation.
        /// </summary>
        public ServerPlatform Platform { get; }

        /// <summary>
        /// The path to the installation.
        /// </summary>
        public string InstallationPath { get; }

        public DarkRiftInstallation(string version, ServerTier tier, ServerPlatform platform, string installationPath)
        {
            Version = version;
            Tier = tier;
            Platform = platform;
            InstallationPath = installationPath;
        }

        /// <summary>
        /// Runs an instance of this DarkRift installation and waits for exit.
        /// </summary>
        /// <param name="args">The args to pass to the server.</param>
        /// <returns>The exit code of the server.</returns>
        public int Run(IEnumerable<string> args)
        {
            // Calculate the executable file to run
            string fullPath;
            if (Platform == ServerPlatform.Framework)
            {
                fullPath = Path.Combine(InstallationPath , "DarkRift.Server.Console.exe");
            }
            else
            {
                fullPath = "dotnet";
                args = args.Prepend(Path.Combine(InstallationPath , "Lib", "DarkRift.Server.Console.dll"));
            }

            using Process process = new Process
            {
                StartInfo = new ProcessStartInfo(fullPath, string.Join(" ", args))
            };
            process.Start();

            process.WaitForExit();

            return process.ExitCode;
        }
    }
}
