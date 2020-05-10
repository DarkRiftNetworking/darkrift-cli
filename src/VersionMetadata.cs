using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace DarkRift.Cli
{
    /// <summary>
    /// Holds a user's profile settings.
    /// </summary>
    [DataContract]
    internal class VersionMetadata
    {
        /// <summary>
        /// The latest stable server version available.
        /// </summary>
        public string Latest => latest;

#pragma warning disable CS0649
        /// <summary>
        /// The latest stable server version available.
        /// </summary>
        [DataMember]
        private string latest;
#pragma warning restore

        /// <summary>
        /// Load's the version metadata from the given string.
        /// </summary>
        /// <returns>The version metadata.</returns>
        public static VersionMetadata Parse(string jsonString)
        {
            using (MemoryStream stream = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(VersionMetadata));
                return (VersionMetadata)serializer.ReadObject(stream);
            }
        }
    }
}
