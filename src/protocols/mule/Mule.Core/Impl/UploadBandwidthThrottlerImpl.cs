using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.Network;
using System.Threading;
using System.Diagnostics;
using Mpd.Utilities;

namespace Mule.Core.Impl
{
    class UploadBandwidthThrottlerImpl : UploadBandwidthThrottler
    {
        #region Fields
        private List<ThrottledControlSocket> controlQueueList_ = new List<ThrottledControlSocket>(); // a queue for all the sockets that want to have Send() called on them. // ZZ:UploadBandWithThrottler (UDP)
        private List<ThrottledControlSocket> controlQueueFirstList_ = new List<ThrottledControlSocket>(); // a queue for all the sockets that want to have Send() called on them. // ZZ:UploadBandWithThrottler (UDP)
        private List<ThrottledControlSocket> tempControlQueueList_ = new List<ThrottledControlSocket>(); // sockets that wants to enter m_ControlQueue_list // ZZ:UploadBandWithThrottler (UDP)
        private List<ThrottledControlSocket> tempControlQueueFirstList_ = new List<ThrottledControlSocket>(); // sockets that wants to enter m_ControlQueue_list and has been able to send before // ZZ:UploadBandWithThrottler (UDP)

        private List<ThrottledFileSocket> standardOrderList_ = new List<ThrottledFileSocket>(); // sockets that have upload slots. Ordered so the most prioritized socket is first

        private object sendLocker_ = new object();
        private object tempQueueLocker_ = new object();

        private ManualResetEvent threadEndedEvent_;
        private AutoResetEvent pauseEvent_;

        private ulong sentBytesSinceLastCall_;
        private ulong sentBytesSinceLastCallOverhead_;
        private uint highestNumberOfFullyActivatedSlots_;

        private bool doRun_;

        private Thread runningThread_ = null;
        #endregion

        #region Constructors
        public UploadBandwidthThrottlerImpl()
        {
            sentBytesSinceLastCall_ = 0;
            sentBytesSinceLastCallOverhead_ = 0;
            highestNumberOfFullyActivatedSlots_ = 0;

            threadEndedEvent_ = new ManualResetEvent(false);
            pauseEvent_ = new AutoResetEvent(true);
        }
        #endregion

        #region UploadBandwidthThrottler Members

        public void QueueForSendingControlPacket(Mule.Network.ThrottledControlSocket socket)
        {
            QueueForSendingControlPacket(socket, false);
        }

        public void QueueForSendingControlPacket(Mule.Network.ThrottledControlSocket socket, bool hasSent)
        {
            // Get critical section
            lock (tempQueueLocker_)
            {

                if (doRun_)
                {
                    if (hasSent)
                    {
                        tempControlQueueFirstList_.Add(socket);
                    }
                    else
                    {
                        tempControlQueueList_.Add(socket);
                    }
                }

            }
        }

        public void RemoveFromAllQueues(Mule.Network.ThrottledControlSocket socket)
        {
            RemoveFromAllQueues(socket, true);
        }

        public void RemoveFromAllQueues(Mule.Network.ThrottledFileSocket socket)
        {
            // Get critical section
            lock (sendLocker_)
            {

                if (doRun_)
                {
                    RemoveFromAllQueues(socket, false);

                    // And remove it from upload slots
                    RemoveFromStandardListNoLock(socket);
                }

                // End critical section
            }
        }

        public ulong NumberOfSentBytesSinceLastCallAndReset
        {
            get
            {
                ulong result = 0;

                lock (sendLocker_)
                {
                    result = sentBytesSinceLastCall_;
                    sentBytesSinceLastCall_ = 0;
                }

                return result;
            }
        }

        public ulong NumberOfSentBytesOverheadSinceLastCallAndReset
        {
            get
            {
                ulong result = 0;

                lock (sendLocker_)
                {
                    result = sentBytesSinceLastCallOverhead_;
                    sentBytesSinceLastCallOverhead_ = 0;
                }

                return result;
            }
        }

