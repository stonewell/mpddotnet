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
using Mule.Network;
using Mule.ED2K;

namespace Mule.Core
{
    public interface ServerConnect
    {
        void ConnectionFailed(ServerSocket sender);
        void ConnectionEstablished(ServerSocket sender);

        void ConnectToAnyServer(/*0, true, true*/);
        void ConnectToAnyServer(uint startAt, bool prioSort/* = false*/, bool isAuto/* = true*/, bool bNoCrypt/* = false*/);
        void ConnectToServer(ED2KServer toconnect, bool multiconnect/* = false*/, bool bNoCrypt/* = false*/);
        void StopConnectionTry();

        void CheckForTimeout();
        void DestroySocket(ServerSocket pSck);	// safe socket closure and destruction
        bool SendPacket(Packet packet, bool delpacket/* = true*/, ServerSocket to/* = 0*/);
        bool IsUDPSocketAvailable { get;}

        bool SendUDPPacket(Packet packet, ED2KServer host);
        bool SendUDPPacket(Packet packet, ED2KServer host, bool delpacket/* = false*/);
        bool SendUDPPacket(Packet packet, ED2KServer host, bool delpacket/* = false*/, ushort nSpecialPort/* = 0*/);
        bool SendUDPPacket(Packet packet, ED2KServer host, bool delpacket/* = false*/, ushort nSpecialPort/* = 0*/, byte[] pRawPacket/* = NULL*/);
        bool SendUDPPacket(Packet packet, ED2KServer host, bool delpacket/* = false*/, ushort nSpecialPort/* = 0*/, byte[] pRawPacket/* = NULL*/, uint nLen/* = 0*/);

        void KeepConnectionAlive();
        bool Disconnect();
        bool IsConnecting { get;}
        bool IsConnected { get; }
        uint ClientID { get; }
        ED2KServer CurrentServer { get;}

        bool IsLowID { get;}
        void SetClientID(uint newid);
        bool IsLocalServer(uint dwIP, ushort nPort);
        void TryAnotherConnectionRequest();
        bool IsSingleConnect { get;}
        void InitLocalIP();
        uint LocalIP { get; }

        bool AwaitingTestFromIP(uint dwIP);
        bool IsConnectedObfuscated();
    }
}
