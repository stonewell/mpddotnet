using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.Network;
using Mule.ED2K;
using Mpd.Utilities;
using Mpd.Generic.IO;
using Mpd.Generic;
using System.Web.Configuration;
using System.Threading;
using System.Net;

namespace Mule.Core.Impl
{
    class ServerConnectImpl : ServerConnect
    {
        #region Fields
        private bool tryObfuscated_;
        private byte maxSimulateCons_;
        private uint startAutoConnectPos_;
        private ServerSocket connectedSocket_;
        private UDPSocket udpsocket_;
        private List<ServerSocket> openSockets_ =
            new List<ServerSocket>();	// list of currently opened sockets
        private Timer retryTimer_;
        private uint clientId_;
        private Dictionary<ulong, ServerSocket> connectionAttemps_ =
            new Dictionary<ulong, ServerSocket>();
        #endregion

        #region ServerConnect Members

        public void ConnectionFailed(Mule.Network.ServerSocket sender)
        {
            if (!IsConnecting && sender != connectedSocket_)
            {
                // just return, cleanup is done by the socket itself
                return;
            }

            ED2KServer pServer = MuleApplication.Instance.ServerList.GetServerByAddress(sender.CurrentServer.Address, sender.CurrentServer.Port);
            switch (sender.ConnectionState)
            {
                case ConnectionStateEnum.CS_FATALERROR:
                    //TODO:Log
                    break;
                case ConnectionStateEnum.CS_DISCONNECTED:
                    MuleApplication.Instance.SharedFiles.ClearED2KPublishInfo();
                    break;
                case ConnectionStateEnum.CS_SERVERDEAD:
                    if (pServer != null)
                    {
                        pServer.AddFailedCount();
                    }
                    break;
                case ConnectionStateEnum.CS_ERROR:
                    break;
                case ConnectionStateEnum.CS_SERVERFULL:
                    break;
                case ConnectionStateEnum.CS_NOTCONNECTED:
                    break;
            }

            // IMPORTANT: mark this socket not to be deleted in StopConnectionTry(),
            // because it will delete itself after this function!
            sender.IsDeleting = true;

            switch (sender.ConnectionState)
            {
                case ConnectionStateEnum.CS_FATALERROR:
                    {
                        bool autoretry = !IsSingleConnect;
                        StopConnectionTry();
                        if (MuleApplication.Instance.Preference.IsReconnect && autoretry &&
                            retryTimer_ == null)
                        {
                            // There are situations where we may get Winsock error codes which indicate
                            // that the network is down, although it is not. Those error codes may get
                            // thrown only for particular IPs. If the first server in our list has such
                            // an IP and will therefor throw such an error we would never connect to
                            // any server at all. To circumvent that, start the next auto-connection
                            // attempt with a different server (use the next server in the list).
                            startAutoConnectPos_ = 0; // default: start at 0
                            if (pServer != null)
                            {
                                // If possible, use the "next" server.
                                int iPosInList =
                                    MuleApplication.Instance.ServerList.GetPositionOfServer(pServer);
                                if (iPosInList >= 0)
                                    startAutoConnectPos_ = (uint)((iPosInList + 1) %
                                        MuleApplication.Instance.ServerList.ServerCount);
                            }

                            retryTimer_ =
                                new Timer(new TimerCallback(RetryConnectTimer),
                                    this, 0,
                            MuleConstants.ONE_SEC_MS * MuleConstants.CS_RETRYCONNECTTIME);
                        }
                        break;
                    }
                case ConnectionStateEnum.CS_DISCONNECTED:
                    {
                        MuleApplication.Instance.SharedFiles.ClearED2KPublishInfo();
                        IsConnected = false;
                        if (connectedSocket_ != null)
                        {
                            connectedSocket_.Close();
                            connectedSocket_ = null;
                        }
                        MuleApplication.Instance.Statistics.ServerConnectTime = 0;
                        MuleApplication.Instance.Statistics.Add2TotalServerDuration();
                        if (MuleApplication.Instance.Preference.IsReconnect && !IsConnecting)
                            ConnectToAnyServer();
                        break;
                    }
                case ConnectionStateEnum.CS_ERROR:
                case ConnectionStateEnum.CS_NOTCONNECTED:
                    {
                        if (!IsConnecting)
                            break;
                    }
                    goto case ConnectionStateEnum.CS_SERVERDEAD;
                case ConnectionStateEnum.CS_SERVERDEAD:
                case ConnectionStateEnum.CS_SERVERFULL:
                    {
                        if (!IsConnecting)
                            break;
                        if (IsSingleConnect)
                        {
                            if (pServer != null && sender.IsServerCryptEnabledConnection &&
                                !MuleApplication.Instance.Preference.IsClientCryptLayerRequired)
                            {
                                // try reconnecting without obfuscation
                                ConnectToServer(pServer, false, true);
                                break;
                            }
                            StopConnectionTry();
                            break;
                        }

                        Dictionary<ulong, ServerSocket>.Enumerator pos = connectionAttemps_.GetEnumerator();
                        while (pos.MoveNext())
                        {
                            if (pos.Current.Value == sender)
                            {
                                connectionAttemps_.Remove(pos.Current.Key);
                                break;
                            }
                        }
                        TryAnotherConnectionRequest();
                    }
                    break;
            }
        }

