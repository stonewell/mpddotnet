using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Utilities;
using System.IO;
using Mule.Preference;
using Mpd.Generic.IO;
using System.Diagnostics;
using Mule.Network;

namespace Mule.ED2K.Impl
{
    class ED2KServerListImpl : ED2KServerList
    {
        #region Fields
        private List<ED2KServer> servers_ = new List<ED2KServer>();
        uint serverpos;
        uint searchserverpos;
        uint statserverpos;
        uint delservercount;
        uint lastSaved;
        #endregion

        #region Constructors
        public ED2KServerListImpl()
        {
            serverpos = 0;
            searchserverpos = 0;
            statserverpos = 0;
            delservercount = 0;
            lastSaved = MpdUtilities.GetTickCount();
        }

        #endregion

        #region ED2KServerList Members

        public bool Init()
        {
            // auto update the list by using an url
            if (MuleApplication.Instance.Preference.DoesAutoUpdateServerList)
                AutoUpdate();

            GiveServersForTraceRoute();

            return true;
        }

        public void Sort()
        {
            servers_.Sort(new Comparison<ED2KServer>((server1, server2) =>
            {
                if (server1 == server2)
                    return 0;

                if (server1.Preference == server2.Preference)
                    return 0;

                if (server1.Preference == ED2KServerPreferenceEnum.SRV_PR_HIGH)
                    return 1;

                if (server1.Preference == ED2KServerPreferenceEnum.SRV_PR_LOW)
                    return -1;

                if (server2.Preference == ED2KServerPreferenceEnum.SRV_PR_LOW)
                    return 1;

                return -1;
            }));
        }

        public void MoveServerDown(ED2KServer pServer)
        {
            if (!servers_.Contains(pServer))
                return;

            servers_.Remove(pServer);
            servers_.Add(pServer);
        }

        public void AutoUpdate()
        {
            if (MuleApplication.Instance.Preference.ServerAutoUpdateUrls.Count == 0)
            {
                return;
            }

            bool bDownloaded = false;

            List<string>.Enumerator it = MuleApplication.Instance.Preference.ServerAutoUpdateUrls.GetEnumerator();

            while (!bDownloaded && it.MoveNext())
            {
                ServerListDownloader downloader =
                    new ServerListDownloader(it.Current, MuleApplication.Instance.Preference.ServerAddresses);
                if (downloader.Download())
                    bDownloaded = true;
            }
        }

        public bool AddServer(ED2KServer pServer)
        {
            return AddServer(pServer, true);
        }

        public bool AddServer(ED2KServer pServer, bool bAddTail)
        {
            if (!IsGoodServerIP(pServer))
            { 
                // check for 0-IP, localhost and optionally for LAN addresses
                return false;
            }

            if (MuleApplication.Instance.Preference.IsFilterServerByIP)
            {
                // IP-Filter: We don't need to reject dynIP-servers here. After the DN was
                // resolved, the IP will get filtered and the server will get removed. This applies
                // for TCP-connections as well as for outgoing UDP-packets because for both protocols
                // we resolve the DN and filter the received IP.
                //if (pServer.HasDynIP())
                //	return false;
                if (MuleApplication.Instance.IPFilter.IsFiltered(pServer.IP))
                {
                    return false;
                }
            }

            ED2KServer pFoundServer = GetServerByAddress(pServer.Address, pServer.Port);
            // Avoid duplicate (dynIP) servers: If the server which is to be added, is a dynIP-server
            // but we don't know yet it's DN, we need to search for an already available server with
            // that IP.
            if (pFoundServer == null && pServer.IP != 0)
                pFoundServer = GetServerByIPTCP(pServer.IP, pServer.Port);

            if (pFoundServer != null)
            {
                pFoundServer.ResetFailedCount();
                return false;
            }

            if (bAddTail)
                servers_.Add(pServer);
            else
                servers_.Insert(0, pServer);
            return true;
        }

        public void RemoveServer(ED2KServer pServer)
        {
            if (!servers_.Contains(pServer)) return;

            servers_.Remove(pServer);
            delservercount++;
            if (MuleApplication.Instance.DownloadQueue.CurrentUDPServer == pServer)
                MuleApplication.Instance.DownloadQueue.CurrentUDPServer = null;
        }

        public void RemoveAllServers()
        {
            delservercount = (uint)servers_.Count;
            servers_.Clear();
        }

