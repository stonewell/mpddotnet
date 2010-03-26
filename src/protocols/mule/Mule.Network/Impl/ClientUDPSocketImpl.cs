using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Mpd.Utilities;
using Mule.Core;
using Mpd.Generic.IO;
using Mpd.Generic;
using Mule.File;
using System.Net.Sockets;

namespace Mule.Network.Impl
{
    class UDPPack
    {
        public UDPPack()
        {
        }

        public Packet packet;
        public uint dwIP;
        public ushort nPort;
        public uint dwTime;
        public bool bEncrypt;
        public bool bKad;
        public uint nReceiverVerifyKey;
        public byte[] pachTargetClientHashORKadID = new byte[16];
    };

    class ClientUDPSocketImpl : EncryptedDatagramSocketImpl, ClientUDPSocket
    {
        #region Fields
        private ushort port_;

        private List<UDPPack> controlpacketQueue_ = new List<UDPPack>();

        private object sendLocker_ = new object(); // ZZ:UploadBandWithThrottler (UDP)
        #endregion

        #region Constructors
        public ClientUDPSocketImpl()
        {
            port_ = 0;
        }
        #endregion

        #region ClientUDPSocket Members

        public bool Create()
        {
            try
            {

                if (MuleApplication.Instance.Preference.UDPPort != 0)
                {
                    Bind(new IPEndPoint(IPAddress.Parse(MuleApplication.Instance.Preference.BindAddr),
                        MuleApplication.Instance.Preference.UDPPort));

                    // the default socket size seems to be not enough for this UDP socket
                    // because we tend to drop packets if several flow in at the same time
                    int val = 64 * 1024;
                    SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.ReceiveBuffer, val);
                }

                port_ = MuleApplication.Instance.Preference.UDPPort;

                return true;
            }
            catch (Exception ex)
            {
                MpdUtilities.DebugLogError("ClientUDPSocket Create Fail", ex);
                return false;
            }
        }

        public bool Rebind()
        {
            if (MuleApplication.Instance.Preference.UDPPort == port_)
                return false;
            Close();
            return Create();
        }

        public ushort ConnectedPort
        {
            get { throw new NotImplementedException(); }
        }

        public bool SendPacket(Packet packet, uint dwIP, ushort nPort, bool bEncrypt,
            byte[] pachTargetClientHashORKadID, bool bKad, uint nReceiverVerifyKey)
        {
            UDPPack newpending = new UDPPack();
            newpending.dwIP = dwIP;
            newpending.nPort = nPort;
            newpending.packet = packet;
            newpending.dwTime = MpdUtilities.GetTickCount();
            newpending.bEncrypt =
                bEncrypt &&
                (pachTargetClientHashORKadID != null ||
                (bKad && nReceiverVerifyKey != 0));
            newpending.bKad = bKad;
            newpending.nReceiverVerifyKey = nReceiverVerifyKey;

            if (newpending.bEncrypt && pachTargetClientHashORKadID != null)
                MpdUtilities.Md4Cpy(newpending.pachTargetClientHashORKadID, pachTargetClientHashORKadID);
            else
                MpdUtilities.Md4Clr(newpending.pachTargetClientHashORKadID);
            lock (sendLocker_)
            {
                controlpacketQueue_.Add(newpending);
            }// sendLocker.Unlock();

            MuleApplication.Instance.UploadBandwidthThrottler.QueueForSendingControlPacket(this);
            return true;
        }
        #endregion

        #region ThrottledControlSocket Members

