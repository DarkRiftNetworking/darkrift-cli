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
        public bool Pro { get; set; }       //TODO make enum
        
        /// <summary>
        /// If .NET standard or .NET framework should be used.
        /// </summary>
        [DataMember()]
        public bool Standard { get; set; }       //TODO make enum

        /// <summary>
        /// Creates a new Runtime configuration element.
        /// </summary>
        /// <param name="version">The version of DarkRift to use.</param>
        /// <param name="pro">The tier of DarkRift to use.</param>
        /// <param name="standard">If .NET standard or .NET framework should be used.</param>
        public Runtime(string version, bool pro, bool standard)
        {
            Version = version;
            Pro = pro;
            Standard = standard;
        }
    }
}