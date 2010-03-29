using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Utilities;
using System.Net;
using System.Diagnostics;
using Mule.Network;
using Kademlia;

namespace Mule.Core.Impl
{
    class PORTANDHASH
    {
        public PORTANDHASH(ushort nPort, object hash)
        {
            this.nPort = nPort;
            this.pHash = hash;
        }

        public ushort nPort;
        public object pHash;
    };

    struct IPANDTICS
    {
        public IPANDTICS(uint dwIP, uint dwInserted)
        {
            this.dwInserted = dwInserted;
            this.dwIP = dwIP;
        }

        public uint dwIP;
        public uint dwInserted;
    };

    struct CONNECTINGCLIENT
    {
        public CONNECTINGCLIENT(UpDownClient c, uint t)
        {
            pClient = c;
            dwInserted = t;
        }
        public UpDownClient pClient;
        public uint dwInserted;
    };


    class DeletedClient
    {
        public DeletedClient(UpDownClient pClient)
        {
            m_cBadRequest = 0;
            m_dwInserted = MpdUtilities.GetTickCount();
            PORTANDHASH porthash = new PORTANDHASH(pClient.UserPort, pClient.Credits);
            m_ItemsList.Add(porthash);
        }

        public List<PORTANDHASH> m_ItemsList = new List<PORTANDHASH>();
        public uint m_dwInserted;
        public uint m_cBadRequest;
    };

    class ClientListImpl : ClientList
    {
        #region Fields
        private UpDownClientList clientList_ = new UpDownClientList();
        private UpDownClientList kadList_ = new UpDownClientList();
        private Dictionary<uint, uint> bannedList_ = new Dictionary<uint, uint>();
        private Dictionary<uint, DeletedClient> trackedClientsList_ = new Dictionary<uint, DeletedClient>();
        private uint lastBannCleanUp_;
        private uint lastTrackedCleanUp_;
        private uint lastClientCleanUp_;
        private UpDownClient buddy_;
        private BuddyStateEnum buddyStatus_;
        private List<IPANDTICS> firewallCheckRequests_ = new List<IPANDTICS>();
        private List<IPANDTICS> directCallbackRequests_ = new List<IPANDTICS>();
        private List<CONNECTINGCLIENT> connectingClients_ = new List<CONNECTINGCLIENT>();
        #endregion

        #region Constructors
        public ClientListImpl()
        {
            lastBannCleanUp_ = 0;
            lastTrackedCleanUp_ = 0;
            lastClientCleanUp_ = 0;
            buddyStatus_ = BuddyStateEnum.Disconnected;
            DeadSourceList = MuleApplication.Instance.CoreObjectManager.CreateDeadSourceList();
            DeadSourceList.Init(true);
            buddy_ = null;
        }
        #endregion

        #region ClientList Members

        public void AddClient(UpDownClient toadd)
        {
            AddClient(toadd, false);
        }

        public void AddClient(UpDownClient toadd, bool bSkipDupTest)
        {
            // skipping the check for duplicate list entries is only to be done for optimization purposes, if the calling
            // function has ensured that this client instance is not already within the list . there are never duplicate
            // client instances in this list.
            if (!bSkipDupTest)
            {
                if (clientList_.Contains(toadd))
                    return;
            }
            clientList_.Add(toadd);
        }

        public void RemoveClient(UpDownClient toremove)
        {
            RemoveClient(toremove, null);
        }

        public void RemoveClient(UpDownClient toremove, string pszReason)
        {
            if (clientList_.Contains(toremove))
            {
                MuleApplication.Instance.UploadQueue.RemoveFromUploadQueue(toremove,
                    string.Format("CClientList::RemoveClient: {0}", pszReason));
                MuleApplication.Instance.UploadQueue.RemoveFromWaitingQueue(toremove);
                MuleApplication.Instance.DownloadQueue.RemoveSource(toremove);
                clientList_.Remove(toremove);
            }
            RemoveFromKadList(toremove);
            RemoveConnectingClient(toremove);
        }

