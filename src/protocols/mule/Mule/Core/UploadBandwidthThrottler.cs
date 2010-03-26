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

        void RemoveFromAllQueues(ThrottledControlSocket socket);
        void RemoveFromAllQueues(ThrottledFileSocket socket);

        ulong NumberOfSentBytesSinceLastCallAndReset { get; }
        ulong NumberOfSentBytesOverheadSinceLastCallAndReset { get; }
        uint HighestNumberOfFullyActivatedSlotsSinceLastCallAndReset { get; }

        uint StandardListSize { get; }

        void AddToStandardList(uint index, ThrottledFileSocket socket);
        bool RemoveFromStandardList(ThrottledFileSocket socket);

        void Start();
        void Pause(bool paused);
        void Stop();

        uint GetSlotLimit(uint currentUpSpeed);
    }
}
