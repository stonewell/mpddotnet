using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Network
{
    public interface UDPSocket : AsyncSocket, EncryptedDatagramSocket, ThrottledControlSocket
    {
    }
}
