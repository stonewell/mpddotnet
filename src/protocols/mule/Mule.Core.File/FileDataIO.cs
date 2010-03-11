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
using Kademlia;

namespace Mule.Core.File
{
    public interface FileDataIO
    {
        int Read(byte[] lpBuf);
        int Read(byte[] lpBuf, int offset, int length);
        void Write(byte[] lpBuf);
        void Write(byte[] lpBuf, int offset, int length);
        void Flush();
        void Close();
        void Abort();
        void SetLength(long length);

        Int64 Seek(Int64 lOff, System.IO.SeekOrigin nFrom);
        Int64 Position { get; }
        Int64 Length { get; }

        byte ReadUInt8();
        UInt16 ReadUInt16();
        uint ReadUInt32();
        ulong ReadUInt64();
        void ReadUInt128(ref KadUInt128 pVal);
        void ReadHash16(byte[] pVal);
        string ReadString(bool bOptUTF8);
        string ReadString(bool bOptUTF8, uint uRawSize);
        string ReadStringUTF8();

        void WriteUInt8(byte nVal);
        void WriteUInt16(UInt16 nVal);
        void WriteUInt32(uint nVal);
        void WriteUInt64(ulong nVal);
        void WriteUInt128(KadUInt128 pVal);
        void WriteHash16(byte[] pVal);
        void WriteString(string rstr, Utf8StrEnum eEncode);
        void WriteString(string psz);
        void WriteLongString(string rstr, Utf8StrEnum eEncode);
        void WriteLongString(string psz);
    }
}
