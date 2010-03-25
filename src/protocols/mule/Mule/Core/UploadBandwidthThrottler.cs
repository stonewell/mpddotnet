using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.Network;

namespace Mule.Core
{
    public interface UploadBandwidthThrottler
    {
        void QueueForSendingControlPacket(ThrottledControlSocket socket);
        void QueueForSendingControlPacket(ThrottledControlSocket socket, bool hasSent);

        void RemoveAllFromQueue(ThrottledControlSocket socket);
    }
}
