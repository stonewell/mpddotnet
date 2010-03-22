using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Utilities;
using System.Net;

namespace Mule.ED2K.Impl
{
    class ED2KServerImpl : ED2KServer
    {
        #region Constructors
        public ED2KServerImpl(ServerMet in_data)
        {
            Port = (ushort)in_data.port;
            IP = in_data.ip;
            DynIP = string.Empty;
            InitAll();
        }

        public ED2KServerImpl(ushort in_port, string i_addr)
        {
            Port = in_port;

            IPAddress address;

            if (IPAddress.TryParse(i_addr, out address))
            {
                if (address == IPAddress.None &&
                    string.Compare(i_addr, "255.255.255.255") != 0)
                {
                    DynIP = i_addr;
                    IP = 0;
                }
                else
                {
                    IP = BitConverter.ToUInt32(address.GetAddressBytes(), 0);
                    DynIP = string.Empty;
                }
            }
            else
            {
                IP = 0;
                DynIP = string.Empty;
            }

            InitAll();
        }

        public ED2KServerImpl(ED2KServer pOld)
        {
            Port = pOld.Port;
            IP = pOld.IP;
            StaticMember = pOld.StaticMember;
            FileCount = pOld.FileCount;
            UserCount = pOld.UserCount;
            Preference = pOld.Preference;
            Ping = pOld.Ping;
            FailedCount = pOld.FailedCount;
            LastPinged = pOld.LastPinged;
            LastPingedTime = pOld.LastPingedTime;
            MaxUsers = pOld.MaxUsers;
            SoftFiles = pOld.SoftFiles;
            HardFiles = pOld.HardFiles;
            LastDescPingedCount = pOld.LastDescPingedCount;
            Description = pOld.Description;
            ListName = pOld.ListName;
            DynIP = pOld.DynIP;
            Version = pOld.Version;
            TCPFlags = pOld.TCPFlags;
            UDPFlags = pOld.UDPFlags;
            DescReqChallenge = pOld.DescReqChallenge;
            LowIDUsers = pOld.LowIDUsers;
            Challenge = pOld.Challenge;
            ServerKeyUDP = pOld.ServerKeyUDP;
            CryptPingReplyPending = pOld.CryptPingReplyPending;
            ServerKeyUDPIP = pOld.ServerKeyUDPIP;
            ObfuscationPortTCP = pOld.ObfuscationPortTCP;
            ObfuscationPortUDP = pOld.ObfuscationPortUDP;
            RealLastPingedTime = pOld.RealLastPingedTime;
        }

        private void InitAll()
        {
            FileCount = 0;
            UserCount = 0;
            Preference = 0;
            Ping = 0;
            FailedCount = 0;
            LastPinged = 0;
            LastPingedTime = 0;
            StaticMember = false;
            MaxUsers = 0;
            SoftFiles = 0;
            HardFiles = 0;
            LastDescPingedCount = 0;
            TCPFlags = 0;
            UDPFlags = 0;
            DescReqChallenge = 0;
            LowIDUsers = 0;
            Challenge = 0;
            serverKeyUDP_ = 0;
            CryptPingReplyPending = false;
            ServerKeyUDPIP = 0;
            ObfuscationPortTCP = 0;
            ObfuscationPortUDP = 0;
            RealLastPingedTime = 0;
        }
        #endregion

        #region ED2KServer Members

        public string ListName
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        private uint ip;

        public uint IP
        {
            get
            {
                return ip;
            }
            set
            {
                ip = value;
                FullIP = MpdUtilities.IP2String(value);
            }
        }

        public string DynIP
        {
            get;
            set;
        }

        public bool HasDynIP
        {
            get { return DynIP != null && DynIP.Length > 0; }
        }

        public string FullIP
        {
            get;
            set;
        }

        public string Address
        {
            get { if (HasDynIP) return DynIP; else return FullIP; }
        }

        public ushort Port
        {
            get;
            set;
        }

        public uint FileCount
        {
            get;
            set;
        }

        public uint UserCount
        {
            get;
            set;
        }

        public ED2KServerPreferenceEnum Preference
        {
            get;
            set;
        }

        public uint Ping
        {
            get;
            set;
        }

        public uint MaxUsers
        {
            get;
            set;
        }

        public uint FailedCount
        {
            get;
            set;
        }

        public void AddFailedCount()
        {
            FailedCount++;
        }

        public void ResetFailedCount()
        {
            FailedCount = 0;
        }

        public uint LastPingedTime
        {
            get;
            set;
        }

        public uint RealLastPingedTime
        {
            get;
            set;
        }

