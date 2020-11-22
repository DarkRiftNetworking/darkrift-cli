using System;
using System.Collections.Generic;
using System.Net;

using DarkRift;
using DarkRift.Server;

namespace __n__.Client
{
    public class NetworkClientConnection1 : NetworkClientConnection
    {
        public override ConnectionState ConnectionState => throw new System.NotImplementedException();

        public override IEnumerable<IPEndPoint> RemoteEndPoints => throw new System.NotImplementedException();

        public RufflesNetworkClientConnection(IPAddress ipAddress, int port)
        {

        }

        public override void Connect()
        {
            throw new System.NotImplementedException();
        }

        public override bool Disconnect()
        {
            throw new System.NotImplementedException();
        }

        public override IPEndPoint GetRemoteEndPoint(string name)
        {
            throw new System.NotImplementedException();
        }

        public override bool SendMessageReliable(MessageBuffer message)
        {
            throw new System.NotImplementedException();
        }

        public override bool SendMessageUnreliable(MessageBuffer message)
        {
            throw new System.NotImplementedException();
        }
    }
}