        public uint HighestNumberOfFullyActivatedSlotsSinceLastCallAndReset
        {
            get
            {
                uint result = 0;

                lock (sendLocker_)
                {
                    result = highestNumberOfFullyActivatedSlots_;
                    sentBytesSinceLastCallOverhead_ = 0;
                }

                return result;
            }
        }

        public uint StandardListSize
        {
            get { return (uint)standardOrderList_.Count; }
        }

        public void AddToStandardList(uint index, Mule.Network.ThrottledFileSocket socket)
        {
            if (socket != null)
            {
                lock (sendLocker_)
                {
                    RemoveFromStandardListNoLock(socket);

                    if (index > (uint)standardOrderList_.Count)
                    {
                        index = (uint)standardOrderList_.Count;
                    }

                    standardOrderList_.Insert((int)index, socket);

                }
            }
        }

        public bool RemoveFromStandardList(Mule.Network.ThrottledFileSocket socket)
        {
            bool returnValue;

            lock (sendLocker_)
            {
                returnValue = RemoveFromStandardListNoLock(socket);
            }

            return returnValue;
        }

        public void Pause(bool paused)
        {
            if (paused)
            {
                pauseEvent_.Reset();
            }
            else
            {
                pauseEvent_.Set();
            }
        }

        public uint GetSlotLimit(uint currentUpSpeed)
        {
            uint upPerClient = MuleConstants.UPLOAD_CLIENT_DATARATE;

            // if throttler doesn't require another slot, go with a slightly more restrictive method
            if (currentUpSpeed > 20 * 1024)
                upPerClient += currentUpSpeed / 43;

            if (upPerClient > 7680)
                upPerClient = 7680;

            //now the final check

            uint nMaxSlots;
            if (currentUpSpeed > 12 * 1024)
                nMaxSlots = (ushort)(((float)currentUpSpeed) / upPerClient);
            else if (currentUpSpeed > 7 * 1024)
                nMaxSlots = MuleConstants.MIN_UP_CLIENTS_ALLOWED + 2;
            else if (currentUpSpeed > 3 * 1024)
                nMaxSlots = MuleConstants.MIN_UP_CLIENTS_ALLOWED + 1;
            else
                nMaxSlots = MuleConstants.MIN_UP_CLIENTS_ALLOWED;

            return Math.Max(nMaxSlots, MuleConstants.MIN_UP_CLIENTS_ALLOWED);
        }


        public void Start()
        {
            runningThread_ = new Thread(new ParameterizedThreadStart(RunInternal));

            lock (sendLocker_)
            {
                doRun_ = true;
            }

            runningThread_.Start(this);
        }

        public void Stop()
        {
            lock (sendLocker_)
            {
                // signal the thread to stop looping and exit.
                doRun_ = false;
            }

            Pause(false);

            // wait for the thread to signal that it has stopped looping.
            threadEndedEvent_.WaitOne();

            runningThread_.Join();
        }
        #endregion

