using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Mpd.Generic.IO;

namespace Mule.Network
{
    public interface NetworkObjectManager
    {
        Packet CreatePacket();

        Packet CreatePacket(byte protocol);

        Packet CreatePacket(byte[] buf, int offset);

        Packet CreatePacket(byte[] header);

        Packet CreatePacket(byte[] pPacketPart, uint nSize, bool bLast, bool bFromPartFile);

        Packet CreatePacket(OperationCodeEnum opCode, uint in_size, byte protocol);

        Packet CreatePacket(OperationCodeEnum opCode, uint in_size);

        Packet CreatePacket(OperationCodeEnum opCode, uint in_size, byte protocol, bool bFromPartFile);

        Packet CreatePacket(SafeMemFile datafile);

        Packet CreatePacket(SafeMemFile datafile, byte protocol);
        Packet CreatePacket(SafeMemFile datafile, byte protocol, OperationCodeEnum ucOpcode);

        Packet CreatePacket(string str, byte ucProtocol, OperationCodeEnum ucOpcode);

        RawPacket CreateRawPacket(string data);

        RawPacket CreateRawPacket(byte[] pcData);

        RawPacket CreateRawPacket(byte[] pcData, bool bFromPartFile);

        RawPacket CreateRawPacket(byte[] pcData, int size);

        RawPacket CreateRawPacket(byte[] pcData, int size, bool bFromPartFile);

        RawPacket CreateRawPacket(byte[] pcData, int offset, int size);

        RawPacket CreateRawPacket(byte[] pcData, int offset, int size, bool bFromPartFile);
    }
}
