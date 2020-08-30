namespace DarkRift.Cli
{
    /// <summary>
    /// An installation of a DarkRift build's documentation.
    /// </summary>
    internal class DocumentationInstallation
    {
        /// <summary>
        /// The version of the installation.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// The path to the installation.
        /// </summary>
        public string InstallationPath { get; }

        public DocumentationInstallation(string version, string installationPath)
        {
            Version = version;
            InstallationPath = installationPath;
        }

        /// <summary>
        /// Opens a browser to this documentation.
        /// </summary>
        public void Open()
        {
            BrowserUtil.OpenTo("file://" + InstallationPath + "/index.html");
        }
    }
}