        public void RemoveDuplicatesByAddress(ED2KServer pExceptThis)
        {
            foreach(ED2KServer pServer in servers_)
            {
                if (pServer == pExceptThis)
                    continue;
                if (string.Compare(pServer.Address, pExceptThis.Address,true) == 0
                    && pServer.Port == pExceptThis.Port)
                {
                    servers_.Remove(pServer);
                    return;
                }
            }
        }

        public void RemoveDuplicatesByIP(ED2KServer pExceptThis)
        {
            foreach (ED2KServer pServer in servers_)
            {
                if (pServer == pExceptThis)
                    continue;
                if (pServer.IP == pExceptThis.IP
                    && pServer.Port == pExceptThis.Port)
                {
                    servers_.Remove(pServer);
                    return;
                }
            }
        }

        public uint ServerCount
        {
            get { return (uint)servers_.Count; }
        }

        public ED2KServer this[int index]
        {
            get { return servers_[index]; }
        }

        public ED2KServer GetSuccServer(ED2KServer lastserver)
        {
            if (servers_.Count == 0)
                return null;
            if (lastserver == null)
                return servers_[0];

            int index = servers_.IndexOf(lastserver);
            if (index < 0)
                return servers_[0];

            if (index + 1 >= servers_.Count)
                return null;
            return servers_[index + 1];
        }

        public ED2KServer GetNextServer(bool bOnlyObfuscated)
        {
            if (serverpos >= (uint)servers_.Count)
                return null;

            ED2KServer nextserver = null;
            int i = 0;
            while (nextserver == null && i < servers_.Count)
            {
                uint posIndex = serverpos;
                if (serverpos >= (uint)servers_.Count)
                {	// check if search position is still valid (could be corrupted by server delete operation)
                    posIndex = 0;
                    serverpos = 0;
                }

                serverpos++;
                i++;
                if (!bOnlyObfuscated || servers_[(int)posIndex].DoesSupportsObfuscationTCP)
                    nextserver = servers_[(int)posIndex];
                else if (serverpos >= (uint)servers_.Count)
                    return null;
            }
            return nextserver;
        }

        public ED2KServer GetServerByAddress(string address, ushort port)
        {
            foreach(ED2KServer s in servers_)
            {
                if ((port == s.Port || port == 0) && string.Compare(s.Address, address) == 0)
                    return s;
            }

            return null;
        }

        public ED2KServer GetServerByIP(uint nIP)
        {
            foreach (ED2KServer s in servers_)
            {
                if (s.IP == nIP)
                    return s;
            }

            return null;
        }

        public ED2KServer GetServerByIPTCP(uint nIP, ushort nTCPPort)
        {
            foreach (ED2KServer s in servers_)
            {
                if (s.IP == nIP && s.Port == nTCPPort)
                    return s;
            }

            return null;
        }

        public ED2KServer GetServerByIPUDP(uint nIP, ushort nUDPPort)
        {
            return GetServerByIPUDP(nIP, nUDPPort, true);
        }

        public ED2KServer GetServerByIPUDP(uint nIP, ushort nUDPPort, bool bObfuscationPorts)
        {
            foreach (ED2KServer s in servers_)
            {
                if (s.IP == nIP && (s.Port == nUDPPort - 4 ||
                    (bObfuscationPorts && (s.ObfuscationPortUDP == nUDPPort) ||
                    (s.Port == nUDPPort - 12))))
                    return s;
            }

            return null;
        }

        public int GetPositionOfServer(ED2KServer pServer)
        {
            return servers_.IndexOf(pServer);
        }

        public uint ServerPostion
        {
            get
            {
                return serverpos;
            }
            set
            {
                if (value < servers_.Count)
                    serverpos = value;
                else
                    serverpos = 0;
            }
        }

        public void ResetSearchServerPos()
        {
            searchserverpos = 0;
        }

        public ED2KServer GetNextSearchServer()
        {
            ED2KServer nextserver = null;
            int i = 0;
            while (nextserver == null && i < servers_.Count)
            {
                uint posIndex = searchserverpos;
                if (searchserverpos >= (uint)servers_.Count)
                {	// check if search position is still valid (could be corrupted by server delete operation)
                    posIndex = 0;
                    searchserverpos = 0;
                }

                nextserver = servers_[(int)posIndex];
                searchserverpos++;
                i++;
                if (searchserverpos == (uint)servers_.Count)
                    searchserverpos = 0;
            }
            return nextserver;
        }

