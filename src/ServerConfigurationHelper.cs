using System.Xml;
using System.Reflection.Metadata;
using System.Xml.Linq;
using System;
using System.IO;
using System.Linq;
using Crayon;
using System.Collections.Generic;

namespace DarkRift.Cli
{
    /// <summary>
    /// Helper for editing the DarkRift server configuration file.
    /// </summary>
    internal class ServerConfigurationHelper
    {
        /// <summary>
        /// The path to the configuration file.
        /// </summary>
        private readonly string configurationPath;

        /// <summary>
        /// The path to the backup of the configuration file.
        /// </summary>
        private readonly string backupConfigurationPath;

        /// <summary>
        /// Creates a new helper for the given configuration file.
        /// </summary>
        /// <param name="configurationPath">The path to the configuration file.</param>
        /// <param name="backupConfigurationPath">The path to the backup of the configuration file.</param>
        public ServerConfigurationHelper(string configurationPath, string backupConfigurationPath)
        {
            this.configurationPath = configurationPath;
            this.backupConfigurationPath = backupConfigurationPath;
        }

        /// <summary>
        /// Adds a plugin search paht to th configuration, if it does not already exist.
        /// </summary>
        /// <param name="path">The search path to add.</param>
        internal void AddPluginSearchPathIfNotPresent(string path)
        {
            if (!File.Exists(configurationPath)) {
                Console.WriteLine(Output.Yellow($"Unable to enable package management in the server configuration file as it is not present at '{configurationPath}'. You must add the following plugin search path to your configuration file for plugin management to work:\n\t<pluginSearchPath src=\"{path}\" dependencyResolutionStrategy=\"RecursiveFromDirectory\" />"));
            }

            XDocument document = XDocument.Load(configurationPath, LoadOptions.PreserveWhitespace);

            IEnumerable<XElement> elements = document.Root.Element("pluginSearch").Elements("pluginSearchPath");
            if (elements.Any(e => e.Attribute("src").Value == path && e.Attribute("dependencyResolutionStrategy").Value == "RecursiveFromDirectory"))
                return;

            Backup();

            XComment comment = new XComment("The following was added automatically by the DarkRift CLI tool to enable package management.");

            XElement element = new XElement("pluginSearchPath");
            element.SetAttributeValue("src", path);
            element.SetAttributeValue("dependencyResolutionStrategy", "RecursiveFromDirectory");

            // Indenting like this *works* but doesn't feel great. It won't look good if a user changes to a different indent size etc.
            document.Root.Element("pluginSearch").Add(new XText("\n    "), comment, new XText("\n    "), element, new XText("\n  "));

            document.Save(configurationPath, SaveOptions.DisableFormatting);
        }

        /// <summary>
        /// Backs up the configuration file before modification.
        /// </summary>
        private void Backup()
        {
            File.Copy(configurationPath, backupConfigurationPath, true);
        }
    }
}
