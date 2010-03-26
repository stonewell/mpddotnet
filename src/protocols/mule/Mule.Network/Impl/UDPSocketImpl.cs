using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Mule.ED2K;
using Mpd.Utilities;
using System.Diagnostics;
using Mpd.Generic.IO;
using Mpd.Generic;
using Mule.File;
using System.Net.Sockets;

namespace Mule.Network.Impl
{
    struct ServerUDPPacket
    {
        public byte[] packet;
        public int size;
        public uint dwIP;
        public ushort nPort;
    };


    class UDPSocketImpl : EncryptedDatagramSocketImpl, UDPSocket
    {
        #region Fields
        private List<ServerUDPPacket> controlpacketQueue_ = new List<ServerUDPPacket>();
        private object sendLocker_ = new object();
        #endregion

        #region UDPSocket Members

        public void SendPacket(Packet packet, Mule.ED2K.ED2KServer host)
        {
            SendPacket(packet, host, 0, null, 0);
        }

        public void SendPacket(Packet packet, Mule.ED2K.ED2KServer host, ushort nSpecialPort)
        {
            SendPacket(packet, host, nSpecialPort, null, 0);
        }

        public void SendPacket(Packet packet, Mule.ED2K.ED2KServer host, ushort nSpecialPort, byte[] pRawPacket)
        {
            SendPacket(packet, host, nSpecialPort, pRawPacket, 0);
        }

        public void SendPacket(Packet packet,
            Mule.ED2K.ED2KServer pServer,
            ushort nSpecialPort, byte[] pInRawPacket, uint nRawLen)
        {
            // Create raw UDP packet
            byte[] pRawPacket;
            uint uRawPacketSize;
            ushort nPort = 0;
            if (packet != null)
            {
                pRawPacket = new byte[packet.Size + 2];
                Array.Copy(packet.UDPHeader, pRawPacket, 2);
                Array.Copy(packet.Buffer, 0, pRawPacket, 2, packet.Size);
                uRawPacketSize = packet.Size + 2;
                if (MuleApplication.Instance.Preference.IsServerCryptLayerUDPEnabled &&
                    pServer.ServerKeyUDP != 0 &&
                    pServer.DoesSupportsObfuscationUDP)
                {
                    uRawPacketSize = (uint)EncryptSendServer(ref pRawPacket, (int)uRawPacketSize, pServer.ServerKeyUDP);
                    nPort = pServer.ObfuscationPortUDP;
                }
                else
                    nPort = (ushort)(pServer.Port + 4);
            }
            else if (pInRawPacket != null)
            {
                // we don't encrypt rawpackets (!)
                pRawPacket = new byte[nRawLen];
                Array.Copy(pInRawPacket, pRawPacket, nRawLen);
                uRawPacketSize = nRawLen;
                nPort = (ushort)(pServer.Port + 4);
            }
            else
            {
                Debug.Assert(false);
                return;
            }
            nPort = (nSpecialPort == 0) ? nPort : nSpecialPort;
            Debug.Assert(nPort != 0);

            // Do we need to resolve the DN of this server?
            IPAddress ipAddr = IPAddress.None;

            IPAddress.TryParse(pServer.Address, out ipAddr);
            if (ipAddr == IPAddress.None)
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(pServer.Address);
                // Get the IP value
                uint nIP = 0;
                if (hostEntry != null && hostEntry.AddressList.Length > 0)
                {
                    nIP = BitConverter.ToUInt32(hostEntry.AddressList[0].GetAddressBytes(), 0);
                }

                if (nIP != 0)
                {
                    bool bRemoveServer = false;
                    if (!MpdUtilities.IsGoodIP(nIP))
                    {
                        // However, if we are currently connected to a "not-good-ip", that IP can't
                        // be that bad -- may only happen when debugging in a LAN.
                        ED2KServer pConnectedServer = MuleApplication.Instance.ServerConnect.CurrentServer;
                        if (pConnectedServer != null || pConnectedServer.IP != nIP)
                        {
                            bRemoveServer = true;
                        }
                    }
                    if (!bRemoveServer && MuleApplication.Instance.Preference.IsFilterServerByIP &&
                        MuleApplication.Instance.IPFilter.IsFiltered(nIP))
                    {
                        bRemoveServer = true;
                    }

                    ED2KServer pTempServer =
                        MuleApplication.Instance.ServerList.GetServerByAddress(pServer.Address, pServer.Port);
                    if (pTempServer != null)
                    {
                        pServer.IP = nIP;
                        // If we already have entries in the server list (dynIP-servers without a DN)
                        // with the same IP as this dynIP-server, remove the duplicates.
                        MuleApplication.Instance.ServerList.RemoveDuplicatesByIP(pServer);
                    }

                    if (bRemoveServer)
                    {
                        return;
                    }

                    // Send all of the queued packets for this server.
                    SendBuffer(nIP, nPort, pRawPacket, uRawPacketSize);
                }
            }
            else
            {
                // No DNS query needed for this server. Just send the packet.
                SendBuffer(BitConverter.ToUInt32(ipAddr.GetAddressBytes(), 0),
                    nPort, pRawPacket, uRawPacketSize);
            }
        }