        public SocketSentBytes SendControlData(uint maxNumberOfBytesToSend, uint minFragSize)
        {
            uint sentBytes = 0;
            lock (sendLocker_)
            {

                while (controlpacketQueue_.Count > 0 &&
                    sentBytes < maxNumberOfBytesToSend)
                {
                    UDPPack cur_packet = controlpacketQueue_[0];
                    if (MpdUtilities.GetTickCount() - cur_packet.dwTime < MuleConstants.UDPMAXQUEUETIME)
                    {
                        uint nLen = cur_packet.packet.Size + 2;
                        byte[] sendbuffer = new byte[nLen];
                        Array.Copy(cur_packet.packet.UDPHeader, sendbuffer, 2);
                        Array.Copy(cur_packet.packet.Buffer, 0, sendbuffer, 2, cur_packet.packet.Size);

                        if (cur_packet.bEncrypt && (MuleApplication.Instance.PublicIP > 0 || cur_packet.bKad))
                        {
                            nLen = (uint)EncryptSendClient(ref sendbuffer, (int)nLen,
                                cur_packet.pachTargetClientHashORKadID, cur_packet.bKad,
                                cur_packet.nReceiverVerifyKey,
                                (cur_packet.bKad ? MuleApplication.Instance.KadEngine.Preference.GetUDPVerifyKey(cur_packet.dwIP) : 0));
                        }

                        if (UDPSendTo(sendbuffer, (int)nLen, cur_packet.dwIP, cur_packet.nPort) == 0)
                        {
                            sentBytes += nLen;

                            controlpacketQueue_.RemoveAt(0);
                        }
                    }
                    else
                    {
                        controlpacketQueue_.RemoveAt(0);
                    }
                }

                if (controlpacketQueue_.Count > 0)
                {
                    MuleApplication.Instance.UploadBandwidthThrottler.QueueForSendingControlPacket(this);
                }
            }//sendLocker.Unlock();

            SocketSentBytes returnVal = new SocketSentBytes(true, 0, sentBytes);
            return returnVal;
        }

        #endregion

        #region Override
        protected override void OnClose(int nErrorCode)
        {
            base.OnClose(nErrorCode);

            MuleApplication.Instance.UploadBandwidthThrottler.RemoveAllFromQueue(this);
            controlpacketQueue_.Clear();
        }

        protected override void OnSend(int nErrorCode)
        {
            if (nErrorCode != 0)
            {
                return;
            }

            // ZZ:UploadBandWithThrottler (UDP) -.
            lock (sendLocker_)
            {
                if (controlpacketQueue_.Count > 0)
                {
                    MuleApplication.Instance.UploadBandwidthThrottler.QueueForSendingControlPacket(this);
                }
            }// sendLocker.Unlock();
            // <-- ZZ:UploadBandWithThrottler (UDP)
        }

