using System;
using System.Runtime.Serialization;

namespace DarkRift.Cli
{
    /// <summary>
    /// Holds a project's runtime settings.
    /// </summary>
    [DataContract(Name = "Runtime")]
    class Runtime
    {
        /// <summary>
        /// The version of DarkRift to use.
        /// </summary>
        [DataMember()]
        public String Version { get; set; }
    
        /// <summary>
        /// The tier of DarkRift to use.
        /// </summary>
        [DataMember()]
        public ServerTier Tier { get; set; }
        
        /// <summary>
        /// If .NET standard or .NET framework should be used.
        /// </summary>
        [DataMember()]
        public ServerPlatform Platform { get; set; }

        /// <summary>
        /// Creates a new Runtime configuration element.
        /// </summary>
        /// <param name="version">The version of DarkRift to use.</param>
        /// <param name="tier">The tier of DarkRift to use.</param>
        /// <param name="platform">If .NET standard or .NET framework should be used.</param>
        public Runtime(string version, ServerTier tier, ServerPlatform platform)
        {
            Version = version;
            Tier = tier;
            Platform = platform;
        }
    }
}