        public void ConnectionEstablished(Mule.Network.ServerSocket sender)
        {
            if (!IsConnecting)
            {
                // we are already IsConnected to another server
                DestroySocket(sender);
                return;
            }

            InitLocalIP();
            if (sender.ConnectionState == ConnectionStateEnum.CS_WAITFORLOGIN)
            {
                ED2KServer pServer =
                    MuleApplication.Instance.ServerList.GetServerByAddress(sender.CurrentServer.Address,
                    sender.CurrentServer.Port);
                if (pServer != null)
                {
                    pServer.ResetFailedCount();
                }

                // Send login packet
                SafeMemFile data = MpdObjectManager.CreateSafeMemFile(256);
                data.WriteHash16(MuleApplication.Instance.Preference.UserHash);
                data.WriteUInt32(ClientID);
                data.WriteUInt16(MuleApplication.Instance.Preference.Port);

                uint tagcount = 4;
                data.WriteUInt32(tagcount);

                Tag tagName = MpdObjectManager.CreateTag(TagTypeEnum.CT_NAME,
                    MuleApplication.Instance.Preference.UserNick);
                tagName.WriteTagToFile(data);

                Tag tagVersion = MpdObjectManager.CreateTag(TagTypeEnum.CT_VERSION, VersionsEnum.EDONKEYVERSION);
                tagVersion.WriteTagToFile(data);

                ServerFlagsEnum dwCryptFlags = 0;
                if (MuleApplication.Instance.Preference.IsClientCryptLayerSupported)
                    dwCryptFlags |= ServerFlagsEnum.SRVCAP_SUPPORTCRYPT;
                if (MuleApplication.Instance.Preference.IsClientCryptLayerRequested)
                    dwCryptFlags |= ServerFlagsEnum.SRVCAP_REQUESTCRYPT;
                if (MuleApplication.Instance.Preference.IsClientCryptLayerRequired)
                    dwCryptFlags |= ServerFlagsEnum.SRVCAP_REQUIRECRYPT;

                Tag tagFlags = MpdObjectManager.CreateTag(TagTypeEnum.CT_SERVER_FLAGS,
                    ServerFlagsEnum.SRVCAP_ZLIB | ServerFlagsEnum.SRVCAP_NEWTAGS |
                    ServerFlagsEnum.SRVCAP_LARGEFILES |
                    ServerFlagsEnum.SRVCAP_UNICODE | dwCryptFlags);

                tagFlags.WriteTagToFile(data);

                // eMule Version (14-Mar-2004: requested by lugdunummaster (need for LowID clients which have no chance 
                // to send an Hello packet to the server during the callback test))
                Tag tagMuleVersion = MpdObjectManager.CreateTag(TagTypeEnum.CT_EMULE_VERSION,
                                    (MuleApplication.Instance.Version.Major << 17) |
                                    (MuleApplication.Instance.Version.Minor << 10) |
                                    (MuleApplication.Instance.Version.Build << 7));
                tagMuleVersion.WriteTagToFile(data);

                Packet packet = MuleApplication.Instance.NetworkObjectManager.CreatePacket(data);
                packet.OperationCode = OperationCodeEnum.OP_LOGINREQUEST;
                MuleApplication.Instance.Statistics.AddUpDataOverheadServer(packet.Size);
                SendPacket(packet, true, sender);
            }
            else if (sender.ConnectionState == ConnectionStateEnum.CS_CONNECTED)
            {
                MuleApplication.Instance.Statistics.Reconnects++;
                MuleApplication.Instance.Statistics.ServerConnectTime = MpdUtilities.GetTickCount();
                IsConnected = true;
                connectedSocket_ = sender;
                StopConnectionTry();
                MuleApplication.Instance.SharedFiles.ClearED2KPublishInfo();
                MuleApplication.Instance.SharedFiles.SendListToServer();

                if (MuleApplication.Instance.Preference.DoesAddServersFromServer)
                {
                    Packet packet =
                        MuleApplication.Instance.NetworkObjectManager.CreatePacket(
                        OperationCodeEnum.OP_GETSERVERLIST, 0);
                    MuleApplication.Instance.Statistics.AddUpDataOverheadServer(packet.Size);
                    SendPacket(packet, true);
                }
            }
        }

