using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mpd.Generic.IO
{
    public interface DataIO
    {
        byte ReadUInt8();
        ushort ReadUInt16();
        uint ReadUInt32();
        ulong ReadUInt64();
        void ReadUInt128(ref UInt128 pVal);
        void ReadHash16(byte[] pVal);
        byte[] ReadBsob(byte[] puSize);
        float ReadFloat();
        string ReadString(/*bool bOptACP = false*/);
        string ReadString(bool bOptUTF8);
        string ReadString(bool bOptUTF8, uint uRawSize);
        string ReadStringUTF8();
        void ReadArray(byte[] lpResult, uint ubyteCount);

        void WriteUInt8(byte nVal);
        void WriteUInt16(ushort nVal);
        void WriteUInt32(uint nVal);
        void WriteUInt64(ulong nVal);
        void WriteUInt128(UInt128 pVal);
        void WriteHash16(byte[] pVal);
        void WriteBsob(byte[] pbyVal, byte uSize);
        void WriteFloat(float fVal);
        void WriteString(string psz);
        void WriteString(string rstr, Utf8StrEnum eEncode);
        void WriteLongString(string rstr, Utf8StrEnum eEncode);
        void WriteLongString(string psz);
        void WriteArray(byte[] lpVal, uint ubyteCount);
    }
}
