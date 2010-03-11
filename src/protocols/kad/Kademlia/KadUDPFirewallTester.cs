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

namespace Kademlia
{
    public interface KadUDPFirewallTester
    {
        // Are we UDP firewalled - if unknown open is assumed unless bOnlyVerified == true 
        bool IsFirewalledUDP(bool bLastStateIfTesting); 
        void SetUDPFWCheckResult(bool bSucceeded, bool bTestCancelled, 
            uint uFromIP, ushort nIncomingPort);
        void ReCheckFirewallUDP(bool bSetUnverified);
        bool IsFWCheckUDPRunning { get;}
        bool IsVerified { get;}
        void AddPossibleTestContact(KadUInt128 uClientID, uint uIp, 
            ushort uUdpPort, ushort uTcpPort, 
            KadUInt128 uTarget, byte uVersion, 
            KadUDPKey cUDPKey, bool bIPVerified);
        // when stopping Kad
        void Reset(); 
        void Connected();
        // try the next available client for the firewallcheck
        void QueryNextClient(); 
    }
}
