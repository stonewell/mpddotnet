using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core.Impl
{
    class ClientListImpl : ClientList
    {
        #region ClientList Members

        public void AddClient(UpDownClient toadd)
        {
            throw new NotImplementedException();
        }

        public void AddClient(UpDownClient toadd, bool bSkipDupTest)
        {
            throw new NotImplementedException();
        }

        public void RemoveClient(UpDownClient toremove)
        {
            throw new NotImplementedException();
        }

        public void RemoveClient(UpDownClient toremove, string pszReason)
        {
            throw new NotImplementedException();
        }

        public void GetStatistics(ref uint totalclient, int[] stats, Dictionary<uint, uint> clientVersionEDonkey, Dictionary<uint, uint> clientVersionEDonkeyHybrid, Dictionary<uint, uint> clientVersionEMule, Dictionary<uint, uint> clientVersionAMule)
        {
            throw new NotImplementedException();
        }

        public uint GetClientCount
        {
            get { throw new NotImplementedException(); }
        }

        public void DeleteAll()
        {
            throw new NotImplementedException();
        }

        public bool AttachToAlreadyKnown(out UpDownClient client, Mule.Network.ClientReqSocket sender)
        {
            throw new NotImplementedException();
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

        public bool GiveClientsForTraceRoute
        {
            get { throw new NotImplementedException(); }
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
    }
}
