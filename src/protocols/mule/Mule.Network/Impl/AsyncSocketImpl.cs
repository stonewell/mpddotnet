using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Mpd.Utilities;

namespace Mule.Network.Impl
{
    partial class AsyncSocketImpl : AsyncSocket
    {
        #region Sockets Manager
        private static readonly AsyncSocketManager socketManager_ =
            new AsyncSocketManager();

        #endregion

        #region Events
        public event SocketEventHandler SocketCreated;
        public event SocketEventHandler SocketDisconnected;
        public event SocketEventHandler SocketClosed;
        public event SocketEventHandler SocketConnected;
        public event SocketEventHandler SocketAccepted;
        public event SocketEventHandler SocketReceiveable;
        public event SocketEventHandler SocketSendable;
        public event SocketErrorEventHandler SocketErrorOccured;
        #endregion

        #region Fields
        private Socket socket_ = null;
        private bool connect_event_fired_ = false;
        private bool listen_called_ = false;
        #endregion

        #region Constructors
        public AsyncSocketImpl()
        {
        }

        protected AsyncSocketImpl(Socket s)
        {
            socket_ = s;

            Init();
        }

        protected AsyncSocketImpl(AsyncSocketImpl sockImpl)
        {
            sockImpl.DisableEvents();
            socketManager_.RemoveAsyncSocket(sockImpl);
            
            socket_ = sockImpl.socket_;

            Init();
        }

        private void Init()
        {
            SocketErrorCode = SocketError.Success;

            InitEvents();
            socket_.Blocking = false;
        }

        protected bool CreateSocket(AddressFamily family,
          SocketType sType, ProtocolType pType)
        {
            if (socket_ != null)
                return true;

            socket_ = new Socket(family, sType, pType);

            Init();

            SocketCreated(this, new SocketEventArgs());

            return true;
        }

        private void InitEvents()
        {
            SocketCreated += (AsyncSocketImpl_SocketCreated);
            SocketClosed += (AsyncSocketImpl_SocketClosed);
            SocketClosed += (AsyncSocketImpl_RemoveMe);
            SocketConnected += (AsyncSocketImpl_SocketConnected);
            SocketDisconnected += (AsyncSocketImpl_SocketDisconnected);
            SocketErrorOccured += (AsyncSocketImpl_SocketError);
            SocketAccepted += (AsyncSocketImpl_SocketAccepted);
            SocketReceiveable += (AsyncSocketImpl_SocketReceiveable);
            SocketSendable += (AsyncSocketImpl_SocketSendable);
        }

        private void DisableEvents()
        {
            SocketCreated -= (AsyncSocketImpl_SocketCreated);
            SocketClosed -= (AsyncSocketImpl_SocketClosed);
            SocketClosed -= (AsyncSocketImpl_RemoveMe);
            SocketConnected -= (AsyncSocketImpl_SocketConnected);
            SocketDisconnected -= (AsyncSocketImpl_SocketDisconnected);
            SocketErrorOccured -= (AsyncSocketImpl_SocketError);
            SocketAccepted -= (AsyncSocketImpl_SocketAccepted);
            SocketReceiveable -= (AsyncSocketImpl_SocketReceiveable);
            SocketSendable -= (AsyncSocketImpl_SocketSendable);
        }
        #endregion

        #region Members
        public void Bind(EndPoint endpoint)
        {
            socket_.Bind(endpoint);
        }

        public void Listen()
        {
            Listen(5);
        }

        public void Listen(int backlog)
        {
            listen_called_ = true;
            socket_.Listen(backlog);
        }

        public AsyncSocket Accept()
        {
            SocketErrorCode = SocketError.Success;

            try
            {
                return CreateAsyncSocket(socket_.Accept());
            }
            catch (SocketException ex)
            {
                SocketErrorCode = ex.SocketErrorCode;

                MpdUtilities.DebugLogError(string.Format("Socket Exception:{0},{1}",
                    ex.ErrorCode, ex.SocketErrorCode), ex);

                return null;
            }
        }

        private AsyncSocket CreateAsyncSocket(Socket socket)
        {
            return new AsyncSocketImpl(socket);
        }

        public virtual bool Connected
        {
            get { return socket_.Connected; }
        }

        public virtual bool Connect(string host, uint port)
        {
            socket_.Connect(host,(int) port);

            return Connected;
        }

        public virtual EndPoint RemoteEndPoint
        {
            get { return socket_.RemoteEndPoint; }
        }

        public SocketError SocketErrorCode { get; set; }

