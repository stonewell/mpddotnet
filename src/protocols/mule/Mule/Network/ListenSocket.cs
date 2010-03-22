using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.Network;
using System.Net;

namespace Mule.Network
{
    public class FilterAcceptClientEventArgs : SocketEventArgs
    {
        public FilterAcceptClientEventArgs(EndPoint ep)
        {
            Filtered = false;
            EndPoint = ep;
        }

        public bool Filtered;
        public EndPoint EndPoint;
    }

    public delegate void FilterAcceptClientEventHandler(FilterAcceptClientEventArgs args);

    public interface ListenSocket : AsyncSocket
    {
        event FilterAcceptClientEventHandler FilterAcceptClient;

        bool StartListening();
        void StopListening();
        void Process();

        void RemoveSocket(ClientReqSocket todel);
        void AddSocket(ClientReqSocket toadd);

        int OpenSocketsCount { get; }

        void KillAllSockets();
        bool TooManySockets();
        bool TooManySockets(bool bIgnoreInterval);

        uint MaxConnectionReached { get; }
        bool IsValidSocket(ClientReqSocket totest);
        
        void AddConnection();
        
        void RecalculateStats();
        
        void ReStartListening();
        
        bool Rebind();
        
        bool SendPortTestReply(char result);
        bool SendPortTestReply(char result, bool disconnect);

        void UpdateConnectionsStatus();
        float MaxConnectionsPerFileModifier { get; }
        uint PeakConnections { get; }
        uint TotalConnectionChecks { get; }
        float AverageConnections { get; }
        uint ActiveConnections { get; }
        ushort ConnectedPort { get; }
        uint TotalHalfConnectSocket { get; set; }
        uint TotalCompleteConnectSocket { get; set; }
    }
}