        #region Privates
        private void RunInternal(object arg)
        {
            uint lastLoopTick = MpdUtilities.TimeGetTime();
            long realBytesToSpend = 0;
            uint allowedDataRate = 0;
            uint rememberedSlotCounter = 0;
            uint lastTickReachedBandwidth = MpdUtilities.TimeGetTime();

            uint nEstiminatedLimit = 0;
            int nSlotsBusyLevel = 0;
            uint nUploadStartTime = 0;
            uint numberOfConsecutiveUpChanges = 0;
            uint numberOfConsecutiveDownChanges = 0;
            uint changesCount = 0;
            uint loopsCount = 0;

            bool bAlwaysEnableBigSocketBuffers = false;

            while (doRun_)
            {
                pauseEvent_.WaitOne();

                uint timeSinceLastLoop = MpdUtilities.TimeGetTime() - lastLoopTick;

                // Get current speed from UploadSpeedSense
                allowedDataRate = MuleApplication.Instance.LastCommonRouteFinder.Upload;

                // check busy level for all the slots (WSAEWOULDBLOCK status)
                uint cBusy = 0;
                uint nCanSend = 0;

                lock (sendLocker_)
                {
                    for (int i = 0; i < standardOrderList_.Count &&
                        (i < 3 || (uint)i < GetSlotLimit(MuleApplication.Instance.UploadQueue.DataRate)); i++)
                    {
                        if (standardOrderList_[i] != null && standardOrderList_[i].HasQueues)
                        {
                            nCanSend++;

                            if (standardOrderList_[i].IsBusy)
                                cBusy++;
                        }
                    }
                }

                // if this is kept, the loop above can be a little optimized (don't count nCanSend, just use nCanSend = GetSlotLimit(MuleApplication.Instance.UploadQueue.DataRate)
                if (MuleApplication.Instance.UploadQueue != null)
                    nCanSend = Math.Max(nCanSend, GetSlotLimit(MuleApplication.Instance.UploadQueue.DataRate));

                // When no upload limit has been set in options, try to guess a good upload limit.
                bool bUploadUnlimited = (MuleApplication.Instance.Preference.MaxUpload == MuleConstants.UNLIMITED);
                if (bUploadUnlimited)
                {
                    loopsCount++;

                    if (nCanSend > 0)
                    {
                        float fBusyPercent = ((float)cBusy / (float)nCanSend) * 100;
                        if (cBusy > 2 && fBusyPercent > 75.00f && nSlotsBusyLevel < 255)
                        {
                            nSlotsBusyLevel++;
                            changesCount++;
                        }
                        else if ((cBusy <= 2 || fBusyPercent < 25.00f) && nSlotsBusyLevel > (-255))
                        {
                            nSlotsBusyLevel--;
                            changesCount++;
                        }
                    }

                    if (nUploadStartTime == 0)
                    {
                        if (standardOrderList_.Count >= 3)
                            nUploadStartTime = MpdUtilities.TimeGetTime();
                    }
                    else if (MpdUtilities.TimeGetTime() - nUploadStartTime > MuleConstants.ONE_SEC_MS * 60)
                    {
                        if (MuleApplication.Instance.UploadQueue != null)
                        {
                            if (nEstiminatedLimit == 0)
                            { // no autolimit was set yet
                                if (nSlotsBusyLevel >= 250)
                                { // sockets indicated that the BW limit has been reached
                                    nEstiminatedLimit = MuleApplication.Instance.UploadQueue.DataRate;
                                    allowedDataRate = Math.Min(nEstiminatedLimit, allowedDataRate);
                                    nSlotsBusyLevel = -200;
                                    changesCount = 0;
                                    loopsCount = 0;
                                }
                            }
                            else
                            {
                                if (nSlotsBusyLevel > 250)
                                {
                                    if (changesCount > 500 || changesCount > 300 && loopsCount > 1000 || loopsCount > 2000)
                                    {
                                        numberOfConsecutiveDownChanges = 0;
                                    }
                                    numberOfConsecutiveDownChanges++;
                                    uint changeDelta = CalculateChangeDelta(numberOfConsecutiveDownChanges);

                                    // Don't lower speed below 1 KBytes/s
                                    if (nEstiminatedLimit < changeDelta + 1024)
                                    {
                                        if (nEstiminatedLimit > 1024)
                                        {
                                            changeDelta = nEstiminatedLimit - 1024;
                                        }
                                        else
                                        {
                                            changeDelta = 0;
                                        }
                                    }
                                    Debug.Assert(nEstiminatedLimit >= changeDelta + 1024);
                                    nEstiminatedLimit -= changeDelta;

                                    numberOfConsecutiveUpChanges = 0;
                                    nSlotsBusyLevel = 0;
                                    changesCount = 0;
                                    loopsCount = 0;
                                }
                                else if (nSlotsBusyLevel < (-250))
                                {
                                    if (changesCount > 500 || changesCount > 300 && loopsCount > 1000 || loopsCount > 2000)
                                    {
                                        numberOfConsecutiveUpChanges = 0;
                                    }
                                    numberOfConsecutiveUpChanges++;
                                    uint changeDelta = CalculateChangeDelta(numberOfConsecutiveUpChanges);

                                    // Don't raise speed unless we are under current allowedDataRate
                                    if (nEstiminatedLimit + changeDelta > allowedDataRate)
                                    {
                                        if (nEstiminatedLimit < allowedDataRate)
                                        {
                                            changeDelta = allowedDataRate - nEstiminatedLimit;
                                        }
                                        else
                                        {
                                            changeDelta = 0;
                                        }
                                    }
                                    Debug.Assert(nEstiminatedLimit < allowedDataRate && nEstiminatedLimit + changeDelta <= allowedDataRate || nEstiminatedLimit >= allowedDataRate && changeDelta == 0);
                                    nEstiminatedLimit += changeDelta;

                                    numberOfConsecutiveDownChanges = 0;
                                    nSlotsBusyLevel = 0;
                                    changesCount = 0;
                                    loopsCount = 0;
                                }

                                allowedDataRate = Math.Min(nEstiminatedLimit, allowedDataRate);
                            }
                        }
                    }
                }

                if (cBusy == nCanSend && standardOrderList_.Count > 0)
                {
                    allowedDataRate = 0;
                    if (nSlotsBusyLevel < 125 && bUploadUnlimited)
                    {
                        nSlotsBusyLevel = 125;
                    }
                }

                uint minFragSize = 1300;
                uint doubleSendSize = minFragSize * 2; // send two packages at a time so they can share an ACK
                if (allowedDataRate < 6 * 1024)
                {
                    minFragSize = 536;
                    doubleSendSize = minFragSize; // don't send two packages at a time at very low speeds to give them a smoother load
                }

                const uint TIME_BETWEEN_UPLOAD_LOOPS = 1;
                uint sleepTime;
                if (allowedDataRate == uint.MaxValue || realBytesToSpend >= 1000 || allowedDataRate == 0 && nEstiminatedLimit == 0)
                {
                    // we could send at once, but sleep a while to not suck up all cpu
                    sleepTime = TIME_BETWEEN_UPLOAD_LOOPS;
                }
                else if (allowedDataRate == 0)
                {
                    sleepTime = Math.Max((uint)Math.Ceiling(((double)doubleSendSize * 1000) / nEstiminatedLimit), TIME_BETWEEN_UPLOAD_LOOPS);
                }
                else
                {
                    // sleep for just as long as we need to get back to having one byte to send
                    sleepTime = Math.Max((uint)Math.Ceiling((double)(-realBytesToSpend + 1000) / allowedDataRate), TIME_BETWEEN_UPLOAD_LOOPS);

                }

                if (timeSinceLastLoop < sleepTime)
                {
                    Thread.Sleep((int)(sleepTime - timeSinceLastLoop));
                }

                uint thisLoopTick = MpdUtilities.TimeGetTime();
                timeSinceLastLoop = thisLoopTick - lastLoopTick;

                // Calculate how many bytes we can spend
                long bytesToSpend = 0;

                if (allowedDataRate != uint.MaxValue)
                {
                    // prevent overflow
                    if (timeSinceLastLoop == 0)
                    {
                        // no time has passed, so don't add any bytes. Shouldn't happen.
                        bytesToSpend = 0; //realBytesToSpend/1000;
                    }
                    else if (long.MaxValue / timeSinceLastLoop > allowedDataRate &&
                        long.MaxValue - allowedDataRate * timeSinceLastLoop > realBytesToSpend)
                    {
                        if (timeSinceLastLoop > sleepTime + 2000)
                        {

                            timeSinceLastLoop = sleepTime + 2000;
                            lastLoopTick = thisLoopTick - timeSinceLastLoop;
                        }

                        realBytesToSpend += allowedDataRate * timeSinceLastLoop;

                        bytesToSpend = realBytesToSpend / 1000;
                    }
                    else
                    {
                        realBytesToSpend = long.MaxValue;
                        bytesToSpend = int.MaxValue;
                    }
                }
                else
                {
                    realBytesToSpend = 0; //long.MaxValue;
                    bytesToSpend = int.MaxValue;
                }

                lastLoopTick = thisLoopTick;

                if (bytesToSpend >= 1 || allowedDataRate == 0)
                {
                    ulong spentBytes = 0;
                    ulong spentOverhead = 0;

                    lock (sendLocker_)
                    {
                        lock (tempQueueLocker_)
                        {

                            // are there any sockets in m_TempControlQueue_list? Move them to normal m_ControlQueue_list;
                            while (tempControlQueueFirstList_.Count > 0)
                            {
                                ThrottledControlSocket moveSocket = tempControlQueueFirstList_[0];
                                tempControlQueueFirstList_.RemoveAt(0);
                                controlQueueFirstList_.Add(moveSocket);
                            }
                            while (tempControlQueueList_.Count > 0)
                            {
                                ThrottledControlSocket moveSocket = tempControlQueueList_[0];
                                tempControlQueueList_.RemoveAt(0);
                                controlQueueList_.Add(moveSocket);
                            }

                        }//tempQueueLocker.Unlock();

                        // Send any queued up control packets first
                        while ((bytesToSpend > 0 && spentBytes < (ulong)bytesToSpend ||
                            allowedDataRate == 0 && spentBytes < 500) &&
                            (controlQueueFirstList_.Count > 0 || controlQueueList_.Count > 0))
                        {
                            ThrottledControlSocket socket = null;

                            if (controlQueueFirstList_.Count > 0)
                            {
                                socket = controlQueueFirstList_[0];
                                controlQueueFirstList_.RemoveAt(0);
                            }
                            else if (controlQueueList_.Count > 0)
                            {
                                socket = controlQueueList_[0];
                                controlQueueList_.RemoveAt(0);
                            }

                            if (socket != null)
                            {
                                SocketSentBytes socketSentBytes =
                                    socket.SendControlData(allowedDataRate > 0 ? (uint)(bytesToSpend - (long)spentBytes) : 1, minFragSize);
                                uint lastSpentBytes = socketSentBytes.SentBytesControlPackets +
                                    socketSentBytes.SentBytesStandardPackets;
                                spentBytes += lastSpentBytes;
                                spentOverhead += socketSentBytes.SentBytesControlPackets;
                            }
                        }

                        // Check if any sockets haven't gotten data for a long time. Then trickle them a package.
                        for (uint slotCounter = 0; slotCounter < (uint)standardOrderList_.Count; slotCounter++)
                        {
                            ThrottledFileSocket socket = standardOrderList_[(int)slotCounter];

                            if (socket != null)
                            {
                                if (thisLoopTick - socket.LastCalledSend > MuleConstants.ONE_SEC_MS)
                                {
                                    // trickle
                                    uint neededBytes = socket.NeededBytes;

                                    if (neededBytes > 0)
                                    {
                                        SocketSentBytes socketSentBytes =
                                            socket.SendFileAndControlData(neededBytes, minFragSize);
                                        uint lastSpentBytes =
                                            socketSentBytes.SentBytesControlPackets + socketSentBytes.SentBytesStandardPackets;
                                        spentBytes += lastSpentBytes;
                                        spentOverhead += socketSentBytes.SentBytesControlPackets;

                                        if (lastSpentBytes > 0 && slotCounter <
                                            highestNumberOfFullyActivatedSlots_)
                                        {
                                            highestNumberOfFullyActivatedSlots_ = slotCounter;
                                        }
                                    }
                                }
                            }
                        }

                        // Equal bandwidth for all slots
                        uint maxSlot = (uint)standardOrderList_.Count;
                        if (maxSlot > 0 && allowedDataRate / maxSlot < MuleConstants.UPLOAD_CLIENT_DATARATE)
                        {
                            maxSlot = allowedDataRate / MuleConstants.UPLOAD_CLIENT_DATARATE;
                        }
                        // if we are uploading fast, increase the sockets sendbuffers in order to be able to archive faster
                        // speeds
                        bool bUseBigBuffers = bAlwaysEnableBigSocketBuffers;
                        if (maxSlot > 0 && (allowedDataRate == uint.MaxValue || allowedDataRate / maxSlot > 100 * 1024) && MuleApplication.Instance.UploadQueue.DataRate > 300 * 1024)
                            bUseBigBuffers = true;

                        if (maxSlot > highestNumberOfFullyActivatedSlots_)
                        {
                            highestNumberOfFullyActivatedSlots_ = maxSlot;
                        }

                        for (uint maxCounter = 0; maxCounter < Math.Min(maxSlot, (uint)standardOrderList_.Count) && bytesToSpend > 0 && spentBytes < (ulong)bytesToSpend; maxCounter++)
                        {
                            if (rememberedSlotCounter >= (uint)standardOrderList_.Count ||
                               rememberedSlotCounter >= maxSlot)
                            {
                                rememberedSlotCounter = 0;
                            }

                            ThrottledFileSocket socket = standardOrderList_[(int)rememberedSlotCounter];
                            if (bUseBigBuffers)
                                socket.UseBigSendBuffer();

                            if (socket != null)
                            {
                                SocketSentBytes socketSentBytes =
                                    socket.SendFileAndControlData((uint)Math.Min(doubleSendSize,
                                    bytesToSpend - (long)spentBytes), doubleSendSize);
                                uint lastSpentBytes = socketSentBytes.SentBytesControlPackets +
                                    socketSentBytes.SentBytesStandardPackets;

                                spentBytes += lastSpentBytes;
                                spentOverhead += socketSentBytes.SentBytesControlPackets;
                            }

                            rememberedSlotCounter++;
                        }

                        // Any bandwidth that hasn't been used yet are used first to last.
                        for (uint slotCounter = 0; slotCounter < (uint)standardOrderList_.Count && bytesToSpend > 0 && spentBytes < (ulong)bytesToSpend; slotCounter++)
                        {
                            ThrottledFileSocket socket = standardOrderList_[(int)slotCounter];

                            if (socket != null)
                            {
                                uint bytesToSpendTemp = (uint)(bytesToSpend - (long)spentBytes);
                                SocketSentBytes socketSentBytes =
                                    socket.SendFileAndControlData(bytesToSpendTemp, doubleSendSize);
                                uint lastSpentBytes =
                                    socketSentBytes.SentBytesControlPackets +
                                    socketSentBytes.SentBytesStandardPackets;

                                spentBytes += lastSpentBytes;
                                spentOverhead += socketSentBytes.SentBytesControlPackets;

                                if (slotCounter + 1 > highestNumberOfFullyActivatedSlots_ &&
                                    (lastSpentBytes < bytesToSpendTemp ||
                                    lastSpentBytes >= doubleSendSize))
                                { // || lastSpentBytes > 0 && spentBytes == bytesToSpend /*|| slotCounter+1 == (uint)m_StandardOrder_list.Count)*/)) {
                                    highestNumberOfFullyActivatedSlots_ = slotCounter + 1;
                                }
                            }
                        }
                        realBytesToSpend -= (long)spentBytes * 1000;

                        // If we couldn't spend all allocated bandwidth this loop, some of it is allowed to be saved
                        // and used the next loop
                        if (realBytesToSpend < -(((long)standardOrderList_.Count + 1) * minFragSize) * 1000)
                        {
                            long newRealBytesToSpend = -(((long)standardOrderList_.Count + 1) * minFragSize) * 1000;

                            realBytesToSpend = newRealBytesToSpend;
                            lastTickReachedBandwidth = thisLoopTick;
                        }
                        else
                        {
                            ulong bandwidthSavedTolerance = 0;
                            if (realBytesToSpend > 0 && (ulong)realBytesToSpend > 999 + bandwidthSavedTolerance)
                            {
                                long newRealBytesToSpend = 999 + (long)bandwidthSavedTolerance;
                                //MuleApplication.Instance.QueueDebugLogLine(false,_T("UploadBandwidthThrottler::RunInternal(): Too high saved bytesToSpend. Limiting value. Old value: %I64i New value: %I64i"), realBytesToSpend, newRealBytesToSpend);
                                realBytesToSpend = newRealBytesToSpend;

                                if (thisLoopTick - lastTickReachedBandwidth > Math.Max(1000, timeSinceLastLoop * 2))
                                {
                                    highestNumberOfFullyActivatedSlots_ = (uint)standardOrderList_.Count + 1;
                                    lastTickReachedBandwidth = thisLoopTick;
                                    //MuleApplication.Instance.QueueDebugLogLine(false, _T("UploadBandwidthThrottler: Throttler requests new slot due to bw not reached. m_highestNumberOfFullyActivatedSlots: %i m_StandardOrder_list.Count: %i tick: %i"), m_highestNumberOfFullyActivatedSlots, m_StandardOrder_list.Count, thisLoopTick);
                                }
                            }
                            else
                            {
                                lastTickReachedBandwidth = thisLoopTick;
                            }
                        }

                        // save info about how much bandwidth we've managed to use since the last time someone polled us about used bandwidth
                        sentBytesSinceLastCall_ += spentBytes;
                        sentBytesSinceLastCallOverhead_ += spentOverhead;

                    }// sendLocker.Unlock();
                }
            }

            threadEndedEvent_.Set();

            lock (tempQueueLocker_)
            {
                tempControlQueueList_.Clear();
                tempControlQueueFirstList_.Clear();
            }

            lock (sendLocker_)
            {

                controlQueueList_.Clear();
                standardOrderList_.Clear();
            }
        }

