using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Generic.IO;
using Mule.Network.Impl;
using System.IO;

namespace Mule.Network.Impl
{
    class NetworkObjectManagerImpl : NetworkObjectManager
    {
        #region NetworkObjectManager Members
        public Packet CreatePacket()
        {
            return new PacketImpl();
        }

        public Packet CreatePacket(byte protocol)
        {
            return new PacketImpl(protocol);
        }

        public Packet CreatePacket(byte[] buf, int offset)
        {
            byte[] header = new byte[6];
            Array.Copy(buf, offset, header, 0, 6);

            return CreatePacket(header);
        }

        public Packet CreatePacket(byte[] header)
        {
            return new PacketImpl(header);
        }

        public Packet CreatePacket(byte[] pPacketPart, uint nSize, bool bLast, bool bFromPartFile)
        {
            return new PacketImpl(pPacketPart, nSize, bLast, bFromPartFile);
        }

        public Packet CreatePacket(OperationCodeEnum opCode, uint in_size, byte protocol)
        {
            return new PacketImpl(opCode, in_size, protocol);
        }

        public Packet CreatePacket(OperationCodeEnum opCode, uint in_size)
        {
            return new PacketImpl(opCode, in_size);
        }

        public Packet CreatePacket(OperationCodeEnum opCode, uint in_size, byte protocol, bool bFromPartFile)
        {
            return new PacketImpl(opCode, in_size, protocol, bFromPartFile);
        }

        public Packet CreatePacket(SafeMemFile datafile)
        {
            return new PacketImpl(datafile);
        }

        public Packet CreatePacket(SafeMemFile datafile, byte protocol)
        {
            return new PacketImpl(datafile, protocol);
        }

        public Packet CreatePacket(SafeMemFile datafile, byte protocol, OperationCodeEnum ucOpcode)
        {
            return new PacketImpl(datafile, protocol, ucOpcode);
        }

        public Packet CreatePacket(string str, byte ucProtocol, OperationCodeEnum ucOpcode)
        {
            return new PacketImpl(str, ucProtocol, ucOpcode);
        }

        public RawPacket CreateRawPacket(string data)
        {
            return new RawPacketImpl(data);
        }

        public RawPacket CreateRawPacket(byte[] pcData)
        {
            return new RawPacketImpl(pcData);
        }

        public RawPacket CreateRawPacket(byte[] pcData, bool bFromPartFile)
        {
            return new RawPacketImpl(pcData, 0, pcData.Length, bFromPartFile);
        }

        public RawPacket CreateRawPacket(byte[] pcData, int size)
        {
            return new RawPacketImpl(pcData, 0, size, false);
        }

        public RawPacket CreateRawPacket(byte[] pcData, int size, bool bFromPartFile)
        {
            return new RawPacketImpl(pcData, 0, size, bFromPartFile);
        }

        public RawPacket CreateRawPacket(byte[] pcData, int offset, int size)
        {
            return new RawPacketImpl(pcData, offset, size, false);
        }

        public RawPacket CreateRawPacket(byte[] pcData, int offset, int size, bool bFromPartFile)
        {
            return new RawPacketImpl(pcData, offset, size, bFromPartFile);
        }

        public ServerSocket CreateServerSocket(Mule.Core.ServerConnect serverConnect, bool singleConnect)
        {
            return new ServerSocketImpl(serverConnect, singleConnect);
        }

        public UDPSocket CreateUDPSocket()
        {
            return new UDPSocketImpl();
        }

        public ListenSocket CreateListenSocket()
        {
            return new ListenSocketImpl();
        }

        public ClientUDPSocket CreateClientUDPSocket()
        {
            return new ClientUDPSocketImpl();
        }

        #endregion
    }
}
