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
using Mpd.Generic.IO;

namespace Kademlia
{
    public interface KadUDPListener : KadPacketTracking
    {
        void Bootstrap(string uIP, ushort uUDPPort);
        void Bootstrap(uint uIP, ushort uUDPPort);
        void Bootstrap(uint uIP, ushort uUDPPort, byte byKadVersion);
        void Bootstrap(uint uIP, ushort uUDPPort, byte byKadVersion, ref UInt128 uCryptTargetID);
        void FirewalledCheck(uint uIP, ushort uUDPPort, KadUDPKey senderUDPKey, byte byKadVersion);
        void SendMyDetails(byte byOpcode, uint uIP, ushort uUDPPort, byte byKadVersion, KadUDPKey targetUDPKey, ref UInt128 uCryptTargetID, bool bRequestAckPackage);
        void SendPublishSourcePacket(KadContact pContact, ref UInt128 uTargetID, ref UInt128 uContactID, TagList tags);
        void SendNullPacket(KadOperationCodeEnum byOpcode, uint uIP, ushort uUDPPort, KadUDPKey targetUDPKey, UInt128 uCryptTargetID);
        void ProcessPacket(byte[] pbyData, uint uLenData, uint uIP, ushort uUDPPort, bool bValidReceiverKey, KadUDPKey senderUDPKey);
        void SendPacket(byte[] pbyData, uint uLenData, uint uDestinationHost, ushort uDestinationPort, KadUDPKey targetUDPKey, ref UInt128 uCryptTargetID);
        void SendPacket(byte[] pbyData, uint uLenData, byte byOpcode, uint uDestinationHost, ushort uDestinationPort, KadUDPKey targetUDPKey, ref UInt128 uCryptTargetID);
        void SendPacket(SafeMemFile pfileData, byte byOpcode, uint uDestinationHost, ushort uDestinationPort, KadUDPKey targetUDPKey, ref UInt128 uCryptTargetID);

        bool FindNodeIDByIP(KadClientSearcher pRequester, uint dwIP, ushort nTCPPort, ushort nUDPPort);
        void ExpireClientSearch();
        void ExpireClientSearch(KadClientSearcher pExpireImmediately);
    }
}
