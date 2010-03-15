#region File Header

//
// Copyright (C) 2008 Jingnan Si
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
    public class KadEntryList : List<KadEntry>
    {
    }

    public interface KadEntry
    {
        KadEntry Copy();

        ulong GetIntTagValue(string strTagName/*, bool bIncludeVirtualTags = true*/);
        ulong GetIntTagValue(string strTagName, bool bIncludeVirtualTags);
        bool GetIntTagValue(string strTagName, ref ulong rValue/*, bool bIncludeVirtualTags = true*/);
        bool GetIntTagValue(string strTagName, ref ulong rValue, bool bIncludeVirtualTags);
        string GetStrTagValue(string strTagName);
        void AddTag(Tag pTag);
        // Adds filename and size to the count if not empty, even if they are not stored as tags
        uint TagCount { get; }
        void WriteTagList(TagIO pData);

        string CommonFileNameLowerCase { get; }
        string CommonFileName { get; }
        void SetFileName(string strName);

        uint IP { get; set; }
        ushort TCPPort { get; set; }
        ushort UDPPort { get; set; }
        UInt128 KeyID { get; set; }
        UInt128 SourceID { get; set; }
        ulong Size { get; set; }
        ulong Lifetime { get; set; }
        bool Source { get; set; }
    }
}
