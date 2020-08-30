using System.IO;
using System;
using System.Net;
using System.IO.Compression;
using Crayon;
using System.Collections.Generic;
using System.Linq;

namespace DarkRift.Cli
{
    /// <summary>
    /// Manages DarkRift documentation installations.
    /// </summary>
    internal class DocumentationManager
    {
        /// <summary>
        /// The remote repository to use.
        /// </summary>
        private readonly RemoteRepository remoteRepository;

        /// <summary>
        /// The directory to place DR installations in.
        /// </summary>
        private readonly string installationDirectory;

        /// <summary>
        /// Creates a new documentation manager.
        /// </summary>
        /// <param name="remoteRepository">The remote respository to download versions from.</param>
        /// <param name="installationDirectory">The directory to place DR documentation in.</param>
        public DocumentationManager(RemoteRepository remoteRepository, string installationDirectory)
        {
            this.installationDirectory = installationDirectory;
            this.remoteRepository = remoteRepository;
        }

        /// <summary>
        /// Gets the path to a specified installation, or null if it is not installed.
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <returns>The path to the installation, or null, if it is not installed.</returns>
        public DocumentationInstallation GetInstallation(string version)
        {
            string path = GetInstallationPath(version);
            if (Directory.Exists(path))
                return new DocumentationInstallation(version, path);

            return null;
        }

        /// <summary>
        /// Gets a list of versions installled
        /// </summary>
        /// <returns>A list of installed versions</returns>
        public List<DocumentationInstallation> GetVersions()
        {
            try
            {
                return Directory.GetDirectories(Path.Combine(installationDirectory))
                                .Select(path => new DocumentationInstallation(Path.GetFileName(path), path))
                                .ToList();
            }
            catch (IOException)
            {
                return new List<DocumentationInstallation>();
            };
        }

        /// <summary>
        /// Installs a version of DarkRift, if not alredy installed.
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <returns>The installation, or null, if it is not available</returns>
        public DocumentationInstallation Install(string version, bool forceRedownload)
        {
            string path = GetInstallationPath(version);
            if (forceRedownload || !Directory.Exists(path))
            {
                if (!remoteRepository.DownloadDocumentationTo(version, path))
                    return null;
            }

            return new DocumentationInstallation(version, path);
        }

        /// <summary>
        /// Gets the path to a specified installation, or null if it is not installed.
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <returns>The path to the installation, or null, if it is not installed.</returns>
        private string GetInstallationPath(string version)
        {
            return Path.Combine(installationDirectory, version);
        }
    }
}
