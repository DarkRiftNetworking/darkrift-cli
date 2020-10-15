using System.IO;
using System;
using Crayon;
using System.Collections.Generic;
using System.Linq;
using DarkRift.Cli.Utility;

namespace DarkRift.Cli
{
    /// <summary>
    /// Manages DarkRift installations.
    /// </summary>
    internal class InstallationManager
    {
        /// <summary>
        /// The remote repository to use.
        /// </summary>
        private readonly IRemoteRepository remoteRepository;

        /// <summary>
        /// The file utility to use.
        /// </summary>
        private readonly IFileUtility fileUtility;

        /// <summary>
        /// The latest version of DarkRift.
        /// </summary>
        private string latestDarkRiftVersion;

        /// <summary>
        /// The directory to place DR installations in.
        /// </summary>
        private readonly string installationDirectory;

        /// <summary>
        /// The application's context.
        /// </summary>
        private readonly IContext context;

        /// <summary>
        /// Creates a new installation manager.
        /// </summary>
        /// <param name="remoteRepository">The remote respository to download versions from.</param>
        /// <param name="fileUtility">The file utility to use.</param>
        /// <param name="installationDirectory">The directory to place DR installations in.</param>
        /// <param name="context">The application's context.</param>
        public InstallationManager(IRemoteRepository remoteRepository, IFileUtility fileUtility, string installationDirectory, IContext context)
        {
            this.installationDirectory = installationDirectory;
            this.fileUtility = fileUtility;
            this.remoteRepository = remoteRepository;
            this.context = context;
        }

        /// <summary>
        /// Gets the path to a specified installation, or null if it is not installed.
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <param name="pro">Whether the pro version should be used.</param>
        /// <param name="platform">Whether the .NET Standard build should be used.</param>
        /// <returns>The path to the installation, or null, if it is not installed.</returns>
        public DarkRiftInstallation GetInstallation(string version, ServerTier tier, ServerPlatform platform)
        {
            string path = GetInstallationPath(version, tier, platform);
            if (fileUtility.DirectoryExists(path))
                return new DarkRiftInstallation(version, tier, platform, path);

            return null;
        }

        /// <summary>
        /// Gets a list of versions installed with specific tier and platform
        /// </summary>
        /// <param name="tier">The tier</param>
        /// <param name="platform">The platform</param>
        /// <returns>A list of installed versions</returns>
        public List<DarkRiftInstallation> GetVersions(ServerTier tier, ServerPlatform platform)
        {
            string searchPath = Path.Combine(installationDirectory, tier.ToString().ToLower(), platform.ToString().ToLower());
            if (!fileUtility.DirectoryExists(searchPath))
                return new List<DarkRiftInstallation>();
            else
                return fileUtility.GetDirectories(searchPath)
                                  .Select(path => new DarkRiftInstallation(path, tier, platform, Path.Combine(searchPath, path)))
                                  .ToList();
        }

        /// <summary>
        /// Installs a version of DarkRift, if not alredy installed.
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <param name="pro">Whether the pro version should be used.</param>
        /// <param name="platform">Whether the .NET Standard build should be used.</param>
        /// <returns>The path to the installation, or null, if it is not available</returns>
        public DarkRiftInstallation Install(string version, ServerTier tier, ServerPlatform platform, bool forceRedownload)
        {
            string path = GetInstallationPath(version, tier, platform);
            if (forceRedownload || !fileUtility.DirectoryExists(path))
            {
                if (!remoteRepository.DownloadVersionTo(version, tier, platform, path))
                    return null;
            }

            return new DarkRiftInstallation(version, tier, platform, path);
        }

        /// <summary>
        /// Queries or loads the latest version of DarkRift
        /// </summary>
        /// <returns>The latest version of DarkRift</returns>
        public string GetLatestDarkRiftVersion()
        {
            if (latestDarkRiftVersion != null)
                return latestDarkRiftVersion;

            latestDarkRiftVersion = remoteRepository.GetLatestDarkRiftVersion();
            if (latestDarkRiftVersion != null)
                return latestDarkRiftVersion;

            latestDarkRiftVersion = context.Profile.LatestKnownDarkRiftVersion;

            if (latestDarkRiftVersion != null)
            {
                Console.WriteLine($"Last known latest version is {latestDarkRiftVersion}.");
                return latestDarkRiftVersion;
            }

            Console.Error.WriteLine(Output.Red($"No latest DarkRift version stored locally!"));
            return null;
        }

        /// <summary>
        /// Gets the path to a specified installation, or null if it is not installed.
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <param name="pro">Whether the pro version should be used.</param>
        /// <param name="platform">Whether the .NET Standard build should be used.</param>
        /// <returns>The path to the installation, or null, if it is not installed.</returns>
        private string GetInstallationPath(string version, ServerTier tier, ServerPlatform platform)
        {
            return Path.Combine(installationDirectory, tier.ToString().ToLower(), platform.ToString().ToLower(), version);
        }
    }
}
