using System.IO;
using System;
using System.Net;
using System.IO.Compression;
using Crayon;
using System.Collections.Generic;
using System.Linq;
using DarkRift.Cli.Utility;

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
        private readonly IRemoteRepository remoteRepository;

        /// <summary>
        /// The file utility to use.
        /// </summary>
        private readonly IFileUtility fileUtility;

        /// <summary>
        /// The directory to place DR installations in.
        /// </summary>
        private readonly string installationDirectory;

        /// <summary>
        /// Creates a new documentation manager.
        /// </summary>
        /// <param name="remoteRepository">The remote respository to download versions from.</param>
        /// <param name="fileUtility">The file utility to use.</param>
        /// <param name="installationDirectory">The directory to place DR documentation in.</param>
        public DocumentationManager(IRemoteRepository remoteRepository, IFileUtility fileUtility, string installationDirectory)
        {
            this.installationDirectory = installationDirectory;
            this.remoteRepository = remoteRepository;
            this.fileUtility = fileUtility;
        }

        /// <summary>
        /// Gets the path to a specified installation, or null if it is not installed.
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <returns>The path to the installation, or null, if it is not installed.</returns>
        public DocumentationInstallation GetInstallation(string version)
        {
            string path = GetInstallationPath(version);
            if (fileUtility.DirectoryExists(path))
                return new DocumentationInstallation(version, path);

            return null;
        }

        /// <summary>
        /// Gets a list of versions installled
        /// </summary>
        /// <returns>A list of installed versions</returns>
        public List<DocumentationInstallation> GetVersions()
        {

            if (!fileUtility.DirectoryExists(installationDirectory))
                return new List<DocumentationInstallation>();
            else
                return fileUtility.GetDirectories(installationDirectory)
                                  .Select(path => new DocumentationInstallation(path, Path.Combine(installationDirectory, path)))
                                  .ToList();
        }

        /// <summary>
        /// Installs a version of DarkRift, if not alredy installed.
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <returns>The installation, or null, if it is not available</returns>
        public DocumentationInstallation Install(string version, bool forceRedownload)
        {
            string path = GetInstallationPath(version);
            if (forceRedownload || !fileUtility.DirectoryExists(path))
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
