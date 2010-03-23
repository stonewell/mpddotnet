using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core.Impl
{
    class ServerConnectImpl : ServerConnect
    {
        #region ServerConnect Members

        public void ConnectionFailed(Mule.Network.ServerSocket sender)
        {
            throw new NotImplementedException();
        }

        public void ConnectionEstablished(Mule.Network.ServerSocket sender)
        {
            throw new NotImplementedException();
        }

        public void ConnectToAnyServer()
        {
            throw new NotImplementedException();
        }

        public void ConnectToAnyServer(uint startAt, bool prioSort, bool isAuto, bool bNoCrypt)
        {
            throw new NotImplementedException();
        }

        public void ConnectToServer(Mule.ED2K.ED2KServer toconnect, bool multiconnect, bool bNoCrypt)
        {
            throw new NotImplementedException();
        }

        public void StopConnectionTry()
        {
            throw new NotImplementedException();
        }

        public void CheckForTimeout()
        {
            throw new NotImplementedException();
        }

        public void DestroySocket(Mule.Network.ServerSocket pSck)
        {
            throw new NotImplementedException();
        }

        public bool SendPacket(Mule.Network.Packet packet)
        {
            throw new NotImplementedException();
        }

        public bool SendPacket(Mule.Network.Packet packet, bool delpacket)
        {
            throw new NotImplementedException();
        }

        public bool SendPacket(Mule.Network.Packet packet, bool delpacket, Mule.Network.ServerSocket to)
        {
            throw new NotImplementedException();
        }

        public bool IsUDPSocketAvailable
        {
            get { throw new NotImplementedException(); }
        }

        public bool SendUDPPacket(Mule.Network.Packet packet, Mule.ED2K.ED2KServer host)
        {
            throw new NotImplementedException();
        }

        public bool SendUDPPacket(Mule.Network.Packet packet, Mule.ED2K.ED2KServer host, bool delpacket)
        {
            throw new NotImplementedException();
        }

        public bool SendUDPPacket(Mule.Network.Packet packet, Mule.ED2K.ED2KServer host, bool delpacket, ushort nSpecialPort)
        {
            throw new NotImplementedException();
        }

        public bool SendUDPPacket(Mule.Network.Packet packet, Mule.ED2K.ED2KServer host, bool delpacket, ushort nSpecialPort, byte[] pRawPacket)
        {
            throw new NotImplementedException();
        }

        public bool SendUDPPacket(Mule.Network.Packet packet, Mule.ED2K.ED2KServer host, bool delpacket, ushort nSpecialPort, byte[] pRawPacket, uint nLen)
        {
            throw new NotImplementedException();
        }

        public void KeepConnectionAlive()
        {
            throw new NotImplementedException();
        }

        public bool Disconnect()
        {
            throw new NotImplementedException();
        }

        public bool IsConnecting
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsConnected
        {
            get { throw new NotImplementedException(); }
        }

        public uint ClientID
        {
            get { throw new NotImplementedException(); }
        }

        public Mule.ED2K.ED2KServer CurrentServer
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsLowID
        {
            get { throw new NotImplementedException(); }
        }

        public void SetClientID(uint newid)
        {
            throw new NotImplementedException();
        }

        public bool IsLocalServer(uint dwIP, ushort nPort)
        {
            throw new NotImplementedException();
        }

        public void TryAnotherConnectionRequest()
        {
            throw new NotImplementedException();
        }

        public bool IsSingleConnect
        {
            get { throw new NotImplementedException(); }
        }

        public void InitLocalIP()
        {
            throw new NotImplementedException();
        }

        public uint LocalIP
        {
            get { throw new NotImplementedException(); }
        }

        public bool AwaitingTestFromIP(uint dwIP)
        {
            throw new NotImplementedException();
        }

        public bool IsConnectedObfuscated()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
