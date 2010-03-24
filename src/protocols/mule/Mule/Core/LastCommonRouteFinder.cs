using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.ED2K;

namespace Mule.Core
{
    public struct CurrentPing
    {
        //uint	datalen;
        public string state;
        public uint latency;
        public uint lowest;
        public uint currentLimit;
    };

    public interface LastCommonRouteFinder
    {
        bool AddHostToCheck(uint ip);
	    bool AddHostsToCheck(IList<ED2KServer> list);
	    bool AddHostsToCheck(UpDownClientList list);

        CurrentPing CurrentPing { get; }
        bool IsAcceptNewClient { get; }

        void SetPrefs(bool pEnabled, uint pCurUpload, 
            uint pMinUpload, uint pMaxUpload, 
            bool pUseMillisecondPingTolerance, double pPingTolerance,
            uint pPingToleranceMilliseconds, uint pGoingUpDivider, 
            uint pGoingDownDivider, 
            uint pNumberOfPingsForAverage, 
            ulong pLowestInitialPingAllowed);
        void InitiateFastReactionPeriod();

        uint Upload { get; }

        void StopFinder();
    }
}
