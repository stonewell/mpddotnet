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
            SendPacket(packet, host, 0, null, 0);
        }

        public void SendPacket(Packet packet, Mule.ED2K.ED2KServer host, ushort nSpecialPort)
        {
            SendPacket(packet, host, nSpecialPort, null, 0);
        }

        public void SendPacket(Packet packet, Mule.ED2K.ED2KServer host, ushort nSpecialPort, byte[] pRawPacket)
        {
            SendPacket(packet, host, nSpecialPort, pRawPacket, 0);
        }

        public void SendPacket(Packet packet, Mule.ED2K.ED2KServer host, ushort nSpecialPort, byte[] pRawPacket, uint nLen)
        {
            throw new NotImplementedException();
        }

        public void Create()
        {
        }
        #endregion

        #region ThrottledControlSocket Members

        public SocketSentBytes SendControlData(uint maxNumberOfBytesToSend, uint minFragSize)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Overrides
        protected override void OnReceive(int nErrorCode)
        {
            base.OnReceive(nErrorCode);
        }

        protected override void OnSend(int nErrorCode)
        {
            base.OnSend(nErrorCode);
        }
        #endregion
    }
}