        protected override void OnReceive(int nErrorCode)
        {

            byte[] buffer = new byte[5000];
            EndPoint endPoint = null;

            int nRealLen = ReceiveFrom(buffer, ref endPoint);
            IPEndPoint ipEndPoint = endPoint as IPEndPoint;

            uint dwIP = 0;

            if (ipEndPoint != null)
                dwIP = BitConverter.ToUInt32(ipEndPoint.Address.GetAddressBytes(), 0);
            if (ipEndPoint != null &&
                !(MuleApplication.Instance.IPFilter.IsFiltered(dwIP) ||
                MuleApplication.Instance.ClientList.IsBannedClient(dwIP)))
            {
                byte[] pBuffer;
                uint nReceiverVerifyKey;
                uint nSenderVerifyKey;

                int nPacketLen = DecryptReceivedClient(buffer, nRealLen,
                    out pBuffer, dwIP,
                    out nReceiverVerifyKey, out nSenderVerifyKey);

                if (nPacketLen >= 1)
                {
                    try
                    {
                        switch (pBuffer[0])
                        {
                            case MuleConstants.PROTOCOL_EMULEPROT:
                                {
                                    if (nPacketLen >= 2)
                                        ProcessPacket(pBuffer, 2, (uint)nPacketLen - 2,
                                            pBuffer[1], dwIP,
                                            (ushort)IPAddress.NetworkToHostOrder(ipEndPoint.Port));
                                    else
                                        throw new MuleException("eMule packet too short");
                                    break;
                                }
                            case MuleConstants.PROTOCOL_KADEMLIAPACKEDPROT:
                                {
                                    MuleApplication.Instance.Statistics.AddDownDataOverheadKad((uint)nPacketLen);
                                    if (nPacketLen >= 2)
                                    {
                                        byte[] unpack = null;
                                        byte[] unpackTmp = null;

                                        if (MpdUtilities.Decompress(pBuffer, 2, (uint)nPacketLen - 2, out unpackTmp))
                                        {
                                            unpack = new byte[unpackTmp.Length + 2];
                                            Array.Copy(unpackTmp, 0, unpack, 2, unpackTmp.Length);

                                            unpack[0] = MuleConstants.PROTOCOL_KADEMLIAHEADER;
                                            unpack[1] = pBuffer[1];
                                            try
                                            {
                                                MuleApplication.Instance.KadEngine.ProcessPacket(unpack,
                                                    (uint)unpack.Length,
                                                    dwIP, (ushort)IPAddress.NetworkToHostOrder(ipEndPoint.Port),
                                                    MuleApplication.Instance.KadEngine.Preference.GetUDPVerifyKey(dwIP) == nReceiverVerifyKey,
                                                    MuleApplication.Instance.KadObjectManager.CreateKadUDPKey(nSenderVerifyKey,
                                                        (uint)MuleApplication.Instance.GetPublicIP(false)));
                                            }
                                            catch
                                            {
                                                throw;
                                            }
                                        }
                                        else
                                        {
                                            throw new MuleException("Failed to uncompress Kad packet!");
                                        }
                                    }
                                    else
                                        throw new MuleException("Kad packet (compressed) too short");
                                    break;
                                }
                            case MuleConstants.PROTOCOL_KADEMLIAHEADER:
                                {
                                    MuleApplication.Instance.Statistics.AddDownDataOverheadKad((uint)nPacketLen);
                                    if (nPacketLen >= 2)
                                        MuleApplication.Instance.KadEngine.ProcessPacket(pBuffer, (uint)nPacketLen,
                                            dwIP, (ushort)ipEndPoint.Port,
                                            MuleApplication.Instance.KadEngine.Preference.GetUDPVerifyKey(dwIP) == nReceiverVerifyKey,
                                            MuleApplication.Instance.KadObjectManager.CreateKadUDPKey(nSenderVerifyKey,
                                                (uint)MuleApplication.Instance.GetPublicIP(false)));
                                    else
                                        throw new MuleException("Kad packet too short");
                                    break;
                                }
                            default:
                                {
                                    throw new MuleException(string.Format("Unknown protocol 0x{0}", pBuffer[0]));
                                }
                        }
                    }
                    catch (Exception error)
                    {
                        MpdUtilities.DebugLogError(error);
                    }
                }
            }
        }
        #endregion

        #region Protected
        private bool ProcessPacket(byte[] packet, uint size, byte opcode, uint ip, ushort port)
        {
            return ProcessPacket(packet, 0, size, opcode, ip, port);
        }