        public void ConnectToAnyServer()
        {
            ConnectToAnyServer(0, true, true);
        }

        public void ConnectToAnyServer(uint startAt)
        {
            ConnectToAnyServer(startAt, false, true, false);
        }

        public void ConnectToAnyServer(uint startAt, bool prioSort)
        {
            ConnectToAnyServer(startAt, prioSort, true, false);
        }

        public void ConnectToAnyServer(uint startAt, bool prioSort, bool isAuto)
        {
            ConnectToAnyServer(startAt, prioSort, isAuto, false);
        }

        public void ConnectToAnyServer(uint startAt, bool prioSort, bool isAuto, bool bNoCrypt)
        {
            StopConnectionTry();
            Disconnect();
            IsConnecting = true;
            IsSingleConnect = false;
            tryObfuscated_ =
                MuleApplication.Instance.Preference.IsServerCryptLayerTCPRequested && !bNoCrypt;

            // Barry - Only auto-connect to static server option
            if (MuleApplication.Instance.Preference.DoesAutoConnectToStaticServersOnly && isAuto)
            {
                bool anystatic = false;
                ED2KServer next_server;
                MuleApplication.Instance.ServerList.ServerPosition = startAt;
                while ((next_server = MuleApplication.Instance.ServerList.GetNextServer(false)) != null)
                {
                    if (next_server.IsStaticMember)
                    {
                        anystatic = true;
                        break;
                    }
                }
                if (!anystatic)
                {
                    IsConnecting = false;
                    return;
                }
            }

            MuleApplication.Instance.ServerList.ServerPosition = startAt;
            if (MuleApplication.Instance.Preference.UseUserSortedServerList && startAt == 0 && prioSort)
                MuleApplication.Instance.ServerList.GetUserSortedServers();
            if (MuleApplication.Instance.Preference.UseServerPriorities && prioSort)
                MuleApplication.Instance.ServerList.Sort();

            if (MuleApplication.Instance.ServerList.ServerCount == 0)
            {
                IsConnecting = false;
                return;
            }
            MuleApplication.Instance.ListenSocket.Process();

            TryAnotherConnectionRequest();
        }

        public void ConnectToServer(ED2KServer toConnect)
        {
            ConnectToServer(toConnect, false, false);
        }

        public void ConnectToServer(ED2KServer toConnect, bool multiconnect)
        {
            ConnectToServer(toConnect, multiconnect, false);
        }

        public void ConnectToServer(Mule.ED2K.ED2KServer toconnect, bool multiconnect, bool bNoCrypt)
        {
            if (!multiconnect)
            {
                StopConnectionTry();
                Disconnect();
            }
            IsConnecting = true;
            IsSingleConnect = !multiconnect;

            ServerSocket newsocket =
                MuleApplication.Instance.NetworkObjectManager.CreateServerSocket(this, !multiconnect);
            openSockets_.Add(newsocket);
            newsocket.ConnectTo(toconnect, bNoCrypt);
            connectionAttemps_[MpdUtilities.GetTickCount()] = newsocket;
        }

