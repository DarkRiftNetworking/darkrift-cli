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
        /// The singleton instance of the profile class.
        /// </summary>
        private static Project instance;

        /// <summary>
        ///     The runtime settings.
        /// </summary>
        public Runtime Runtime { get; set; }

        /// <summary>
        /// Load's the project from disk.
        /// </summary>
        /// <returns>The project.</returns>
        public static Project Load()
        {
            if (instance == null)
            {
                try {
                    using (XmlReader reader = XmlReader.Create("Project.xml"))
                    {
                        XmlSerializer ser = new XmlSerializer(typeof(Project));
                        instance = (Project)ser.Deserialize(reader);
                    }
                }
                catch (IOException)
                {
                    instance = new Project();
                }
            }

            return instance;
        }

        /// <summary>
        /// Saves any edits to the project to disk.
        /// </summary>
        public void Save()
        {
            using (XmlWriter writer = XmlWriter.Create("Project.xml", new XmlWriterSettings { Indent = true }))
            {
                XmlSerializer ser = new XmlSerializer(typeof(Project));
                ser.Serialize(writer, this);
            }
        }

        /// <summary>
        /// Returns if the current directory is the directory where project is located
        /// by checking existence of Project.xml file.
        /// </summary>
        /// <returns>Returns if the current directory is a project directory</returns>
        public static bool IsCurrentDirectoryAProject()
        {
            return File.Exists("Project.xml");
        }
    }
}