        public void GetStatistics(ref uint totalclient, int[] stats, Dictionary<uint, uint> clientVersionEDonkey, Dictionary<uint, uint> clientVersionEDonkeyHybrid, Dictionary<uint, uint> clientVersionEMule, Dictionary<uint, uint> clientVersionAMule)
        {
            totalclient = (uint)clientList_.Count;
            Array.Clear(stats, 0, stats.Length);

            foreach (UpDownClient cur_client in clientList_)
            {

                if (cur_client.HasLowID)
                    stats[14]++;

                switch (cur_client.ClientSoft)
                {
                    case ClientSoftwareEnum.SO_EMULE:
                    case ClientSoftwareEnum.SO_OLDEMULE:
                        stats[2]++;
                        clientVersionEMule[cur_client.Version]++;
                        break;

                    case ClientSoftwareEnum.SO_EDONKEYHYBRID:
                        stats[4]++;
                        clientVersionEDonkeyHybrid[cur_client.Version]++;
                        break;

                    case ClientSoftwareEnum.SO_AMULE:
                        stats[10]++;
                        clientVersionAMule[cur_client.Version]++;
                        break;

                    case ClientSoftwareEnum.SO_EDONKEY:
                        stats[1]++;
                        clientVersionEDonkey[cur_client.Version]++;
                        break;

                    case ClientSoftwareEnum.SO_MLDONKEY:
                        stats[3]++;
                        break;

                    case ClientSoftwareEnum.SO_SHAREAZA:
                        stats[11]++;
                        break;

                    // all remaining 'eMule Compatible' clients
                    case ClientSoftwareEnum.SO_CDONKEY:
                    case ClientSoftwareEnum.SO_XMULE:
                    case ClientSoftwareEnum.SO_LPHANT:
                        stats[5]++;
                        break;

                    default:
                        stats[0]++;
                        break;
                }

                if (cur_client.Credits != null)
                {
                    switch (cur_client.Credits.GetCurrentIdentState(cur_client.IP))
                    {
                        case IdentStateEnum.IS_IDENTIFIED:
                            stats[12]++;
                            break;
                        case IdentStateEnum.IS_IDFAILED:
                        case IdentStateEnum.IS_IDNEEDED:
                        case IdentStateEnum.IS_IDBADGUY:
                            stats[13]++;
                            break;
                    }
                }

                if (cur_client.DownloadState == DownloadStateEnum.DS_ERROR)
                    stats[6]++; // Error

                switch (cur_client.UserPort)
                {
                    case 4662:
                        stats[8]++; // Default Port
                        break;
                    default:
                        stats[9]++; // Other Port
                        break;
                }

                // Network client stats
                if (cur_client.ServerIP != 0 && cur_client.ServerPort != 0)
                {
                    stats[15]++;		// eD2K
                    if (cur_client.KadPort != 0)
                    {
                        stats[17]++;	// eD2K/Kad
                        stats[16]++;	// Kad
                    }
                }
                else if (cur_client.KadPort != 0)
                    stats[16]++;		// Kad
                else
                    stats[18]++;		// Unknown
            }
        }

        public uint ClientCount
        {
            get { return (uint)clientList_.Count; }
        }

        public void DeleteAll()
        {
            MuleApplication.Instance.UploadQueue.DeleteAll();
            MuleApplication.Instance.DownloadQueue.DeleteAll();
            clientList_.Clear();
        }

        public bool AttachToAlreadyKnown(ref UpDownClient client, Mule.Network.ClientReqSocket sender)
        {
            UpDownClient tocheck = client;
            UpDownClient found_client = null;
            UpDownClient found_client2 = null;
            foreach (UpDownClient cur_client in clientList_)
            {
                if (tocheck.Compare(cur_client, false))
                { //matching userhash
                    found_client2 = cur_client;
                }
                if (tocheck.Compare(cur_client, true))
                {	 //matching IP
                    found_client = cur_client;
                    break;
                }
            }
            if (found_client == null)
                found_client = found_client2;

            if (found_client != null)
            {
                if (tocheck == found_client)
                {
                    //we found the same client instance (client may have sent more than one OP_HELLO). do not delete that client!
                    return true;
                }
                if (sender != null)
                {
                    if (found_client.ClientSocket != null)
                    {
                        if (found_client.ClientSocket.IsConnected
                            && (found_client.IP != tocheck.IP ||
                            found_client.UserPort != tocheck.UserPort))
                        {
                            // if found_client is connected and has the IS_IDENTIFIED, it's safe to say that the other one is a bad guy
                            if (found_client.Credits != null &&
                                found_client.Credits.GetCurrentIdentState(found_client.IP) == IdentStateEnum.IS_IDENTIFIED)
                            {
                                tocheck.Ban();
                                return false;
                            }

                            //IDS_CLIENTCOL Warning: Found matching client, to a currently connected client: %s (%s) and %s (%s)
                            return false;
                        }
                        found_client.ClientSocket.Close();
                    }
                    found_client.ClientSocket = sender;
                    tocheck.ClientSocket = null;
                }
                client = null;
                client = found_client;
                return true;
            }
            return false;
        }

        public UpDownClient FindClientByIP(uint clientip, uint port)
        {
            foreach (UpDownClient cur_client in clientList_)
            {
                if (cur_client.IP == clientip && cur_client.UserPort == port)
                    return cur_client;
            }
            return null;
        }

