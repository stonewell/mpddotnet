using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Network
{
    public interface UDPSocket : AsyncSocket, EncryptedDatagramSocket, ThrottledControlSocket
    {
        void SendPacket(Packet packet,
            Mule.ED2K.ED2KServer host);
        void SendPacket(Packet packet,
            Mule.ED2K.ED2KServer host,
            ushort nSpecialPort);
        void SendPacket(Packet packet,
            Mule.ED2K.ED2KServer host,
            ushort nSpecialPort,
            byte[] pRawPacket);
        void SendPacket(Packet packet, 
            Mule.ED2K.ED2KServer host, 
            ushort nSpecialPort, 
            byte[] pRawPacket, 
            uint nLen);

        void Create();
    }
}
