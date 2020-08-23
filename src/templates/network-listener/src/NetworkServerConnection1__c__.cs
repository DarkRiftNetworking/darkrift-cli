using System.Collections.Generic;
using System.Net;
using DarkRift;
using DarkRift.Server;

namespace __n__
{
    /// <summary>
    /// An unimplemented NetworkServerConnection.
    /// </summary>
    internal class NetworkServerConnection1 : NetworkServerConnection
    {
        /// <summary>
        /// The state of this connection.
        /// </summary>
        public override ConnectionState ConnectionState => throw new System.NotImplementedException();

        /// <summary>
        /// The remote end points that are connected via this connection.
        /// </summary>
        public override IEnumerable<IPEndPoint> RemoteEndPoints => throw new System.NotImplementedException();

        /// <summary>
        /// Disconnects this connection.
        /// </summary>
        public override bool Disconnect()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Gets the specified remote end point by name.
        /// </summary>
        public override IPEndPoint GetRemoteEndPoint(string name)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Sends a message reliably.
        /// </summary>
        public override bool SendMessageReliable(MessageBuffer message)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Sends a message unreliably.
        /// </summary>
        public override bool SendMessageUnreliable(MessageBuffer message)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Starts listening for messages on this connection.
        /// </summary>
        public override void StartListening()
        {
            throw new System.NotImplementedException();
        }
    }
}
