using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core.Impl
{
    class UploadBandwidthThrottlerImpl : UploadBandwidthThrottler
    {
        #region UploadBandwidthThrottler Members

        public void QueueForSendingControlPacket(Mule.Network.ThrottledControlSocket socket)
        {
            throw new NotImplementedException();
        }

        public void QueueForSendingControlPacket(Mule.Network.ThrottledControlSocket socket, bool hasSent)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region UploadBandwidthThrottler Members


        public void RemoveAllFromQueue(Mule.Network.ThrottledControlSocket socket)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
