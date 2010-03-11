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
using Mpd.Generic.Types;

namespace Kademlia
{
    public class KadContactMap : Dictionary<UInt128, KadContact>
    {
    }

    public class KadContactList : List<KadContact>
    {
    }

    public interface KadContact
    {
        void GetClientID(ref UInt128 puId);
        void GetClientID(ref string puId);
        UInt128 ClientID { get; set;}

        void GetDistance(ref UInt128 puDistance);
        void GetDistance(ref string psDistance);
        UInt128 GetDistance();

        uint IPAddress { get; set; }
        void GetIPAddress(string psIp);

        ushort TCPPort { get;set;}
        void GetTCPPort(string psPort);

        ushort UDPPort { get;set;}
        void GetUDPPort(string psPort);

        byte GetType();
        void UpdateType();
        void CheckingType();

        bool GuiRefs { get; set;}

        bool InUse { get;}
        void IncUse();
        void DecUse();

        byte Version { get; set; }

        ulong CreatedTime { get; }
        ulong ExpireTime { get; }
        ulong LastTypeSet { get; }
        ulong LastSeen { get; }
        bool CheckIfKad2();

        bool ReceivedHelloPacket { get; set;}

        KadUDPKey UDPKey { get; set; }

        bool IsIpVerified { get; set;}
    }
}