        public UpDownClient FindClientByUserHash(byte[] clienthash, uint dwIP, ushort nTCPPort)
        {
            UpDownClient pFound = null;
            foreach (UpDownClient cur_client in clientList_)
            {
                if (MpdUtilities.Md4Cmp(cur_client.UserHash, clienthash) == 0)
                {
                    if ((dwIP == 0 || dwIP == cur_client.IP) &&
                        (nTCPPort == 0 || nTCPPort == cur_client.UserPort))
                        return cur_client;
                    else
                        pFound = pFound != null ? pFound : cur_client;
                }
            }
            return pFound;
        }

        public UpDownClient FindClientByIP(uint clientip)
        {
            foreach (UpDownClient cur_client in clientList_)
            {
                if (cur_client.IP == clientip)
                    return cur_client;
            }
            return null;
        }

        public UpDownClient FindClientByIP_UDP(uint clientip, uint nUDPport)
        {
            foreach (UpDownClient cur_client in clientList_)
            {
                if (cur_client.IP == clientip && cur_client.UDPPort == nUDPport)
                    return cur_client;
            }
            return null;
        }

        public UpDownClient FindClientByServerID(uint uServerIP, uint uUserID)
        {
            uint uHybridUserID = (uint)IPAddress.NetworkToHostOrder(uUserID);
            foreach (UpDownClient cur_client in clientList_)
            {
                if (cur_client.ServerIP == uServerIP &&
                    cur_client.UserIDHybrid == uHybridUserID)
                    return cur_client;
            }
            return null;
        }

        public UpDownClient FindClientByUserID_KadPort(uint clientID, ushort kadPort)
        {
            foreach (UpDownClient cur_client in clientList_)
            {
                if (cur_client.UserIDHybrid == clientID && cur_client.KadPort == kadPort)
                    return cur_client;
            }
            return null;
        }

        public UpDownClient FindClientByIP_KadPort(uint ip, ushort port)
        {
            foreach (UpDownClient cur_client in clientList_)
            {
                if (cur_client.IP == ip && cur_client.KadPort == port)
                    return cur_client;
            }
            return null;
        }

        public void AddBannedClient(uint dwIP)
        {
            bannedList_[dwIP] = MpdUtilities.GetTickCount();
        }

        public bool IsBannedClient(uint dwIP)
        {
            if (bannedList_.ContainsKey(dwIP))
            {
                uint dwBantime = bannedList_[dwIP];
                if (dwBantime + MuleConstants.CLIENTBANTIME > MpdUtilities.GetTickCount())
                    return true;
            }
            return false;
        }

        public void RemoveBannedClient(uint dwIP)
        {
            if (bannedList_.ContainsKey(dwIP))
                bannedList_.Remove(dwIP);
        }

        public uint BannedCount
        {
            get { return (uint)bannedList_.Count; }
        }

        public void RemoveAllBannedClients()
        {
            bannedList_.Clear();
        }

        public void AddTrackClient(UpDownClient toadd)
        {
            DeletedClient pResult = null;
            if (trackedClientsList_.ContainsKey(toadd.IP))
            {
                pResult = trackedClientsList_[toadd.IP];
                pResult.m_dwInserted = MpdUtilities.GetTickCount();
                for (int i = 0; i != pResult.m_ItemsList.Count; i++)
                {
                    if (pResult.m_ItemsList[i].nPort == toadd.UserPort)
                    {
                        // already tracked, update
                        pResult.m_ItemsList[i].pHash = toadd.Credits;
                        return;
                    }
                }
                PORTANDHASH porthash = new PORTANDHASH(toadd.UserPort, toadd.Credits);
                pResult.m_ItemsList.Add(porthash);
            }
            else
            {
                trackedClientsList_[toadd.IP] = new DeletedClient(toadd);
            }
        }

        public bool ComparePriorUserhash(uint dwIP, ushort nPort, byte[] pNewHash)
        {
            DeletedClient pResult = null;
            if (trackedClientsList_.ContainsKey(dwIP))
            {
                pResult = trackedClientsList_[dwIP];
                for (int i = 0; i != pResult.m_ItemsList.Count; i++)
                {
                    if (pResult.m_ItemsList[i].nPort == nPort)
                    {
                        if (pResult.m_ItemsList[i].pHash != pNewHash)
                            return false;
                        else
                            break;
                    }
                }
            }
            return true;
        }

        public uint GetClientsFromIP(uint dwIP)
        {
            DeletedClient pResult;
            if (trackedClientsList_.ContainsKey(dwIP))
            {
                pResult = trackedClientsList_[dwIP];
                return (uint)pResult.m_ItemsList.Count;
            }
            return 0;
        }

