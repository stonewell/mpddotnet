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

namespace Mule.ED2K
{
    public enum ED2KServerPriorityEnum
    {
        SRV_PR_LOW = 2,
        SRV_PR_NORMAL = 0,
        SRV_PR_HIGH = 1,
    }

    [Flags]
    public enum ED2KServerTcpFlagsEnum
    {
        SRV_TCPFLG_COMPRESSION = 0x00000001,
        SRV_TCPFLG_NEWTAGS = 0x00000008,
        SRV_TCPFLG_UNICODE = 0x00000010,
        SRV_TCPFLG_RELATEDSEARCH = 0x00000040,
        SRV_TCPFLG_TYPETAGINTEGER = 0x00000080,
        SRV_TCPFLG_LARGEFILES = 0x00000100,
        SRV_TCPFLG_TCPOBFUSCATION = 0x00000400,
    }

    [Flags]
    public enum ED2KServerUdpFlagsEnum
    {
        SRV_UDPFLG_EXT_GETSOURCES = 0x00000001,
        SRV_UDPFLG_EXT_GETFILES = 0x00000002,
        SRV_UDPFLG_NEWTAGS = 0x00000008,
        SRV_UDPFLG_UNICODE = 0x00000010,
        SRV_UDPFLG_EXT_GETSOURCES2 = 0x00000020,
        SRV_UDPFLG_LARGEFILES = 0x00000100,
        SRV_UDPFLG_UDPOBFUSCATION = 0x00000200,
        SRV_UDPFLG_TCPOBFUSCATION = 0x00000400,
    }

    public struct ServerMet
    {
        public uint ip;
        public uint port;
        public uint tagcount;
    };

    public interface ED2KServer
    {
        string ServerName { get; set; }
        string Description { get; set; }
        uint IP { get; set; }
        string DynIP { get; set; }
        bool HasDynIP { get; }


        string FullIP { get; }
        string Address { get; }
        ushort Port { get; }

        uint FileCount { get; set; }

        uint UserCount { get; set; }

        ED2KServerPriorityEnum Priority { get; set; }
        uint Ping { get; set; }
        uint MaxUsers { get; set; }

        uint FailedCount { get; set; }

        void AddFailedCount();
        void ResetFailedCount();

        uint LastPingedTime { get; set; }
        uint RealLastPingedTime { get; set; }
        uint LastPinged { get; set; }

        uint LastDescPingedCount { get; }
        void SetLastDescPingedCount(bool reset);

        bool IsStaticMember { get; set; }

        uint Challenge { get; set; }
        uint DescReqChallenge { get; set; }
        uint SoftFiles { get; set; }
        uint HardFiles { get; set; }

        string Version { get; set; }

        ED2KServerTcpFlagsEnum TCPFlags { get; set; }
        ED2KServerUdpFlagsEnum UDPFlags { get; set; }
        uint LowIDUsers { get; set; }

        ushort ObfuscationPortTCP { get; set; }
        ushort ObfuscationPortUDP { get; set; }

        uint ServerKeyUDP { get; set; }
        uint GetServerKeyUDP(bool bForce);

        bool CryptPingReplyPending { get; set; }

        uint ServerKeyUDPIP { get; }

        bool UnicodeSupport { get; }
        bool RelatedSearchSupport { get; }
        bool DoesSupportsLargeFilesTCP { get; }
        bool DoesSupportsLargeFilesUDP { get; }
        bool DoesSupportsObfuscationUDP { get; }
        bool DoesSupportsObfuscationTCP { get; }
        bool DoesSupportsGetSourcesObfuscation { get; }

        bool IsEqual(ED2KServer pServer);
    }
}
