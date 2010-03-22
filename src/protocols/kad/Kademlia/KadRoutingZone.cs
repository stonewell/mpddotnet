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
using Mpd.Generic;

namespace Kademlia
{
    public class KadEventMap : Dictionary<KadRoutingZone, KadRoutingZone>
    {
    }

    public interface KadRoutingZone
    {
        uint Consolidate();
        bool OnBigTimer();
        void OnSmallTimer();
        bool Add(UInt128 uID, uint uIP, ushort uUDPPort,
            ushort uTCPPort, byte uVersion,
            KadUDPKey cUDPKey,
            ref bool bIPVerified,
            bool bUpdate,
            bool bFromNodesDat, bool bFromHello);
        bool AddUnfiltered(UInt128 uID, uint uIP, ushort uUDPPort,
            ushort uTCPPort, byte uVersion,
            KadUDPKey cUDPKey, ref bool bIPVerified,
            bool bUpdate, bool bFromNodesDat, bool bFromHello);
        bool Add(KadContact pContact, ref bool bUpdate,
            ref bool bOutIPVerified);
        void ReadFile(/*string strSpecialNodesdate = _T("")*/);
        void ReadFile(string strSpecialNodesdate);
        bool VerifyContact(UInt128 uID, uint uIP);
        KadContact GetContact(UInt128 uID);
        KadContact GetContact(uint uIP, ushort nPort, bool bTCPPort);
        KadContact GetRandomContact(uint nMaxType, uint nMinKadVersion);
        uint GetNumContacts();
        void GetNumContacts(ref uint nInOutContacts, ref uint nInOutFilteredContacts, byte byMinVersion);
        void GetAllEntries(KadContactList plistResult/*, bool bEmptyFirst = true*/);
        void GetAllEntries(KadContactList plistResult, bool bEmptyFirst);
        void GetClosestTo(uint uMaxType, UInt128 uTarget,
            UInt128 uDistance, uint uMaxRequired,
            KadContactMap plistResult/*, 
                bool bEmptyFirst = true, bool bSetInUse = false*/
                                                             );
        void GetClosestTo(uint uMaxType, UInt128 uTarget,
            UInt128 uDistance, uint uMaxRequired,
            KadContactMap plistResult,
            bool bEmptyFirst/* = true, bool bSetInUse = false*/);
        void GetClosestTo(uint uMaxType, UInt128 uTarget,
            UInt128 uDistance, uint uMaxRequired,
            KadContactMap plistResult,
            bool bEmptyFirst, bool bSetInUse);
        uint GetBootstrapContacts(KadContactList plistResult, uint uMaxRequired);
        uint EstimateCount();
        ulong NextBigTimer { get;set; }
        ulong NextSmallTimer { get;set; }
    }
}