        public void ServerStats()
        {
            // Update the server list even if we are connected to Kademlia only. The idea is for both networks to keep 
            // each other up to date.. Kad network can get you back into the ED2K network.. And the ED2K network can get 
            // you back into the Kad network..
            if (MuleApplication.Instance.ServerConnect.IsConnected && 
                MuleApplication.Instance.ServerConnect.IsUDPSocketAvailable && 
                servers_.Count > 0)
            {
                ED2KServer ping_server = GetNextStatServer();
                if (ping_server == null)
                    return;

                uint tNow = MpdUtilities.Time();
                ED2KServer test = ping_server;
                while (ping_server.LastPingedTime != 0 && 
                    (tNow - ping_server.LastPingedTime) < MuleConstants.UDPSERVSTATREASKTIME)
                {
                    ping_server = GetNextStatServer();
                    if (ping_server == test)
                        return;
                }
                // IP-filter: We do not need to IP-filter any servers here, even dynIP-servers are not
                // needed to get filtered here. See also comments in 'CServerSocket::ConnectTo'.
                if (ping_server.FailedCount >= MuleApplication.Instance.Preference.DeadServerRetries)
                {
                    RemoveServer(ping_server);
                    return;
                }

                Random rand = new Random((int)tNow);
                ping_server.RealLastPingedTime = tNow; // this is not used to calcualte the next ping, but only to ensure a minimum delay for premature pings
                if (!ping_server.CryptPingReplyPending && 
                    (tNow - ping_server.LastPingedTime) >= MuleConstants.UDPSERVSTATREASKTIME && 
                    MuleApplication.Instance.PublicIP != 0 &&
                    MuleApplication.Instance.Preference.IsServerCryptLayerUDPEnabled)
                {
                    // we try a obfsucation ping first and wait 20 seconds for an answer
                    // if it doesn'T get responsed, we don't count it as error but continue with a normal ping
                    ping_server.CryptPingReplyPending = true;
                    int nPacketLen = 4 + (byte)(rand.Next() % 16); // max padding 16 bytes
                    byte[] pRawPacket = new byte[nPacketLen];
                    int dwChallenge = (rand.Next() << 17) | (rand.Next() << 2) | (rand.Next() & 0x03);
                    if (dwChallenge == 0)
                        dwChallenge++;
                    Array.Copy(BitConverter.GetBytes(dwChallenge), pRawPacket, 4);
                    for (uint i = 4; i < nPacketLen; i++) // fillung up the remaining bytes with random data
                        pRawPacket[i] = (byte)rand.Next();

                    ping_server.Challenge = (uint)dwChallenge;
                    ping_server.LastPinged = MpdUtilities.GetTickCount();
                    ping_server.LastPingedTime = 
                        ((tNow - (uint)MuleConstants.UDPSERVSTATREASKTIME) + 20); // give it 20 seconds to respond

                    MuleApplication.Instance.Statistics.AddUpDataOverheadServer((uint)nPacketLen);
                    MuleApplication.Instance.ServerConnect.SendUDPPacket(null, 
                        ping_server, true, (ushort)(ping_server.Port + 12), pRawPacket, (uint)nPacketLen);
                }
                else if (ping_server.CryptPingReplyPending || 
                    MuleApplication.Instance.PublicIP == 0 || 
                    !MuleApplication.Instance.Preference.IsServerCryptLayerUDPEnabled)
                {
                    // our obfsucation ping request was not answered, so probably the server doesn'T supports obfuscation
                    // continue with a normal request
                    ping_server.CryptPingReplyPending = false;
                    Packet packet = MuleApplication.Instance.NetworkObjectManager.CreatePacket(OperationCodeEnum.OP_GLOBSERVSTATREQ, 4);
                    int uChallenge = 0x55AA0000 + MpdUtilities.GetRandomUInt16();
                    ping_server.Challenge = (uint)uChallenge;
                    Array.Copy(BitConverter.GetBytes(uChallenge), packet.Buffer, 4);
                    ping_server.LastPinged = MpdUtilities.GetTickCount();
                    ping_server.LastPingedTime = (uint)(tNow - (rand.Next() % MuleConstants.ONE_HOUR_SEC));
                    ping_server.AddFailedCount();
                    MuleApplication.Instance.Statistics.AddUpDataOverheadServer(packet.Size);
                    MuleApplication.Instance.ServerConnect.SendUDPPacket(packet, ping_server, true);
                }
                else
                    Debug.Assert(false);
            }
        }

        public ED2KServer GetNextStatServer()
        {
            ED2KServer nextserver = null;
            int i = 0;
            while (nextserver == null && i < servers_.Count)
            {
                uint posIndex = statserverpos;
                if (statserverpos >= (uint)servers_.Count)
                {	// check if search position is still valid (could be corrupted by server delete operation)
                    posIndex = 0;
                    statserverpos = 0;
                }

                nextserver = servers_[(int)posIndex];
                statserverpos++;
                i++;
                if (statserverpos == (uint)servers_.Count)
                    statserverpos = 0;
            }
            return nextserver;
        }

