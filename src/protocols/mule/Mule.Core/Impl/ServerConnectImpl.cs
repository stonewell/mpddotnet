using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.Network;
using Mule.ED2K;
using Mpd.Utilities;
using Mpd.Generic.IO;
using Mpd.Generic;

namespace Mule.Core.Impl
{
    class ServerConnectImpl : ServerConnect
    {
        #region Fields
        private bool connecting;
        private bool singleconnecting;
        private bool connected;
        private bool m_bTryObfuscated;
        private byte max_simcons;
        private uint m_uStartAutoConnectPos;
        private ServerSocket connectedsocket;
        private UDPSocket udpsocket;
        private List<object> m_lstOpenSockets =
            new List<object>();	// list of currently opened sockets
        private uint m_idRetryTimer;
        private uint m_nLocalIP;
        private Dictionary<ulong, ServerSocket> connectionattemps =
            new Dictionary<ulong, ServerSocket>();
        #endregion

        #region ServerConnect Members

        public void ConnectionFailed(Mule.Network.ServerSocket sender)
        {
            throw new NotImplementedException();
        }

        public void ConnectionEstablished(Mule.Network.ServerSocket sender)
        {
    //if (!connecting) {
    //    // we are already connected to another server
    //    DestroySocket(sender);
    //    return;
    //}
	
    //InitLocalIP();
    //if (sender.GetConnectionState() == CS_WAITFORLOGIN)
    //{
    //    ED2KServer pServer = MuleApplication.Instance.ServerList.GetServerByAddress(sender.CurrentServer.GetAddress(), sender.cur_server.GetPort());
    //    if (pServer != null) {
    //        pServer.ResetFailedCount();
    //    }

    //    // Send login packet
    //    SafeMemFile data = MpdObjectManager.CreateSafeMemFile(256);
    //    data.WriteHash16(MuleApplication.Instance.Preference.GetUserHash());
    //    data.WriteUInt32(GetClientID());
    //    data.WriteUInt16(MuleApplication.Instance.Preference.GetPort());

    //    UINT tagcount = 4;
    //    data.WriteUInt32(tagcount);

    //    CTag tagName(CT_NAME, MuleApplication.Instance.Preference.GetUserNick());
    //    tagName.WriteTagToFile(&data);

    //    CTag tagVersion(CT_VERSION, EDONKEYVERSION);
    //    tagVersion.WriteTagToFile(&data);

    //    uint32 dwCryptFlags = 0;
    //    if (MuleApplication.Instance.Preference.IsClientCryptLayerSupported())
    //        dwCryptFlags |= SRVCAP_SUPPORTCRYPT;
    //    if (MuleApplication.Instance.Preference.IsClientCryptLayerRequested())
    //        dwCryptFlags |= SRVCAP_REQUESTCRYPT;
    //    if (MuleApplication.Instance.Preference.IsClientCryptLayerRequired())
    //        dwCryptFlags |= SRVCAP_REQUIRECRYPT;

    //    CTag tagFlags(CT_SERVER_FLAGS, SRVCAP_ZLIB | SRVCAP_NEWTAGS | SRVCAP_LARGEFILES | SRVCAP_UNICODE | dwCryptFlags);
    //    tagFlags.WriteTagToFile(&data);

    //    // eMule Version (14-Mar-2004: requested by lugdunummaster (need for LowID clients which have no chance 
    //    // to send an Hello packet to the server during the callback test))
    //    CTag tagMuleVersion(CT_EMULE_VERSION, 
    //                        //(uCompatibleClientID		<< 24) |
    //                        (CemuleApp::m_nVersionMjr	<< 17) |
    //                        (CemuleApp::m_nVersionMin	<< 10) |
    //                        (CemuleApp::m_nVersionUpd	<<  7) );
    //    tagMuleVersion.WriteTagToFile(&data);

    //    Packet* packet = new Packet(&data);
    //    packet.opcode = OP_LOGINREQUEST;
    //    if (MuleApplication.Instance.Preference.GetDebugServerTCPLevel() > 0)
    //        Debug(_T(">>> Sending OP__LoginRequest\n"));
    //    theStats.AddUpDataOverheadServer(packet.size);
    //    SendPacket(packet, true, sender);
    //}
    //else if (sender.GetConnectionState() == CS_CONNECTED)
    //{
    //    theStats.reconnects++;
    //    theStats.serverConnectTime = GetTickCount();
    //    connected = true;
    //    CString strMsg;
    //    if (sender.IsObfusicating())
    //        strMsg.Format(GetResString(IDS_CONNECTEDTOOBFUSCATED) + _T(" (%s:%u)"), sender.cur_server.GetListName(), sender.cur_server.GetAddress(), sender.cur_server.GetObfuscationPortTCP());
    //    else
    //        strMsg.Format(GetResString(IDS_CONNECTEDTO) + _T(" (%s:%u)"), sender.cur_server.GetListName(), sender.cur_server.GetAddress(), sender.cur_server.GetPort());

    //    Log(LOG_SUCCESS | LOG_STATUSBAR, strMsg);
    //    theApp.emuledlg.ShowConnectionState();
    //    connectedsocket = sender;
    //    StopConnectionTry();
    //    theApp.sharedfiles.ClearED2KPublishInfo();
    //    theApp.sharedfiles.SendListToServer();
    //    theApp.emuledlg.serverwnd.serverlistctrl.RemoveAllDeadServers();

    //    // tecxx 1609 2002 - serverlist update
    //    if (MuleApplication.Instance.Preference.GetAddServersFromServer())
    //    {
    //        Packet* packet = new Packet(OP_GETSERVERLIST,0);
    //        if (MuleApplication.Instance.Preference.GetDebugServerTCPLevel() > 0)
    //            Debug(_T(">>> Sending OP__GetServerList\n"));
    //        theStats.AddUpDataOverheadServer(packet.size);
    //        SendPacket(packet, true);
    //    }

    //    ED2KServer pServer = MuleApplication.Instance.ServerList.GetServerByAddress(sender.cur_server.GetAddress(), sender.cur_server.GetPort());
    //    if (pServer)
    //        theApp.emuledlg.serverwnd.serverlistctrl.RefreshServer(pServer);
    //}
    //theApp.emuledlg.ShowConnectionState();
        }

