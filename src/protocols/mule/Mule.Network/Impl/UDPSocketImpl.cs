using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Network.Impl
{
    class UDPSocketImpl : EncryptedDatagramSocketImpl, UDPSocket
    {
        #region UDPSocket Members

        public void SendPacket(Packet packet, Mule.ED2K.ED2KServer host)
        {
            throw new NotImplementedException();
        }

        public void SendPacket(Packet packet, Mule.ED2K.ED2KServer host, ushort nSpecialPort)
        {
            throw new NotImplementedException();
        }

        public void SendPacket(Packet packet, Mule.ED2K.ED2KServer host, ushort nSpecialPort, byte[] pRawPacket)
        {
            throw new NotImplementedException();
        }

        public void SendPacket(Packet packet, Mule.ED2K.ED2KServer host, ushort nSpecialPort, byte[] pRawPacket, uint nLen)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ThrottledControlSocket Members

        public SocketSentBytes SendControlData(uint maxNumberOfBytesToSend, uint minFragSize)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
