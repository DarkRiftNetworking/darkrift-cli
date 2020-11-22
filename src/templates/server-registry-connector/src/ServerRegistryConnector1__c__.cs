using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace __n__
{
    /// <summary>
    /// DarkRift ServerRegistryConnector1 plugin.
    /// </sumamry>
    public class ServerRegistryConnector1 : ServerRegistryConnector
    {
        public override bool ThreadSafe => true;

        public override Version Version => new Version(1, 0, 0);

        public ServerRegistryConnector(ServerRegistryConnectorLoadData pluginLoadData) : base(pluginLoadData)
        {

        }

        protected override void DeregisterServer()
        {

        }

        protected override ushort RegisterServer(string group, string host, ushort port, IDictionary<string, string> properties)
        {

        }

        /// <summary>
        ///     Disposes of the client.
        /// </summary>
        /// <param name="disposing">If we are disopsing.</param>
        protected override void Dispose(bool disposing)
        {

        }
    }
}