        public void StopConnectionTry()
        {
            connectionAttemps_.Clear();
            IsConnecting = false;
            IsSingleConnect = false;

            if (retryTimer_ != null)
            {
                retryTimer_.Dispose();
                retryTimer_ = null;
            }

            // close all currenty opened sockets except the one which is IsConnected to our current server
            foreach (ServerSocket pSck in openSockets_)
            {
                if (pSck == connectedSocket_)		// don't destroy socket which is IsConnected to server
                    continue;
                if (pSck.IsDeleting == false)	// don't destroy socket if it is going to destroy itself later on
                    DestroySocket(pSck);
            }
        }

        public void CheckForTimeout()
        {
            uint dwServerConnectTimeout = MuleConstants.CONSERVTIMEOUT;
            // If we are using a proxy, increase server connection timeout to default connection timeout
            if (MuleApplication.Instance.Preference.ProxySettings.UseProxy)
                dwServerConnectTimeout = Math.Max(dwServerConnectTimeout, MuleConstants.CONNECTION_TIMEOUT);

            uint dwCurTick = MpdUtilities.GetTickCount();

            int pos = 0;

            while (pos < connectionAttemps_.Count)
            {
                List<ulong> keys = connectionAttemps_.Keys.ToList();

                ulong key = keys[pos];

                ServerSocket sock = connectionAttemps_[key];

                if (sock == null)
                {
                    connectionAttemps_.Remove(key);
                    return;
                }

                if (dwCurTick - key > dwServerConnectTimeout)
                {
                    connectionAttemps_.Remove(key);
                    DestroySocket(sock);
                    if (IsSingleConnect)
                        StopConnectionTry();
                    else
                        TryAnotherConnectionRequest();
                }
                else
                {
                    pos++;
                }
            }
        }

        public void DestroySocket(Mule.Network.ServerSocket pSck)
        {
            if (pSck == null)
                return;
            // remove socket from list of opened sockets
            foreach (ServerSocket pTestSck in openSockets_)
            {
                if (pTestSck == pSck)
                {
                    openSockets_.Remove(pTestSck);
                    break;
                }
            }

            if (pSck != null)
            { // deadlake PROXYSUPPORT - changed to AsyncSocketEx
                pSck.Close();
            }
        }

        public bool SendPacket(Mule.Network.Packet packet)
        {
            return SendPacket(packet, true, null);
        }

        public bool SendPacket(Mule.Network.Packet packet, bool delpacket)
        {
            return SendPacket(packet, delpacket, null);
        }

