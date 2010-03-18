using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Generic.IO;
using Mule.Network.Impl;
using System.IO;

namespace Mule.Network
{
    public class NetworkObjectManager
    {
        public static Packet CreatePacket()
        {
            return new PacketImpl();
        }

        public static Packet CreatePacket(byte protocol)
        {
            return new PacketImpl(protocol);
        }

        public static Packet CreatePacket(byte[] buf, int offset)
        {
            byte[] header = new byte[6];
            Array.Copy(buf, offset, header, 0, 6);

            return CreatePacket(header);
        }

        public static Packet CreatePacket(byte[] header)
        {
            return new PacketImpl(header);
        }

        public static Packet CreatePacket(byte[] pPacketPart, uint nSize, bool bLast, bool bFromPartFile)
        {
            return new PacketImpl(pPacketPart, nSize, bLast, bFromPartFile);
        }

        public static Packet CreatePacket(OperationCodeEnum opCode, uint in_size, byte protocol)
        {
            return new PacketImpl(opCode, in_size, protocol);
        }

        public static Packet CreatePacket(OperationCodeEnum opCode, uint in_size)
        {
            return new PacketImpl(opCode, in_size);
        }

        public static Packet CreatePacket(OperationCodeEnum opCode, uint in_size, byte protocol, bool bFromPartFile)
        {
            return new PacketImpl(opCode, in_size, protocol, bFromPartFile);
        }

        public static Packet CreatePacket(MemoryStream datafile)
        {
            return new PacketImpl(datafile);
        }

        public static Packet CreatePacket(MemoryStream datafile, byte protocol)
        {
            return new PacketImpl(datafile, protocol);
        }

        public static Packet CreatePacket(MemoryStream datafile, byte protocol, OperationCodeEnum ucOpcode)
        {
            return new PacketImpl(datafile, protocol, ucOpcode);
        }

        public static Packet CreatePacket(string str, byte ucProtocol, OperationCodeEnum ucOpcode)
        {
            return new PacketImpl(str, ucProtocol, ucOpcode);
        }

        public static RawPacket CreateRawPacket(string data)
        {
            return new RawPacketImpl(data);
        }

        public static RawPacket CreateRawPacket(byte[] pcData)
        {
            return new RawPacketImpl(pcData);
        }

        public static RawPacket CreateRawPacket(byte[] pcData, bool bFromPartFile)
        {
            return new RawPacketImpl(pcData, 0, pcData.Length, bFromPartFile);
        }

        public static RawPacket CreateRawPacket(byte[] pcData, int size)
        {
            return new RawPacketImpl(pcData, 0, size, false);
        }

        public static RawPacket CreateRawPacket(byte[] pcData, int size, bool bFromPartFile)
        {
            return new RawPacketImpl(pcData, 0, size, bFromPartFile);
        }

        public static RawPacket CreateRawPacket(byte[] pcData, int offset, int size)
        {
            return new RawPacketImpl(pcData, offset, size, false);
        }

        public static RawPacket CreateRawPacket(byte[] pcData, int offset, int size, bool bFromPartFile)
        {
            return new RawPacketImpl(pcData, offset, size, bFromPartFile);
        }
    }
}