        public bool Create()
        {
            if (!base.CreateSocket(new IPEndPoint(IPAddress.Any, 0).AddressFamily, 
                SocketType.Dgram, 
                ProtocolType.Udp))
                return false;

            if (MuleApplication.Instance.Preference.ServerUDPPort != 0)
            {
                Bind(new IPEndPoint(IPAddress.Parse(MuleApplication.Instance.Preference.BindAddr),
                    MuleApplication.Instance.Preference.ServerUDPPort == 0xFFFF ? 0 :
                        MuleApplication.Instance.Preference.ServerUDPPort));

                return true;
            }

            return false;
        }
        #endregion

        #region ThrottledControlSocket Members

        public SocketSentBytes SendControlData(uint maxNumberOfBytesToSend, uint minFragSize)
        {
            // ZZ:UploadBandWithThrottler (UDP) -.
            // NOTE: *** This function is invoked from a *different* thread!
            uint sentBytes = 0;
            lock (sendLocker_)
            {
                // <-- ZZ:UploadBandWithThrottler (UDP)
                while (controlpacketQueue_.Count > 0 && 
                    sentBytes < maxNumberOfBytesToSend) // ZZ:UploadBandWithThrottler (UDP)
                {
                    ServerUDPPacket packet = controlpacketQueue_[0];
                    int sendSuccess = UDPSendTo(packet.packet, packet.size, packet.dwIP, packet.nPort);
                    if (sendSuccess >= 0)
                    {
                        if (sendSuccess > 0)
                        {
                            sentBytes += (uint)packet.size; // ZZ:UploadBandWithThrottler (UDP)
                        }

                        controlpacketQueue_.RemoveAt(0);
                    }
                }

                // ZZ:UploadBandWithThrottler (UDP) -.
                if (controlpacketQueue_.Count > 0)
                {
                    MuleApplication.Instance.UploadBandwidthThrottler.QueueForSendingControlPacket(this);
                }
            }//sendLocker.Unlock();

            SocketSentBytes returnVal = new SocketSentBytes(true, 0, sentBytes);
            return returnVal;
            // <-- ZZ:UploadBandWithThrottler (UDP)
        }

        #endregion

        #region Overrides
        protected override void OnClose(int nErrorCode)
        {
            base.OnClose(nErrorCode);
            MuleApplication.Instance.UploadBandwidthThrottler.RemoveFromAllQueues(this);
            controlpacketQueue_.Clear();
        }

