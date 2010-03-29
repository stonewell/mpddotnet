using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Utilities;

namespace Mule.Core.Impl
{
    struct PORTANDHASH
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
        public uint dwIP;
        public uint dwInserted;
    };

    struct CONNECTINGCLIENT
    {
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

    enum BuddyStateEnum
    {
        Disconnected,
        Connecting,
        Connected
    };

    class ClientListImpl : ClientList
    {
        #region Fields
        private UpDownClientList list = new UpDownClientList();
        private UpDownClientList m_KadList = new UpDownClientList();
        private Dictionary<uint, uint> m_bannedList = new Dictionary<uint, uint>();
        private Dictionary<uint, DeletedClient> m_trackedClientsList = new Dictionary<uint, DeletedClient>();
        private uint m_dwLastBannCleanUp;
        private uint m_dwLastTrackedCleanUp;
        private uint m_dwLastClientCleanUp;
        private UpDownClient m_pBuddy;
        private BuddyStateEnum m_nBuddyStatus;
        private List<IPANDTICS> listFirewallCheckRequests = new List<IPANDTICS>();
        private List<IPANDTICS> listDirectCallbackRequests = new List<IPANDTICS>();
        private List<CONNECTINGCLIENT> m_liConnectingClients = new List<CONNECTINGCLIENT>();
        #endregion

        #region Constructors
        public ClientListImpl()
        {
            m_dwLastBannCleanUp = 0;
            m_dwLastTrackedCleanUp = 0;
            m_dwLastClientCleanUp = 0;
            m_nBuddyStatus = BuddyStateEnum.Disconnected;
            DeadSourceList.Init(true);
            m_pBuddy = null;
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
                if (list.Contains(toadd))
                    return;
            }
            list.Add(toadd);
        }

        public void RemoveClient(UpDownClient toremove)
        {
            RemoveClient(toremove, null);
        }

        public void RemoveClient(UpDownClient toremove, string pszReason)
        {
            if (list.Contains(toremove))
            {
                MuleApplication.Instance.UploadQueue.RemoveFromUploadQueue(toremove,
                    string.Format("CClientList::RemoveClient: {0}" , pszReason));
                MuleApplication.Instance.UploadQueue.RemoveFromWaitingQueue(toremove);
                MuleApplication.Instance.DownloadQueue.RemoveSource(toremove);
                list.Remove(toremove);
            }
            RemoveFromKadList(toremove);
            RemoveConnectingClient(toremove);
        }

        public void GetStatistics(ref uint totalclient, int[] stats, Dictionary<uint, uint> clientVersionEDonkey, Dictionary<uint, uint> clientVersionEDonkeyHybrid, Dictionary<uint, uint> clientVersionEMule, Dictionary<uint, uint> clientVersionAMule)
        {
            totalclient = (uint)list.Count;
            Array.Clear(stats, 0, stats.Length);

            foreach (UpDownClient cur_client in list)
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

        public uint GetClientCount
        {
            get { throw new NotImplementedException(); }
        }

        public void DeleteAll()
        {
            MuleApplication.Instance.UploadQueue.DeleteAll();
            MuleApplication.Instance.DownloadQueue.DeleteAll();
            list.Clear();
        }

        public bool AttachToAlreadyKnown(ref UpDownClient client, Mule.Network.ClientReqSocket sender)
        {
            UpDownClient tocheck = client;
            UpDownClient found_client = null;
            UpDownClient found_client2 = null;
            foreach(UpDownClient cur_client in list)
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
                        found_client.ClientSocket.Client = null;
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
            throw new NotImplementedException();
        }

        public UpDownClient FindClientByUserHash(byte[] clienthash, uint dwIP, ushort nTCPPort)
        {
            throw new NotImplementedException();
        }

        public UpDownClient FindClientByIP(uint clientip)
        {
            throw new NotImplementedException();
        }

        public UpDownClient FindClientByIP_UDP(uint clientip, uint nUDPport)
        {
            throw new NotImplementedException();
        }

        public UpDownClient FindClientByServerID(uint uServerIP, uint uUserID)
        {
            throw new NotImplementedException();
        }

        public UpDownClient FindClientByUserID_KadPort(uint clientID, ushort kadPort)
        {
            throw new NotImplementedException();
        }

        public UpDownClient FindClientByIP_KadPort(uint ip, ushort port)
        {
            throw new NotImplementedException();
        }

        public void AddBannedClient(uint dwIP)
        {
            throw new NotImplementedException();
        }

        public bool IsBannedClient(uint dwIP)
        {
            throw new NotImplementedException();
        }

        public void RemoveBannedClient(uint dwIP)
        {
            throw new NotImplementedException();
        }

        public uint BannedCount
        {
            get { throw new NotImplementedException(); }
        }

        public void RemoveAllBannedClients()
        {
            throw new NotImplementedException();
        }

        public void AddTrackClient(UpDownClient toadd)
        {
            throw new NotImplementedException();
        }

        public bool ComparePriorUserhash(uint dwIP, ushort nPort, byte[] pNewHash)
        {
            throw new NotImplementedException();
        }

        public uint GetClientsFromIP(uint dwIP)
        {
            throw new NotImplementedException();
        }

        public void TrackBadRequest(UpDownClient upcClient, int nIncreaseCounter)
        {
            throw new NotImplementedException();
        }

        public uint GetBadRequests(UpDownClient upcClient)
        {
            throw new NotImplementedException();
        }

        public uint TrackedCount
        {
            get { throw new NotImplementedException(); }
        }

        public void RemoveAllTrackedClients()
        {
            throw new NotImplementedException();
        }

        public bool RequestTCP(Kademlia.KadContact contact, byte byConnectOptions)
        {
            throw new NotImplementedException();
        }

        public void RequestBuddy(Kademlia.KadContact contact, byte byConnectOptions)
        {
            throw new NotImplementedException();
        }

        public bool IncomingBuddy(Kademlia.KadContact contact, Mpd.Generic.UInt128 buddyID)
        {
            throw new NotImplementedException();
        }

        public void RemoveFromKadList(UpDownClient torem)
        {
            throw new NotImplementedException();
        }

        public void AddToKadList(UpDownClient toadd)
        {
            throw new NotImplementedException();
        }

        public bool DoRequestFirewallCheckUDP(Kademlia.KadContact contact)
        {
            throw new NotImplementedException();
        }

        public byte BuddyStatus
        {
            get { throw new NotImplementedException(); }
        }

        public UpDownClient Buddy
        {
            get { throw new NotImplementedException(); }
        }

        public void AddKadFirewallRequest(uint dwIP)
        {
            throw new NotImplementedException();
        }

        public bool IsKadFirewallCheckIP(uint dwIP)
        {
            throw new NotImplementedException();
        }

        public void AddTrackCallbackRequests(uint dwIP)
        {
            throw new NotImplementedException();
        }

        public bool AllowCalbackRequest(uint dwIP)
        {
            throw new NotImplementedException();
        }

        public void AddConnectingClient(UpDownClient pToAdd)
        {
            throw new NotImplementedException();
        }

        public void RemoveConnectingClient(UpDownClient pToRemove)
        {
            throw new NotImplementedException();
        }

        public void Process()
        {
            throw new NotImplementedException();
        }

        public bool IsValidClient(UpDownClient tocheck)
        {
            throw new NotImplementedException();
        }

        public void Debug_SocketDeleted(Mule.Network.ClientReqSocket deleted)
        {
            throw new NotImplementedException();
        }

        public bool GiveClientsForTraceRoute()
        {
            return MuleApplication.Instance.LastCommonRouteFinder.AddHostsToCheck(list);
        }

        public void ProcessA4AFClients()
        {
            throw new NotImplementedException();
        }

        public DeadSourceList DeadSourceList
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region Protected
        protected void CleanUpClientList()
        {
        }

        protected void ProcessConnectingClientsList()
        {
        }
        #endregion
    }
}
