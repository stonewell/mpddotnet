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
    public interface KadSearchManager
    {
        bool IsSearching(uint uSearchID);
        void StopSearch(uint uSearchID, bool bDelayDelete);
        void StopAllSearches();
        KadSearch PrepareLookup(KadSearchTypeEnum uType, bool bStart, UInt128 uID);
        KadSearch PrepareFindKeywords(bool bUnicode, string szKeyword, uint uSearchTermsSize, byte[] pucSearchTermsData);
        bool StartSearch(KadSearch pSearch);
        void ProcessResponse(UInt128 uTarget, uint uFromIP, ushort uFromPort, KadContactList plistResults);
        void ProcessResult(UInt128 uTarget, UInt128 uAnswer, TagList plistInfo);
        void ProcessPublishResult(UInt128 uTarget, byte uLoad, bool bLoadResponse);
        void GetWords(string sz, KadWordList plistWords);
        void UpdateStats();
        bool AlreadySearchingFor(UInt128 uTarget);

        void CancelNodeFWCheckUDPSearch();
        bool FindNodeFWCheckUDP();
        bool IsFWCheckUDPSearch(UInt128 uTarget);
        void SetNextSearchID(uint uNextID);
    }
}
