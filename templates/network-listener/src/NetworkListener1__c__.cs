using System;

using DarkRift;
using DarkRift.Server;

namespace __n__
{
    /// <summary>
    /// A simple network listener template.
    /// </summary>
    class NetworkListener1 : NetworkListener
    {
        /// <summary>
        /// The version number of your log writer.
        /// </summary>
        public override Version Version => new Version(1, 0, 0);

        /// <summary>
        /// Constructor. Called as the network listener is loaded.
        /// </summary>
        public NetworkListener1(NetworkListenerLoadData networkListenerLoadData) : base(networkListenerLoadData)
        {
        }

        /// <summary>
        /// Starts listening for new clients.
        /// </summary>
        public override void StartListening()
        {
            // TODO Start listening for clients. When you get one, call RegisterConnection:
            RegisterConnection(new NetworkServerConnection1());
        }
    }
}