        public bool SendPacket(Mule.Network.Packet packet, bool delpacket, Mule.Network.ServerSocket to)
        {
            if (to != null)
            {
                if (IsConnected)
                {
                    connectedSocket_.SendPacket(packet, delpacket, true);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                to.SendPacket(packet, delpacket, true);
            }
            return true;
        }

        public bool IsUDPSocketAvailable
        {
            get { return udpsocket_ != null; }
        }

        public bool SendUDPPacket(Mule.Network.Packet packet, Mule.ED2K.ED2KServer host)
        {
            return SendUDPPacket(packet, host, false, 0, null, 0);
        }

        public bool SendUDPPacket(Mule.Network.Packet packet, Mule.ED2K.ED2KServer host, bool delpacket)
        {
            return SendUDPPacket(packet, host, delpacket, 0, null, 0);
        }

        public bool SendUDPPacket(Mule.Network.Packet packet, Mule.ED2K.ED2KServer host,
            bool delpacket, ushort nSpecialPort)
        {
            return SendUDPPacket(packet, host, delpacket, nSpecialPort, null, 0);
        }

        public bool SendUDPPacket(Mule.Network.Packet packet, Mule.ED2K.ED2KServer host,
            bool delpacket, ushort nSpecialPort, byte[] pRawPacket)
        {
            return SendUDPPacket(packet, host, delpacket, nSpecialPort, pRawPacket, 0);
        }

        public bool SendUDPPacket(Mule.Network.Packet packet, Mule.ED2K.ED2KServer host, bool delpacket, ushort nSpecialPort, byte[] pRawPacket, uint nLen)
        {
            if (MuleApplication.Instance.IsConnected)
            {
                if (udpsocket_ != null)
                    udpsocket_.SendPacket(packet, host, nSpecialPort, pRawPacket, nLen);
            }
            return true;
        }

        public void KeepConnectionAlive()
        {
            uint dwServerKeepAliveTimeout = MuleApplication.Instance.Preference.ServerKeepAliveTimeout;
            if (dwServerKeepAliveTimeout > 0 &&
                IsConnected &&
                connectedSocket_ != null &&
                connectedSocket_.ConnectionState == ConnectionStateEnum.CS_CONNECTED &&
                MpdUtilities.GetTickCount() - connectedSocket_.LastTransmission >= dwServerKeepAliveTimeout)
            {
                // "Ping" the server if the TCP connection was not used for the specified interval with 
                // an empty publish files packet . recommended by lugdunummaster himself!
                SafeMemFile files = MpdObjectManager.CreateSafeMemFile(4);
                files.WriteUInt32(0); // nr. of files
                Packet packet = MuleApplication.Instance.NetworkObjectManager.CreatePacket(files);
                packet.OperationCode = OperationCodeEnum.OP_OFFERFILES;
                MuleApplication.Instance.Statistics.AddUpDataOverheadServer(packet.Size);
                connectedSocket_.SendPacket(packet, true);
            }
        }

        public bool Disconnect()
        {
            if (IsConnected && connectedSocket_ != null)
            {
                MuleApplication.Instance.SharedFiles.ClearED2KPublishInfo();
                IsConnected = false;
                ED2KServer pServer =
                    MuleApplication.Instance.ServerList.GetServerByAddress(connectedSocket_.CurrentServer.Address, connectedSocket_.CurrentServer.Port);
                MuleApplication.Instance.PublicIP = 0;
                DestroySocket(connectedSocket_);
                connectedSocket_ = null;
                MuleApplication.Instance.Statistics.ServerConnectTime = 0;
                MuleApplication.Instance.Statistics.Add2TotalServerDuration();
                return true;
            }
            return false;
        }

        public bool IsConnecting
        {
            get;
            set;
        }

        public bool IsConnected
        {
            get;
            set;
        }

        public uint ClientID
        {
            get
            {
                return clientId_;
            }

            set
            {
                clientId_ = value;

                if (!MuleUtilities.IsLowID(value))
                    MuleApplication.Instance.PublicIP = (int)value;
            }
        }

        public Mule.ED2K.ED2KServer CurrentServer
        {
            get
            {
                if (IsConnected && connectedSocket_ != null)
                    return connectedSocket_.CurrentServer;
                return null;
            }
        }

        public bool IsLowID
        {
            get { return MuleUtilities.IsLowID(ClientID); }
        }

        public bool IsLocalServer(uint dwIP, ushort nPort)
        {
            if (IsConnected)
            {
                if (connectedSocket_.CurrentServer.IP == dwIP &&
                    connectedSocket_.CurrentServer.Port == nPort)
                    return true;
            }
            return false;
        }

        public void TryAnotherConnectionRequest()
        {
            if (connectionAttemps_.Count < (MuleApplication.Instance.Preference.IsSafeServerConnectEnabled ? 1 : 2))
            {
                ED2KServer next_server =
                    MuleApplication.Instance.ServerList.GetNextServer(tryObfuscated_);
                if (next_server == null)
                {
                    if (connectionAttemps_.Count == 0)
                    {
                        if (tryObfuscated_ && !MuleApplication.Instance.Preference.IsClientCryptLayerRequired)
                        {
                            // try all servers on the non-obfuscated port next
                            tryObfuscated_ = false;
                            ConnectToAnyServer(0, true, true, true);
                        }
                        else if (retryTimer_ == null)
                        {
                            // 05-Nov-2003: If we have a very short server list, we could put serious load on those few servers
                            // if we start the next connection tries without waiting.
                            startAutoConnectPos_ = 0; // default: start at 0
                        }
                    }
                    return;
                }

                // Barry - Only auto-connect to static server option
                if (MuleApplication.Instance.Preference.DoesAutoConnectToStaticServersOnly)
                {
                    if (next_server.IsStaticMember)
                        ConnectToServer(next_server, true, !tryObfuscated_);
                }
                else
                    ConnectToServer(next_server, true, !tryObfuscated_);
            }
        }

        public bool IsSingleConnect
        {
            get;
            set;
        }

        public void InitLocalIP()
        {
            LocalIP = 0;

            // Using 'gethostname/gethostbyname' does not solve the problem when we have more than 
            // one IP address. Using 'gethostname/gethostbyname' even seems to return the last IP 
            // address which we got. e.g. if we already got an IP from our ISP, 
            // 'gethostname/gethostbyname' will returned that (primary) IP, but if we add another
            // IP by opening a VPN connection, 'gethostname' will still return the same hostname, 
            // but 'gethostbyname' will return the 2nd IP.
            // To weaken that problem at least for users which are binding eMule to a certain IP,
            // we use the explicitly specified bind address as our local IP address.
            if (MuleApplication.Instance.Preference.BindAddr != null)
            {
                IPAddress ulBindAddr = null;

                if (IPAddress.TryParse(MuleApplication.Instance.Preference.BindAddr,
                    out ulBindAddr))
                {
                    LocalIP = BitConverter.ToUInt32(ulBindAddr.GetAddressBytes(), 0);
                    return;
                }
            }

            // Don't use 'gethostbyname(null)'. The winsock DLL may be replaced by a DLL from a third party
            // which is not fully compatible to the original winsock DLL. ppl reported crash with SCORSOCK.DLL
            // when using 'gethostbyname(null)'.
            try
            {
                string hostName = Dns.GetHostName();
                IPHostEntry hostEntry = Dns.GetHostEntry(hostName);

                if (hostEntry.AddressList != null && hostEntry.AddressList.Length > 0)
                    LocalIP = BitConverter.ToUInt32(hostEntry.AddressList[0].GetAddressBytes(), 0);
            }
            catch (Exception ex)
            {
                MpdUtilities.DebugLogError(ex);
            }
        }

        public uint LocalIP
        {
            get;
            set;
        }

        public bool AwaitingTestFromIP(uint dwIP)
        {
            if (connectionAttemps_.Count == 0)
                return false;
            Dictionary<ulong, ServerSocket>.Enumerator pos = connectionAttemps_.GetEnumerator();
            while (pos.MoveNext())
            {
                if (pos.Current.Value != null &&
                    pos.Current.Value.CurrentServer != null &&
                    pos.Current.Value.CurrentServer.IP == dwIP &&
                    pos.Current.Value.ConnectionState == ConnectionStateEnum.CS_WAITFORLOGIN)
                    return true;
            }
            return false;
        }

        public bool IsConnectedObfuscated
        {
            get
            {
                return connectedSocket_ != null && connectedSocket_.IsObfusicating;
            }
        }

        public void Stop()
        {
            // stop all connections
            StopConnectionTry();
            // close IsConnected socket, if any
            DestroySocket(connectedSocket_);
            connectedSocket_ = null;
            // close udp socket
            if (udpsocket_ != null)
            {
                udpsocket_.Close();
            }
        }
        #endregion

        #region Privates

        private void RetryConnectTimer(object state)
        {
            // NOTE: Always handle all type of MFC exceptions in TimerProcs - otherwise we'll get mem leaks
            try
            {
                StopConnectionTry();
                if (IsConnected)
                    return;
                if (startAutoConnectPos_ >= MuleApplication.Instance.ServerList.ServerCount)
                    startAutoConnectPos_ = 0;
                ConnectToAnyServer(startAutoConnectPos_, true, true);

            }
            catch(Exception ex)
            {
                MpdUtilities.DebugLogError(ex);
            }
        }

        #endregion

        #region Constructors
        public ServerConnectImpl()
        {
            connectedSocket_ = null;
            maxSimulateCons_ = (byte)((MuleApplication.Instance.Preference.IsSafeServerConnectEnabled) ? 1 : 2);
            IsConnecting = false;
            IsConnected = false;
            clientId_ = 0;
            IsSingleConnect = false;
            if (MuleApplication.Instance.Preference.ServerUDPPort != 0)
            {
                udpsocket_ = MuleApplication.Instance.NetworkObjectManager.CreateUDPSocket(); // initalize socket for udp packets
            }
            else
                udpsocket_ = null;
            retryTimer_ = null;
            startAutoConnectPos_ = 0;
            InitLocalIP();
        }
        #endregion
    }
}