        private void RemoveFromAllQueues(ThrottledControlSocket socket, bool dolock)
        {
            if (dolock)
            {
                // Get critical section
                System.Threading.Monitor.Enter(sendLocker_);
            }

            try
            {
                if (doRun_)
                {
                    // Remove this socket from control packet queue
                    controlQueueList_.Remove(socket);
                    controlQueueFirstList_.Remove(socket);


                    lock (tempQueueLocker_)
                    {
                        tempControlQueueList_.Remove(socket);
                        tempControlQueueFirstList_.Remove(socket);
                    }
                }
            }
            finally
            {
                if (dolock)
                {
                    System.Threading.Monitor.Exit(sendLocker_);
                }
            }
        }

        private bool RemoveFromStandardListNoLock(ThrottledFileSocket socket)
        {
            bool foundSocket = standardOrderList_.Contains(socket);

            if (foundSocket)
            {
                standardOrderList_.Remove(socket);

                if (highestNumberOfFullyActivatedSlots_ > (uint)standardOrderList_.Count)
                {
                    highestNumberOfFullyActivatedSlots_ = (uint)standardOrderList_.Count;
                }
            }

            return foundSocket;
        }

        private uint CalculateChangeDelta(uint numberOfConsecutiveChanges)
        {
            switch (numberOfConsecutiveChanges)
            {
                case 0: return 50;
                case 1: return 50;
                case 2: return 128;
                case 3: return 256;
                case 4: return 512;
                case 5: return 512 + 256;
                case 6: return 1 * 1024;
                case 7: return 1 * 1024 + 256;
                default: return 1 * 1024 + 512;
            }
        }
        #endregion
    }
}