        public bool IsGoodServerIP(ED2KServer pServer)
        {
            if (pServer.HasDynIP)
                return true;
            return MpdUtilities.IsGoodIPPort(pServer.IP, pServer.Port);
        }

        public void GetStatus(ref uint total, ref uint failed, ref uint user, ref uint file, ref uint lowiduser, ref uint totaluser, ref uint totalfile, ref float occ)
        {
            total = (uint)servers_.Count;
            failed = 0;
            user = 0;
            file = 0;
            totaluser = 0;
            totalfile = 0;
            occ = 0;
            lowiduser = 0;

            uint maxuserknownmax = 0;
            uint totaluserknownmax = 0;
            foreach(ED2KServer curr in servers_)
            {
                if (curr.FailedCount > 0)
                {
                    failed++;
                }
                else
                {
                    user += curr.UserCount;
                    file += curr.FileCount;
                    lowiduser += curr.LowIDUsers;
                }
                totaluser += curr.UserCount;
                totalfile += curr.FileCount;

                if (curr.MaxUsers > 0)
                {
                    totaluserknownmax += curr.UserCount; // total users on servers with known maximum
                    maxuserknownmax += curr.MaxUsers;
                }
            }

            if (maxuserknownmax > 0)
                occ = (float)(totaluserknownmax * 100) / maxuserknownmax;
        }

        public void GetAvgFile(ref uint average)
        {
            //Since there is no real way to know how many files are in the kad network,
            //I figure to try to use the ED2K network stats to find how many files the
            //average user shares..
            uint totaluser = 0;
            uint totalfile = 0;
            foreach (ED2KServer curr in servers_)
            {
                //If this server has reported Users/Files and doesn't limit it's files too much
                //use this in the calculation..
                if (curr.UserCount > 0 && curr.FileCount > 0 && curr.SoftFiles > 1000)
                {
                    totaluser += curr.UserCount;
                    totalfile += curr.FileCount;
                }
            }
            //If the user count is a little low, don't send back a average..
            //I added 50 to the count as many servers do not allow a large amount of files to be shared..
            //Therefore the extimate here will be lower then the actual.
            //I would love to add a way for the client to send some statistics back so we could see the real
            //values here..
            if (totaluser > 500000)
                average = (totalfile / totaluser) + 50;
            else
                average = 0;
        }

        public void GetUserFileStatus(ref uint user, ref uint file)
        {
            user = 0;
            file = 0;
            foreach (ED2KServer curr in servers_)
            {
                if (curr.FailedCount > 0)
                {
                    user += curr.UserCount;
                    file += curr.FileCount;
                }
            }
        }

        public uint DeletedServerCount
        {
            get { return delservercount; }
        }

        public bool GiveServersForTraceRoute()
        {
            return MuleApplication.Instance.LastCommonRouteFinder.AddHostsToCheck(servers_);
        }

        public void CheckForExpiredUDPKeys()
        {
            if (!MuleApplication.Instance.Preference.IsServerCryptLayerUDPEnabled)
                return;

            uint cKeysTotal = 0;
            uint cKeysExpired = 0;
            uint cPingDelayed = 0;
            uint dwIP = (uint)MuleApplication.Instance.PublicIP;
            uint tNow = (uint)MpdUtilities.Time();

            foreach(ED2KServer pServer in servers_)
            {
                if (pServer.DoesSupportsObfuscationUDP && 
                    pServer.GetServerKeyUDP(true) != 0 && 
                    pServer.ServerKeyUDPIP != dwIP)
                {
                    cKeysTotal++;
                    cKeysExpired++;
                    if (tNow - pServer.RealLastPingedTime < MuleConstants.UDPSERVSTATMINREASKTIME)
                    {
                        cPingDelayed++;
                        // next ping: Now + (MinimumDelay - already elapsed time)
                        pServer.LastPingedTime = 
                            (tNow - (uint)MuleConstants.UDPSERVSTATREASKTIME) +
                            (MuleConstants.UDPSERVSTATMINREASKTIME - (tNow - pServer.RealLastPingedTime));
                    }
                    else
                        pServer.LastPingedTime = 0;
                }
                else if (pServer.DoesSupportsObfuscationUDP && pServer.GetServerKeyUDP(false) != 0)
                    cKeysTotal++;
            }
        }

        #endregion
    }
}
