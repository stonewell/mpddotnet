using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Network.Impl
{
    class ClientUDPSocketImpl : EncryptedDatagramSocketImpl, ClientUDPSocket
    {
        #region ClientUDPSocket Members

        public bool Create()
        {
            throw new NotImplementedException();
        }

        public bool Rebind()
        {
            throw new NotImplementedException();
        }

        public ushort ConnectedPort
        {
            get { throw new NotImplementedException(); }
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