        private bool ProcessPacket(byte[] packet, uint offset, uint size,
            byte opcode, uint ip, ushort port)
        {
            switch ((OperationCodeEnum)opcode)
            {
                case OperationCodeEnum.OP_REASKCALLBACKUDP:
                    {
                        MuleApplication.Instance.Statistics.AddDownDataOverheadOther(size);
                        UpDownClient buddy = MuleApplication.Instance.ClientList.Buddy;
                        if (buddy != null)
                        {
                            if (size < 17 || buddy.ClientSocket == null)
                                break;
                            if (MpdUtilities.Md4Cmp(packet, (int)offset, buddy.BuddyID, 0) == 0)
                            {
                                Array.Copy(BitConverter.GetBytes(ip), 0,
                                    packet, offset + 10, 4);
                                Array.Copy(BitConverter.GetBytes(port), 0,
                                    packet, offset + 14, 2);
                                Packet response =
                                    MuleApplication.Instance.NetworkObjectManager.CreatePacket(MuleConstants.PROTOCOL_EMULEPROT);
                                response.OperationCode = OperationCodeEnum.OP_REASKCALLBACKTCP;
                                response.Buffer = new byte[size];
                                Array.Copy(packet, offset + 10, response.Buffer, 0, size - 10);
                                response.Size = size - 10;
                                MuleApplication.Instance.Statistics.AddUpDataOverheadFileRequest(response.Size);
                                buddy.SendPacket(response, true);
                            }
                        }
                        break;
                    }
                case OperationCodeEnum.OP_REASKFILEPING:
                    {
                        MuleApplication.Instance.Statistics.AddDownDataOverheadFileRequest(size);
                        SafeMemFile data_in = MpdObjectManager.CreateSafeMemFile(packet, offset, size);
                        byte[] reqfilehash = new byte[16];
                        data_in.ReadHash16(reqfilehash);
                        KnownFile reqfile = MuleApplication.Instance.SharedFiles.GetFileByID(reqfilehash);

                        bool bSenderMultipleIpUnknown = false;
                        UpDownClient sender =
                            MuleApplication.Instance.UploadQueue.GetWaitingClientByIP_UDP(ip, port,
                                true, ref bSenderMultipleIpUnknown);

                        if (reqfile == null)
                        {
                            Packet response =
                                MuleApplication.Instance.NetworkObjectManager.CreatePacket(OperationCodeEnum.OP_FILENOTFOUND,
                                0, MuleConstants.PROTOCOL_EMULEPROT);
                            MuleApplication.Instance.Statistics.AddUpDataOverheadFileRequest(response.Size);
                            if (sender != null)
                                SendPacket(response, ip, port, sender.ShouldReceiveCryptUDPPackets,
                                    sender.UserHash, false, 0);
                            else
                                SendPacket(response, ip, port, false, null, false, 0);
                            break;
                        }

                        if (sender != null)
                        {
                            //Make sure we are still thinking about the same file
                            if (MpdUtilities.Md4Cmp(reqfilehash, sender.UploadFileID) == 0)
                            {
                                sender.AddAskedCount();
                                sender.SetLastUpRequest();
                                //I messed up when I first added extended info to UDP
                                //I should have originally used the entire ProcessExtenedInfo the first time.
                                //So now I am forced to check UDPVersion to see if we are sending all the extended info.
                                //For now on, we should not have to change anything here if we change
                                //anything to the extended info data as this will be taken care of in ProcessExtendedInfo()
                                //Update extended info. 
                                if (sender.UDPVersion > 3)
                                {
                                    sender.ProcessExtendedInfo(data_in, reqfile);
                                }
                                //Update our complete source counts.
                                else if (sender.UDPVersion > 2)
                                {
                                    ushort nCompleteCountLast = sender.UpCompleteSourcesCount;
                                    ushort nCompleteCountNew = data_in.ReadUInt16();
                                    sender.UpCompleteSourcesCount = nCompleteCountNew;
                                    if (nCompleteCountLast != nCompleteCountNew)
                                    {
                                        reqfile.UpdatePartsInfo();
                                    }
                                }
                                SafeMemFile data_out = MpdObjectManager.CreateSafeMemFile(128);
                                if (sender.UDPVersion > 3)
                                {
                                    if (reqfile.IsPartFile)
                                        ((PartFile)reqfile).WritePartStatus(data_out);
                                    else
                                        data_out.WriteUInt16(0);
                                }
                                data_out.WriteUInt16((ushort)(MuleApplication.Instance.UploadQueue.GetWaitingPosition(sender)));
                                Packet response =
                                    MuleApplication.Instance.NetworkObjectManager.CreatePacket(data_out,
                                    MuleConstants.PROTOCOL_EMULEPROT);
                                response.OperationCode = OperationCodeEnum.OP_REASKACK;
                                MuleApplication.Instance.Statistics.AddUpDataOverheadFileRequest(response.Size);
                                SendPacket(response, ip, port, sender.ShouldReceiveCryptUDPPackets,
                                    sender.UserHash, false, 0);
                            }
                        }
                        else
                        {
                            // Don't answer him. We probably have him on our queue already, but can't locate him. Force him to establish a TCP connection
                            if (!bSenderMultipleIpUnknown)
                            {
                                if (((uint)MuleApplication.Instance.UploadQueue.WaitingUserCount + 50) >
                                    MuleApplication.Instance.Preference.QueueSize)
                                {
                                    Packet response =
                                        MuleApplication.Instance.NetworkObjectManager.CreatePacket(OperationCodeEnum.OP_QUEUEFULL,
                                            0, MuleConstants.PROTOCOL_EMULEPROT);
                                    MuleApplication.Instance.Statistics.AddUpDataOverheadFileRequest(response.Size);
                                    SendPacket(response, ip, port, false, null, false, 0); // we cannot answer this one encrypted since we dont know this client
                                }
                            }
                        }
                        break;
                    }
                case OperationCodeEnum.OP_QUEUEFULL:
                    {
                        MuleApplication.Instance.Statistics.AddDownDataOverheadFileRequest(size);
                        UpDownClient sender = MuleApplication.Instance.DownloadQueue.GetDownloadClientByIP_UDP(ip, port, true);
                        if (sender != null && sender.UDPPacketPending)
                        {
                            sender.IsRemoteQueueFull = true;
                            sender.UDPReaskACK(0);
                        }
                        break;
                    }
                case OperationCodeEnum.OP_REASKACK:
                    {
                        MuleApplication.Instance.Statistics.AddDownDataOverheadFileRequest(size);
                        UpDownClient sender =
                            MuleApplication.Instance.DownloadQueue.GetDownloadClientByIP_UDP(ip, port, true);
                        if (sender != null && sender.UDPPacketPending)
                        {
                            SafeMemFile data_in = MpdObjectManager.CreateSafeMemFile(packet, size);
                            if (sender.UDPVersion > 3)
                            {
                                sender.ProcessFileStatus(true, data_in, sender.RequestFile);
                            }
                            ushort nRank = data_in.ReadUInt16();
                            sender.IsRemoteQueueFull = false;
                            sender.UDPReaskACK(nRank);
                            sender.AddAskedCountDown();
                        }

                        break;
                    }
                case OperationCodeEnum.OP_FILENOTFOUND:
                    {
                        MuleApplication.Instance.Statistics.AddDownDataOverheadFileRequest(size);
                        UpDownClient sender =
                            MuleApplication.Instance.DownloadQueue.GetDownloadClientByIP_UDP(ip, port, true);
                        if (sender != null && sender.UDPPacketPending)
                        {
                            sender.UDPReaskFNF(); // may delete 'sender'!
                            sender = null;
                        }

                        break;
                    }
                case OperationCodeEnum.OP_PORTTEST:
                    {
                        MuleApplication.Instance.Statistics.AddDownDataOverheadOther(size);
                        if (size == 1)
                        {
                            if (packet[0] == 0x12)
                            {
                                bool ret = MuleApplication.Instance.ListenSocket.SendPortTestReply('1', true);
                            }
                        }
                        break;
                    }
                case OperationCodeEnum.OP_DIRECTCALLBACKREQ:
                    {
                        if (!MuleApplication.Instance.ClientList.AllowCalbackRequest(ip))
                        {
                            break;
                        }
                        // do we accept callbackrequests at all?
                        if (MuleApplication.Instance.KadEngine.IsRunning &&
                            MuleApplication.Instance.KadEngine.IsFirewalled)
                        {
                            MuleApplication.Instance.ClientList.AddTrackCallbackRequests(ip);
                            SafeMemFile data = MpdObjectManager.CreateSafeMemFile(packet, size);
                            ushort nRemoteTCPPort = data.ReadUInt16();
                            byte[] uchUserHash = new byte[16];
                            data.ReadHash16(uchUserHash);
                            byte byConnectOptions = data.ReadUInt8();
                            UpDownClient pRequester =
                                MuleApplication.Instance.ClientList.FindClientByUserHash(uchUserHash, ip,
                                    nRemoteTCPPort);
                            if (pRequester == null)
                            {
                                pRequester =
                                    MuleApplication.Instance.CoreObjectManager.CreateUpDownClient(null,
                                        nRemoteTCPPort, ip, 0, 0, true);
                                pRequester.UserHash = uchUserHash;
                                MuleApplication.Instance.ClientList.AddClient(pRequester);
                            }
                            pRequester.SetConnectOptions(byConnectOptions, true, false);
                            pRequester.DoesDirectUDPCallbackSupport = false;
                            pRequester.IP = ip;
                            pRequester.UserPort = nRemoteTCPPort;
                            pRequester.TryToConnect();
                        }

                        break;
                    }
                default:
                    MuleApplication.Instance.Statistics.AddDownDataOverheadOther(size);
                    return false;
            }
            return true;
        }

        #endregion

        #region Private
        private int UDPSendTo(byte[] lpBuf, int nBufLen, uint dwIP, ushort nPort)
        {
            int result = base.SendTo(lpBuf, nBufLen,
                SocketFlags.None,
                new IPEndPoint((long)dwIP, (int)nPort));
            return 0;
        }
        #endregion
    }
}