        public void TrackBadRequest(UpDownClient upcClient, int nIncreaseCounter)
        {
            DeletedClient pResult = null;
            if (upcClient.IP == 0)
            {
                Debug.Assert(false);
                return;
            }
            if (trackedClientsList_.ContainsKey(upcClient.IP))
            {
                pResult = trackedClientsList_[upcClient.IP];
                pResult.m_dwInserted = MpdUtilities.GetTickCount();
                pResult.m_cBadRequest += (uint)nIncreaseCounter;
            }
            else
            {
                DeletedClient ccToAdd = new DeletedClient(upcClient);
                ccToAdd.m_cBadRequest = (uint)nIncreaseCounter;
                trackedClientsList_[upcClient.IP] = ccToAdd;
            }
        }

        public uint GetBadRequests(UpDownClient upcClient)
        {
            DeletedClient pResult = null;
            if (upcClient.IP == 0)
            {
                return 0;
            }
            if (trackedClientsList_.ContainsKey(upcClient.IP))
            {
                pResult = trackedClientsList_[upcClient.IP];
                return pResult.m_cBadRequest;
            }
            else
                return 0;
        }

        public uint TrackedCount
        {
            get { return (uint)trackedClientsList_.Count; }
        }

        public void RemoveAllTrackedClients()
        {
            trackedClientsList_.Clear();
        }

        public bool RequestTCP(Kademlia.KadContact contact, byte byConnectOptions)
        {
            uint nContactIP = (uint)IPAddress.NetworkToHostOrder(contact.IPAddress);
            // don't connect ourself
            if (MuleApplication.Instance.ServerConnect.LocalIP == nContactIP &&
                MuleApplication.Instance.Preference.Port == contact.TCPPort)
                return false;

            UpDownClient pNewClient = FindClientByIP(nContactIP, contact.TCPPort);

            if (pNewClient == null)
                pNewClient = MuleApplication.Instance.CoreObjectManager.CreateUpDownClient(0, contact.TCPPort, contact.IPAddress, 0, 0, false);
            else if (pNewClient.KadState != KadStateEnum.KS_NONE)
                return false; // already busy with this client in some way (probably buddy stuff), don't mess with it

            //Add client to the lists to be processed.
            pNewClient.KadPort = contact.UDPPort;
            pNewClient.KadState = KadStateEnum.KS_QUEUED_FWCHECK;
            if (contact.ClientID != null)
            {
                pNewClient.UserHash = contact.ClientID.Bytes;
                pNewClient.SetConnectOptions(byConnectOptions, true, false);
            }
            kadList_.Add(pNewClient);
            //This method checks if this is a dup already.
            AddClient(pNewClient);
            return true;
        }

        public void RequestBuddy(Kademlia.KadContact contact, byte byConnectOptions)
        {
            uint nContactIP = (uint)IPAddress.NetworkToHostOrder(contact.IPAddress);
            // don't connect ourself
            if (MuleApplication.Instance.ServerConnect.LocalIP == nContactIP &&
                MuleApplication.Instance.Preference.Port == contact.TCPPort)
                return;
            UpDownClient pNewClient = FindClientByIP(nContactIP, contact.TCPPort);
            if (pNewClient == null)
                pNewClient =
                    MuleApplication.Instance.CoreObjectManager.CreateUpDownClient(0,
                    contact.TCPPort, contact.IPAddress, 0, 0, false);
            else if (pNewClient.KadState != KadStateEnum.KS_NONE)
                return; // already busy with this client in some way (probably fw stuff), don't mess with it
            else if (IsKadFirewallCheckIP(nContactIP))
            { // doing a kad firewall check with this IP, abort 
                return;
            }
            //Add client to the lists to be processed.
            pNewClient.KadPort = contact.UDPPort;
            pNewClient.KadState = KadStateEnum.KS_QUEUED_BUDDY;
            pNewClient.UserHash = contact.ClientID.Bytes;
            pNewClient.SetConnectOptions(byConnectOptions, true, false);
            AddToKadList(pNewClient);
            //This method checks if this is a dup already.
            AddClient(pNewClient);
        }

        public bool IncomingBuddy(Kademlia.KadContact contact, Mpd.Generic.UInt128 buddyID)
        {
            uint nContactIP = (uint)IPAddress.NetworkToHostOrder(contact.IPAddress);
            //If eMule already knows this client, abort this.. It could cause conflicts.
            //Although the odds of this happening is very small, it could still happen.
            if (FindClientByIP(nContactIP, contact.TCPPort) != null)
                return false;
            else if (IsKadFirewallCheckIP(nContactIP))
            { // doing a kad firewall check with this IP, abort 
                return false;
            }
            else if (MuleApplication.Instance.ServerConnect.LocalIP == nContactIP &&
                MuleApplication.Instance.Preference.Port == contact.TCPPort)
                return false; // don't connect ourself

            //Add client to the lists to be processed.
            UpDownClient pNewClient = MuleApplication.Instance.CoreObjectManager.CreateUpDownClient(0,
                contact.TCPPort, contact.IPAddress, 0, 0, false);
            pNewClient.KadPort = contact.UDPPort;
            pNewClient.KadState = KadStateEnum.KS_INCOMING_BUDDY;
            pNewClient.UserHash = contact.ClientID.Bytes;
            pNewClient.BuddyID = buddyID.Bytes;
            AddToKadList(pNewClient);
            AddClient(pNewClient);
            return true;
        }

