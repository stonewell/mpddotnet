using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.Preference;
using System.Net;
using System.Net.Sockets;

namespace Mule.Network
{
    public class SocketEventArgs
    {
        public SocketEventArgs()
        {
        }
    }

    public class SocketErrorEventArgs : SocketEventArgs
    {
        public SocketErrorEventArgs()
        {
        }

        public SocketErrorEventArgs(int errCode)
        {
            ErrorCode = errCode;
        }

        public int ErrorCode;
    }

    public delegate void SocketEventHandler(object sender, SocketEventArgs arg);
    public delegate void SocketErrorEventHandler(object sender, SocketErrorEventArgs arg);

    public interface AsyncSocket
    {
        event SocketEventHandler SocketCreated;
        event SocketEventHandler SocketDisconnected;
        event SocketEventHandler SocketClosed;
        event SocketEventHandler SocketConnected;
        event SocketEventHandler SocketAccepted;
        event SocketEventHandler SocketReceiveable;
        event SocketEventHandler SocketSendable;
        event SocketErrorEventHandler SocketErrorOccured;

        
        
        bool Connected { get; }
        bool Connect(string host, uint port);
        
        EndPoint RemoteEndPoint { get; }

        int Send(byte[] pBuffer, int offset, int nBufLen);
        int Send(byte[] pBuffer, int nBufLen);
        int Send(byte[] pBuffer, int nBufLen, SocketFlags nFlags);
        int Send(byte[] pBuffer, int offset, int nBufLen, SocketFlags nFlags);

        int Receive(byte[] pBuffer, int offset, int nBufLen);
        int Receive(byte[] pBuffer, int nBufLen);
        int Receive(byte[] pBuffer, int nBufLen, SocketFlags nFlags);
        int Receive(byte[] pBuffer, int offset, int nBufLen, SocketFlags nFlags);

        int ReceiveFrom(byte[] buf, ref EndPoint endPoint);
        int ReceiveFrom(byte[] buf, SocketFlags flags, ref EndPoint endPoint);
        int ReceiveFrom(byte[] buf, int size, SocketFlags flags, ref EndPoint endPoint);
        int ReceiveFrom(byte[] buf, int offset, int size, SocketFlags flags, ref EndPoint endPoint);

        int SendTo(byte[] buf, EndPoint endPoint);
        int SendTo(byte[] buf, SocketFlags flags, EndPoint endPoint);
        int SendTo(byte[] buf, int size, SocketFlags flags, EndPoint endPoint);
        int SendTo(byte[] buf, int offset, int size, SocketFlags flags, EndPoint endPoint);

        object GetSocketOption(SocketOptionLevel level, SocketOptionName name);
        void GetSocketOption(SocketOptionLevel level, SocketOptionName name, byte[] val);
        byte[] GetSocketOption(SocketOptionLevel level, SocketOptionName name, int len);

        void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue);
        void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue);
        void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue);
        void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue);

        void Close();
        void Close(int timeout);
        void Shutdown(SocketShutdown how);

        SocketError SocketErrorCode { get; set; }
    }
}
