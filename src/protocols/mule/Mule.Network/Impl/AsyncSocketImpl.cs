using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Mpd.Utilities;
using Mule.Preference;

namespace Mule.Network.Impl
{
    public class AsyncSocketImpl : AsyncSocket
    {
        #region Fields
        private Socket socket_ = null;
        #endregion

        #region Constructors
        public AsyncSocketImpl(MuleApplication muleApp, AddressFamily family,
          SocketType sType, ProtocolType pType)
        {
            MuleApp = muleApp;

            socket_ = new Socket(family, sType, pType);
        }

        ~AsyncSocketImpl()
        {
            Dispose(true);
        }
        #endregion

        #region IDisposable Members
        private bool disposed_ = false;

        public virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed_)
            {
                if (disposing)
                {
                    CleanUp();
                }

                disposed_ = true;
            }
        }

        public virtual void CleanUp()
        {
            socket_.Close();
        }

        public void Dispose()
        {
            Dispose(false);
        }

        #endregion

        #region Members
        public MuleApplication MuleApp { get; set; }

        public virtual bool Connected
        {
            get { return socket_.Connected; }
        }

        public virtual void Connect(string host, int port)
        {
            socket_.Connect(host, port);
        }

        public virtual EndPoint RemoteEndPoint
        {
            get { return socket_.RemoteEndPoint; }
        }

        public virtual void Close()
        {
            socket_.Close();
        }

        public virtual void Close(int timeout)
        {
            socket_.Close(timeout);
        }

        public virtual int Send(byte[] pBuffer, int offset, int nBufLen)
        {
            return Send(pBuffer, offset, nBufLen, SocketFlags.None);
        }

        public virtual int Send(byte[] pBuffer, int nBufLen)
        {
            return Send(pBuffer, nBufLen, SocketFlags.None);
        }

        public virtual int Send(byte[] pBuffer, int nBufLen, SocketFlags nFlags)
        {
            return Send(pBuffer, 0, nBufLen, SocketFlags.None);
        }

        public virtual int Send(byte[] pBuffer, int offset, int nBufLen, SocketFlags nFlags)
        {
            try
            {
                return socket_.Send(pBuffer, offset, nBufLen, (SocketFlags)nFlags);
            }
            catch (SocketException ex)
            {
                MpdUtilities.DebugLogError("Send Fail", ex);
                return -1;
            }
        }

        public virtual int Receive(byte[] pBuffer, int offset, int nBufLen)
        {
            return Receive(pBuffer, offset, nBufLen, SocketFlags.None);
        }

        public virtual int Receive(byte[] pBuffer, int nBufLen)
        {
            return Receive(pBuffer, nBufLen, SocketFlags.None);
        }

        public virtual int Receive(byte[] pBuffer, int nBufLen, SocketFlags nFlags)
        {
            return Receive(pBuffer, 0, nBufLen, nFlags);
        }

        public virtual int Receive(byte[] pBuffer, int offset, int nBufLen, SocketFlags nFlags)
        {
            try
            {
                return socket_.Receive(pBuffer, offset, nBufLen, nFlags);
            }
            catch (SocketException ex)
            {
                MpdUtilities.DebugLogError("Receive Fail", ex);
                return -1;
            }
        }

        public const int SOCKET_ERROR = -1;

        public bool IsSendBlocked { get; set; }
        public bool IsReceiveBlocked { get; set; }

        protected virtual void OnError(int nErrorCode)
        {
        }

        protected virtual void OnClose(int nErrorCode)
        {
        }

        protected virtual void OnReceive(int nErrorCode)
        {
        }

        protected virtual void OnSend(int nErrorCode)
        {
        }

        protected virtual void OnConnect(int nErrorCode)
        {
        }

        protected virtual void OnAccept(int nErrorCode)
        {
        }

        public object GetSocketOption(SocketOptionLevel level, SocketOptionName name)
        {
            return socket_.GetSocketOption(level, name);
        }

        public void GetSocketOption(SocketOptionLevel level, SocketOptionName name, byte[] val)
        {
            socket_.GetSocketOption(level, name, val);
            socket_.SetSocketOption(level, name, val);
        }

        public byte[] GetSocketOption(SocketOptionLevel level, SocketOptionName name, int len)
        {
            return socket_.GetSocketOption(level, name, len);
        }

        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue)
        {
            socket_.SetSocketOption(optionLevel, optionName, optionValue);
        }

        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
        {
            socket_.SetSocketOption(optionLevel, optionName, optionValue);
        }

        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
        {
            socket_.SetSocketOption(optionLevel, optionName, optionValue);
        }
        
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue)
        {
            socket_.SetSocketOption(optionLevel, optionName, optionValue);
        }

        public void Shutdown(SocketShutdown how)
        {
            socket_.Shutdown(how);
        }

        #endregion
    }
}