        public void RemoveFromKadList(UpDownClient torem)
        {
            if (kadList_.Contains(torem))
            {
                if (torem == buddy_)
                {
                    buddy_ = null;
                }
                kadList_.Remove(torem);
            }
        }

        public void AddToKadList(UpDownClient toadd)
        {
            if (toadd == null)
                return;

            if (kadList_.Contains(toadd))
            {
                return;
            }
            kadList_.Add(toadd);
        }

        public bool DoRequestFirewallCheckUDP(Kademlia.KadContact contact)
        {
            // first make sure we don't know this IP already from somewhere
            if (FindClientByIP((uint)IPAddress.NetworkToHostOrder(contact.IPAddress)) != null)
                return false;
            // fine, justcreate the client object, set the state and wait
            // TODO: We don't know the clients usershash, this means we cannot build an obfuscated connection, which 
            // again mean that the whole check won't work on "Require Obfuscation" setting, which is not a huge problem,
            // but certainly not nice. Only somewhat acceptable way to solve this is to use the KadID instead.
            UpDownClient pNewClient =
                MuleApplication.Instance.CoreObjectManager.CreateUpDownClient(0,
                contact.TCPPort, contact.IPAddress, 0, 0, false);
            pNewClient.KadState = KadStateEnum.KS_QUEUED_FWCHECK_UDP;
            AddToKadList(pNewClient);
            AddClient(pNewClient);
            return true;
        }

        public BuddyStateEnum BuddyStatus
        {
            get { return buddyStatus_; }
        }

        public UpDownClient Buddy
        {
            get { return buddy_; }
        }

        public void AddKadFirewallRequest(uint dwIP)
        {
            IPANDTICS add = new IPANDTICS(dwIP, MpdUtilities.GetTickCount());
            firewallCheckRequests_.Insert(0, add);
            while (firewallCheckRequests_.Count > 0)
            {
                if (MpdUtilities.GetTickCount() - firewallCheckRequests_.Last().dwInserted > MuleConstants.ONE_SEC_MS * 180)
                    firewallCheckRequests_.Remove(firewallCheckRequests_.Last());
                else
                    break;
            }
        }

        public bool IsKadFirewallCheckIP(uint dwIP)
        {
            foreach (IPANDTICS v in firewallCheckRequests_)
            {
                if (v.dwIP == dwIP && MpdUtilities.GetTickCount() - v.dwInserted < MuleConstants.ONE_SEC_MS * 180)
                    return true;
            }
            return false;
        }

        public void AddTrackCallbackRequests(uint dwIP)
        {
            IPANDTICS add = new IPANDTICS(dwIP, MpdUtilities.GetTickCount());
            directCallbackRequests_.Insert(0, add);
            while (directCallbackRequests_.Count > 0)
            {
                if (MpdUtilities.GetTickCount() - directCallbackRequests_.Last().dwInserted > MuleConstants.ONE_MIN_MS * 3)
                    directCallbackRequests_.Remove(directCallbackRequests_.Last());
                else
                    break;
            }
        }

        public bool AllowCalbackRequest(uint dwIP)
        {
            foreach (IPANDTICS c in directCallbackRequests_)
            {
                if (c.dwIP == dwIP && MpdUtilities.GetTickCount() - c.dwInserted < MuleConstants.ONE_MIN_MS * 3)
                    return false;
            }
            return true;
        }

        public void AddConnectingClient(UpDownClient pToAdd)
        {
            foreach (CONNECTINGCLIENT client in connectingClients_)
            {
                if (client.pClient == pToAdd)
                {
                    return;
                }
            }
            CONNECTINGCLIENT cc = new CONNECTINGCLIENT(pToAdd, MpdUtilities.GetTickCount());
            connectingClients_.Add(cc);
        }

        public void RemoveConnectingClient(UpDownClient pToRemove)
        {
            foreach (CONNECTINGCLIENT client in connectingClients_)
            {
                if (client.pClient == pToRemove)
                {
                    connectingClients_.Remove(client);
                    return;
                }
            }
        }

