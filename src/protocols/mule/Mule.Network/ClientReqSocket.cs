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

    public struct QueryClientTimeOutArguments
    {
    }

    public delegate uint QueryClientTimeOutHandler(QueryClientTimeOutArguments arg);

    public struct CheckClientTimeOutArguments
    {
    }

    public delegate uint CheckClientTimeOutHandler(CheckClientTimeOutArguments arg);

    public struct SafeDeleteArguments
    {
        public SafeDeleteArguments(ClientReqSocket client)
        {
            ClientSocket = client;
        }

        public ClientReqSocket ClientSocket;
    }

    public delegate uint SafeDeleteHandler(SafeDeleteArguments arg);

    public struct ClientDisconnectArguments
    {
        public ClientDisconnectArguments(string reason, bool fromSocket)
        {
            Reason = reason;
            FromSocket = fromSocket;
        }

        public string Reason;
        public bool FromSocket;
    }

    public delegate bool ClientDisconnectHandler(ClientDisconnectArguments arg);

    public interface ClientReqSocket : EMSocket
    {
        event QueryClientTimeOutHandler QueryClientTimeOut;
        event CheckClientTimeOutHandler CheckClientTimeOut;
        event SafeDeleteHandler ClientSafeDelete;
        event ClientDisconnectHandler ClientDisconnect;

        void Disconnect(string reason);
        void WaitForOnConnect();
        void ResetTimeOutTimer();
        bool CheckTimeOut();
        void SafeDelete();
        bool Create();
    }
}
