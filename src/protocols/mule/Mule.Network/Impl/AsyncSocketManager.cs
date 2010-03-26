using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using Mpd.Utilities;

namespace Mule.Network.Impl
{
    partial class AsyncSocketImpl
    {
        private class AsyncSocketManager
        {
            private object locker_ = new object();
            private Dictionary<Socket, AsyncSocketImpl> sockets_ =
                new Dictionary<Socket, AsyncSocketImpl>();
            private Thread runningThread_ = null;
            private bool shutDown_ = false;

            public AsyncSocketManager()
            {
                MuleApplication.Instance.ShutDownMuleApplication += new EventHandler(Instance_ShutDownMuleApplication);
                runningThread_ =
                    new Thread(new ThreadStart(SocketManagerThreadFunc));

                shutDown_ = false;

                runningThread_.Start();
            }

            private void Instance_ShutDownMuleApplication(object sender, EventArgs e)
            {
                try
                {
                    shutDown_ = true;

                    runningThread_.Join();
                }
                catch
                {
                }
            }

            private const int TIMEOUT = 1000;

            private void SocketManagerThreadFunc()
            {
                while (!shutDown_)
                {
                    List<Socket> readSockets = new List<Socket>();
                    List<Socket> writeSockets = new List<Socket>();
                    List<Socket> errorSockets = new List<Socket>();

                    lock (locker_)
                    {
                        sockets_.Keys.ToList().ForEach(s =>
                        {
                            readSockets.Add(s);
                            writeSockets.Add(s);
                            errorSockets.Add(s);
                        });
                    }

                    if (readSockets.Count == 0)
                    {
                        Thread.Sleep(TIMEOUT);
                        continue;
                    }

                    try
                    {
                        Socket.Select(readSockets, writeSockets, errorSockets, TIMEOUT * 1000);

                        if (shutDown_)
                            return;

                        lock (locker_)
                        {
                            readSockets.ForEach(s =>
                                {
                                    if (!sockets_.ContainsKey(s)) return;

                                    AsyncSocketImpl socket = sockets_[s];

                                    if (socket.listen_called_)
                                        socket.FireOnAcceptEvent();
                                    else
                                        socket.FireOnReceiveEvent();
                                });

                            writeSockets.ForEach(s =>
                                {
                                    if (!sockets_.ContainsKey(s)) return;

                                    AsyncSocketImpl socket = sockets_[s];

                                    if (socket.Connected && socket.connect_event_fired_)
                                    {
                                        socket.FireOnSendEvent();
                                    }
                                    else
                                    {
                                        socket.connect_event_fired_ = true;
                                        socket.FireOnConnectEvent();
                                    }
                                });
                            errorSockets.ForEach(s =>
                            {
                                if (!sockets_.ContainsKey(s)) return;

                                AsyncSocketImpl socket = sockets_[s];

                                socket.FireOnErrorEvent(10061);
                            });
                        }
                    }
                    catch (SocketException ex)
                    {
                        MpdUtilities.DebugLogError(string.Format("Socket Exception:{0},{1}",
                            ex.ErrorCode, ex.SocketErrorCode), ex);
                    }
                    catch (Exception ex)
                    {
                        MpdUtilities.DebugLogError(ex);
                    }
                }//while
            }

            internal void AddAsyncSocket(AsyncSocketImpl socket)
            {
                lock (locker_)
                {
                    if (!sockets_.ContainsKey(socket.socket_))
                        sockets_[socket.socket_] = socket;
                }
            }

            internal void RemoveAsyncSocket(AsyncSocketImpl socket)
            {
                lock (locker_)
                {
                    if (sockets_.ContainsKey(socket.socket_))
                        sockets_.Remove(socket.socket_);
                }
            }
        }

        private void FireOnAcceptEvent()
        {
            if (SocketAccepted != null)
                SocketAccepted(this, new SocketEventArgs());
        }

        private void FireOnReceiveEvent()
        {
            if (SocketReceiveable != null)
                SocketReceiveable(this, new SocketEventArgs());
        }

        private void FireOnSendEvent()
        {
            if (SocketSendable != null)
                SocketSendable(this, new SocketEventArgs());
        }

        private void FireOnConnectEvent()
        {
            if (SocketConnected != null)
                SocketConnected(this, new SocketEventArgs());
        }

        private void FireOnErrorEvent(int errCode)
        {
            if (SocketErrorOccured != null)
                SocketErrorOccured(this, new SocketErrorEventArgs(errCode));
        }
    }
}
