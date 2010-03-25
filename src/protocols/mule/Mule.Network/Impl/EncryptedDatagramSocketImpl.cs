using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Mule.Network.Impl
{
    class EncryptedDatagramSocketImpl : AsyncSocketImpl, EncryptedDatagramSocket
    {
        #region Constructors
        public EncryptedDatagramSocketImpl() :
            base(new IPEndPoint(IPAddress.Any, 0).AddressFamily, SocketType.Dgram, ProtocolType.Tcp)
        {
        }
        #endregion

        #region EncryptedDatagramSocket Members

        public virtual int DecryptReceivedClient(byte[] pbyBufIn, int nBufLen, out byte[] ppbyBufOut, uint dwIP, out uint nReceiverVerifyKey, out uint nSenderVerifyKey)
        {
            throw new NotImplementedException();
        }

        public virtual int EncryptSendClient(out byte[] ppbyBuf, int nBufLen, byte[] pachClientHashOrKadID, bool bKad, uint nReceiverVerifyKey, uint nSenderVerifyKey)
        {
            throw new NotImplementedException();
        }

        public virtual int DecryptReceivedServer(byte[] pbyBufIn, int nBufLen, out byte[] ppbyBufOut, uint dwBaseKey, uint dbgIP)
        {
            throw new NotImplementedException();
        }

        public virtual int EncryptSendServer(out byte[] ppbyBuf, int nBufLen, uint dwBaseKey)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
