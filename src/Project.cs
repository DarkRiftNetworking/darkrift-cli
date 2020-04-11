using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace DarkRift.Cli
{
    /// <summary>
    /// Holds a project's settings.
    /// </summary>
    // Namespace here is because we used to use DataContractSerializer
    [XmlRoot(Namespace="http://schemas.datacontract.org/2004/07/DarkRift.Cli")]
    public class Project
    {
        /// <summary>
        ///     The runtime settings.
        /// </summary>
        public Runtime Runtime { get; set; }

        /// <summary>
        /// The path project packages should be stored in.
        /// </summary>
        public string LocalPackageDirectory { get; set; } = Path.Combine(".", "packages");

        /// <summary>
        /// Load's the project from disk.
        /// </summary>
        /// <param>The file to load.</param>
        /// <returns>The project.</returns>
        public static Project Load(string path)
        {
            using XmlReader reader = XmlReader.Create(path);
            XmlSerializer ser = new XmlSerializer(typeof(Project));
            return (Project)ser.Deserialize(reader);
        }

        /// <summary>
        /// Saves any edits to the project to disk.
        /// </summary>
        /// <param>The file to save to.</param>
        public void Save(string path)
        {
            using XmlWriter writer = XmlWriter.Create(path, new XmlWriterSettings { Indent = true });
            XmlSerializer ser = new XmlSerializer(typeof(Project));
            ser.Serialize(writer, this);
        }
    }
}
