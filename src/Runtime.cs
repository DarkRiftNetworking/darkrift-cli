﻿using System;

namespace DarkRift.Cli
{
    /// <summary>
    /// Holds a project's runtime settings.
    /// </summary>
    public class Runtime
    {
        /// <summary>
        /// The version of DarkRift to use.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// If .NET core or .NET framework should be used.
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// The tier of DarkRift to use.
        /// </summary>
        public ServerTier Tier { get; set; }

        /// <summary>
        /// Creates a new Runtime configuration element.
        /// </summary>
        public Runtime()
        {
        }

        /// <summary>
        /// Creates a new Runtime configuration element.
        /// </summary>
        /// <param name="version">The version of DarkRift to use.</param>
        /// <param name="tier">The tier of DarkRift to use.</param>
        /// <param name="platform">If .NET standard or .NET framework should be used.</param>
        public Runtime(string version, ServerTier tier, string platform)
        {
            Version = version;
            Tier = tier;
            Platform = platform;
        }
    }
}
