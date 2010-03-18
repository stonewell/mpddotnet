using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.Network.Impl;
using Mule.Preference;
using Mule.Network;
using Mpd.Utilities;

using System.Diagnostics;
using Mpd.Generic.IO;
using Mpd.Generic;
using Mule.File;

namespace Mule.Network.Impl
{
    class ClientReqSocketImpl : EMSocketImpl, ClientReqSocket
    {
        #region Events
        public event ClientTimeoutEventHandler QueryClientTimeOut;
        public event ClientTimeoutEventHandler CheckClientTimeOut;
        public event SocketEventHandler SocketStateChanging;
        public event SocketEventHandler SocketStateChanged;
        #endregion

        #region Constructors
        public ClientReqSocketImpl()
            : base()
        {
            ResetTimeOutTimer();
            socketState_ = SocketStateEnum.SS_Other;
        }
        #endregion

        #region ClientReqSocket Members
        private void Disconnect(string reason)
        {
            ConnectionState = ConnectionStateEnum.ES_DISCONNECTED;

            DisonnectReason =
                string.Format("ClientRequestSocket Disconnected:{0}", reason);

            Shutdown(System.Net.Sockets.SocketShutdown.Both);
        }

        private void WaitForOnConnect()
        {
            SocketState =(SocketStateEnum.SS_Half);
        }

        private void ResetTimeOutTimer()
        {
            timeout_timer_ = MpdUtilities.GetTickCount();
        }

        public string DisonnectReason { get; set; }

        public override uint TimeOut
        {
            get
            {
                if (QueryClientTimeOut != null)
                {
                    uint clientTimeout = QueryClientTimeOut(this, new SocketEventArgs());
                    return Math.Max(base.TimeOut, clientTimeout);
                }

                return base.TimeOut;
            }
            set
            {
                base.TimeOut = value;
            }
        }

        public bool CheckTimeOut()
        {
            if (socketState_ == SocketStateEnum.SS_Half)
            {
                //This socket is still in a half connection state.. Because of SP2, we don't know
                //if this socket is actually failing, or if this socket is just queued in SP2's new
                //protection queue. Therefore we give the socket a chance to either finally report
                //the connection error, or finally make it through SP2's new queued socket system..
                if (MpdUtilities.GetTickCount() - timeout_timer_ > base.TimeOut * 4)
                {
                    timeout_timer_ = MpdUtilities.GetTickCount();
                    string str = string.Format(("Timeout: State:{0} = SocketStateEnum.SS_Half"), socketState_);
                    Disconnect(str);
                    return true;
                }
                return false;
            }

            uint uTimeout = TimeOut;

            if (CheckClientTimeOut != null)
            {
                uTimeout += CheckClientTimeOut(this, new SocketEventArgs());
            }

            if (MpdUtilities.GetTickCount() - timeout_timer_ > uTimeout)
            {
                timeout_timer_ = MpdUtilities.GetTickCount();
                string str = string.Format(("Timeout: State:{0} (0 = SocketStateEnum.SS_Other, 1 = SocketStateEnum.SS_Half, 2 = SocketStateEnum.SS_Complete"), socketState_);
                Disconnect(str);
                return true;
            }
            return false;
        }

        #endregion

        #region Members
        protected override void OnReceive(int nErrorCode)
        {
            ResetTimeOutTimer();
            base.OnReceive(nErrorCode);
        }

        protected override void OnConnect(int nErrorCode)
        {
            SocketState =(SocketStateEnum.SS_Complete);

            base.OnConnect(nErrorCode);

            if (nErrorCode != 0)
            {
                Disconnect(string.Format("TCP ERROR:{0}", (EMSocketErrorCodeEnum)nErrorCode));
            }
            else
            {
                //This socket may have been delayed by SP2 protection, lets make sure it doesn't time out instantly.
                ResetTimeOutTimer();
            }
        }

        protected override void OnClose(int nErrorCode)
        {
            SocketState = (SocketStateEnum.SS_Other);

            Disconnect("Close");

            base.OnClose(nErrorCode);
        }

        protected override void OnSend(int nErrorCode)
        {
            ResetTimeOutTimer();
            base.OnSend(nErrorCode);
        }

        protected override void OnError(int nErrorCode)
        {
            Disconnect(string.Format("Tcp Error:{0}", (EMSocketErrorCodeEnum)nErrorCode));
        }

        public SocketStateEnum SocketState
        {
            get
            {
                return socketState_;
            }

            set
            {
                //If no change, do nothing..
                if (value == socketState_)
                    return;

                if (SocketStateChanging != null)
                {
                    SocketStateChanging(this, new SocketEventArgs());
                }

                //Set state to new state..
                socketState_ = value;

                if (SocketStateChanged != null)
                {
                    SocketStateChanged(this, new SocketEventArgs());
                }
            }
        }

        public override SocketSentBytes SendControlData(uint maxNumberOfBytesToSend, uint minFragSize)
        {
            SocketSentBytes returnStatus = base.SendControlData(maxNumberOfBytesToSend, minFragSize);
            if (returnStatus.Success && (returnStatus.SentBytesControlPackets > 0 || returnStatus.SentBytesStandardPackets > 0))
                ResetTimeOutTimer();
            return returnStatus;
        }

        public override SocketSentBytes SendFileAndControlData(uint maxNumberOfBytesToSend, uint minFragSize)
        {
            SocketSentBytes returnStatus = base.SendFileAndControlData(maxNumberOfBytesToSend, minFragSize);
            if (returnStatus.Success && (returnStatus.SentBytesControlPackets > 0 || returnStatus.SentBytesStandardPackets > 0))
                ResetTimeOutTimer();
            return returnStatus;
        }

        public override void SendPacket(Packet packet, bool delpacket, bool controlpacket, uint actualPayloadSize, bool bForceImmediateSend)
        {
            ResetTimeOutTimer();
            base.SendPacket(packet, delpacket, controlpacket, actualPayloadSize, bForceImmediateSend);
        }

        protected uint timeout_timer_;
        protected SocketStateEnum socketState_;
        #endregion
    }
}
