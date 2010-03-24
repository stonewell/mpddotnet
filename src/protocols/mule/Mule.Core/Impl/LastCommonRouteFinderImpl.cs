using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Utilities;
using System.Threading;
using Mule.ED2K;

namespace Mule.Core.Impl
{
    class LastCommonRouteFinderImpl : LastCommonRouteFinder
    {
        #region Fields
        private bool doRun_;
        private bool acceptNewClient_;
        private bool enabled_;
        private bool needMoreHosts_;

        private object addHostLocker_ = new object();
        private object prefsLocker_ = new object();
        private object uploadLocker_ = new object();
        private object pingLocker_ = new object();

        private EventWaitHandle threadEndedEvent_;
        private EventWaitHandle newTraceRouteHostEvent_;
        private EventWaitHandle prefsEvent_;

        private Dictionary<uint, uint> hostsToTraceRoute_ = new Dictionary<uint, uint>();

        private List<uint> pingDelays_ = new List<uint>();

        private ulong pingDelaysTotal_;

        private uint minUpload_;
        private uint maxUpload_;
        private uint curUpload_;
        private uint upload_;

        private double pingTolerance_;
        private uint pingToleranceMilliseconds_;
        private bool useMillisecondPingTolerance_;
        private uint goingUpDivider_;
        private uint goingDownDivider_;
        private uint numberOfPingsForAverage_;

        private uint pingAverage_;
        private uint lowestPing_;
        private ulong lowestInitialPingAllowed_;

        private bool initiateFastReactionPeriod_;

        private string state_;
        #endregion

        #region Constructors
        public LastCommonRouteFinderImpl()
        {
            minUpload_ = 1;
            maxUpload_ = uint.MaxValue;
            upload_ = uint.MaxValue;
            curUpload_ = 1;

            pingToleranceMilliseconds_ = 200;
            useMillisecondPingTolerance_ = false;
            numberOfPingsForAverage_ = 0;
            pingAverage_ = 0;
            lowestPing_ = 0;
            lowestInitialPingAllowed_ = 20;
            pingDelaysTotal_ = 0;

            state_ = "";

            needMoreHosts_ = false;

            threadEndedEvent_ = new ManualResetEvent(false);
            newTraceRouteHostEvent_ = new AutoResetEvent(false);
            prefsEvent_ = new AutoResetEvent(false);

            initiateFastReactionPeriod_ = false;

            enabled_ = false;
            doRun_ = true;

            new Thread(new ParameterizedThreadStart(RunThread)).Start();
        }
        #endregion

        #region LastCommonRouteFinder Members
        public bool AddHostToCheck(uint ip)
        {
            bool gotEnoughHosts = true;

            if (needMoreHosts_ && MpdUtilities.IsGoodIP(ip, true))
            {
                lock (addHostLocker_)
                {

                    if (needMoreHosts_)
                    {
                        gotEnoughHosts = AddHostToCheckNoLock(ip);
                    }
                }
            }

            return gotEnoughHosts;
        }

        public bool AddHostsToCheck(IList<Mule.ED2K.ED2KServer> list)
        {
            bool gotEnoughHosts = true;

            if (needMoreHosts_)
            {
                lock (addHostLocker_)
                {
                    if (needMoreHosts_)
                    {
                        if (list.Count > 0)
                        {
                            Random rand = new Random();
                            uint startPos = (uint)(rand.Next() /
                                (MuleConstants.RAND_MAX / (Math.Min(list.Count, 100))));

                            uint tryCount = 0;
                            while (needMoreHosts_ && tryCount < (uint)list.Count)
                            {
                                tryCount++;
                                startPos %= (uint)list.Count;

                                ED2KServer server = list[(int)startPos];

                                startPos++;

                                uint ip = server.IP;

                                AddHostToCheckNoLock(ip);
                            }
                        }
                    }

                    gotEnoughHosts = !needMoreHosts_;
                }
            }

            return gotEnoughHosts;
        }

        public bool AddHostsToCheck(UpDownClientList list)
        {
            bool gotEnoughHosts = true;

            if (needMoreHosts_)
            {
                lock (addHostLocker_)
                {

                    if (needMoreHosts_)
                    {
                        if (list.Count > 0)
                        {
                            Random rand = new Random();
                            uint startPos = (uint)(rand.Next() /
                                (MuleConstants.RAND_MAX / (Math.Min(list.Count, 100))));

                            uint tryCount = 0;
                            while (needMoreHosts_ && tryCount < (uint)list.Count)
                            {
                                tryCount++;

                                startPos %= (uint)list.Count;

                                UpDownClient client = list[(int)startPos];

                                startPos++;

                                uint ip = client.IP;

                                AddHostToCheckNoLock(ip);
                            }
                        }
                    }

                    gotEnoughHosts = !needMoreHosts_;

                }
            }

            return gotEnoughHosts;
        }

