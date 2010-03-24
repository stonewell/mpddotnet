using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;
using System.Net;

namespace Mule.Core.Impl
{
    class PingerConstants
    {
        public const uint DEFAULT_TTL = 64;

        public const IPStatus IP_SUCCESS = (uint)IPStatus.Success;
        public const IPStatus IP_DEST_NET_UNREACHABLE = IPStatus.DestinationNetworkUnreachable;
        public const IPStatus IP_DEST_HOST_UNREACHABLE = IPStatus.DestinationHostUnreachable;
        public const IPStatus IP_DEST_PROT_UNREACHABLE = IPStatus.DestinationProtocolUnreachable;
        public const IPStatus IP_DEST_PORT_UNREACHABLE = IPStatus.DestinationPortUnreachable;
        public const IPStatus IP_NO_RESOURCES = IPStatus.NoResources;
        public const IPStatus IP_BAD_OPTION = IPStatus.BadOption;
        public const IPStatus IP_HW_ERROR = IPStatus.HardwareError;
        public const IPStatus IP_PACKET_TOO_BIG = IPStatus.PacketTooBig;
        public const IPStatus IP_REQ_TIMED_OUT = IPStatus.TimedOut;
        public const IPStatus IP_BAD_ROUTE = IPStatus.BadRoute;
        public const IPStatus IP_TTL_EXPIRED_TRANSIT = IPStatus.TtlExpired;
        public const IPStatus IP_TTL_EXPIRED_REASSEM = IPStatus.TtlReassemblyTimeExceeded;
        public const IPStatus IP_PARAM_PROBLEM = IPStatus.ParameterProblem;
        public const IPStatus IP_SOURCE_QUENCH = IPStatus.SourceQuench;
        public const IPStatus IP_BAD_DESTINATION = IPStatus.BadDestination;
    }

    struct PingStatus
    {
        public PingStatus(int dummy)
        {
            success = false;
            status =  IPStatus.Success;
            delay = 0;
            destinationAddress = 0;
            ttl = 0;
            error = IPStatus.Success;
        }

        public bool success;
        public IPStatus status;
        public float delay;
        public uint destinationAddress;
        public uint ttl;

        public IPStatus error;
    };

    class Pinger
    {
        public PingStatus Ping(uint curAddress)
        {
            return Ping(curAddress, PingerConstants.DEFAULT_TTL, false, false);
        }

        public PingStatus Ping(uint curAddress, uint ttl)
        {
            return Ping(curAddress, ttl, false, false);
        }

        public PingStatus Ping(uint curAddress, uint ttl, bool doLog)
        {
            return Ping(curAddress, ttl, doLog, false);
        }

        public PingStatus Ping(uint curAddress, uint ttl, bool doLog, bool useUdp)
        {
            Ping p = new Ping();
            PingOptions po = new PingOptions((int)ttl, true);
            
            int timeout = 3000;
            byte[] buffer = Encoding.Default.GetBytes("abcd");

            PingReply pr =
                p.Send(new IPAddress((long)curAddress), timeout, buffer, po);

            PingStatus ps = new PingStatus();

            ps.success = pr.Status == IPStatus.Success;
            ps.error = pr.Status;
            ps.status = pr.Status;
            ps.ttl = ttl;
            ps.destinationAddress = curAddress;
            ps.delay = pr.RoundtripTime;

            return ps;
        }

        public void PIcmpErr(IPStatus nIcmpErr)
        {
        }
    }
}
