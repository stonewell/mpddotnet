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
    }
}
