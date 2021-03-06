﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.Preference;
using System.Net;
using System.Net.Sockets;
using Mpd.Utilities;
using System.Diagnostics;

namespace Mule.Network.Impl
{
    class ListenSocketImpl : AsyncSocketImpl, ListenSocket
    {
        #region Fields
        private bool bListening_ = false;
        private List<ClientReqSocket> socket_list_ = new List<ClientReqSocket>();
        private ushort openSocketsInterval_ = 0;
        private ushort[] connectionStates_ = new ushort[3] { 0, 0, 0 };
        private int pendingConnections_ = 0;
        private ushort port_ = 0;
        private object locker_ = new object();
        #endregion

        #region Events
        public event FilterAcceptClientEventHandler FilterAcceptClient;
        #endregion

        #region Constructors
        public ListenSocketImpl()
        {
        }
        #endregion

        #region ListenSocket Members

        public bool StartListening()
        {
            try
            {
                if (!base.CreateSocket(new IPEndPoint(0, 0).AddressFamily, 
                    SocketType.Stream, ProtocolType.Tcp))
                    return false;

                IPAddress address = IPAddress.Any;

                if (MuleApplication.Instance.Preference.BindAddr != null &&
                    MuleApplication.Instance.Preference.BindAddr.Length > 0)
                {
                    IPAddress.TryParse(MuleApplication.Instance.Preference.BindAddr, out address);
                }

                IPEndPoint endpoint =
                    new IPEndPoint(address,
                        MuleApplication.Instance.Preference.Port);

                Bind(endpoint);

                Listen();

                port_ = MuleApplication.Instance.Preference.Port;
                bListening_ = true;

                return true;
            }
            catch (Exception ex)
            {
                MpdUtilities.DebugLogError("Start Listening Error", ex);
                return false;
            }
        }

        public void StopListening()
        {
            bListening_ = false;
            MaxConnectionReached++;
        }

        public void Process()
        {
            openSocketsInterval_ = 0;
            List<ClientReqSocket> tmp = new List<ClientReqSocket>();

            lock (locker_)
            {
                tmp.AddRange(socket_list_);
            }

            tmp.ForEach(s =>
                {
                    if (s.ConnectionState == ConnectionStateEnum.CS_DISCONNECTED)
                        s.Close();
                    else
                        s.CheckTimeOut();
                });

            if ((OpenSocketsCount + 5 < MuleApplication.Instance.Preference.MaxConnections || 
                MuleApplication.Instance.ServerConnect.IsConnecting) && 
                !bListening_)
                ReStartListening();
        }

        public void RemoveSocket(ClientReqSocket todel)
        {
            lock (locker_)
            {
                if (!socket_list_.Contains(todel))
                    return;
             
                todel.SocketClosed -= ClientReqSocket_Close;
                todel.SocketStateChanging -= ClientReqSocket_SocketStateChanging;
                todel.SocketStateChanged -= ClientReqSocket_SocketStateChanged;

                socket_list_.Remove(todel);
            }
        }

        public void AddSocket(ClientReqSocket toadd)
        {
            lock (locker_)
            {
                if (socket_list_.Contains(toadd))
                    return;

                AddConnection();
                toadd.SocketClosed += ClientReqSocket_Close;
                toadd.SocketStateChanging += ClientReqSocket_SocketStateChanging;
                toadd.SocketStateChanged += ClientReqSocket_SocketStateChanged;

                socket_list_.Add(toadd);
            }
        }

        public int OpenSocketsCount
        {
            get { return socket_list_.Count; }
        }

        public void KillAllSockets()
        {
            lock (locker_)
            {
                socket_list_.Clear();
            }
        }

        public bool TooManySockets()
        {
            return TooManySockets(false);
        }

