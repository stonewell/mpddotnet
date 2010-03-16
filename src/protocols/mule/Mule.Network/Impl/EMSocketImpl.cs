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
using Mule.Definitions;
using Mule.Preference;
using Mpd.Utilities;
using System.Diagnostics;
using System.Net.Sockets;
using Mpd.Generic;

namespace Mule.Network.Impl
{
    abstract class EMSocketImpl : EncryptedStreamSocketImpl, EMSocket
    {
        #region Fields
        protected byte byConnected_;

        // Download (pseudo) rate control
        private bool pendingOnReceive_;

        // Download partial header
        // actually, this holds only 'PACKET_HEADER_SIZE-1' bytes.
        private byte[] pendingHeader_ = new byte[MuleConstants.PACKET_HEADER_SIZE];
        private uint pendingHeaderSize_;

        // Download partial packet
        private Packet pendingPacket_;
        private uint pendingPacketSize_;

        // Upload control
        private byte[] sendbuffer_ = null;
        private uint sendblen_;
        private uint sent_;

        private Queue<Packet> controlpacket_queue_ = new Queue<Packet>();
        private Queue<StandardPacketQueueEntry> standartpacket_queue_ = new Queue<StandardPacketQueueEntry>();
        private bool currentPacket_is_controlpacket_;
        private object sendLocker_ = new object();
        private ulong numberOfSentBytesCompleteFile_;
        private ulong numberOfSentBytesPartFile_;
        private ulong numberOfSentBytesControlPacket_;
        private bool currentPackageIsFromPartFile_;
        private bool accelerateUpload_;
        private uint lastCalledSend_;
        private uint lastSent_;
        private uint lastFinishedStandard_;
        private uint actualPayloadSize_;
        private uint actualPayloadSizeSent_;
        private bool hasSent_;

        private uint downloadLimit_ = 0;
        private bool enableDownloadLimit_ = false;
        private bool useBigSendBuffer_;
        #endregion

        #region Constructors
        public EMSocketImpl(MuleApplication muleApp)
            : base(muleApp)
        {
            byConnected_ = Convert.ToByte(EMSocketStateEnum.ES_NOTCONNECTED);
            TimeOut = MuleConstants.CONNECTION_TIMEOUT; // default timeout for ed2k sockets

            // Download (pseudo) rate control	
            downloadLimit_ = 0;
            enableDownloadLimit_ = false;
            pendingOnReceive_ = false;

            // Download partial header
            pendingHeaderSize_ = 0;

            // Download partial packet
            pendingPacket_ = null;
            pendingPacketSize_ = 0;

            // Upload control
            sendbuffer_ = null;
            sendblen_ = 0;
            sent_ = 0;
            //bLinkedPackets_ = false;

            currentPacket_is_controlpacket_ = false;
            currentPackageIsFromPartFile_ = false;

            numberOfSentBytesCompleteFile_ = 0;
            numberOfSentBytesPartFile_ = 0;
            numberOfSentBytesControlPacket_ = 0;

            lastCalledSend_ = MpdUtilities.GetTickCount();
            lastSent_ = MpdUtilities.GetTickCount() - 1000;

            accelerateUpload_ = false;

            actualPayloadSize_ = 0;
            actualPayloadSizeSent_ = 0;

            hasSent_ = false;
            useBigSendBuffer_ = false;
        }

        #endregion

        #region EMSocket Members
        public virtual void CleanUp()
        {
            // need to be locked here to know that the other methods
            // won't be in the middle of things
            lock (sendLocker_)
            {
                byConnected_ = Convert.ToByte(EMSocketStateEnum.ES_DISCONNECTED);
            }

            // now that we know no other method will keep adding to the queue
            // we can remove ourself from the queue
            MuleApp.RemoveFromAllQueues(this);

            ClearQueues();
            RemoveAllLayers(); // deadlake PROXYSUPPORT
        }

