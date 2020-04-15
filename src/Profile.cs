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
    [XmlRoot(Namespace="http://schemas.datacontract.org/2004/07/DarkRift.Cli")]
    public class Profile
    {
        /// <summary>
        /// The DarkRift settings directory path.
        /// </summary>
        private static readonly string USER_DR_DIR = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".darkrift");

        /// <summary>
        /// The singleton instance of the profile class.
        /// </summary>
        private static Profile instance;

        /// <summary>
        /// The user's Unity Asset Store invoice number.
        /// </summary>
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// The latest version we know of for DarkRift.
        /// </summary>
        public Version LatestKnownDarkRiftVersion { get; set; }

        /// <summary>
        /// Load's the user's profile from disk.
        /// </summary>
        /// <returns>The user's profile.</returns>
        public static Profile Load()
        {
            if (instance == null)
            {
                try {
                    using (XmlReader reader = XmlReader.Create(Path.Combine(USER_DR_DIR, "profile.xml")))
                    {
                        XmlSerializer ser = new XmlSerializer(typeof(Profile));
                        instance = (Profile)ser.Deserialize(reader);
                    }
                }
                catch (IOException)
                {
                    instance = new Profile();
                }
            }

            return instance;
        }

        /// <summary>
        /// Saves any edits to the user's profile to disk.
        /// </summary>
        public void Save()
        {
            Directory.CreateDirectory(USER_DR_DIR);
            using (XmlWriter writer = XmlWriter.Create(Path.Combine(USER_DR_DIR, "profile.xml"), new XmlWriterSettings { Indent = true }))
            {
                XmlSerializer ser = new XmlSerializer(typeof(Profile));
                ser.Serialize(writer, this);
            }
        }
    }
}
