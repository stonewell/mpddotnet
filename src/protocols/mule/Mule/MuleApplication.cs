using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.Preference;

namespace Mule
{
    public interface MuleApplication
    {
        bool AwaitingTestFromIP(uint ip);
        bool IsKadFirewallCheckIP(uint ip);
        
        MulePreference Preference { get; }

        bool IsServerConnecting { get; }
        bool IsConnected { get; }

        void QueueForSendingControlPacket(ThrottledControlSocket socket);
        void QueueForSendingControlPacket(ThrottledControlSocket socket, bool hasSent);
    }
}
