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
using Mule.Core.File;

namespace Mule.Core.ED2K
{
    public interface ED2KServer
    {
        bool AddTagFromFile(FileDataIO servermet);

        string ListName { get; set;}
        string Description { get; set;}
        uint IP { get; set;}
        string DynIP { get; set; }
        bool HasDynIP { get; }


        string FullIP { get;}
        string Address { get; }
        ushort Port { get;}

        uint FileCount { get; set; }

        uint UserCount { get; set; }

        uint Preference { get; set; }
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

        bool StaticMember { get; set;}

        uint Challenge { get; set; }
        uint DescReqChallenge { get; set; }
        uint SoftFiles { get; set; }
        uint HardFiles { get; set; }

        string Version { get; set; }

        uint TCPFlags { get; set; }
        uint UDPFlags { get; set; }
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
