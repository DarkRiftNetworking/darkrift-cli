using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace DarkRift.Cli
{
    /// <summary>
    /// Holds a project's settings.
    /// </summary>
    [DataContract(Name = "Project")]
    class Project
    {
        /// <summary>
        /// The singleton instance of the profile class.
        /// </summary>
        static Project instance;

        /// <summary>
        ///     The runtime settings.
        /// </summary>
        [DataMember]
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
                        DataContractSerializer ser = new DataContractSerializer(typeof(Project));
                        instance = (Project)ser.ReadObject(reader, true);
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
                DataContractSerializer ser = new DataContractSerializer(typeof(Project));
                ser.WriteObject(writer, this);
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