        public CurrentPing CurrentPing
        {
            get
            {
                CurrentPing returnVal;

                if (enabled_)
                {
                    lock (pingLocker_)
                    {
                        returnVal.state = state_;
                        returnVal.latency = pingAverage_;
                        returnVal.lowest = lowestPing_;
                        returnVal.currentLimit = upload_;
                    }
                }
                else
                {
                    returnVal.state = string.Empty;
                    returnVal.latency = 0;
                    returnVal.lowest = 0;
                    returnVal.currentLimit = 0;
                }

                return returnVal;
            }
        }
        public bool IsAcceptNewClient
        {
            get
            {
                return acceptNewClient_ || !enabled_;
            }
        }

        public void SetPrefs(bool pEnabled, uint pCurUpload,
            uint pMinUpload, uint pMaxUpload,
            bool pUseMillisecondPingTolerance, double pPingTolerance,
            uint pPingToleranceMilliseconds, uint pGoingUpDivider,
            uint pGoingDownDivider,
            uint pNumberOfPingsForAverage,
            ulong pLowestInitialPingAllowed)
        {
            bool sendEvent = false;

            lock (prefsLocker_)
            {
                if (pMinUpload <= 1024)
                {
                    minUpload_ = 1024;
                }
                else
                {
                    minUpload_ = pMinUpload;
                }

                if (pMaxUpload != 0)
                {
                    maxUpload_ = pMaxUpload;
                    if (maxUpload_ < minUpload_)
                    {
                        minUpload_ = maxUpload_;
                    }
                }
                else
                {
                    maxUpload_ = pCurUpload + 10 * 1024; //uint.MaxValue;
                }

                if (pEnabled && enabled_ == false)
                {
                    sendEvent = true;
                }
                else if (pEnabled == false)
                {
                    //prefsEvent.ResetEvent();
                    sendEvent = true;
                }

                enabled_ = pEnabled;
                useMillisecondPingTolerance_ = pUseMillisecondPingTolerance;
                pingTolerance_ = pPingTolerance;
                pingToleranceMilliseconds_ = pPingToleranceMilliseconds;
                goingUpDivider_ = pGoingUpDivider;
                goingDownDivider_ = pGoingDownDivider;
                curUpload_ = pCurUpload;
                numberOfPingsForAverage_ = pNumberOfPingsForAverage;
                lowestInitialPingAllowed_ = pLowestInitialPingAllowed;

                lock (uploadLocker_)
                {

                    if (upload_ > maxUpload_ || pEnabled == false)
                    {
                        upload_ = maxUpload_;
                    }

                }// uploadLocker.Unlock();
            }//prefsLocker.Unlock;

            if (sendEvent)
            {
                prefsEvent_.Set();
            }
        }

        public void InitiateFastReactionPeriod()
        {
            lock (prefsLocker_)
            {

                initiateFastReactionPeriod_ = true;

            }// prefsLocker.Unlock();
        }

        public uint Upload
        {
            get
            {
                uint returnValue;

                lock (uploadLocker_)
                {

                    returnValue = upload_;

                }// uploadLocker.Unlock();

                return returnValue;
            }

            private set
            {
                lock (uploadLocker_)
                {

                    upload_ = value;

                }// uploadLocker.Unlock();
            }
        }

        public void StopFinder()
        {
            // signal the thread to stop looping and exit.
            doRun_ = false;

            prefsEvent_.Set();
            newTraceRouteHostEvent_.Set();

            // wait for the thread to signal that it has stopped looping.
            threadEndedEvent_.WaitOne();
        }
        #endregion

        #region Privates
        private bool AddHostToCheckNoLock(uint ip)
        {
            if (needMoreHosts_ && MpdUtilities.IsGoodIP(ip, true))
            {
                //hostsToTraceRoute.AddTail(ip);
                hostsToTraceRoute_[ip] = 0;

                if (hostsToTraceRoute_.Count >= 10)
                {
                    needMoreHosts_ = false;

                    // Signal that there's hosts to fetch.
                    newTraceRouteHostEvent_.Set();
                }
            }

            return !needMoreHosts_;
        }

        private uint Median(List<uint> list)
        {
            int size = list.Count;

            if (size == 1)
            {
                return list[0];
            }
            else if (size == 2)
            {
                return (list[0] + list[1]) / 2;
            }
            else if (size > 2)
            {
                // if more than 2 elements, we need to sort them to find the middle.
                uint[] arr = new uint[size];

                uint counter = 0;
                foreach (uint u in list)
                {
                    arr[counter] = u;
                    counter++;
                }

                Array.Sort(arr);

                double returnVal;

                if (size % 2 > 0)
                    returnVal = arr[size / 2];
                else
                    returnVal = (arr[size / 2 - 1] + arr[size / 2]) / 2;

                return (uint)returnVal;
            }
            else
            {
                // Undefined! Shouldn't be called with no elements in list.
                return 0;
            }
        }