        public bool TooManySockets(bool bIgnoreInterval)
        {
            if (OpenSocketsCount > MuleApplication.Instance.Preference.MaxConnections
                || (openSocketsInterval_ > (MuleApplication.Instance.Preference.MaxConnectionsPerFile * MaxConnectionsPerFileModifier) && 
                !bIgnoreInterval)
                || (TotalHalfConnectSocket >= MuleApplication.Instance.Preference.MaxHalfConnections && 
                !bIgnoreInterval))
                return true;
            return false;
        }

        public uint MaxConnectionReached
        {
            get;
            set;
        }

        public bool IsValidSocket(ClientReqSocket totest)
        {
            lock (locker_)
            {
                return socket_list_.Contains(totest);
            }
        }

        public void AddConnection()
        {
            openSocketsInterval_++;
        }

        public void RecalculateStats()
        {
            Array.Clear(connectionStates_, 0, connectionStates_.Length);

            lock (locker_)
            {
                socket_list_.ForEach(s =>
                    {
                        switch (s.ConnectionState)
                        {
                            case ConnectionStateEnum.CS_DISCONNECTED:
                                connectionStates_[0]++;
                                break;
                            case ConnectionStateEnum.CS_NOTCONNECTED:
                                connectionStates_[1]++;
                                break;
                            case ConnectionStateEnum.CS_CONNECTED:
                                connectionStates_[2]++;
                                break;
                        }
                    });
            }
        }

        public void ReStartListening()
        {
            bListening_ = true;

            if (pendingConnections_ > 0)
            {
                pendingConnections_--;
                OnAccept(0);
            }
        }

        public bool Rebind()
        {
            if (MuleApplication.Instance.Preference.Port == port_)
                return false;

            Close();
            KillAllSockets();

            return StartListening();
        }

        public bool SendPortTestReply(char result)
        {
            return SendPortTestReply(result, false);
        }

        public bool SendPortTestReply(char result, bool disconnect)
        {
            //TODO:
            return false;
        }

        public void UpdateConnectionsStatus()
        {
            ActiveConnections = (uint)OpenSocketsCount;

            // Update statistics for 'peak connections'
            if (PeakConnections < ActiveConnections)
                PeakConnections = ActiveConnections;
            if (PeakConnections > MuleApplication.Instance.Preference.ConnectionPeakConnections)
                MuleApplication.Instance.Preference.ConnectionPeakConnections = PeakConnections;

            if (MuleApplication.Instance.IsConnected)
            {
                TotalConnectionChecks++;
                if (TotalConnectionChecks == 0)
                {
                    // wrap around occured, avoid division by zero
                    TotalConnectionChecks = 100;
                }

                // Get a weight for the 'avg. connections' value. The longer we run the higher 
                // gets the weight (the percent of 'avg. connections' we use).
                float fPercent = (float)(TotalConnectionChecks - 1) / (float)TotalConnectionChecks;
                if (fPercent > 0.99F)
                    fPercent = 0.99F;

                // The longer we run the more we use the 'avg. connections' value and the less we
                // use the 'active connections' value. However, if we are running quite some time
                // without any connections (except the server connection) we will eventually create 
                // a floating point underflow exception.
                AverageConnections = AverageConnections * fPercent + ActiveConnections * (1.0F - fPercent);
                if (AverageConnections < 0.001F)
                    AverageConnections = 0.001F;	// avoid floating point underflow
            }
        }

        public float MaxConnectionsPerFileModifier
        {
            get 
            {
                float SpikeSize = OpenSocketsCount - AverageConnections;
                if (SpikeSize < 1.0F)
                    return 1.0F;

                float SpikeTolerance = 25.0F * (float)MuleApplication.Instance.Preference.MaxConnectionsPerFile / 10.0F;
                if (SpikeSize > SpikeTolerance)
                    return 0;

                float Modifier = 1.0F - SpikeSize / SpikeTolerance;
                return Modifier;
            }
        }

        public uint PeakConnections
        {
            get;
            set;
        }

        public uint TotalConnectionChecks
        {
            get;
            set;
        }