        public void SendPacket(Packet packet)
        {
            SendPacket(packet, true, true, 0, false);
        }

        public void SendPacket(Packet packet, bool delpacket)
        {
            SendPacket(packet, delpacket, true, 0, false);
        }

        public void SendPacket(Packet packet, bool delpacket, bool controlpacket)
        {
            SendPacket(packet, delpacket, controlpacket, 0, false);
        }

        public void SendPacket(Packet packet, bool delpacket, bool controlpacket, uint actualPayloadSize)
        {
            SendPacket(packet, delpacket, controlpacket, actualPayloadSize, false);
        }

        public virtual void SendPacket(Packet packet, bool delpacket, bool controlpacket, uint actualPayloadSize, bool bForceImmediateSend)
        {
            lock (sendLocker_)
            {
                do
                {
                    if (byConnected_ == Convert.ToByte(EMSocketStateEnum.ES_DISCONNECTED))
                    {
                        break;
                    }
                    else
                    {
                        if (!delpacket)
                        {
                            //Debug.Assert ( !packet.IsSplitted() );
                            Packet copy = NetworkObjectManager.CreatePacket(packet.OperationCode, packet.Size);
                            Array.Copy(copy.Buffer, packet.Buffer, packet.Size);
                            packet = copy;
                        }

                        if (controlpacket)
                        {
                            controlpacket_queue_.Enqueue(packet);

                            // queue up for controlpacket
                            MuleApp.QueueForSendingControlPacket(this, HasSent);
                        }
                        else
                        {
                            bool first = !((sendbuffer_ != null &&
                                !currentPacket_is_controlpacket_) ||
                                standartpacket_queue_.Count != 0);
                            StandardPacketQueueEntry queueEntry = new StandardPacketQueueEntry(packet, actualPayloadSize);
                            standartpacket_queue_.Enqueue(queueEntry);

                            // reset timeout for the first time
                            if (first)
                            {
                                lastFinishedStandard_ = MpdUtilities.GetTickCount();
                                accelerateUpload_ = true;	// Always accelerate first packet in a block
                            }
                        }
                    }
                }
                while (false);
            }

            if (bForceImmediateSend)
            {
                Debug.Assert(controlpacket_queue_.Count == 1);
                Send(1024, 0, true);
            }
        }

        public bool IsConnected
        {
            get { return byConnected_ == Convert.ToByte(EMSocketStateEnum.ES_CONNECTED); }
        }

        public byte ConnectionState
        {
            get { return byConnected_; }
        }

        public virtual bool IsRawDataMode
        {
            get { return false; }
        }

        public uint DownloadLimit
        {
            get { return downloadLimit_; }
            set
            {
                downloadLimit_ = value;
                enableDownloadLimit_ = true;

                // CPU load improvement
                if (value > 0 && pendingOnReceive_ == true)
                {
                    OnReceive(0);
                }
            }
        }

        public bool EnableDownloadLimit
        {
            get { return enableDownloadLimit_; }
            set
            {
                enableDownloadLimit_ = value;
                // CPU load improvement
                if (pendingOnReceive_ == true)
                {
                    OnReceive(0);
                }
            }
        }

        public virtual bool IsBusy
        {
            get;
            set;
        }

        public bool HasQueues
        {
            get
            {
                return (sendbuffer_ != null ||
                    standartpacket_queue_.Count > 0 ||
                    controlpacket_queue_.Count > 0);
            }
        }

        private const int BIGSIZE = 128 * 1024;