        public virtual void Close()
        {
            socket_.Close();
            SocketClosed(this, new SocketEventArgs());
        }

        public virtual void Close(int timeout)
        {
            socket_.Close(timeout);
            SocketClosed(this, new SocketEventArgs());
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
            SocketErrorCode = SocketError.Success;
            try
            {
                return socket_.Send(pBuffer, offset, nBufLen, (SocketFlags)nFlags);
            }
            catch (SocketException ex)
            {
                MpdUtilities.DebugLogError(string.Format("Socket Exception:{0},{1}",
                    ex.ErrorCode, ex.SocketErrorCode), ex);

                SocketErrorCode = ex.SocketErrorCode;

                return SOCKET_ERROR;
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
            SocketErrorCode = SocketError.Success;
            try
            {
                return socket_.Receive(pBuffer, offset, nBufLen, nFlags);
            }
            catch (SocketException ex)
            {
                SocketErrorCode = ex.SocketErrorCode;
                MpdUtilities.DebugLogError(string.Format("Socket Exception:{0},{1}",
                    ex.ErrorCode, ex.SocketErrorCode), ex);
                return SOCKET_ERROR;
            }
        }

        public const int SOCKET_ERROR = -1;

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

        protected virtual void OnDisconnect(int errCode)
        {
        }

        protected virtual void OnCreate(int errCode)
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

            SocketDisconnected(this, new SocketEventArgs());
        }

        public int ReceiveFrom(byte[] buf, ref EndPoint endPoint)
        {
            return socket_.ReceiveFrom(buf, ref endPoint);
        }

        public int ReceiveFrom(byte[] buf, SocketFlags flags, ref EndPoint endPoint)
        {
            return socket_.ReceiveFrom(buf, flags,ref endPoint);
        }

        public int ReceiveFrom(byte[] buf, int size, SocketFlags flags, ref EndPoint endPoint)
        {
            return socket_.ReceiveFrom(buf, size, flags, ref endPoint);
        }

        public int ReceiveFrom(byte[] buf, int offset, int size, SocketFlags flags, ref EndPoint endPoint)
        {
            return socket_.ReceiveFrom(buf, offset, size, flags, ref endPoint);
        }

        public int SendTo(byte[] buf, EndPoint endPoint)
        {
            return socket_.SendTo(buf, endPoint);
        }

        public int SendTo(byte[] buf, SocketFlags flags, EndPoint endPoint)
        {
            return socket_.SendTo(buf, flags, endPoint);
        }

        public int SendTo(byte[] buf, int size, SocketFlags flags, EndPoint endPoint)
        {
            return socket_.SendTo(buf, size, flags, endPoint);
        }

        public int SendTo(byte[] buf, int offset, int size, SocketFlags flags, EndPoint endPoint)
        {
            return socket_.SendTo(buf, offset, size, flags, endPoint);
        }
        #endregion

        #region Event Handlers
        private void AsyncSocketImpl_SocketError(object sender, SocketErrorEventArgs arg)
        {
            OnError(arg.ErrorCode);
        }

        private void AsyncSocketImpl_SocketDisconnected(object sender, SocketEventArgs arg)
        {
            OnDisconnect(0);
        }

        private void AsyncSocketImpl_SocketConnected(object sender, SocketEventArgs arg)
        {
            OnConnect(0);
        }

        private void AsyncSocketImpl_RemoveMe(object sender, SocketEventArgs arg)
        {
            socketManager_.RemoveAsyncSocket(this);
        }

        private void AsyncSocketImpl_SocketClosed(object sender, SocketEventArgs arg)
        {
            OnClose(0);
        }

        private void AsyncSocketImpl_SocketCreated(object sender, SocketEventArgs arg)
        {
            OnCreate(0);

            socketManager_.AddAsyncSocket(this);
        }

        private void AsyncSocketImpl_SocketAccepted(object sender, SocketEventArgs arg)
        {
            OnAccept(0);
        }

        private void AsyncSocketImpl_SocketSendable(object sender, SocketEventArgs arg)
        {
            OnSend(0);
        }

        private void AsyncSocketImpl_SocketReceiveable(object sender, SocketEventArgs arg)
        {
            OnReceive(0);
        }

        public bool AttachHandle(AsyncSocketImpl sockImpl)
        {
            if (sockImpl == null) return false;

            sockImpl.DisableEvents();
            socketManager_.RemoveAsyncSocket(sockImpl);

            DisableEvents();
            socket_ = sockImpl.socket_;

            Init();

            return true;
        }

        #endregion
    }
}