        private SafeMemFile MpdObjectmanager(int p)
        {
            throw new NotImplementedException();
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
            connecting = true;
            singleconnecting = false;
            m_bTryObfuscated =
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
                    connecting = false;
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
                connecting = false;
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
            connecting = true;
            singleconnecting = !multiconnect;

            ServerSocket newsocket =
                MuleApplication.Instance.NetworkObjectManager.CreateServerSocket(this, !multiconnect);
            m_lstOpenSockets.Add(newsocket);
            newsocket.ConnectTo(toconnect, bNoCrypt);
            connectionattemps[MpdUtilities.GetTickCount()] = newsocket;
        }

        public void StopConnectionTry()
        {
            connectionattemps.Clear();
            connecting = false;
            singleconnecting = false;

            //TODO:
            //if (m_idRetryTimer)
            //{
            //    KillTimer(NULL, m_idRetryTimer);
            //    m_idRetryTimer = 0;
            //}

            // close all currenty opened sockets except the one which is connected to our current server
            foreach(ServerSocket pSck in m_lstOpenSockets)
            {
                if (pSck == connectedsocket)		// don't destroy socket which is connected to server
                    continue;
                if (pSck.IsDeleting == false)	// don't destroy socket if it is going to destroy itself later on
                    DestroySocket(pSck);
            }
        }

        public void CheckForTimeout()
        {
            throw new NotImplementedException();
        }

        public void DestroySocket(Mule.Network.ServerSocket pSck)
        {
            throw new NotImplementedException();
        }

