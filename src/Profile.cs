using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace DarkRift.Cli
{
    /// <summary>
    /// Holds a user's profile settings.
    /// </summary>
    // Namespace here is because we used to use DataContractSerializer
    [XmlRoot(Namespace = "http://schemas.datacontract.org/2004/07/DarkRift.Cli")]
    public class Profile
    {
        /// <summary>
        /// The user's Unity Asset Store invoice number.
        /// </summary>
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// The latest version we know of for DarkRift.
        /// </summary>
        public string LatestKnownDarkRiftVersion { get; set; }

        /// <summary>
        /// Load's the user's profile from disk.
        /// </summary>
        /// <param>The file to load.</param>
        /// <returns>The user's profile.</returns>
        internal static Profile Load(string path)
        {
            using XmlReader reader = XmlReader.Create(path);
            XmlSerializer ser = new XmlSerializer(typeof(Profile));
            return (Profile)ser.Deserialize(reader);
        }

        /// <summary>
        /// Saves any edits to the user's profile to disk.
        /// </summary>
        /// <param>The file to save to.</param>
        internal void Save(string path)
        {
            using XmlWriter writer = XmlWriter.Create(path, new XmlWriterSettings { Indent = true });
            XmlSerializer ser = new XmlSerializer(typeof(Profile));
            ser.Serialize(writer, this);
        }
    }
}
