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
    public interface KadDataIO
    {
        byte ReadByte();
        UInt16 ReadUInt16();
        uint ReadUInt32();
        ulong ReadUInt64();
        void ReadUInt128(KadUInt128 puValue);
        void ReadHash(byte[] pbyValue);
        byte[] ReadBsob(byte[] puSize);
        float ReadFloat();
        string ReadStringUTF8(/*bool bOptACP = false*/);
        string ReadStringUTF8(bool bOptACP);
        void WriteString(string strVal);
        KadTag ReadTag(/*bool bOptACP = false*/);
        KadTag ReadTag(bool bOptACP);
        void ReadTagList(KadTagList pTaglist/*, bool bOptACP = false*/);
        void ReadTagList(KadTagList pTaglist, bool bOptACP);
        void WriteByte(byte byVal);
        void WriteUInt16(UInt16 uVal);
        void WriteUInt32(uint uVal);
        void WriteUInt64(ulong uVal);
        void WriteUInt128(KadUInt128 uVal);
        void WriteHash(byte[] pbyVal);
        void WriteBsob(byte[] pbyVal, byte uSize);
        void WriteFloat(float fVal);
        void WriteTag(KadTag pTag);
        void WriteTag(string szName, byte uValue);
        void WriteTag(string szName, UInt16 uValue);
        void WriteTag(string szName, uint uValue);
        void WriteTag(string szName, ulong uValue);
        void WriteTag(string szName, float fValue);
        void WriteTagList(KadTagList tagList);
        void ReadArray(byte[] lpResult, uint ubyteCount);
        void WriteArray(byte[] lpVal, uint ubyteCount);
        uint Available { get;}
    }
}