        public bool SendPacket(Mule.Network.Packet packet)
        {
            throw new NotImplementedException();
        }

        public bool SendPacket(Mule.Network.Packet packet, bool delpacket)
        {
            throw new NotImplementedException();
        }

        public bool SendPacket(Mule.Network.Packet packet, bool delpacket, Mule.Network.ServerSocket to)
        {
            throw new NotImplementedException();
        }

        public bool IsUDPSocketAvailable
        {
            get { throw new NotImplementedException(); }
        }

        public bool SendUDPPacket(Mule.Network.Packet packet, Mule.ED2K.ED2KServer host)
        {
            throw new NotImplementedException();
        }

        public bool SendUDPPacket(Mule.Network.Packet packet, Mule.ED2K.ED2KServer host, bool delpacket)
        {
            throw new NotImplementedException();
        }

        public bool SendUDPPacket(Mule.Network.Packet packet, Mule.ED2K.ED2KServer host, bool delpacket, ushort nSpecialPort)
        {
            throw new NotImplementedException();
        }

        public bool SendUDPPacket(Mule.Network.Packet packet, Mule.ED2K.ED2KServer host, bool delpacket, ushort nSpecialPort, byte[] pRawPacket)
        {
            throw new NotImplementedException();
        }

        public bool SendUDPPacket(Mule.Network.Packet packet, Mule.ED2K.ED2KServer host, bool delpacket, ushort nSpecialPort, byte[] pRawPacket, uint nLen)
        {
            throw new NotImplementedException();
        }

        public void KeepConnectionAlive()
        {
            throw new NotImplementedException();
        }

        public bool Disconnect()
        {
            throw new NotImplementedException();
        }

        public bool IsConnecting
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsConnected
        {
            get { throw new NotImplementedException(); }
        }

        public uint ClientID
        {
            get { throw new NotImplementedException(); }
        }

        public Mule.ED2K.ED2KServer CurrentServer
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsLowID
        {
            get { throw new NotImplementedException(); }
        }

        public void SetClientID(uint newid)
        {
            throw new NotImplementedException();
        }

        public bool IsLocalServer(uint dwIP, ushort nPort)
        {
            throw new NotImplementedException();
        }

        public void TryAnotherConnectionRequest()
        {
            if (connectionattemps.Count < (MuleApplication.Instance.Preference.IsSafeServerConnectEnabled ? 1 : 2))
            {
                ED2KServer next_server =
                    MuleApplication.Instance.ServerList.GetNextServer(m_bTryObfuscated);
                if (next_server == null)
                {
                    if (connectionattemps.Count == 0)
                    {
                        if (m_bTryObfuscated && !MuleApplication.Instance.Preference.IsClientCryptLayerRequired)
                        {
                            // try all servers on the non-obfuscated port next
                            m_bTryObfuscated = false;
                            ConnectToAnyServer(0, true, true, true);
                        }
                        else if (m_idRetryTimer == 0)
                        {
                            // 05-Nov-2003: If we have a very short server list, we could put serious load on those few servers
                            // if we start the next connection tries without waiting.
                            m_uStartAutoConnectPos = 0; // default: start at 0
                        }
                    }
                    return;
                }

                // Barry - Only auto-connect to static server option
                if (MuleApplication.Instance.Preference.DoesAutoConnectToStaticServersOnly)
                {
                    if (next_server.IsStaticMember)
                        ConnectToServer(next_server, true, !m_bTryObfuscated);
                }
                else
                    ConnectToServer(next_server, true, !m_bTryObfuscated);
            }
        }

        public bool IsSingleConnect
        {
            get { throw new NotImplementedException(); }
        }

        public void InitLocalIP()
        {
            throw new NotImplementedException();
        }

        public uint LocalIP
        {
            get { throw new NotImplementedException(); }
        }

        public bool AwaitingTestFromIP(uint dwIP)
        {
            throw new NotImplementedException();
        }

        public bool IsConnectedObfuscated()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
