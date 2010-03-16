using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Preference
{
    public interface MuleApplication
    {
        bool AwaitingTestFromIP(uint ip);
        bool IsKadFirewallCheckIP(uint ip);
        
        MulePreference Preference { get; }
        
        void QueueForSendingControlPacket(object socket);
        void QueueForSendingControlPacket(object socket, bool hasSent);

        void RemoveFromAllQueues(object socket);

        void ListenSocketRemoveSocket(object socket);
        void ListenSocketAddSocket(object socket);
        bool ListenSocketIsValidSocket(object socket);

        uint ListenSocketTotalHalfCon { get; set; }
        uint ListenSocketTotalComp { get; set; }

        void ListSocketAddConnection();
    }
}
