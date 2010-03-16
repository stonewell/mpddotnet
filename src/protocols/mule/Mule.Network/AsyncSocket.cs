using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.Preference;
using System.Net;
using System.Net.Sockets;

namespace Mule.Network
{
    public interface AsyncSocket : IDisposable
    {
        MuleApplication MuleApp { get; set; }
        bool Connected { get; }
        void Connect(string host, int port);
        EndPoint RemoteEndPoint { get; }
        bool IsSendBlocked { get; }
        bool IsReceiveBlocked { get; }
        void Close();
        void Close(int timeout);

        int Send(byte[] pBuffer, int offset, int nBufLen);
        int Send(byte[] pBuffer, int nBufLen);
        int Send(byte[] pBuffer, int nBufLen, SocketFlags nFlags);
        int Send(byte[] pBuffer, int offset, int nBufLen, SocketFlags nFlags);

        int Receive(byte[] pBuffer, int offset, int nBufLen);
        int Receive(byte[] pBuffer, int nBufLen);
        int Receive(byte[] pBuffer, int nBufLen, SocketFlags nFlags);
        int Receive(byte[] pBuffer, int offset, int nBufLen, SocketFlags nFlags);
        object GetSocketOption(SocketOptionLevel level, SocketOptionName name);

        void GetSocketOption(SocketOptionLevel level, SocketOptionName name, byte[] val);

        byte[] GetSocketOption(SocketOptionLevel level, SocketOptionName name, int len);
        void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue);

        void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue);

        void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue);

        void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue);
    }
}