        protected override void OnReceive(int nErrorCode)
        {
            byte[] buffer = new byte[5000];
            byte[] pBuffer = buffer;
            EndPoint endpoint = null;

            int length = ReceiveFrom(buffer, ref endpoint);

            if (length != SOCKET_ERROR)
            {
                IPEndPoint ipEndPoint = endpoint as IPEndPoint;
                int nPayLoadLen = length;
                ED2KServer pServer =
                    MuleApplication.Instance.ServerList.GetServerByIPUDP(BitConverter.ToUInt32(ipEndPoint.Address.GetAddressBytes(), 0),
                        (ushort)IPAddress.NetworkToHostOrder(ipEndPoint.Port), true);
                if (pServer != null && MuleApplication.Instance.Preference.IsServerCryptLayerUDPEnabled &&
                    ((pServer.ServerKeyUDP != 0 && pServer.DoesSupportsObfuscationUDP) ||
                    (pServer.CryptPingReplyPending && pServer.Challenge != 0)))
                {
                    // TODO
                    uint dwKey = 0;
                    if (pServer.CryptPingReplyPending && pServer.Challenge != 0 /* && pServer.GetPort() == ntohs(sockAddr.sin_port) - 12 */)
                        dwKey = pServer.Challenge;
                    else
                        dwKey = pServer.ServerKeyUDP;

                    Debug.Assert(dwKey != 0);
                    nPayLoadLen = DecryptReceivedServer(buffer, length, out pBuffer, dwKey,
                        BitConverter.ToUInt32(ipEndPoint.Address.GetAddressBytes(), 0));
                }

                if (pBuffer[0] == MuleConstants.PROTOCOL_EDONKEYPROT)
                    ProcessPacket(pBuffer, 2, nPayLoadLen - 2, pBuffer[1],
                        BitConverter.ToUInt32(ipEndPoint.Address.GetAddressBytes(), 0),
                        (ushort)IPAddress.NetworkToHostOrder(ipEndPoint.Port));
            }
            else
            {
                IPEndPoint ipEndPoint = endpoint as IPEndPoint;

                if (ipEndPoint != null)
                {
                    ED2KServer pServer =
                        MuleApplication.Instance.ServerList.GetServerByIPUDP(BitConverter.ToUInt32(ipEndPoint.Address.GetAddressBytes(), 0),
                        (ushort)IPAddress.NetworkToHostOrder(ipEndPoint.Port), true);
                    if (pServer != null &&
                        !pServer.CryptPingReplyPending &&
                        MpdUtilities.GetTickCount() - pServer.LastPinged >= MuleConstants.ONE_SEC_MS * 30)
                    {
                        pServer.AddFailedCount();
                    }
                }
            }
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
        #endregion

        #region Privates
        private void SendBuffer(uint nIP, ushort nPort, byte[] pPacket, uint uSize)
        {
            // ZZ:UploadBandWithThrottler (UDP) -.
            ServerUDPPacket newpending = new ServerUDPPacket();
            newpending.dwIP = nIP;
            newpending.nPort = nPort;
            newpending.packet = pPacket;
            newpending.size = (int)uSize;
            lock (sendLocker_)
            {
                controlpacketQueue_.Add(newpending);
            }// sendLocker.Unlock();
            MuleApplication.Instance.UploadBandwidthThrottler.QueueForSendingControlPacket(this);
            // <-- ZZ:UploadBandWithThrottler (UDP)
        }

        private bool ProcessPacket(byte[] buffer, int offset, int size, uint opcode, uint nIP, ushort nUDPPort)
        {
            try
            {
                MuleApplication.Instance.Statistics.AddDownDataOverheadServer((uint)size);
                ED2KServer pServer = MuleApplication.Instance.ServerList.GetServerByIPUDP(nIP, nUDPPort, true);
                if (pServer != null)
                {
                    pServer.ResetFailedCount();
                }

                switch ((OperationCodeEnum)opcode)
                {
                    case OperationCodeEnum.OP_GLOBSEARCHRES:
                        {
                            SafeMemFile data = MpdObjectManager.CreateSafeMemFile(buffer, size);
                            // process all search result packets
                            int iLeft;
                            do
                            {
                                uint uResultCount = MuleApplication.Instance.SearchList.ProcessUDPSearchAnswer(data, true/*pServer.GetUnicodeSupport()*/, nIP, nUDPPort - 4);

                                // check if there is another source packet
                                iLeft = (int)(data.Length - data.Position);
                                if (iLeft >= 2)
                                {
                                    byte protocol = data.ReadUInt8();
                                    iLeft--;
                                    if (protocol != MuleConstants.PROTOCOL_EDONKEYPROT)
                                    {
                                        data.Seek(-1, System.IO.SeekOrigin.Current);
                                        iLeft += 1;
                                        break;
                                    }

                                    byte opcode1 = data.ReadUInt8();
                                    iLeft--;
                                    if (opcode1 != (byte)OperationCodeEnum.OP_GLOBSEARCHRES)
                                    {
                                        data.Seek(-2, System.IO.SeekOrigin.Current);
                                        iLeft += 2;
                                        break;
                                    }
                                }
                            }
                            while (iLeft > 0);
                            break;
                        }
                    case OperationCodeEnum.OP_GLOBFOUNDSOURCES:
                        {
                            SafeMemFile data = MpdObjectManager.CreateSafeMemFile(buffer, size);
                            // process all source packets
                            int iLeft;
                            do
                            {
                                byte[] fileid = new byte[16];
                                data.ReadHash16(fileid);
                                PartFile file = MuleApplication.Instance.DownloadQueue.GetFileByID(fileid);
                                if (file != null)
                                    file.AddSources(data, nIP, (ushort)(nUDPPort - 4), false);
                                else
                                {
                                    // skip sources for that file
                                    uint count = data.ReadUInt8();
                                    data.Seek(count * (4 + 2), System.IO.SeekOrigin.Current);
                                }

                                // check if there is another source packet
                                iLeft = (int)(data.Length - data.Position);
                                if (iLeft >= 2)
                                {
                                    byte protocol = data.ReadUInt8();
                                    iLeft--;
                                    if (protocol != MuleConstants.PROTOCOL_EDONKEYPROT)
                                    {
                                        data.Seek(-1, System.IO.SeekOrigin.Current);
                                        iLeft += 1;
                                        break;
                                    }

                                    byte opcode1 = data.ReadUInt8();
                                    iLeft--;
                                    if (opcode1 != (byte)OperationCodeEnum.OP_GLOBFOUNDSOURCES)
                                    {
                                        data.Seek(-2, System.IO.SeekOrigin.Current);
                                        iLeft += 2;
                                        break;
                                    }
                                }
                            }
                            while (iLeft > 0);

                            break;
                        }
                    case OperationCodeEnum.OP_GLOBSERVSTATRES:
                        {
                            if (size < 12 || pServer == null)
                                return true;
                            uint challenge = BitConverter.ToUInt32(buffer, 0);
                            if (challenge != pServer.Challenge)
                            {
                                return true;
                            }
                            if (pServer != null)
                            {
                                pServer.Challenge = 0;
                                pServer.CryptPingReplyPending = false;
                                uint tNow = MpdUtilities.Time();
                                Random rand = new Random();
                                // if we used Obfuscated ping, we still need to reset the time properly
                                pServer.LastPingedTime =
                                    Convert.ToUInt32(tNow - (rand.Next() % MuleConstants.ONE_HOUR_SEC));
                            }
                            uint cur_user = BitConverter.ToUInt32(buffer, 4);
                            uint cur_files = BitConverter.ToUInt32(buffer, 8);
                            uint cur_maxusers = 0;
                            uint cur_softfiles = 0;
                            uint cur_hardfiles = 0;
                            uint uUDPFlags = 0;
                            uint uLowIDUsers = 0;
                            uint dwServerUDPKey = 0;
                            ushort nTCPObfuscationPort = 0;
                            ushort nUDPObfuscationPort = 0;

                            if (size >= 16)
                            {
                                cur_maxusers = BitConverter.ToUInt32(buffer, 12);
                            }
                            if (size >= 24)
                            {
                                cur_softfiles = BitConverter.ToUInt32(buffer, 16);
                                cur_hardfiles = BitConverter.ToUInt32(buffer, 20);
                            }
                            if (size >= 28)
                            {
                                uUDPFlags = BitConverter.ToUInt32(buffer, 24);
                            }
                            if (size >= 32)
                            {
                                uLowIDUsers = BitConverter.ToUInt32(buffer, 28);
                            }
                            if (size >= 40)
                            {
                                // TODO debug check if this packet was encrypted if it has a key
                                nUDPObfuscationPort = BitConverter.ToUInt16(buffer, 32);
                                nTCPObfuscationPort = BitConverter.ToUInt16(buffer, 34); ;
                                dwServerUDPKey = BitConverter.ToUInt32(buffer, 36);
                            }
                            if (pServer != null)
                            {
                                pServer.Ping = MpdUtilities.GetTickCount() - pServer.LastPinged;
                                pServer.UserCount = cur_user;
                                pServer.FileCount = cur_files;
                                pServer.MaxUsers = cur_maxusers;
                                pServer.SoftFiles = cur_softfiles;
                                pServer.HardFiles = cur_hardfiles;
                                pServer.ServerKeyUDP = dwServerUDPKey;
                                pServer.ObfuscationPortTCP = nTCPObfuscationPort;
                                pServer.ObfuscationPortUDP = nUDPObfuscationPort;
                                // if the received UDP flags do not match any already stored UDP flags, 
                                // reset the server version string because the version (which was determined by last connecting to
                                // that server) is most likely not accurat any longer.
                                // this may also give 'false' results because we don't know the UDP flags when connecting to a server
                                // with TCP.
                                //if (pServer.GetUDPFlags() != uUDPFlags)
                                //	pServer.Version(_T = "");
                                pServer.UDPFlags = (ED2KServerUdpFlagsEnum)uUDPFlags;
                                pServer.LowIDUsers = uLowIDUsers;

                                pServer.SetLastDescPingedCount(false);
                                if (pServer.LastDescPingedCount < 2)
                                {
                                    // eserver 16.45+ supports a new OP_SERVER_DESC_RES answer, if the OP_SERVER_DESC_REQ contains a uint
                                    // challenge, the server returns additional info with OP_SERVER_DESC_RES. To properly distinguish the
                                    // old and new OP_SERVER_DESC_RES answer, the challenge has to be selected carefully. The first 2 bytes 
                                    // of the challenge (in network byte order) MUST NOT be a valid string-len-int16!
                                    Packet packet1 =
                                        MuleApplication.Instance.NetworkObjectManager.CreatePacket(OperationCodeEnum.OP_SERVER_DESC_REQ, 4);
                                    uint uDescReqChallenge =
                                        ((uint)MpdUtilities.GetRandomUInt16() << 16) +
                                        MuleConstants.INV_SERV_DESC_LEN; // 0xF0FF = an 'invalid' string length.
                                    pServer.DescReqChallenge = uDescReqChallenge;
                                    Array.Copy(BitConverter.GetBytes(uDescReqChallenge), packet1.Buffer, 4);
                                    MuleApplication.Instance.Statistics.AddUpDataOverheadServer(packet1.Size);
                                    MuleApplication.Instance.ServerConnect.SendUDPPacket(packet1, pServer, true);
                                }
                                else
                                {
                                    pServer.SetLastDescPingedCount(true);
                                }
                            }
                            break;
                        }

                    case OperationCodeEnum.OP_SERVER_DESC_RES:
                        {
                            if (pServer == null)
                                return true;

                            // old packet: <name_len 2><name name_len><desc_len 2 desc_en>
                            // new packet: <challenge 4><taglist>
                            //
                            // NOTE: To properly distinguish between the two packets which are both useing the same opcode...
                            // the first two bytes of <challenge> (in network byte order) have to be an invalid <name_len> at least.

                            SafeMemFile srvinfo = MpdObjectManager.CreateSafeMemFile(buffer, size);
                            if (size >= 8 && BitConverter.ToUInt16(buffer, 0) == MuleConstants.INV_SERV_DESC_LEN)
                            {
                                if (pServer.DescReqChallenge != 0 && BitConverter.ToUInt32(buffer, 0) == pServer.DescReqChallenge)
                                {
                                    pServer.DescReqChallenge = 0;
                                    srvinfo.ReadUInt32(); // skip challenge
                                    uint uTags = srvinfo.ReadUInt32();
                                    for (uint i = 0; i < uTags; i++)
                                    {
                                        Tag tag = MpdObjectManager.CreateTag(srvinfo, true/*pServer.GetUnicodeSupport()*/);
                                        if (tag.NameID == MuleConstants.ST_SERVERNAME && tag.IsStr)
                                            pServer.ListName = tag.Str;
                                        else if (tag.NameID == MuleConstants.ST_DESCRIPTION && tag.IsStr)
                                            pServer.Description = tag.Str;
                                        else if (tag.NameID == MuleConstants.ST_DYNIP && tag.IsStr)
                                        {
                                            // Verify that we really received a DN.
                                            IPAddress address;


                                            if (!IPAddress.TryParse(tag.Str, out address) ||
                                                address == IPAddress.None)
                                            {
                                                string strOldDynIP = pServer.DynIP;
                                                pServer.DynIP = tag.Str;
                                                // If a dynIP-server changed its address or, if this is the
                                                // first time we get the dynIP-address for a server which we
                                                // already have as non-dynIP in our list, we need to remove
                                                // an already available server with the same 'dynIP:port'.
                                                if (string.Compare(strOldDynIP, pServer.DynIP, true) != 0)
                                                    MuleApplication.Instance.ServerList.RemoveDuplicatesByAddress(pServer);
                                            }
                                        }
                                        else if (tag.NameID == MuleConstants.ST_VERSION && tag.IsStr)
                                            pServer.Version = tag.Str;
                                        else if (tag.NameID == MuleConstants.ST_VERSION && tag.IsInt)
                                        {
                                            pServer.Version =
                                                string.Format("{0}.{1}", tag.Int >> 16, tag.Int & 0xFFFF);
                                        }
                                    }
                                }
                                else
                                {
                                    // A server sent us a new server description packet (including a challenge) although we did not
                                    // ask for it. This may happen, if there are multiple servers running on the same machine with
                                    // multiple IPs. If such a server is asked for a description, the server will answer 2 times,
                                    // but with the same IP.
                                }
                            }
                            else
                            {
                                string strName = srvinfo.ReadString(true/*pServer.GetUnicodeSupport()*/);
                                string strDesc = srvinfo.ReadString(true/*pServer.GetUnicodeSupport()*/);
                                pServer.Description = strDesc;
                                pServer.ListName = strName;
                            }

                            break;
                        }
                    default:
                        return false;
                }

                return true;
            }
            catch (Exception error)
            {
                ProcessPacketError((uint)size, (uint)opcode, nIP, nUDPPort, error);
                if (opcode == (byte)OperationCodeEnum.OP_GLOBSEARCHRES ||
                    opcode == (byte)OperationCodeEnum.OP_GLOBFOUNDSOURCES)
                    return true;
            }
            return false;
        }

        private void ProcessPacketError(uint size, uint opcode, uint nIP, ushort nUDPPort,
            Exception ex)
        {
            string strName = string.Empty;
            ED2KServer pServer = MuleApplication.Instance.ServerList.GetServerByIPUDP(nIP, nUDPPort);
            if (pServer != null)
                strName = " (" + pServer.ListName + ")";
            MpdUtilities.DebugLogWarning(false,
                string.Format("Error: Failed to process server UDP packet from {0}:{1}{2} opcode=0x{3} size={4} - {5}",
            MpdUtilities.IP2String(nIP), nUDPPort, strName, opcode, size, ex.Message), ex);
        }

        private int UDPSendTo(byte[] lpBuf, int nBufLen, uint dwIP, ushort nPort)
        {
            // NOTE: *** This function is invoked from a *different* thread!
            int iResult = base.SendTo(lpBuf, nBufLen,
                SocketFlags.None, new IPEndPoint(new IPAddress(dwIP), (int)nPort));
            if (iResult == SOCKET_ERROR)
            {
                return 0; // error
            }
            return 1; // success
        }
        #endregion
    }
}
