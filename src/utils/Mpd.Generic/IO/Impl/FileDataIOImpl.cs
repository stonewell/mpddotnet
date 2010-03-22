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
using System.IO;
using Mpd.Generic;
using Mpd.Generic.IO;

namespace Mpd.Generic.IO.Impl
{
    abstract class FileDataIOImpl : FileDataIO
    {
        #region Fields
        private Stream stream_ = null;
        private BinaryReader binary_reader_ = null;
        private BinaryWriter binary_writer_ = null;

        #endregion

        #region Constructors
        public FileDataIOImpl(Stream st)
        {
            stream_ = st;

            if (stream_ == null)
                throw new ArgumentNullException("st", "must provide an valid stream");

            binary_reader_ = new BinaryReader(stream_);
            binary_writer_ = new BinaryWriter(stream_);
        }
        #endregion

        #region FileDataIO Members
        public Stream BaseStream
        {
            get
            {
                return stream_;
            }
        }

        public int Read(byte[] lpBuf)
        {
            return Read(lpBuf, 0, lpBuf.Length);
        }

        public void Write(byte[] lpBuf)
        {
            Write(lpBuf, 0, lpBuf.Length);
        }

        public virtual long Seek(long lOff, System.IO.SeekOrigin nFrom)
        {
            if (!stream_.CanSeek)
                throw new ApplicationException("Stream didn't support seek");

            return stream_.Seek(lOff, nFrom);
        }

        public virtual long Position
        {
            get
            {
                return stream_.Position;
            }
        }

        public virtual long Length
        {
            get
            {
                return stream_.Length;
            }
        }

        public byte ReadUInt8()
        {
            return binary_reader_.ReadByte();
        }

        public ushort ReadUInt16()
        {
            return binary_reader_.ReadUInt16();
        }

        public uint ReadUInt32()
        {
            return binary_reader_.ReadUInt32();
        }

        public ulong ReadUInt64()
        {
            return binary_reader_.ReadUInt64();
        }

        public void ReadUInt128(ref UInt128 pVal)
        {
            byte[] buf = new byte[16];

            if (Read(buf) == 16)
            {
                pVal.Bytes = buf;
            }
        }

        public void ReadHash16(byte[] pVal)
        {
            Read(pVal, 0, 16);
        }

        public string ReadString(bool bOptUTF8)
        {
            ushort uLen = ReadUInt16();

            return ReadString(bOptUTF8, uLen);
        }

        public string ReadString(bool bOptUTF8, uint uRawSize)
        {
	        byte[] acRaw = new byte[uRawSize];
	        Read(acRaw);

	        if (uRawSize >= 3 && 
                acRaw[0] == 0xEFU && 
                acRaw[1] == 0xBBU && 
                acRaw[2] == 0xBFU)
	        {
                return Encoding.UTF8.GetString(acRaw);
	        }
	        else if (bOptUTF8)
	        {
                return Encoding.UTF8.GetString(acRaw);
	        }

	        return Encoding.Default.GetString(acRaw);
        }

        public string ReadStringUTF8()
        {
            ushort uRawSize = ReadUInt16();

            byte[] raw = new byte[uRawSize];

            Read(raw);

            return Encoding.UTF8.GetString(raw);
        }

        public void WriteUInt8(byte nVal)
        {
            binary_writer_.Write(nVal);
        }

        public void WriteUInt16(ushort nVal)
        {
            binary_writer_.Write(nVal);
        }

        public void WriteUInt32(uint nVal)
        {
            binary_writer_.Write(nVal);
        }

        public void WriteUInt64(ulong nVal)
        {
            binary_writer_.Write(nVal);
        }

        public void WriteUInt128(UInt128 pVal)
        {
            binary_writer_.Write(pVal.Bytes);
        }

        public void WriteHash16(byte[] pVal)
        {
            binary_writer_.Write(pVal);
        }

        public void WriteString(string rstr, Utf8StrEnum eEncode)
        {
            byte[] buf = null;

            if (eEncode == Utf8StrEnum.utf8strNone)
            {
                buf = Encoding.Default.GetBytes(rstr);
            }
            else
            {
                buf = Encoding.UTF8.GetBytes(rstr);
            }

            WriteUInt16(Convert.ToUInt16(buf.Length));
            Write(buf);
        }

        public void WriteString(string psz)
        {
            WriteString(psz, Utf8StrEnum.utf8strNone);
        }

        public void WriteLongString(string rstr, Utf8StrEnum eEncode)
        {
            byte[] buf = null;

            if (eEncode == Utf8StrEnum.utf8strNone)
            {
                buf = Encoding.Default.GetBytes(rstr);
            }
            else
            {
                buf = Encoding.UTF8.GetBytes(rstr);
            }

            WriteUInt32(Convert.ToUInt32(buf.Length));
            Write(buf);
        }

        public void WriteLongString(string psz)
        {
            WriteLongString(psz, Utf8StrEnum.utf8strNone);
        }

        public virtual int Read(byte[] lpBuf, int offset, int length)
        {
            if (!stream_.CanRead)
                throw new ApplicationException("Stream can not read");

            return stream_.Read(lpBuf, offset, length);
        }

        public virtual void Write(byte[] lpBuf, int offset, int length)
        {
            if (!stream_.CanWrite)
                throw new ApplicationException("Stream can not write");

            stream_.Write(lpBuf, offset, length);
        }

        public virtual void Flush()
        {
            stream_.Flush();
        }

        public virtual void Close()
        {
            stream_.Close();
        }

        public virtual void SetLength(long length)
        {
            stream_.SetLength(length);
        }

        public virtual void Abort()
        {
        }
        #endregion

        #region DataIO Members


        public byte[] ReadBsob(byte[] puSize)
        {
            throw new NotImplementedException();
        }

        public float ReadFloat()
        {
            throw new NotImplementedException();
        }

        public string ReadString()
        {
            throw new NotImplementedException();
        }

        public void ReadArray(byte[] lpResult, uint ubyteCount)
        {
            throw new NotImplementedException();
        }

        public void WriteBsob(byte[] pbyVal, byte uSize)
        {
            throw new NotImplementedException();
        }

        public void WriteFloat(float fVal)
        {
            throw new NotImplementedException();
        }

        public void WriteArray(byte[] lpVal, uint ubyteCount)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region TagIO Members

        public Tag ReadTag()
        {
            throw new NotImplementedException();
        }

        public Tag ReadTag(bool bOptACP)
        {
            throw new NotImplementedException();
        }

        public void ReadTagList(TagList pTaglist)
        {
            throw new NotImplementedException();
        }

        public void ReadTagList(TagList pTaglist, bool bOptACP)
        {
            throw new NotImplementedException();
        }

        public void WriteTag(Tag pTag)
        {
            throw new NotImplementedException();
        }

        public void WriteTag(string szName, byte uValue)
        {
            throw new NotImplementedException();
        }

        public void WriteTag(string szName, ushort uValue)
        {
            throw new NotImplementedException();
        }

        public void WriteTag(string szName, uint uValue)
        {
            throw new NotImplementedException();
        }

        public void WriteTag(string szName, ulong uValue)
        {
            throw new NotImplementedException();
        }

        public void WriteTag(string szName, float fValue)
        {
            throw new NotImplementedException();
        }

        public void WriteTagList(TagList tagList)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
