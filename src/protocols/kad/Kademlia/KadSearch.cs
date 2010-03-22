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
    class KadSearchMap : Dictionary<UInt128, KadSearch>
    {
    }

    public enum KadSearchTypeEnum
    {
        NODE,
        NODECOMPLETE,
        FILE,
        KEYWORD,
        NOTES,
        STOREFILE,
        STOREKEYWORD,
        STORENOTES,
        FINDBUDDY,
        FINDSOURCE,
        NODESPECIAL, // nodesearch request from requester "outside" of kad to find the IP of a given nodeid
        NODEFWCHECKUDP // find new unknown IPs for a UDP firewallcheck
    };

    public interface KadSearch
    {
        uint SearchTypes { get;set;}
        UInt128 TargetId { get; set; }
        string FileName { get; set; }
        KadClientSearcher NodeSpecialSearchRequester { get;set;}

        uint SearchID { get; }

        uint Answers { get; }
        uint KadPacketSent { get; }
        uint RequestAnswer { get; }
        uint NodeLoad { get; }
        uint NodeLoadResonse { get; }
        uint NodeLoadTotal { get; }


        void SetSearchTermData(byte[] pucSearchTermsData);

        void AddFileID(UInt128 uID);
        bool Stoping();
        void UpdateNodeLoad(byte uLoad);
    }
}