        public virtual bool UseBigSendBuffer()
        {
            if (useBigSendBuffer_)
                return true;
            useBigSendBuffer_ = true;
            int val = BIGSIZE;
            int oldval = 0;
            oldval = Convert.ToInt32(GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer));
            if (val > oldval)
                SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, val);
            val = Convert.ToInt32(GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer));
            return val == BIGSIZE;
        }

        public virtual uint TimeOut
        {
            get;set;
        }

        public virtual bool Connect(string lpszHostAddress, uint nHostPort)
        {
            InitProxySupport();

            try
            {
                base.Connect(lpszHostAddress, Convert.ToInt32(nHostPort));
                return base.Connected;
            }
            catch (Exception ex)
            {
                MpdUtilities.DebugLogError("Connect Fail", ex);
                return base.Connected;
            }
        }

        public void InitProxySupport()
        {
            //DO NOTHING
        }

        public virtual void RemoveAllLayers()
        {
        }

        public string LastProxyError
        {
            get { return string.Empty; }
        }

        public bool ProxyConnectFailed
        {
            get { return false; }
        }

        public string GetFullErrorMessage(uint dwError)
        {
            return string.Empty;
        }

        public uint LastCalledSend
        {
            get { return lastCalledSend_; }
        }

        public ulong GetSentBytesCompleteFileSinceLastCallAndReset()
        {
            lock (sendLocker_)
            {
                ulong sentBytes = numberOfSentBytesCompleteFile_;
                numberOfSentBytesCompleteFile_ = 0;
                return sentBytes;
            }
        }

        public ulong GetSentBytesPartFileSinceLastCallAndReset()
        {
            lock (sendLocker_)
            {
                ulong sentBytes = numberOfSentBytesPartFile_;
                numberOfSentBytesPartFile_ = 0;
                return sentBytes;
            }
        }

        public ulong GetSentBytesControlPacketSinceLastCallAndReset()
        {
            lock (sendLocker_)
            {
                ulong sentBytes = numberOfSentBytesControlPacket_;
                numberOfSentBytesControlPacket_ = 0;
                return sentBytes;
            }
        }

        public ulong GetSentPayloadSinceLastCallAndReset()
        {
            lock (sendLocker_)
            {
                ulong sentBytes = actualPayloadSizeSent_;
                actualPayloadSizeSent_ = 0;
                return sentBytes;
            }
        }

        public void TruncateQueues()
        {
            lock (sendLocker_)
            {

                standartpacket_queue_.Clear();

            }
        }

        public virtual SocketSentBytes SendControlData(uint maxNumberOfBytesToSend, uint minFragSize)
        {
            return Send(maxNumberOfBytesToSend, minFragSize, true);
        }

        public virtual SocketSentBytes SendFileAndControlData(uint maxNumberOfBytesToSend, uint minFragSize)
        {
            return Send(maxNumberOfBytesToSend, minFragSize, false);
        }

        public uint NeededBytes
        {
            get
            {
                ulong sizeleft, sizetotal;
                ulong timeleft, timetotal;
                uint sendgap;

                lock (sendLocker_)
                {
                    if (byConnected_ == Convert.ToByte(EMSocketStateEnum.ES_DISCONNECTED))
                    {
                        return 0;
                    }

                    if (!((sendbuffer_ != null && !currentPacket_is_controlpacket_) ||
                        standartpacket_queue_.Count != 0))
                    {
                        // No standard packet to send. Even if data needs to be sent to prevent timout, there's nothing to send.
                        return 0;
                    }

                    if (((sendbuffer_ != null && !currentPacket_is_controlpacket_)) &&
                        controlpacket_queue_.Count != 0)
                        accelerateUpload_ = true;	// We might be trying to send a block request, accelerate packet

                    sendgap = MpdUtilities.GetTickCount() - lastCalledSend_;

                    timetotal = Convert.ToUInt64(accelerateUpload_ ? 45000 : 90000);
                    timeleft = MpdUtilities.GetTickCount() - lastFinishedStandard_;
                    if (sendbuffer_ != null && !currentPacket_is_controlpacket_)
                    {
                        sizeleft = sendblen_ - sent_;
                        sizetotal = sendblen_;
                    }
                    else
                    {
                        sizeleft = sizetotal = standartpacket_queue_.Peek().Packet.RealPacketSize;
                    }
                }

                if (timeleft >= timetotal)
                    return (uint)sizeleft;
                timeleft = timetotal - timeleft;
                if (timeleft * sizetotal >= timetotal * sizeleft)
                {
                    // don't use 'GetTimeOut' here in case the timeout value is high,
                    if (sendgap > MuleConstants.ONE_SEC_MS * 20)
                        return 1;	// Don't let the socket itself time out - Might happen when switching from spread(non-focus) slot to trickle slot
                    return 0;
                }
                ulong decval = timeleft * sizetotal / timetotal;
                if (decval == 0)
                    return (uint)sizeleft;
                if (decval < sizeleft)
                    return (uint)(sizeleft - decval + 1);	// Round up
                else
                    return 1;
            }

        }

        #endregion

        protected abstract void DataReceived(byte[] pcData, uint uSize);

        protected abstract bool PacketReceived(Packet packet);

        protected override void OnClose(int nErrorCode)
        {
            // need to be locked here to know that the other methods
            // won't be in the middle of things
            lock (sendLocker_)
            {
                byConnected_ = Convert.ToByte(EMSocketStateEnum.ES_DISCONNECTED);
            }

            // now that we know no other method will keep adding to the queue
            // we can remove ourself from the queue
            MuleApp.RemoveFromAllQueues(this);

            base.OnClose(nErrorCode); // deadlake changed socket to PROXYSUPPORT ( AsyncSocketEx )
            RemoveAllLayers(); // deadlake PROXYSUPPORT
            ClearQueues();
        }

        protected override void OnSend(int nErrorCode)
        {
            if (nErrorCode != 0)
            {
                OnError(nErrorCode);
                return;
            }

            base.OnSend(0);

            lock (sendLocker_)
            {
                do
                {

                    IsBusy = false;

                    if (byConnected_ == Convert.ToByte(EMSocketStateEnum.ES_DISCONNECTED))
                    {
                        break;
                    }
                    else
                        byConnected_ = Convert.ToByte(EMSocketStateEnum.ES_CONNECTED);

                    if (currentPacket_is_controlpacket_)
                    {
                        // queue up for control packet
                        MuleApp.QueueForSendingControlPacket(this, HasSent);
                    }
                } while (false);
            }
        }

        // the 2 meg size was taken from another place
        private const uint GLOBAL_READ_BUFFER_SIZE = 2000000;
        private static byte[] GlobalReadBuffer = new byte[GLOBAL_READ_BUFFER_SIZE];

        protected override void OnReceive(int nErrorCode)
        {
            // Check for an error code
            if (nErrorCode != 0)
            {
                OnError(nErrorCode);
                return;
            }

            // Check current connection state
            if (byConnected_ == Convert.ToByte(EMSocketStateEnum.ES_DISCONNECTED))
            {
                return;
            }
            else
            {
                byConnected_ = Convert.ToByte(EMSocketStateEnum.ES_CONNECTED); // ES_DISCONNECTED, ES_NOTCONNECTED, ES_CONNECTED
            }

            // CPU load improvement
            if (EnableDownloadLimit && DownloadLimit == 0)
            {
                pendingOnReceive_ = true;

                //Receive(GlobalReadBuffer + pendingHeaderSize, 0);
                return;
            }

            // Remark: an overflow can not occur here
            uint readMax = GLOBAL_READ_BUFFER_SIZE - pendingHeaderSize_;
            if (EnableDownloadLimit && readMax > DownloadLimit)
            {
                readMax = DownloadLimit;
            }

            // We attempt to read up to 2 megs at a time (minus whatever is in our internal read buffer)
            int ret = Receive(GlobalReadBuffer, Convert.ToInt32(pendingHeaderSize_), Convert.ToInt32(readMax));
            if (ret == SOCKET_ERROR ||
                byConnected_ == Convert.ToByte(EMSocketStateEnum.ES_DISCONNECTED))
            {
                return;
            }

            // Bandwidth control
            if (EnableDownloadLimit)
            {
                // Update limit
                downloadLimit_ -= Convert.ToUInt32(RealReceivedBytes);
            }

            // CPU load improvement
            // Detect if the socket's buffer is empty (or the size did match...)
            pendingOnReceive_ = fullReceive_;

            if (ret == 0)
                return;

            // Copy back the partial header into the global read buffer for processing
            if (pendingHeaderSize_ > 0)
            {
                Array.Copy(GlobalReadBuffer, pendingHeader_, pendingHeaderSize_);
                ret += Convert.ToInt32(pendingHeaderSize_);
                pendingHeaderSize_ = 0;
            }

            if (IsRawDataMode)
            {
                DataReceived(GlobalReadBuffer, Convert.ToUInt32(ret));
                return;
            }

            byte[] rptr = GlobalReadBuffer; // floating index initialized with begin of buffer
            int buffer_end = ret;
            int offset = 0;

            // Loop, processing packets until we run out of them
            while ((buffer_end - offset >= MuleConstants.PACKET_HEADER_SIZE) ||
                ((pendingPacket_ != null) && (buffer_end - offset > 0)))
            {
                // Two possibilities here: 
                //
                // 1. There is no pending incoming packet
                // 2. There is already a partial pending incoming packet
                //
                // It's important to remember that emule exchange two kinds of packet
                // - The control packet
                // - The data packet for the transport of the block
                // 
                // The biggest part of the traffic is done with the data packets. 
                // The default size of one block is 10240 bytes (or less if compressed), but the
                // maximal size for one packet on the network is 1300 bytes. It's the reason
                // why most of the Blocks are splitted before to be sent. 
                //
                // Conclusion: When the download limit is disabled, this method can be at least 
                // called 8 times (10240/1300) by the lower layer before a splitted packet is 
                // rebuild and transferred to the above layer for processing.
                //
                // The purpose of this algorithm is to limit the amount of data exchanged between buffers

                if (pendingPacket_ == null)
                {
                    pendingPacket_ = NetworkObjectManager.CreatePacket(GlobalReadBuffer, offset); // Create new packet container. 
                    offset += 6;                        // Only the header is initialized so far

                    // Bugfix We still need to check for a valid protocol
                    // Remark: the default eMule v0.26b had removed this test......
                    switch (pendingPacket_.Protocol)
                    {
                        case MuleConstants.OP_EDONKEYPROT:
                        case MuleConstants.OP_PACKEDPROT:
                        case MuleConstants.OP_EMULEPROT:
                            break;
                        default:
                            pendingPacket_ = null;
                            OnError(Convert.ToInt32(EMSocketErrorCodeEnum.ERR_WRONGHEADER));
                            return;
                    }

                    // Security: Check for buffer overflow (2MB)
                    if (pendingPacket_.Size > GLOBAL_READ_BUFFER_SIZE)
                    {
                        pendingPacket_ = null;
                        OnError(Convert.ToInt32(EMSocketErrorCodeEnum.ERR_TOOBIG));
                        return;
                    }

                    // Init data buffer
                    pendingPacket_.Buffer = new byte[pendingPacket_.Size + 1];
                    pendingPacketSize_ = 0;
                }

                // Bytes ready to be copied into packet's internal buffer
                Debug.Assert(offset <= buffer_end);
                uint toCopy = ((pendingPacket_.Size - pendingPacketSize_) < (uint)(buffer_end - offset)) ?
                                 (pendingPacket_.Size - pendingPacketSize_) : (uint)(buffer_end - offset);

                // Copy Bytes from Global buffer to packet's internal buffer
                Array.Copy(pendingPacket_.Buffer, pendingPacketSize_, rptr, offset, toCopy);
                pendingPacketSize_ += toCopy;
                offset += Convert.ToInt32(toCopy);

                // Check if packet is complet
                Debug.Assert(pendingPacket_.Size >= pendingPacketSize_);
                if (pendingPacket_.Size == pendingPacketSize_)
                {
                    // Process packet
                    bool bPacketResult = PacketReceived(pendingPacket_);
                    pendingPacket_ = null;
                    pendingPacketSize_ = 0;

                    if (!bPacketResult)
                        return;
                }
            }

            // Finally, if there is any data left over, save it for next time
            Debug.Assert(offset <= buffer_end);
            Debug.Assert(buffer_end - offset < MuleConstants.PACKET_HEADER_SIZE);
            if (offset != buffer_end)
            {
                // Keep the partial head
                pendingHeaderSize_ = Convert.ToUInt32(buffer_end - offset);
                Array.Copy(pendingHeader_, 0, rptr, offset, pendingHeaderSize_);
            }
        }

        protected virtual SocketSentBytes Send(uint maxNumberOfBytesToSend, uint minFragSize, bool onlyAllowedToSendControlPacket)
        {
            lock (sendLocker_)
            {
                if (byConnected_ == Convert.ToByte(EMSocketStateEnum.ES_DISCONNECTED))
                {
                    return new SocketSentBytes(false, 0, 0);
                }

                bool anErrorHasOccured = false;
                uint sentStandardPacketBytesThisCall = 0;
                uint sentControlPacketBytesThisCall = 0;

                if (byConnected_ == Convert.ToByte(EMSocketStateEnum.ES_CONNECTED)
                    && IsEncryptionLayerReady &&
                    !(IsBusy && onlyAllowedToSendControlPacket))
                {
                    if (minFragSize < 1)
                    {
                        minFragSize = 1;
                    }

                    maxNumberOfBytesToSend = GetNextFragSize(maxNumberOfBytesToSend, minFragSize);

                    bool bWasLongTimeSinceSend = (MpdUtilities.GetTickCount() - lastSent_) > 1000;

                    lastCalledSend_ = MpdUtilities.GetTickCount();

                    while (sentStandardPacketBytesThisCall + sentControlPacketBytesThisCall < maxNumberOfBytesToSend
                        && !anErrorHasOccured && // don't send more than allowed. Also, there should have been no error in earlier loop
                          (sendbuffer_ != null || controlpacket_queue_.Count != 0 || standartpacket_queue_.Count != 0) && // there must exist something to send
                           (!onlyAllowedToSendControlPacket || // this means we are allowed to send both types of packets, so proceed
                            sendbuffer_ != null && currentPacket_is_controlpacket_ || // We are in the progress of sending a control packet. We are always allowed to send those
                            sentStandardPacketBytesThisCall + sentControlPacketBytesThisCall > 0 &&
                            (sentStandardPacketBytesThisCall + sentControlPacketBytesThisCall) % minFragSize != 0 || // Once we've started, continue to send until an even minFragsize to minimize packet overhead
                            sendbuffer_ == null && controlpacket_queue_.Count != 0 || // There's a control packet in queue, and we are not currently sending anything, so we will handle the control packet next
                            sendbuffer_ != null && !currentPacket_is_controlpacket_ &&
                            bWasLongTimeSinceSend && controlpacket_queue_.Count != 0 &&
                            (sentStandardPacketBytesThisCall + sentControlPacketBytesThisCall) < minFragSize // We have waited to long to clean the current packet (which may be a standard packet that is in the way). Proceed no matter what the value of onlyAllowedToSendControlPacket.
                           )
                         )
                    {

                        // If we are currently not in the progress of sending a packet, we will need to find the next one to send
                        if (sendbuffer_ == null)
                        {
                            Packet curPacket = null;
                            if (controlpacket_queue_.Count != 0)
                            {
                                // There's a control packet to send
                                currentPacket_is_controlpacket_ = true;
                                curPacket = controlpacket_queue_.Dequeue();
                            }
                            else if (standartpacket_queue_.Count > 0)
                            {
                                // There's a standard packet to send
                                currentPacket_is_controlpacket_ = false;
                                StandardPacketQueueEntry queueEntry = standartpacket_queue_.Dequeue();
                                curPacket = queueEntry.Packet;
                                actualPayloadSize_ = queueEntry.ActualPayloadSize;

                                // remember this for statistics purposes.
                                currentPackageIsFromPartFile_ = curPacket.IsFromPF;
                            }
                            else
                            {
                                // if we reach this point, then there's something wrong with the while condition above!
                                Debug.Assert(false);
                                MpdUtilities.QueueDebugLogLine(true, ("EMSocket: Couldn't get a new packet! There's an error in the first while condition in EMSocket::Send()"));

                                return new SocketSentBytes(true, sentStandardPacketBytesThisCall, sentControlPacketBytesThisCall);
                            }

                            // We found a package to send. Get the data to send from the
                            // package container and dispose of the container.
                            sendblen_ = curPacket.RealPacketSize;
                            sendbuffer_ = curPacket.DetachPacket();
                            sent_ = 0;
                            curPacket = null;

                            // encrypting which cannot be done transparent by base class
                            CryptPrepareSendData(sendbuffer_, sendblen_);
                        }

                        // At this point we've got a packet to send in sendbuffer_. Try to send it. Loop until entire packet
                        // is sent, or until we reach maximum bytes to send for this call, or until we get an error.
                        // NOTE! If send would block (returns WSAEWOULDBLOCK), we will return from this method INSIDE this loop.
                        while (sent_ < sendblen_ &&
                               sentStandardPacketBytesThisCall + sentControlPacketBytesThisCall < maxNumberOfBytesToSend &&
                               (
                                !onlyAllowedToSendControlPacket || // this means we are allowed to send both types of packets, so proceed
                                currentPacket_is_controlpacket_ ||
                                bWasLongTimeSinceSend &&
                                (sentStandardPacketBytesThisCall + sentControlPacketBytesThisCall) < minFragSize ||
                                (sentStandardPacketBytesThisCall + sentControlPacketBytesThisCall) % minFragSize != 0
                               ) &&
                               !anErrorHasOccured)
                        {
                            uint tosend = sendblen_ - sent_;
                            if (!onlyAllowedToSendControlPacket || currentPacket_is_controlpacket_)
                            {
                                if (maxNumberOfBytesToSend >= sentStandardPacketBytesThisCall + sentControlPacketBytesThisCall
                                    && tosend > maxNumberOfBytesToSend - (sentStandardPacketBytesThisCall + sentControlPacketBytesThisCall))
                                {
                                    tosend = maxNumberOfBytesToSend - (sentStandardPacketBytesThisCall + sentControlPacketBytesThisCall);
                                }
                            }
                            else if (bWasLongTimeSinceSend && (sentStandardPacketBytesThisCall + sentControlPacketBytesThisCall) < minFragSize)
                            {
                                if (minFragSize >= sentStandardPacketBytesThisCall + sentControlPacketBytesThisCall
                                    && tosend > minFragSize - (sentStandardPacketBytesThisCall + sentControlPacketBytesThisCall))
                                {
                                    tosend = minFragSize - (sentStandardPacketBytesThisCall + sentControlPacketBytesThisCall);
                                }
                            }
                            else
                            {
                                uint nextFragMaxBytesToSent =
                                    GetNextFragSize(sentStandardPacketBytesThisCall + sentControlPacketBytesThisCall,
                                        minFragSize);
                                if (nextFragMaxBytesToSent >= sentStandardPacketBytesThisCall + sentControlPacketBytesThisCall
                                    && tosend > nextFragMaxBytesToSent - (sentStandardPacketBytesThisCall + sentControlPacketBytesThisCall))
                                {
                                    tosend = nextFragMaxBytesToSent - (sentStandardPacketBytesThisCall + sentControlPacketBytesThisCall);
                                }
                            }
                            Debug.Assert(tosend != 0 && tosend <= sendblen_ - sent_);

                            //DWORD tempStartSendTick = ::GetTickCount();

                            lastSent_ = MpdUtilities.GetTickCount();

                            int result = base.Send(sendbuffer_, Convert.ToInt32(sent_), Convert.ToInt32(tosend)); // deadlake PROXYSUPPORT - changed to AsyncSocketEx
                            if (result == SOCKET_ERROR)
                            {
                                if (IsSendBlocked)
                                {
                                    IsBusy = true;

                                    // Send() blocked, onsend will be called when ready to send again
                                    return new SocketSentBytes(true, sentStandardPacketBytesThisCall, sentControlPacketBytesThisCall);
                                }
                                else
                                {
                                    // Send() gave an error
                                    anErrorHasOccured = true;
                                    //DEBUG_ONLY( AddDebugLogLine(true,"EMSocket: An error has occured: %i", error) );
                                }
                            }
                            else
                            {
                                // we managed to send some bytes. Perform bookkeeping.
                                IsBusy = false;
                                hasSent_ = true;

                                sent_ += Convert.ToUInt32(result);

                                // Log send bytes in correct class
                                if (!currentPacket_is_controlpacket_)
                                {
                                    sentStandardPacketBytesThisCall += Convert.ToUInt32(result);

                                    if (currentPackageIsFromPartFile_)
                                    {
                                        numberOfSentBytesPartFile_ += Convert.ToUInt32(result);
                                    }
                                    else
                                    {
                                        numberOfSentBytesCompleteFile_ += Convert.ToUInt32(result);
                                    }
                                }
                                else
                                {
                                    sentControlPacketBytesThisCall += Convert.ToUInt32(result);
                                    numberOfSentBytesControlPacket_ += Convert.ToUInt32(result);
                                }
                            }
                        }

                        if (sent_ == sendblen_)
                        {
                            // we are done sending the current package. Delete it and set
                            // sendbuffer_ to null so a new packet can be fetched.
                            sendbuffer_ = null;
                            sendblen_ = 0;

                            if (!currentPacket_is_controlpacket_)
                            {
                                actualPayloadSizeSent_ += actualPayloadSize_;
                                actualPayloadSize_ = 0;

                                lastFinishedStandard_ = MpdUtilities.GetTickCount(); // reset timeout
                                accelerateUpload_ = false; // Safe until told otherwise
                            }

                            sent_ = 0;
                        }
                    }
                }

                if (onlyAllowedToSendControlPacket &&
                    (controlpacket_queue_.Count > 0 ||
                    sendbuffer_ != null && currentPacket_is_controlpacket_))
                {
                    // enter control packet send queue
                    // we might enter control packet queue several times for the same package,
                    // but that costs very little overhead. Less overhead than trying to make sure
                    // that we only enter the queue once.
                    MuleApp.QueueForSendingControlPacket(this, HasSent);
                }

                //CleanSendLatencyList();

                return new SocketSentBytes(!anErrorHasOccured, sentStandardPacketBytesThisCall, sentControlPacketBytesThisCall);
            }//lock
        }

        private void ClearQueues()
        {
            lock (sendLocker_)
            {
                controlpacket_queue_.Clear();
                standartpacket_queue_.Clear();
            }

            // Download (pseudo) rate control	
            downloadLimit_ = 0;
            enableDownloadLimit_ = false;
            pendingOnReceive_ = false;

            // Download partial header
            pendingHeaderSize_ = 0;

            // Download partial packet
            pendingPacket_ = null;
            pendingPacketSize_ = 0;

            // Upload control
            sendbuffer_ = null;

            sendblen_ = 0;
            sent_ = 0;
        }

        private uint GetNextFragSize(uint current, uint minFragSize)
        {
            if (current % minFragSize == 0)
            {
                return current;
            }
            else
            {
                return minFragSize * (current / minFragSize + 1);
            }
        }

        private bool HasSent { get { return hasSent_; } }
    }
}
