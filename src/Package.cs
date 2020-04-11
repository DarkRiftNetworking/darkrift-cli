using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace DarkRift.Cli
{
    /// <summary>
    /// Holds a package's settings.
    /// </summary>
    // Namespace here is because we used to use DataContractSerializer
    [XmlRoot(Namespace="http://schemas.datacontract.org/2004/07/DarkRift.Cli")]
    public class Package
    {
        // TODO add more fields

        /// <summary>
        /// The name of the package.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The URL of the package.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The email of the author of the package.
        /// </summary>
        public string AuthorEmail { get; set; }

        /// <summary>
        /// The author of the package.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// The description of the package.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The group of the package.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        ///     The runtime settings.
        /// </summary>
        [XmlArray]
        public List<PackageDependency> PackageDependencies { get; set; }

        /// <summary>
        /// Load's the package settings from disk.
        /// </summary>
        /// <param name="dirPath">The path to load the Package.xml file from.</param>
        /// <returns>The package.</returns>
        public static Package Load(string dirPath)
        {
            try {
                using (XmlReader reader = XmlReader.Create(Path.Combine(dirPath, "Package.xml")))
                {
                    XmlSerializer ser = new XmlSerializer(typeof(Package));
                    return (Package)ser.Deserialize(reader);
                }
            }
            catch (IOException)
            {
                return null;
            }
        }

        /// <summary>
        /// Saves any edits to the project to disk.
        /// </summary>
        /// <param name="dirPath">The path to save the Package.xml file in.</param>
        public void Save(string dirPath)
        {
            using (XmlWriter writer = XmlWriter.Create(Path.Combine(dirPath, "Package.xml"), new XmlWriterSettings { Indent = true }))
            {
                XmlSerializer ser = new XmlSerializer(typeof(Package));
                ser.Serialize(writer, this);
            }
        }

        /// <summary>
        /// Returns if the current directory is the directory where a package is located
        /// by checking existence of Package.xml file.
        /// </summary>
        /// <param name="path">The path to check the Package.xml file is in.</param>
        /// <returns>Returns if the current directory is a package directory</returns>
        public static bool IsDirectoryAPackage(string path)
        {
            return File.Exists(Path.Combine(dirPath, "Package.xml"));
        }
    }
}