        public void Process()
        {
            ///////////////////////////////////////////////////////////////////////////
            // Cleanup banned client list
            //
            uint cur_tick = MpdUtilities.GetTickCount();
            if (lastBannCleanUp_ + MuleConstants.BAN_CLEANUP_TIME < cur_tick)
            {
                lastBannCleanUp_ = cur_tick;

                Dictionary<uint, uint>.Enumerator pos = bannedList_.GetEnumerator();
                while (pos.MoveNext())
                {
                    if (pos.Current.Value + MuleConstants.CLIENTBANTIME < cur_tick)
                        RemoveBannedClient(pos.Current.Key);
                }
            }

            ///////////////////////////////////////////////////////////////////////////
            // Cleanup tracked client list
            //
            if (lastTrackedCleanUp_ + MuleConstants.TRACKED_CLEANUP_TIME < cur_tick)
            {
                lastTrackedCleanUp_ = cur_tick;
                Dictionary<uint, DeletedClient>.Enumerator pos = trackedClientsList_.GetEnumerator();
                while (pos.MoveNext())
                {
                    if (pos.Current.Value.m_dwInserted + MuleConstants.KEEPTRACK_TIME < cur_tick)
                    {
                        trackedClientsList_.Remove(pos.Current.Key);
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////
            // Process Kad client list
            //
            //We need to try to connect to the clients in m_KadList
            //If connected, remove them from the list and send a message back to Kad so we can send a ACK.
            //If we don't connect, we need to remove the client..
            //The sockets timeout should delete this object.
            // buddy is just a flag that is used to make sure we are still connected or connecting to a buddy.
            BuddyStateEnum buddy = BuddyStateEnum.Disconnected;

            for (int pos1 = 0; pos1 < kadList_.Count; pos1++)
            {
                UpDownClient cur_client = kadList_[pos1];
                if (!MuleApplication.Instance.KadEngine.IsRunning)
                {
                    //Clear out this list if we stop running Kad.
                    //Setting the Kad state to KS_NONE causes it to be removed in the switch below.
                    cur_client.KadState = KadStateEnum.KS_NONE;
                }
                switch (cur_client.KadState)
                {
                    case KadStateEnum.KS_QUEUED_FWCHECK:
                    case KadStateEnum.KS_QUEUED_FWCHECK_UDP:
                        //Another client asked us to try to connect to them to check their firewalled status.
                        cur_client.TryToConnect(true, true);
                        break;
                    case KadStateEnum.KS_CONNECTING_FWCHECK:
                        //Ignore this state as we are just waiting for results.
                        break;
                    case KadStateEnum.KS_FWCHECK_UDP:
                    case KadStateEnum.KS_CONNECTING_FWCHECK_UDP:
                        // we want a UDP firewallcheck from this client and are just waiting to get connected to send the request
                        break;
                    case KadStateEnum.KS_CONNECTED_FWCHECK:
                        //We successfully connected to the client.
                        //We now send a ack to let them know.
                        if (cur_client.KadVersion >= (byte)VersionsEnum.KADEMLIA_VERSION7_49a)
                        {
                            // the result is now sent per TCP instead of UDP, because this will fail if our intern UDP port is unreachable.
                            // But we want the TCP testresult regardless if UDP is firewalled, the new UDP state and test takes care of the rest					
                            Packet pPacket =
                                MuleApplication.Instance.NetworkObjectManager.CreatePacket(OperationCodeEnum.OP_KAD_FWTCPCHECK_ACK,
                                    0, MuleConstants.PROTOCOL_EMULEPROT);
                            if (!cur_client.SafeConnectAndSendPacket(pPacket))
                                cur_client = null;
                        }
                        else
                        {
                            MuleApplication.Instance.KadEngine.UDPListener.SendNullPacket(KadOperationCodeEnum.KADEMLIA_FIREWALLED_ACK_RES,
                                (uint)IPAddress.NetworkToHostOrder(cur_client.IP),
                                cur_client.KadPort, null, null);
                        }
                        //We are done with this client. Set Kad status to KS_NONE and it will be removed in the next cycle.
                        if (cur_client != null)
                            cur_client.KadState = KadStateEnum.KS_NONE;
                        break;

                    case KadStateEnum.KS_INCOMING_BUDDY:
                        //A firewalled client wants us to be his buddy.
                        //If we already have a buddy, we set Kad state to KS_NONE and it's removed in the next cycle.
                        //If not, this client will change to KS_CONNECTED_BUDDY when it connects.
                        if (buddyStatus_ == BuddyStateEnum.Connected)
                            cur_client.KadState = KadStateEnum.KS_NONE;
                        break;

                    case KadStateEnum.KS_QUEUED_BUDDY:
                        //We are firewalled and want to request this client to be a buddy.
                        //But first we check to make sure we are not already trying another client.
                        //If we are not already trying. We try to connect to this client.
                        //If we are already connected to a buddy, we set this client to KS_NONE and it's removed next cycle.
                        //If we are trying to connect to a buddy, we just ignore as the one we are trying may fail and we can then try this one.
                        if (buddyStatus_ == BuddyStateEnum.Disconnected)
                        {
                            buddy = BuddyStateEnum.Connecting;
                            buddyStatus_ = BuddyStateEnum.Connecting;
                            cur_client.KadState = KadStateEnum.KS_CONNECTING_BUDDY;
                            cur_client.TryToConnect(true, true);
                        }
                        else if (buddyStatus_ == BuddyStateEnum.Connected)
                            cur_client.KadState = KadStateEnum.KS_NONE;
                        break;

                    case KadStateEnum.KS_CONNECTING_BUDDY:
                        //We are trying to connect to this client.
                        //Although it should NOT happen, we make sure we are not already connected to a buddy.
                        //If we are we set to KS_NONE and it's removed next cycle.
                        //But if we are not already connected, make sure we set the flag to connecting so we know 
                        //things are working correctly.
                        if (buddyStatus_ == BuddyStateEnum.Connected)
                            cur_client.KadState = KadStateEnum.KS_NONE;
                        else
                        {
                            buddy = BuddyStateEnum.Connecting;
                        }
                        break;

                    case KadStateEnum.KS_CONNECTED_BUDDY:
                        //A potential connected buddy client wanting to me in the Kad network
                        //We set our flag to connected to make sure things are still working correctly.
                        buddy = BuddyStateEnum.Connected;

                        //If m_nBuddyStatus is not connected already, we set this client as our buddy!
                        if (buddyStatus_ != BuddyStateEnum.Connected)
                        {
                            buddy_ = cur_client;
                            buddyStatus_ = BuddyStateEnum.Connected;
                        }
                        if (buddy_ == cur_client &&
                            MuleApplication.Instance.IsFirewalled &&
                            cur_client.SendBuddyPingPong)
                        {
                            Packet buddyPing =
                                MuleApplication.Instance.NetworkObjectManager.CreatePacket(OperationCodeEnum.OP_BUDDYPING, 0,
                                MuleConstants.PROTOCOL_EMULEPROT);
                            MuleApplication.Instance.Statistics.AddUpDataOverheadOther(buddyPing.Size);
                            cur_client.SetLastBuddyPingPongTime();
                        }
                        break;

                    default:
                        RemoveFromKadList(cur_client);
                        pos1--;
                        break;
                }
            }

            //We either never had a buddy, or lost our buddy..
            if (buddy == BuddyStateEnum.Disconnected)
            {
                if (buddyStatus_ != BuddyStateEnum.Disconnected || buddy_ != null)
                {
                    if (MuleApplication.Instance.KadEngine.IsRunning &&
                        MuleApplication.Instance.IsFirewalled &&
                        MuleApplication.Instance.KadEngine.UDPFirewallTester.IsFirewalledUDP(true))
                    {
                        //We are a lowID client and we just lost our buddy.
                        //Go ahead and instantly try to find a new buddy.
                        MuleApplication.Instance.KadEngine.Preference.SetFindBuddy();
                    }
                    buddy_ = null;
                    buddyStatus_ = BuddyStateEnum.Disconnected;
                }
            }

            if (MuleApplication.Instance.KadEngine.IsConnected)
            {
                //we only need a buddy if direct callback is not available
                if (MuleApplication.Instance.KadEngine.IsFirewalled && MuleApplication.Instance.KadEngine.UDPFirewallTester.IsFirewalledUDP(true))
                {
                    //TODO 0.49b: Kad buddies won'T work with RequireCrypt, so it is disabled for now but should (and will)
                    //be fixed in later version
                    // Update: Buddy connections itself support obfuscation properly since 0.49a (this makes it work fine if our buddy uses require crypt)
                    // ,however callback requests don't support it yet so we wouldn't be able to answer callback requests with RequireCrypt, protocolchange intended for the next version
                    if (buddyStatus_ == BuddyStateEnum.Disconnected &&
                        MuleApplication.Instance.KadEngine.Preference.FindBuddy &&
                        !MuleApplication.Instance.Preference.IsClientCryptLayerRequired)
                    {
                        //We are a firewalled client with no buddy. We have also waited a set time 
                        //to try to avoid a false firewalled status.. So lets look for a buddy..
                        if (null == MuleApplication.Instance.KadEngine.SearchManager.PrepareLookup(KadSearchTypeEnum.FINDBUDDY, true,
                            MuleApplication.Instance.KadObjectManager.CreateUInt128(true).Xor(MuleApplication.Instance.KadEngine.Preference.KadID)))
                        {
                            //This search ID was already going. Most likely reason is that
                            //we found and lost our buddy very quickly and the last search hadn't
                            //had time to be removed yet. Go ahead and set this to happen again
                            //next time around.
                            MuleApplication.Instance.KadEngine.Preference.SetFindBuddy();
                        }
                    }
                }
                else
                {
                    if (buddy_ != null)
                    {
                        //Lets make sure that if we have a buddy, they are firewalled!
                        //If they are also not firewalled, then someone must have fixed their firewall or stopped saturating their line.. 
                        //We just set the state of this buddy to KS_NONE and things will be cleared up with the next cycle.
                        if (!buddy_.HasLowID)
                            buddy_.KadState = KadStateEnum.KS_NONE;
                    }
                }
            }
            else
            {
                if (buddy_ != null)
                {
                    //We are not connected anymore. Just set this buddy to KS_NONE and things will be cleared out on next cycle.
                    buddy_.KadState = KadStateEnum.KS_NONE;
                }
            }

            ///////////////////////////////////////////////////////////////////////////
            // Cleanup client list
            //
            CleanUpClientList();

            ///////////////////////////////////////////////////////////////////////////
            // Process Direct Callbacks for Timeouts
            //
            ProcessConnectingClientsList();
        }

        public bool IsValidClient(UpDownClient tocheck)
        {
            return clientList_.Contains(tocheck);
        }

        public bool GiveClientsForTraceRoute()
        {
            return MuleApplication.Instance.LastCommonRouteFinder.AddHostsToCheck(clientList_);
        }

        public void ProcessA4AFClients()
        {
            //if(thePrefs.GetLogA4AF()) AddDebugLogLine(false, _T(">>> Starting A4AF check"));
            int pos = 0;
            while (pos < clientList_.Count)
            {
                UpDownClient cur_client = clientList_[pos];

                int count = clientList_.Count;

                if (cur_client.DownloadState != DownloadStateEnum.DS_DOWNLOADING &&
                   cur_client.DownloadState != DownloadStateEnum.DS_CONNECTED &&
                   (cur_client.OtherRequestsList.Count > 0 || cur_client.OtherNoNeededList.Count > 0))
                {
                    cur_client.SwapToAnotherFile("Periodic A4AF check CClientList::ProcessA4AFClients()", false, false, false, null, true, false);
                }

                if (count == clientList_.Count)
                    pos++;
            }
        }

        public DeadSourceList DeadSourceList
        {
            get;
            set;
        }

        #endregion

        #region Protected
        protected void CleanUpClientList()
        {
            // we remove clients which are not needed any more by time
            // this check is also done on CUpDownClient::Disconnected, however it will not catch all
            // cases (if a client changes the state without beeing connected
            //
            // Adding this check directly to every point where any state changes would be more effective,
            // is however not compatible with the current code, because there are points where a client has
            // no state for some code lines and the code is also not prepared that a client object gets
            // invalid while working with it (aka setting a new state)
            // so this way is just the easy and safe one to go (as long as emule is basically single threaded)
            uint cur_tick = MpdUtilities.GetTickCount();
            if (lastClientCleanUp_ + MuleConstants.CLIENTLIST_CLEANUP_TIME < cur_tick)
            {
                lastClientCleanUp_ = cur_tick;
                int pos = 0;

                while (pos < clientList_.Count)
                {
                    UpDownClient pCurClient = clientList_[pos];
                    if ((pCurClient.UploadState == UploadStateEnum.US_NONE ||
                        pCurClient.UploadState == UploadStateEnum.US_BANNED &&
                        !pCurClient.IsBanned)
                        && pCurClient.DownloadState == DownloadStateEnum.DS_NONE
                        && pCurClient.ChatState == ChatStateEnum.MS_NONE
                        && pCurClient.KadState == KadStateEnum.KS_NONE
                        && pCurClient.ClientSocket == null)
                    {
                        pCurClient.CleanUp();
                    }
                    else
                    {
                        pos++;
                    }
                }
            }
        }

        protected void ProcessConnectingClientsList()
        {
            // we do check if any connects have timed out by now
            uint cur_tick = MpdUtilities.GetTickCount();
            int pos1 = 0;
            while (pos1 < connectingClients_.Count)
            {
                CONNECTINGCLIENT cc = connectingClients_[pos1];
                if (cc.dwInserted + MuleConstants.ONE_SEC_MS * 45 < cur_tick)
                {
                    connectingClients_.RemoveAt(pos1);
                    if (cc.pClient.Disconnected("Connectiontry Timeout"))
                        cc.pClient.CleanUp();
                }
                else
                {
                    pos1++;
                }
            }
        }
        #endregion
    }
}