        public uint LastPinged
        {
            get;
            set;
        }

        public uint LastDescPingedCount
        {
            get;
            set;
        }

        public void SetLastDescPingedCount(bool reset)
        {
            if (reset)
                LastDescPingedCount = 0;
            else
                LastDescPingedCount++;
        }

        public bool StaticMember
        {
            get;
            set;
        }

        public uint Challenge
        {
            get;
            set;
        }

        public uint DescReqChallenge
        {
            get;
            set;
        }

        public uint SoftFiles
        {
            get;
            set;
        }

        public uint HardFiles
        {
            get;
            set;
        }

        public string Version
        {
            get;
            set;
        }

        public ED2KServerTcpFlagsEnum TCPFlags
        {
            get;
            set;
        }

        public ED2KServerUdpFlagsEnum UDPFlags
        {
            get;
            set;
        }

        public uint LowIDUsers
        {
            get;
            set;
        }

        public ushort ObfuscationPortTCP
        {
            get;
            set;
        }

        public ushort ObfuscationPortUDP
        {
            get;
            set;
        }

        private uint serverKeyUDP_;

        public uint ServerKeyUDP
        {
            get
            {
                return GetServerKeyUDP(false);
            }

            set
            {
                serverKeyUDP_ = value;
                ServerKeyUDPIP = (uint)MuleApplication.Instance.PublicIP;
            }
        }

        public uint GetServerKeyUDP(bool bForce)
        {
            if (ServerKeyUDPIP != 0 &&
                ServerKeyUDPIP == MuleApplication.Instance.PublicIP ||
                bForce)
                return serverKeyUDP_;
            else
                return 0;
        }

        public bool CryptPingReplyPending
        {
            get;
            set;
        }

        public uint ServerKeyUDPIP
        {
            get;
            set;
        }

        public bool UnicodeSupport
        {
            get
            {
                return (TCPFlags & ED2KServerTcpFlagsEnum.SRV_TCPFLG_UNICODE) ==
                    ED2KServerTcpFlagsEnum.SRV_TCPFLG_UNICODE;
            }
        }

        public bool RelatedSearchSupport
        {
            get
            {
                return (TCPFlags & ED2KServerTcpFlagsEnum.SRV_TCPFLG_RELATEDSEARCH) ==
                    ED2KServerTcpFlagsEnum.SRV_TCPFLG_RELATEDSEARCH;
            }
        }

        public bool DoesSupportsLargeFilesTCP
        {
            get
            {
                return (TCPFlags & ED2KServerTcpFlagsEnum.SRV_TCPFLG_LARGEFILES) ==
                    ED2KServerTcpFlagsEnum.SRV_TCPFLG_LARGEFILES;
            }
        }

        public bool DoesSupportsLargeFilesUDP
        {
            get
            {
                return (UDPFlags & ED2KServerUdpFlagsEnum.SRV_UDPFLG_LARGEFILES) ==
                    ED2KServerUdpFlagsEnum.SRV_UDPFLG_LARGEFILES;
            }
        }

        public bool DoesSupportsObfuscationUDP
        {
            get
            {
                return (UDPFlags & ED2KServerUdpFlagsEnum.SRV_UDPFLG_UDPOBFUSCATION) ==
                    ED2KServerUdpFlagsEnum.SRV_UDPFLG_UDPOBFUSCATION;
            }
        }

        public bool DoesSupportsObfuscationTCP
        {
            get
            {
                return ObfuscationPortTCP != 0 &&
                    (DoesSupportsObfuscationUDP ||
                    DoesSupportsGetSourcesObfuscation);
            }
        }

        public bool DoesSupportsGetSourcesObfuscation
        {
            get
            {
                return (TCPFlags & ED2KServerTcpFlagsEnum.SRV_TCPFLG_TCPOBFUSCATION) ==
                    ED2KServerTcpFlagsEnum.SRV_TCPFLG_TCPOBFUSCATION;
            }
        }

        public bool IsEqual(ED2KServer pServer)
        {
            return Equals(pServer);
        }

        public override bool Equals(object obj)
        {
            if (obj is ED2KServer)
            {
                ED2KServer pServer = obj as ED2KServer;

                if (Port != pServer.Port)
                    return false;
                if (HasDynIP && pServer.HasDynIP)
                    return string.Compare(DynIP, pServer.DynIP, true) == 0;
                if (HasDynIP || pServer.HasDynIP)
                    return false;
                return (IP == pServer.IP);
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return string.Format("{0}|{1}|{2}|{3}",
                Port, HasDynIP, DynIP, IP).GetHashCode();
        }
        #endregion
    }
}
