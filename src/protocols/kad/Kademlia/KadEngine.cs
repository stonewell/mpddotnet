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
using Kademlia.Preference;
using Mpd.Generic.Types;

namespace Kademlia
{
    public sealed class KadEngine
    {
        #region Fields
        private KadObjectManager objectManager_ = null;
        private KadSearchManager searchManager_ = null;
        private KadUDPFirewallTester udpFirewallTester_ = null;
        #endregion

        #region Constructors
        public KadEngine()
        {
            objectManager_ = new KadObjectManager(this);
        }
        #endregion

        #region Properties
        public KadObjectManager ObjectManager
        {
            get { return objectManager_; }
        }

        public KadSearchManager SearchManager
        {
            get
            {
                return searchManager_;
            }
        }

        public KadUDPFirewallTester UDPFirewallTester
        {
            get
            {
                return udpFirewallTester_;
            }
        }
        #endregion

        #region Methods
        public void Start()
        {
        }

        public void Start(KadPreference pPrefs)
        {
        }

        public void Stop()
        {
        }

        public KadPreference Preference
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public KadRoutingZone KadRoutingZone
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public KadUDPListener UDPListener
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public KadIndexed Indexed
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public bool IsRunning
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public bool IsConnected
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public bool IsFirewalled
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public void RecheckFirewalled()
        {
        }

        public uint GetKademliaUsers(/*bool bNewMethod = false*/)
        {
            return GetKademliaUsers(false);
        }

        public uint GetKademliaUsers(bool bNewMethod/* = false*/)
        {
            throw new NotImplementedException();
        }

        public uint KademliaFiles
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public uint TotalStoreKey
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public uint TotalStoreSrc
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public uint TotalStoreNotes
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public uint TotalFile
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public bool Publish
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public uint IPAddress
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public void Bootstrap(uint uIP, ushort uPort, bool bKad2)
        {
            throw new NotImplementedException();
        }

        public void Bootstrap(string szHost, ushort uPort, bool bKad2)
        {
            throw new NotImplementedException();
        }
        public void ProcessPacket(byte[] pbyData,
            uint uIP, ushort uPort, bool bValidReceiverKey, KadUDPKey senderUDPKey)
        {
            throw new NotImplementedException();
        }
        public void AddEvent(KadRoutingZone pZone)
        {
            throw new NotImplementedException();
        }
        public void RemoveEvent(KadRoutingZone pZone)
        {
            throw new NotImplementedException();
        }
        public void Process()
        {
            throw new NotImplementedException();
        }
        public bool InitUnicode()
        {
            throw new NotImplementedException();
        }
        public void StatsAddClosestDistance(UInt128 uDist)
        {
            throw new NotImplementedException();
        }

        public bool FindNodeIDByIP(KadClientSearcher rRequester, 
            uint dwIP, ushort nTCPPort, ushort nUDPPort)
        {
            throw new NotImplementedException();
        }
        public bool FindIPByNodeID(KadClientSearcher rRequester, byte[] pachNodeID)
        {
            throw new NotImplementedException();
        }
        public void CancelClientSearch(KadClientSearcher rFromRequester)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
