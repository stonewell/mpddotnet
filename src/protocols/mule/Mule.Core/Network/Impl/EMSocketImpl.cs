#region File Header

//
//  Copyright (C) 2008 Jingnan Si
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
//
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using Mule.Core.Network.AsynchSocket;

namespace Mule.Core.Network.Impl
{
    abstract class EMSocketImpl : EncryptedStreamSocketImpl, EMSocket
    {
        #region EMSocket Members

        public void SendPacket(Packet packet, bool delpacket, bool controlpacket, uint actualPayloadSize, bool bForceImmediateSend)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool IsConnected
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public byte ConState
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool IsRawDataMode
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public void SetDownloadLimit(uint limit)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void DisableDownloadLimit()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool AsyncSelect(long lEvent)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool IsBusy
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool HasQueues
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public uint TimeOut
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public bool Connect(string lpszHostAddress, uint nHostPort)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void InitProxySupport()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void RemoveAllLayers()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public string LastProxyError
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool ProxyConnectFailed
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public string GetFullErrorMessage(uint dwError)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public uint LastCalledSend
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public ulong GetSentBytesCompleteFileSinceLastCallAndReset()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public ulong GetSentBytesPartFileSinceLastCallAndReset()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public ulong GetSentBytesControlPacketSinceLastCallAndReset()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public ulong GetSentPayloadSinceLastCallAndReset()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void TruncateQueues()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public SocketSentBytes SendControlData(uint maxNumberOfBytesToSend, uint minFragSize)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public SocketSentBytes SendFileAndControlData(uint maxNumberOfBytesToSend, uint minFragSize)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public uint GetNeededBytes()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        protected virtual int OnLayerCallback(AsyncSocketExLayer pLayer, int nType, int nCode, uint wParam, uint lParam)
        {
            return -1;
        }

        protected virtual void DataReceived(byte[] pcData, uint uSize)
        {
        }

        protected abstract bool PacketReceived(Packet packet);
        protected abstract override void OnError(int nErrorCode);

        protected virtual void OnClose(int nErrorCode)
        {
        }

        protected override void OnSend(int nErrorCode)
        {
        }

        protected virtual void OnReceive(int nErrorCode)
        {
        }

        protected byte byConnected_;
        protected uint timeOut_;
        protected bool proxyConnectFailed_;
        protected AsyncProxySocketLayer proxyLayer_;
        protected string m_strLastProxyError;

        protected virtual SocketSentBytes Send(uint maxNumberOfBytesToSend, uint minFragSize, bool onlyAllowedToSendControlPacket)
        {
            throw new Exception("UnSupport");
        }

        private void ClearQueues()
        {
        }

        private new int Receive(byte[] lpBuf, int nBufLen, int nFlags)
        {
            return -1;
        }

        private uint GetNextFragSize(uint current, uint minFragSize)
        {
            return 0;
        }

        private bool HasSent { get { return hasSent_; } }

        // Download (pseudo) rate control
        private uint downloadLimit_;
        private bool downloadLimitEnable_;
        private bool pendingOnReceive_;

        // Download partial header
        // actually, this holds only 'PACKET_HEADER_SIZE-1' bytes.
        private byte[] pendingHeader_ = new byte[CoreConstants.PACKET_HEADER_SIZE];
        private uint pendingHeaderSize_;

        // Download partial packet
        private Packet pendingPacket_;
        private uint pendingPacketSize_;

        // Upload control
        private byte[] sendbuffer_;
        private uint sendblen_;
        private uint sent_;

        private List<Packet> controlpacket_queue_;
        private List<StandardPacketQueueEntry> standartpacket_queue_;
        private bool currentPacket_is_controlpacket_;
        private object sendLocker_ = new object();
        private ulong numberOfSentBytesCompleteFile_;
        private ulong numberOfSentBytesPartFile_;
        private ulong numberOfSentBytesControlPacket_;
        private bool currentPackageIsFromPartFile_;
        private bool bAccelerateUpload_;
        private uint lastCalledSend_;
        private uint lastSent_;
        private uint lastFinishedStandard_;
        private uint m_actualPayloadSize_;
        private uint m_actualPayloadSizeSent_;
        private bool bBusy_;
        private bool hasSent_;
    }
}
