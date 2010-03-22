using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.Network;

namespace Mule.Network
{
    public enum SocketStateEnum
    {
        SS_Other,		//These are sockets we created that may or may not be used.. Or incoming connections.
        SS_Half,		//These are sockets that we called ->connect(..) and waiting for some kind of response.
        SS_Complete	//These are sockets that have responded with either a connection or error.
    };

    public delegate uint ClientTimeoutEventHandler(object sender, SocketEventArgs arg);

    public interface ClientReqSocket : EMSocket
    {
        event ClientTimeoutEventHandler QueryClientTimeOut;
        event ClientTimeoutEventHandler CheckClientTimeOut;

        event SocketEventHandler SocketStateChanging;
        event SocketEventHandler SocketStateChanged;

        string DisonnectReason { get; }

        SocketStateEnum SocketState { get; }

        bool CheckTimeOut();
    }
}
