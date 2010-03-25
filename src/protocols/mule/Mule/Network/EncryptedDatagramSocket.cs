using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Network
{
    public interface EncryptedDatagramSocket
    {
        int DecryptReceivedClient(byte[] pbyBufIn, int nBufLen,
            out byte[] ppbyBufOut, uint dwIP, 
            out uint nReceiverVerifyKey, out uint nSenderVerifyKey);
        int EncryptSendClient(ref byte[] ppbyBuf, int nBufLen,
            byte[] pachClientHashOrKadID, bool bKad,
            uint nReceiverVerifyKey, uint nSenderVerifyKey);
        int DecryptReceivedServer(byte[] pbyBufIn, int nBufLen,
            out byte[] ppbyBufOut, uint dwBaseKey, uint dbgIP);
        int EncryptSendServer(ref byte[] ppbyBuf, int nBufLen, uint dwBaseKey);
    }
}