        private void RunThread(object arg)
        {
            try
            {
                Pinger pinger = new Pinger();
                bool hasSucceededAtLeastOnce = false;

                while (doRun_)
                {
                    // wait for updated prefs
                    prefsEvent_.WaitOne();

                    bool enabled = enabled_;

                    // retry loop. enabled will be set to false in end of this loop, if to many failures (tries too large)
                    while (doRun_ && enabled)
                    {
                        bool foundLastCommonHost = false;
                        uint lastCommonHost = 0;
                        uint lastCommonTTL = 0;
                        uint hostToPing = 0;
                        bool useUdp = false;

                        hostsToTraceRoute_.Clear();

                        pingDelays_.Clear();
                        pingDelaysTotal_ = 0;

                        lock (pingLocker_)
                        {
                            pingAverage_ = 0;
                            lowestPing_ = 0;
                            state_ = "";//TODO:GetResString(IDS_USS_STATE_PREPARING);
                        }//pingLocker.Unlock();

                        // Calculate a good starting value for the upload control. If the user has entered a max upload value, we use that. Otherwise 10 KBytes/s
                        int startUpload = (int)((maxUpload_ != uint.MaxValue) ? maxUpload_ : 10 * 1024);

                        //bool atLeastOnePingSucceded = false;
                        while (doRun_ && enabled && foundLastCommonHost == false)
                        {
                            uint traceRouteTries = 0;
                            while (doRun_ && enabled && foundLastCommonHost == false &&
                                (traceRouteTries < 5 || hasSucceededAtLeastOnce &&
                                traceRouteTries < uint.MaxValue) &&
                                (hostsToTraceRoute_.Count < 10 ||
                                hasSucceededAtLeastOnce))
                            {
                                traceRouteTries++;

                                lastCommonHost = 0;

                                MpdUtilities.QueueDebugLogLine(false, string.Format("UploadSpeedSense: Try #{0}. Collecting hosts...", traceRouteTries));

                                lock (addHostLocker_)
                                {
                                    needMoreHosts_ = true;
                                }//addHostLocker.Unlock();

                                // wait for hosts to traceroute
                                newTraceRouteHostEvent_.WaitOne();

                                MpdUtilities.QueueDebugLogLine(false,
                                    "UploadSpeedSense: Got enough hosts. Listing the hosts that will be tracerouted:");

                                Dictionary<uint, uint>.Enumerator pos = hostsToTraceRoute_.GetEnumerator();
                                int counter = 0;
                                while (pos.MoveNext())
                                {
                                    counter++;
                                    uint hostToTraceRoute, dummy;

                                    hostToTraceRoute = pos.Current.Key;
                                    dummy = pos.Current.Value;

                                    MpdUtilities.QueueDebugLogLine(false, string.Format("UploadSpeedSense: Host #{0}: {1}",
                                        counter, MpdUtilities.IP2String(hostToTraceRoute)));
                                }

                                // find the last common host, using traceroute
                                MpdUtilities.QueueDebugLogLine(false, "UploadSpeedSense: Starting traceroutes to find last common host.");

                                // for the tracerouting phase (preparing...) we need to disable uploads so we get a faster traceroute and better ping values.
                                Upload = 2 * 1024;
                                Thread.Sleep((int)MuleConstants.ONE_SEC_MS);

                                if (enabled_ == false)
                                {
                                    enabled = false;
                                }

                                bool failed = false;

                                uint curHost = 0;
                                for (uint ttl = 1; doRun_ && enabled &&
                                    (curHost != 0 && ttl <= 64 || curHost == 0 && ttl < 5) &&
                                    foundLastCommonHost == false && failed == false; ttl++)
                                {
                                    MpdUtilities.QueueDebugLogLine(false,
                                        string.Format("UploadSpeedSense: Pinging for TTL {0}...", ttl));

                                    useUdp = false; // PENDING: Get default value from prefs?

                                    curHost = 0;
                                    if (enabled_ == false)
                                    {
                                        enabled = false;
                                    }

                                    uint lastSuccedingPingAddress = 0;
                                    uint lastDestinationAddress = 0;
                                    uint hostsToTraceRouteCounter = 0;
                                    bool failedThisTtl = false;
                                    pos = hostsToTraceRoute_.GetEnumerator();
                                    while (doRun_ && enabled && failed == false &&
                                        failedThisTtl == false && pos.MoveNext() &&
                                          (lastDestinationAddress == 0 || lastDestinationAddress == curHost)) // || pingStatus.success == false && pingStatus.error == PingerConstants.IP_REQIMED_OUT ))
                                    {
                                        PingStatus pingStatus = new PingStatus(0);

                                        hostsToTraceRouteCounter++;

                                        // this is the current address we send ping to, in loop below.
                                        // PENDING: Don't confuse this with curHost, which is unfortunately almost
                                        // the same name. Will rename one of these variables as soon as possible, to
                                        // get more different names.
                                        uint curAddress = pos.Current.Key;

                                        pingStatus.success = false;
                                        for (counter = 0;
                                            doRun_ && enabled && counter < 2 &&
                                            (pingStatus.success == false ||
                                            pingStatus.success == true &&
                                            pingStatus.status != PingerConstants.IP_SUCCESS &&
                                            pingStatus.status != PingerConstants.IP_TTL_EXPIRED_TRANSIT); counter++)
                                        {
                                            pingStatus = pinger.Ping(curAddress, ttl, true, useUdp);
                                            if (doRun_ && enabled &&
                                               (
                                                pingStatus.success == false ||
                                                pingStatus.success == true &&
                                                pingStatus.status != PingerConstants.IP_SUCCESS &&
                                                pingStatus.status != PingerConstants.IP_TTL_EXPIRED_TRANSIT
                                               ) &&
                                               counter < 3 - 1)
                                            {
                                                MpdUtilities.QueueDebugLogLine(false,
                                                    string.Format("UploadSpeedSense: Failure #{0} to ping host! " +
                                                        "(TTL: {1} IP: {2} error: {3}). Sleeping 1 sec before retry. " +
                                                        "Error info follows.", counter + 1, ttl,
                                                        MpdUtilities.IP2String(curAddress),
                                                        (pingStatus.success) ? pingStatus.status : pingStatus.error));
                                                pinger.PIcmpErr(
                                                    (pingStatus.success) ? pingStatus.status : pingStatus.error);

                                                Thread.Sleep(1000);

                                                if (enabled_ == false)
                                                    enabled = false;

                                                // trying other ping method
                                                useUdp = !useUdp;
                                            }
                                        }

                                        if (pingStatus.success == true &&
                                            pingStatus.status == PingerConstants.IP_TTL_EXPIRED_TRANSIT)
                                        {
                                            if (curHost == 0)
                                                curHost = pingStatus.destinationAddress;
                                            //atLeastOnePingSucceded = true;
                                            lastSuccedingPingAddress = curAddress;
                                            lastDestinationAddress = pingStatus.destinationAddress;
                                        }
                                        else
                                        {
                                            // failed to ping this host for some reason.
                                            // Or we reached the actual host we are pinging. We don't want that, since it is too close.
                                            // Remove it.
                                            if (pingStatus.success == true && pingStatus.status == PingerConstants.IP_SUCCESS)
                                            {
                                                MpdUtilities.QueueDebugLogLine(false,
                                                    string.Format("UploadSpeedSense: Host was too close! " +
                                                "Removing this host. (TTL: {0} IP: {1} status: {2}). " +
                                                "Removing this host and restarting host collection.", ttl,
                                                MpdUtilities.IP2String(curAddress), pingStatus.status));

                                                hostsToTraceRoute_.Remove(curAddress);
                                            }
                                            else if (pingStatus.success == true &&
                                                pingStatus.status == PingerConstants.IP_DEST_HOST_UNREACHABLE)
                                            {
                                                MpdUtilities.QueueDebugLogLine(false,
                                                    string.Format("UploadSpeedSense: Host unreacheable! " +
                                                    "(TTL: {0} IP: {1} status: {2}). " +
                                                    "Removing this host. Status info follows.", ttl,
                                                    MpdUtilities.IP2String(curAddress), pingStatus.status));
                                                pinger.PIcmpErr(pingStatus.status);

                                                hostsToTraceRoute_.Remove(curAddress);
                                            }
                                            else if (pingStatus.success == true)
                                            {
                                                MpdUtilities.QueueDebugLogLine(false,
                                                    string.Format("UploadSpeedSense: Unknown ping status! " +
                                                    "(TTL: {0} IP: {1} status: {2}). Reason follows. " +
                                                    "Changing ping method to see if it helps.", ttl,
                                                    MpdUtilities.IP2String(curAddress), pingStatus.status));
                                                pinger.PIcmpErr(pingStatus.status);
                                                useUdp = !useUdp;
                                            }
                                            else
                                            {
                                                if (pingStatus.error == PingerConstants.IP_REQ_TIMED_OUT)
                                                {
                                                    MpdUtilities.QueueDebugLogLine(false,
                                                        string.Format("UploadSpeedSense: Timeout when pinging a host! " +
                                                        "(TTL: {0} IP: {1} Error: {2}). " +
                                                        "Keeping host. Error info follows.", ttl,
                                                        MpdUtilities.IP2String(curAddress), pingStatus.error));
                                                    pinger.PIcmpErr(pingStatus.error);

                                                    if (hostsToTraceRouteCounter > 2 && lastSuccedingPingAddress == 0)
                                                    {
                                                        // several pings have timed out on this ttl. Probably we can't ping on this ttl at all
                                                        failedThisTtl = true;
                                                    }
                                                }
                                                else
                                                {
                                                    MpdUtilities.QueueDebugLogLine(false,
                                                        string.Format("UploadSpeedSense: Unknown pinging error! " +
                                                        "(TTL: {0} IP: {1} status: {2}). " +
                                                        "Reason follows. Changing ping method to see if it helps.",
                                                        ttl, MpdUtilities.IP2String(curAddress), pingStatus.error));
                                                    pinger.PIcmpErr(pingStatus.error);
                                                    useUdp = !useUdp;
                                                }
                                            }

                                            if (hostsToTraceRoute_.Count <= 8)
                                            {
                                                MpdUtilities.QueueDebugLogLine(false,
                                                    "UploadSpeedSense: To few hosts to traceroute left." +
                                                    " Restarting host colletion.");
                                                failed = true;
                                            }
                                        }
                                    }

                                    if (failed == false)
                                    {
                                        if (curHost != 0 && lastDestinationAddress != 0)
                                        {
                                            if (lastDestinationAddress == curHost)
                                            {
                                                MpdUtilities.QueueDebugLogLine(false,
                                                    string.Format("UploadSpeedSense: Host at TTL {0}: {1}"),
                                                    ttl, MpdUtilities.IP2String(curHost));

                                                lastCommonHost = curHost;
                                                lastCommonTTL = ttl;
                                            }
                                            else /*if(lastSuccedingPingAddress != 0)*/
                                            {
                                                foundLastCommonHost = true;
                                                hostToPing = lastSuccedingPingAddress;

                                                string hostToPingString = MpdUtilities.IP2String(hostToPing);

                                                if (lastCommonHost != 0)
                                                {
                                                    MpdUtilities.QueueDebugLogLine(false, ("UploadSpeedSense: Found differing host at TTL %i: %s. This will be the host to ping."), ttl, hostToPingString);
                                                }
                                                else
                                                {
                                                    string lastCommonHostString = MpdUtilities.IP2String(lastDestinationAddress);

                                                    lastCommonHost = lastDestinationAddress;
                                                    lastCommonTTL = ttl;
                                                    MpdUtilities.QueueDebugLogLine(false,
                                                        string.Format("UploadSpeedSense: Found differing host at " +
                                                        "TTL {0}, but last ttl couldn't be pinged so we don't " +
                                                        "know last common host. Taking a chance and using first " +
                                                    "differing ip as last commonhost. Host to ping: {1}." +
                                                    " Faked LastCommonHost: {2}",
                                                    ttl, hostToPingString, lastCommonHostString));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (ttl < 4)
                                            {
                                                MpdUtilities.QueueDebugLogLine(false,
                                                    string.Format("UploadSpeedSense: Could perform no ping at all at" +
                                                    " TTL {0}. Trying next ttl.", ttl));
                                            }
                                            else
                                            {
                                                MpdUtilities.QueueDebugLogLine(false,
                                                    string.Format("UploadSpeedSense: Could perform no ping at all at " +
                                                    "TTL {0}. Giving up.", ttl));
                                            }
                                            lastCommonHost = 0;
                                        }
                                    }
                                }

                                if (foundLastCommonHost == false && traceRouteTries >= 3)
                                {
                                    MpdUtilities.QueueDebugLogLine(false,
                                        "UploadSpeedSense: Tracerouting failed several times. Waiting a few minutes before trying again.");

                                    Upload = maxUpload_;

                                    lock (pingLocker_)
                                    {
                                        state_ = "";//TODO:GetResString(IDS_USS_STATE_WAITING);
                                    }//pingLocker.Unlock();

                                    prefsEvent_.WaitOne(3 * 60 * 1000);

                                    lock (pingLocker_)
                                    {
                                        state_ = "";//TODO:GetResString(IDS_USS_STATE_PREPARING);
                                    }
                                }

                                if (enabled_ == false)
                                {
                                    enabled = false;
                                }
                            }

                            if (enabled)
                            {
                                MpdUtilities.QueueDebugLogLine(false,
                                    "UploadSpeedSense: Done tracerouting. Evaluating results.");

                                if (foundLastCommonHost == true)
                                {
                                    // log result
                                    MpdUtilities.QueueDebugLogLine(false,
                                        string.Format("UploadSpeedSense: Found last common host. LastCommonHost:" +
                                     " {0} @ TTL: {1}"),
                                     MpdUtilities.IP2String(lastCommonHost), lastCommonTTL);

                                    MpdUtilities.QueueDebugLogLine(false,
                                        string.Format("UploadSpeedSense: Found last common host. HostToPing:{0}"),
                                        MpdUtilities.IP2String(hostToPing));
                                }
                                else
                                {
                                    MpdUtilities.QueueDebugLogLine(false, "IDS_USSRACEROUTEOFTENFAILED");
                                    MpdUtilities.QueueLogLine(true, "IDS_USSRACEROUTEOFTENFAILED");
                                    enabled = false;

                                    lock (pingLocker_)
                                    {
                                        state_ = "GetResString(IDS_USS_STATE_ERROR)";
                                    }//pingLocker.Unlock();

                                    // PENDING: this may not be thread safe
                                    MuleApplication.Instance.Preference.IsDynamicUploadEnabled = false;
                                }
                            }
                        }

                        if (enabled_ == false)
                        {
                            enabled = false;
                        }

                        if (doRun_ && enabled)
                        {
                            MpdUtilities.QueueDebugLogLine(false,
                                "UploadSpeedSense: Finding a start value for lowest ping...");
                        }

                        // PENDING:
                        ulong lowestInitialPingAllowed = 0;
                        lock (prefsLocker_)
                        {
                            lowestInitialPingAllowed = lowestInitialPingAllowed_;
                        }//prefsLocker.Unlock();

                        uint initial_ping = int.MaxValue;

                        bool foundWorkingPingMethod = false;
                        // finding lowest ping
                        for (int initialPingCounter = 0;
                            doRun_ && enabled && initialPingCounter < 10; initialPingCounter++)
                        {
                            Thread.Sleep(200);

                            PingStatus pingStatus = pinger.Ping(hostToPing, lastCommonTTL, true, useUdp);

                            if (pingStatus.success && pingStatus.status == PingerConstants.IP_TTL_EXPIRED_TRANSIT)
                            {
                                foundWorkingPingMethod = true;

                                if (pingStatus.delay > 0 && pingStatus.delay < initial_ping)
                                {
                                    initial_ping = (uint)Math.Max(pingStatus.delay, lowestInitialPingAllowed);
                                }
                            }
                            else
                            {
                                MpdUtilities.QueueDebugLogLine(false,
                                    string.Format("UploadSpeedSense: {0}-Ping #{1} failed. Reason follows"),
                                    useUdp ? ("UDP") : ("ICMP"), initialPingCounter);
                                pinger.PIcmpErr(pingStatus.error);

                                // trying other ping method
                                if (!pingStatus.success && !foundWorkingPingMethod)
                                {
                                    useUdp = !useUdp;
                                }
                            }

                            if (enabled_ == false)
                            {
                                enabled = false;
                            }
                        }

                        // Set the upload to a good starting point
                        Upload = (uint)startUpload;
                        Thread.Sleep((int)MuleConstants.ONE_SEC_MS);
                        uint initTime = MpdUtilities.GetTickCount();

                        // if all pings returned 0, initial_ping will not have been changed from default value.
                        // then set initial_ping to lowestInitialPingAllowed
                        if (initial_ping == int.MaxValue)
                            initial_ping = (uint)lowestInitialPingAllowed;

                        uint upload = 0;

                        hasSucceededAtLeastOnce = true;

                        if (doRun_ && enabled)
                        {
                            if (initial_ping > lowestInitialPingAllowed)
                            {
                                MpdUtilities.QueueDebugLogLine(false,
                                    string.Format("UploadSpeedSense: Lowest ping: {0} ms"), initial_ping);
                            }
                            else
                            {
                                MpdUtilities.QueueDebugLogLine(false,
                                    string.Format("UploadSpeedSense: Lowest ping: {0} ms. " +
                                    "(Filtered lower values. Lowest ping is never allowed to go under {1} ms)"),
                                    initial_ping, lowestInitialPingAllowed);
                            }
                            lock (prefsLocker_)
                            {
                                upload = curUpload_;

                                if (upload < minUpload_)
                                {
                                    upload = minUpload_;
                                }
                                if (upload > maxUpload_)
                                {
                                    upload = maxUpload_;
                                }
                            }//prefsLocker.Unlock();
                        }

                        if (enabled_ == false)
                        {
                            enabled = false;
                        }

                        if (doRun_ && enabled)
                        {
                            MpdUtilities.QueueDebugLogLine(false, "GetResString(IDS_USS_STARTING)");
                            MpdUtilities.QueueLogLine(true, "GetResString(IDS_USS_STARTING)  ");
                        }

                        lock (pingLocker_)
                        {
                            state_ = ("");
                        }//pingLocker.Unlock();

                        // There may be several reasons to start over with tracerouting again.
                        // Currently we only restart if we get an unexpected ip back from the
                        // ping at the set TTL.
                        bool restart = false;

                        uint lastLoopTick = MpdUtilities.GetTickCount();
                        uint lastUploadReset = 0;

                        while (doRun_ && enabled && restart == false)
                        {
                            uint ticksBetweenPings = 1000;
                            if (upload > 0)
                            {
                                // ping packages being 64 bytes, this should use 1% of bandwidth (one hundredth of bw).
                                ticksBetweenPings = (64 * 100 * 1000) / upload;

                                if (ticksBetweenPings < 125)
                                {
                                    // never ping more than 8 packages a second
                                    ticksBetweenPings = 125;
                                }
                                else if (ticksBetweenPings > 1000)
                                {
                                    ticksBetweenPings = 1000;
                                }
                            }

                            uint curTick = MpdUtilities.GetTickCount();

                            uint timeSinceLastLoop = curTick - lastLoopTick;
                            if (timeSinceLastLoop < ticksBetweenPings)
                            {
                                //MpdUtilities.QueueDebugLogLine(false,("UploadSpeedSense: Sleeping %i ms, timeSinceLastLoop %i ms ticksBetweenPings %i ms"), ticksBetweenPings-timeSinceLastLoop, timeSinceLastLoop, ticksBetweenPings);
                                Thread.Sleep((int)(ticksBetweenPings - timeSinceLastLoop));
                            }

                            lastLoopTick = curTick;

                            double pingTolerance;
                            uint pingToleranceMilliseconds;
                            bool useMillisecondPingTolerance;
                            uint goingUpDivider;
                            uint goingDownDivider;
                            uint numberOfPingsForAverage;
                            uint curUpload;
                            bool initiateFastReactionPeriod;

                            lock (prefsLocker_)
                            {
                                pingTolerance = pingTolerance_;
                                pingToleranceMilliseconds = pingToleranceMilliseconds_;
                                useMillisecondPingTolerance = useMillisecondPingTolerance_;
                                goingUpDivider = goingUpDivider_;
                                goingDownDivider = goingDownDivider_;
                                numberOfPingsForAverage = numberOfPingsForAverage_;
                                lowestInitialPingAllowed = lowestInitialPingAllowed_; // PENDING
                                curUpload = curUpload_;

                                initiateFastReactionPeriod = initiateFastReactionPeriod_;
                                initiateFastReactionPeriod_ = false;
                            }//prefsLocker.Unlock();

                            if (!initiateFastReactionPeriod)
                            {
                                MpdUtilities.QueueDebugLogLine(false, "GetResString(IDS_USS_MANUALUPLOADLIMITDETECTED)");
                                MpdUtilities.QueueLogLine(true, "GetResString(IDS_USS_MANUALUPLOADLIMITDETECTED) ");

                                // the first 60 seconds will use hardcoded up/down slowness that is faster
                                initTime = MpdUtilities.GetTickCount();
                            }

                            uint tempTick = MpdUtilities.GetTickCount();

                            if (tempTick - initTime < MuleConstants.ONE_SEC_MS * 20)
                            {
                                goingUpDivider = 1;
                                goingDownDivider = 1;
                            }
                            else if (tempTick - initTime < MuleConstants.ONE_SEC_MS * (30))
                            {
                                goingUpDivider = (uint)(goingUpDivider * 0.25);
                                goingDownDivider = (uint)(goingDownDivider * 0.25);
                            }
                            else if (tempTick - initTime < MuleConstants.ONE_SEC_MS * (40))
                            {
                                goingUpDivider = (uint)(goingUpDivider * 0.5);
                                goingDownDivider = (uint)(goingDownDivider * 0.5);
                            }
                            else if (tempTick - initTime < MuleConstants.ONE_SEC_MS * (60))
                            {
                                goingUpDivider = (uint)(goingUpDivider * 0.75);
                                goingDownDivider = (uint)(goingDownDivider * 0.75);
                            }
                            else if (tempTick - initTime < MuleConstants.ONE_SEC_MS * (61))
                            {
                                lastUploadReset = tempTick;
                                lock (prefsLocker_)
                                {
                                    upload = curUpload_;
                                }//prefsLocker.Unlock();
                            }

                            goingDownDivider = Math.Max(goingDownDivider, 1);
                            goingUpDivider = Math.Max(goingUpDivider, 1);

                            uint soll_ping = (uint)(initial_ping * pingTolerance);
                            if (useMillisecondPingTolerance)
                            {
                                soll_ping = pingToleranceMilliseconds;
                            }
                            else
                            {
                                soll_ping = (uint)(initial_ping * pingTolerance);
                            }

                            uint raw_ping = soll_ping; // this value will cause the upload speed not to change at all.

                            bool pingFailure = false;
                            for (ulong pingTries = 0;
                                doRun_ && enabled && (pingTries == 0 || pingFailure)
                                && pingTries < 60; pingTries++)
                            {
                                if (enabled_ == false)
                                {
                                    enabled = false;
                                }

                                // ping the host to ping
                                PingStatus pingStatus = pinger.Ping(hostToPing, lastCommonTTL, false, useUdp);

                                if (pingStatus.success && pingStatus.status == PingerConstants.IP_TTL_EXPIRED_TRANSIT)
                                {
                                    if (pingStatus.destinationAddress != lastCommonHost)
                                    {
                                        // something has changed about the topology! We got another ip back from this ttl than expected.
                                        // Do the tracerouting again to figure out new topology
                                        string lastCommonHostAddressString = MpdUtilities.IP2String(lastCommonHost);
                                        string destinationAddressString = MpdUtilities.IP2String(pingStatus.destinationAddress);

                                        MpdUtilities.QueueDebugLogLine(false,
                                            string.Format("UploadSpeedSense: Network topology has changed." +
                                            " TTL: {0} Expected ip: {1} Got ip: {2} Will do a new traceroute.",
                                            lastCommonTTL, lastCommonHostAddressString, destinationAddressString));
                                        restart = true;
                                    }

                                    raw_ping = (uint)pingStatus.delay;

                                    if (pingFailure)
                                    {
                                        // only several pings in row should fails, the total doesn't count, so reset for each successful ping
                                        pingFailure = false;

                                        //MpdUtilities.QueueDebugLogLine(false,("UploadSpeedSense: Ping #%i successful. Continuing."), pingTries);
                                    }
                                }
                                else
                                {
                                    raw_ping = soll_ping * 3 + initial_ping * 3; // this value will cause the upload speed be lowered.

                                    pingFailure = true;

                                    if (enabled_ == false)
                                    {
                                        enabled = false;
                                    }
                                    else if (pingTries > 3)
                                    {
                                        prefsEvent_.WaitOne(1000);
                                    }

                                    //MpdUtilities.QueueDebugLogLine(false,("UploadSpeedSense: %s-Ping #%i failed. Reason follows"), useUdp?("UDP"):("ICMP"), pingTries);
                                    //pinger.PIcmpErr(pingStatus.error);
                                }

                                if (enabled_ == false)
                                {
                                    enabled = false;
                                }
                            }

                            if (pingFailure)
                            {
                                if (enabled)
                                {
                                    MpdUtilities.QueueDebugLogLine(false,
                                        "UploadSpeedSense: No response to pings for a long time. Restarting...");
                                }
                                restart = true;
                            }

                            if (restart == false)
                            {
                                if (raw_ping > 0 &&
                                    raw_ping < initial_ping && initial_ping > lowestInitialPingAllowed)
                                {
                                    MpdUtilities.QueueDebugLogLine(false,
                                        string.Format("UploadSpeedSense: New lowest ping: {0} ms. Old: {1} ms",
                                        Math.Max(raw_ping, lowestInitialPingAllowed), initial_ping));
                                    initial_ping = (uint)Math.Max(raw_ping, lowestInitialPingAllowed);
                                }

                                pingDelaysTotal_ += raw_ping;
                                pingDelays_.Add(raw_ping);
                                while (pingDelays_.Count != 0 && (uint)pingDelays_.Count > numberOfPingsForAverage)
                                {
                                    uint pingDelay = pingDelays_[0];
                                    pingDelays_.RemoveAt(0);
                                    pingDelaysTotal_ -= pingDelay;
                                }

                                uint pingAverage = Median(pingDelays_); //(pingDelaysTotal/pingDelays.Count);
                                int normalized_ping = (int)(pingAverage - initial_ping);

                                //{
                                //    prefsLocker.Lock();
                                //    uint tempCurUpload = curUpload_;
                                //    prefsLocker.Unlock();

                                //    MpdUtilities.QueueDebugLogLine(false, ("USS-Debug: %i %i %i"), raw_ping, upload, tempCurUpload);
                                //}

                                lock (pingLocker_)
                                {
                                    pingAverage_ = (uint)pingAverage;
                                    lowestPing_ = initial_ping;
                                }//pingLocker.Unlock();

                                // Calculate Waiting Time
                                long hping = ((int)soll_ping) - normalized_ping;

                                // Calculate change of upload speed
                                if (hping < 0)
                                {
                                    //Ping too high
                                    acceptNewClient_ = false;

                                    // lower the speed
                                    long ulDiff = hping * 1024 * 10 / (long)(goingDownDivider * initial_ping);

                                    //MpdUtilities.QueueDebugLogLine(false,("UploadSpeedSense: Down! Ping cur %i ms. Ave %I64i ms %i values. New Upload %i + %I64i = %I64i"), raw_ping, pingDelaysTotal/pingDelays.Count, pingDelays.Count, upload, ulDiff, upload+ulDiff);
                                    // prevent underflow
                                    if (upload > -ulDiff)
                                    {
                                        upload = (uint)(upload + ulDiff);
                                    }
                                    else
                                    {
                                        upload = 0;
                                    }
                                }
                                else if (hping > 0)
                                {
                                    //Ping lower than max allowed
                                    acceptNewClient_ = true;

                                    if (curUpload + 30 * 1024 > upload)
                                    {
                                        // raise the speed
                                        ulong ulDiff = (ulong)hping * 1024 * 10 / (ulong)(goingUpDivider * initial_ping);

                                        //MpdUtilities.QueueDebugLogLine(false,("UploadSpeedSense: Up! Ping cur %i ms. Ave %I64i ms %i values. New Upload %i + %I64i = %I64i"), raw_ping, pingDelaysTotal/pingDelays.Count, pingDelays.Count, upload, ulDiff, upload+ulDiff);
                                        // prevent overflow
                                        if (int.MaxValue - upload > ulDiff)
                                        {
                                            upload = (uint)(upload + ulDiff);
                                        }
                                        else
                                        {
                                            upload = int.MaxValue;
                                        }
                                    }
                                }
                                lock (prefsLocker_)
                                {
                                    if (upload < minUpload_)
                                    {
                                        upload = minUpload_;
                                        acceptNewClient_ = true;
                                    }
                                    if (upload > maxUpload_)
                                    {
                                        upload = maxUpload_;
                                    }
                                }//prefsLocker.Unlock();
                                Upload = upload;
                                if (enabled_ == false)
                                {
                                    enabled = false;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MpdUtilities.DebugLogError("LastCommonRouteFinder Runthread Fail", ex);
            }
            finally
            {
                // Signal that we have ended.
                threadEndedEvent_.Set();
            }
        }
        #endregion
    }
}
