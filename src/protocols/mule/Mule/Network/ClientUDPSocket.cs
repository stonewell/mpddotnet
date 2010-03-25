using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Network
{
    public interface ClientUDPSocket : AsyncSocket, EncryptedDatagramSocket, ThrottledControlSocket
    {
        bool Create();
        bool Rebind();
        ushort ConnectedPort { get; }
    }
}
