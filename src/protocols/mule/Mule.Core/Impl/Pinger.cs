using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core.Impl
{
    class PingerConstants
    {
        public const uint DEFAULT_TTL = 64;

        public const uint IP_STATUS_BASE = 11000;
        public const uint IP_SUCCESS = 0;
        public const uint IP_BUF_TOO_SMALL = (IP_STATUS_BASE + 1);
        public const uint IP_DEST_NET_UNREACHABLE = (IP_STATUS_BASE + 2);
        public const uint IP_DEST_HOST_UNREACHABLE = (IP_STATUS_BASE + 3);
        public const uint IP_DEST_PROT_UNREACHABLE = (IP_STATUS_BASE + 4);
        public const uint IP_DEST_PORT_UNREACHABLE = (IP_STATUS_BASE + 5);
        public const uint IP_NO_RESOURCES = (IP_STATUS_BASE + 6);
        public const uint IP_BAD_OPTION = (IP_STATUS_BASE + 7);
        public const uint IP_HW_ERROR = (IP_STATUS_BASE + 8);
        public const uint IP_PACKET_TOO_BIG = (IP_STATUS_BASE + 9);
        public const uint IP_REQ_TIMED_OUT = (IP_STATUS_BASE + 10);
        public const uint IP_BAD_REQ = (IP_STATUS_BASE + 11);
        public const uint IP_BAD_ROUTE = (IP_STATUS_BASE + 12);
        public const uint IP_TTL_EXPIRED_TRANSIT = (IP_STATUS_BASE + 13);
        public const uint IP_TTL_EXPIRED_REASSEM = (IP_STATUS_BASE + 14);
        public const uint IP_PARAM_PROBLEM = (IP_STATUS_BASE + 15);
        public const uint IP_SOURCE_QUENCH = (IP_STATUS_BASE + 16);
        public const uint IP_OPTION_TOO_BIG = (IP_STATUS_BASE + 17);
        public const uint IP_BAD_DESTINATION = (IP_STATUS_BASE + 18);
        public const uint IP_ADDR_DELETED = (IP_STATUS_BASE + 19);
        public const uint IP_SPEC_MTU_CHANGE = (IP_STATUS_BASE + 20);
        public const uint IP_MTU_CHANGE = (IP_STATUS_BASE + 21);
        public const uint IP_UNLOAD = (IP_STATUS_BASE + 22);
        public const uint IP_GENERAL_FAILURE = (IP_STATUS_BASE + 50);
        public const uint MAX_IP_STATUS = IP_GENERAL_FAILURE;
        public const uint IP_PENDING = (IP_STATUS_BASE + 255);
    }

    struct PingStatus
    {
        public PingStatus(int dummy)
        {
            success = false;
            status = 0;
            delay = 0;
            destinationAddress = 0;
            ttl = 0;
            error = 0;
        }

        public bool success;
        public uint status;
        public float delay;
        public uint destinationAddress;
        public uint ttl;

        public uint error;
    };

    class Pinger
    {
        internal PingStatus Ping(uint curAddress, uint ttl, bool p, bool useUdp)
        {
            throw new NotImplementedException();
        }

        internal void PIcmpErr(uint p)
        {
            throw new NotImplementedException();
        }
    }
}