        public float AverageConnections
        {
            get;
            set;
        }

        public uint ActiveConnections
        {
            get;
            set;
        }

        public ushort ConnectedPort
        {
            get;
            set;
        }

        public uint TotalHalfConnectSocket
        {
            get;
            set;
        }

        public uint TotalCompleteConnectSocket
        {
            get;
            set;
        }

        protected override void OnClose(int nErrorCode)
        {
            base.OnClose(nErrorCode);
            KillAllSockets();
        }

        protected override void OnAccept(int nErrorCode)
        {
            if (nErrorCode == 0)
            {
                pendingConnections_++;
                if (pendingConnections_ < 1)
                {
                    Debug.Assert(false);
                    pendingConnections_ = 1;
                }

                if (TooManySockets(true) && !MuleApplication.Instance.ServerConnect.IsConnecting)
                {
                    StopListening();
                    return;
                }
                else if (!bListening_)
                    ReStartListening(); //If the client is still at maxconnections, this will allow it to go above it.. But if you don't, you will get a lowID on all servers.

                uint nFataErrors = 0;
                while (pendingConnections_ > 0)
                {
                    pendingConnections_--;

                    AsyncSocket socket = Accept();

                    if (socket == null)
                    {
                        if (SocketErrorCode == SocketError.WouldBlock)
                        {
                            pendingConnections_ = 0;
                            break;
                        }
                        else
                        {
                            if (SocketErrorCode != SocketError.ConnectionRefused)
                            {
                                nFataErrors++;
                            }
                        }
                        if (nFataErrors > 10)
                        {
                            // the question is what todo on a error. We cant just ignore it because then the backlog will fill up
                            // and lock everything. We can also just endlos try to repeat it because this will lock up eMule
                            // this should basically never happen anyway
                            // however if we are in such a position, try to reinitalize the socket.
                            Close();
                            StartListening();
                            pendingConnections_ = 0;
                            break;
                        }
                        continue;
                    }

                    if (FilterClient(socket.RemoteEndPoint))
                        continue;

                    ClientReqSocketImpl newclient = new ClientReqSocketImpl();
                    if (newclient.AttachHandle(socket as AsyncSocketImpl))
                    {
                        AddSocket(newclient);
                    }

                    Debug.Assert(pendingConnections_ >= 0);
                }
            }

        }
        #endregion

        #region Members

        private bool FilterClient(EndPoint endPoint)
        {
            if (FilterAcceptClient != null)
            {
                FilterAcceptClientEventArgs args = new FilterAcceptClientEventArgs(endPoint);

                FilterAcceptClient(args);

                return args.Filtered;
            }

            return false;
        }

        private void ClientReqSocket_SocketStateChanging(object sender, SocketEventArgs arg)
        {
            ClientReqSocket cs = sender as ClientReqSocket;

            if (cs == null) return;

            //Decrease count of old state..
            switch (cs.SocketState)
            {
                case SocketStateEnum.SS_Half:
                    TotalHalfConnectSocket--;
                    break;
                case SocketStateEnum.SS_Complete:
                    TotalCompleteConnectSocket--;
                    break;
            }
        }

        private void ClientReqSocket_SocketStateChanged(object sender, SocketEventArgs arg)
        {
            ClientReqSocket cs = sender as ClientReqSocket;

            if (cs == null) return;

            //Increase count of old state..
            switch (cs.SocketState)
            {
                case SocketStateEnum.SS_Half:
                    TotalHalfConnectSocket++;
                    break;
                case SocketStateEnum.SS_Complete:
                    TotalCompleteConnectSocket++;
                    break;
            }
        }

        private void ClientReqSocket_Close(object sender, SocketEventArgs arg)
        {
            ClientReqSocket cs = sender as ClientReqSocket;

            if (cs == null) return;

            RemoveSocket(cs);
        }
        #endregion
    }
}
