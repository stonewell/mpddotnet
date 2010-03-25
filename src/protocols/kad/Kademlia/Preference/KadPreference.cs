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

namespace Kademlia.Preference
{
    public interface KadPreference
    {
        void GetKadID(ref UInt128 puID);
        void GetKadID(ref string psID);
        UInt128 KadID { get; set; }
        void GetClientHash(ref UInt128 puID);
        void GetClientHash(ref string psID);
        UInt128 ClientHash { get; set; }
        uint IPAddress { get; set; }
        bool RecheckIP { get; }
        void SetRecheckIP();
        void IncRecheckIP();
        bool HasHadContact { get; }
        void SLastContact();
        bool HasLostConnection { get; }
        uint LastContact { get; }
        bool IsFirewalled { get; }
        void SetFirewalled();
        void IncFirewalled();

        byte TotalFile { get; set; }
        byte TotalStoreSrc { get; set; }
        byte TotalStoreKey { get; set; }
        byte TotalSource { get; set; }
        byte TotalNotes { get; set; }
        byte TotalStoreNotes { get; set; }
        uint KademliaUsers { get; set; }
        uint KademliaFiles { get; }
        void SetKademliaFiles();
        bool IsPublish { get; set; }
        bool FindBuddy { get; set; }
        void SetFindBuddy();
        bool UseExternKadPort { get; set; }
        ushort ExternalKadPort { get; }
        void SetExternKadPort(ushort uVal, uint nFromIP);
        bool FindExternKadPort();
        bool FindExternKadPort(bool bReset);
        ushort InternKadPort { get; }
        byte GetMyConnectOptions();
        byte GetMyConnectOptions(bool bEncryption);
        byte GetMyConnectOptions(bool bEncryption, bool bCallback);
        void StatsIncUDPFirewalledNodes(bool bFirewalled);
        void StatsIncTCPFirewalledNodes(bool bFirewalled);
        float StatsFirewalledRatio(bool bUDP);
        float StatsKadV8Ratio();

        uint GetUDPVerifyKey(uint dwTargetIP);
    }
}
