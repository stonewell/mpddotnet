using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Mpd.Utilities;
using System.Diagnostics;
using Mule.Network;
using Mule.File;

namespace Mule.Core.Impl
{
    class UploadQueueImpl : UploadQueue
    {
        #region Constants and Static
        private static uint counter, sec, statsave;
        private static uint s_uSaveStatistics = 0;
        private static uint igraph, i2Secs;

        private const uint HIGHSPEED_UPLOADRATE_START = 500 * 1024;
        private const uint HIGHSPEED_UPLOADRATE_END = 300 * 1024;
        #endregion

        #region Fields
        private List<ulong> avarage_dr_list = new List<ulong>();
        private List<ulong> avarage_friend_dr_list = new List<ulong>();
        private List<uint> avarage_tick_list = new List<uint>();
        private List<int> activeClients_list = new List<int>();
        private List<uint> activeClients_tick_list = new List<uint>();
        private uint datarate;   //datarate sent to network (including friends)
        private uint friendDatarate; // datarate of sent to friends (included in above total)
        private Timer h_timer;
        private Timer m_hHighSpeedUploadTimer;
        private uint successfullupcount;
        private uint failedupcount;
        private uint totaluploadtime;
        private uint m_nLastStartUpload;
        private uint m_dwRemovedClientByScore;
        private uint m_imaxscore;
        private uint m_dwLastCalculatedAverageCombinedFilePrioAndCredit;
        private float m_fAverageCombinedFilePrioAndCredit;
        private uint m_iHighestNumberOfFullyActivatedSlotsSinceLastCall;
        private uint m_MaxActiveClients;
        private uint m_MaxActiveClientsShortTime;
        private uint m_lastCalculatedDataRateTick;
        private ulong m_avarage_dr_sum;
        private uint m_dwLastResortedUploadSlots;
        private bool m_bStatisticsWaitingListDirty;
        #endregion

        #region Constructors
        public UploadQueueImpl()
        {
            WaitingList = new List<UpDownClient>();
            UploadingList = new List<UpDownClient>();

            h_timer = new Timer(new TimerCallback(UploadTimer), this, 0, 100);
            datarate = 0;
            counter = 0;
            successfullupcount = 0;
            failedupcount = 0;
            totaluploadtime = 0;
            m_nLastStartUpload = 0;
            statsave = 0;
            i2Secs = 0;
            m_dwRemovedClientByScore = MpdUtilities.GetTickCount();
            m_iHighestNumberOfFullyActivatedSlotsSinceLastCall = 0;
            m_MaxActiveClients = 0;
            m_MaxActiveClientsShortTime = 0;

            m_lastCalculatedDataRateTick = 0;
            m_avarage_dr_sum = 0;
            friendDatarate = 0;

            m_dwLastResortedUploadSlots = 0;
            m_hHighSpeedUploadTimer = null;
            m_bStatisticsWaitingListDirty = true;
        }
        #endregion

        #region UploadQueue Members

        public void AddClientToQueue(UpDownClient client)
        {
            AddClientToQueue(client, false);
        }

        public void Process()
        {
            uint curTick = MpdUtilities.GetTickCount();

            UpdateActiveClientsInfo(curTick);

            if (ForceNewClient())
            {
                // There's not enough open uploads. Open another one.
                AddUpNextClient("Not enough open upload slots for current ul speed");
            }

            // The loop that feeds the upload slots with data.
            int pos = 0;
            while (pos < UploadingList.Count)
            {
                // Get the client. Note! Also updates pos as a side effect.
                UpDownClient cur_client = UploadingList[pos];
                //It seems chatting or friend slots can get stuck at times in upload.. This needs looked into..
                if (cur_client.ClientSocket == null)
                {
                    RemoveFromUploadQueue(cur_client,
                        "Uploading to client without ClientSocket? (CUploadQueue::Process)");
                    cur_client.Disconnected("CUploadQueue::Process");
                }
                else
                {
                    cur_client.SendBlockData();
                    pos++;
                }
            }

            // Save used bandwidth for speed calculations
            ulong sentBytes = MuleApplication.Instance.UploadBandwidthThrottler.NumberOfSentBytesSinceLastCallAndReset;
            avarage_dr_list.Add(sentBytes);
            m_avarage_dr_sum += sentBytes;

            ulong tm =
                    MuleApplication.Instance.UploadBandwidthThrottler.NumberOfSentBytesOverheadSinceLastCallAndReset;

            avarage_friend_dr_list.Add(MuleApplication.Instance.Statistics.SessionSentBytesToFriend);

            // Save time beetween each speed snapshot
            avarage_tick_list.Add(curTick);

            // don't save more than 30 secs of data
            while (avarage_tick_list.Count > 3 &&
                avarage_friend_dr_list.Count > 0 &&
                MpdUtilities.GetTickCount() - avarage_tick_list[0] > 30 * 1000)
            {
                m_avarage_dr_sum -= avarage_dr_list[0];
                avarage_dr_list.RemoveAt(0);
                avarage_friend_dr_list.RemoveAt(0);
                avarage_tick_list.RemoveAt(0);
            }

            if (DataRate > HIGHSPEED_UPLOADRATE_START && m_hHighSpeedUploadTimer == null)
                UseHighSpeedUploadTimer(true);
            else if (DataRate < HIGHSPEED_UPLOADRATE_END && m_hHighSpeedUploadTimer != null)
                UseHighSpeedUploadTimer(false);
        }

        public void AddClientToQueue(UpDownClient client, bool bIgnoreTimelimit)
        {
            //This is to keep users from abusing the limits we put on lowID callbacks.
            //1)Check if we are connected to any network and that we are a lowID.
            //(Although this check shouldn't matter as they wouldn't have found us..
            // But, maybe I'm missing something, so it's best to check as a precaution.)
            //2)Check if the user is connected to Kad. We do allow all Kad Callbacks.
            //3)Check if the user is in our download list or a friend..
            //We give these users a special pass as they are helping us..
            //4)Are we connected to a server? If we are, is the user on the same server?
            //TCP lowID callbacks are also allowed..
            //5)If the queue is very short, allow anyone in as we want to make sure
            //our upload is always used.
            if (MuleApplication.Instance.IsConnected
                && MuleApplication.Instance.IsFirewalled
                && client.KadPort == 0
                && client.DownloadState == DownloadStateEnum.DS_NONE
                && !client.IsFriend
                && MuleApplication.Instance.ServerConnect != null
                && !MuleApplication.Instance.ServerConnect.IsLocalServer(client.ServerIP, client.ServerPort)
                && WaitingUserCount > 50)
                return;
            client.AddAskedCount();
            client.SetLastUpRequest();
            if (!bIgnoreTimelimit)
                client.AddRequestCount(client.UploadFileID);
            if (client.IsBanned)
                return;
            ushort cSameIP = 0;
            // check for double
            int pos1 = 0;

            KnownFile reqfile = null;

            while (pos1 < WaitingList.Count)
            {
                UpDownClient cur_client = WaitingList[pos1];
                if (cur_client == client)
                {
                    if (client.DoesAddNextConnect && AcceptNewClient(client.DoesAddNextConnect))
                    {
                        //Special care is given to lowID clients that missed their upload slot
                        //due to the saving bandwidth on callbacks.
                        client.DoesAddNextConnect = false;
                        RemoveFromWaitingQueue(client, true);
                        // statistic values // TODO: Maybe we should change this to count each request for a file only once and ignore reasks
                        reqfile =
                            MuleApplication.Instance.SharedFiles.GetFileByID(client.UploadFileID);
                        if (reqfile != null)
                            reqfile.Statistic.AddRequest();
                        AddUpNextClient("Adding ****lowid when reconnecting.", client);
                        return;
                    }
                    client.SendRankingInfo();
                    return;
                }
                else if (client.Compare(cur_client))
                {
                    MuleApplication.Instance.ClientList.AddTrackClient(client); // in any case keep track of this client

                    // another client with same ip:port or hash
                    // this happens only in rare cases, because same userhash / ip:ports are assigned to the right client on connecting in most cases
                    if (cur_client.Credits != null && cur_client.Credits.GetCurrentIdentState(cur_client.IP) ==
                        IdentStateEnum.IS_IDENTIFIED)
                    {
                        //cur_client has a valid secure hash, don't remove him
                        return;
                    }
                    if (client.Credits != null && client.Credits.GetCurrentIdentState(client.IP) ==
                        IdentStateEnum.IS_IDENTIFIED)
                    {
                        //client has a valid secure hash, add him remove other one
                        RemoveFromWaitingQueue(pos1, true);
                        if (cur_client.ClientSocket == null)
                        {
                            cur_client.Disconnected("AddClientToQueue - same userhash 1");
                        }
                    }
                    else
                    {
                        // remove both since we do not know who the bad one is
                        RemoveFromWaitingQueue(pos1, true);
                        if (cur_client.ClientSocket == null)
                        {
                            cur_client.Disconnected("AddClientToQueue - same userhash 2");
                        }
                        return;
                    }
                }
                else if (client.IP == cur_client.IP)
                {
                    // same IP, different port, different userhash
                    cSameIP++;
                    pos1++;
                }
                else
                {
                    pos1++;
                }
            }

            if (cSameIP >= 3)
            {
                // do not accept more than 3 clients from the same IP
                return;
            }
            else if (MuleApplication.Instance.ClientList.GetClientsFromIP(client.IP) >= 3)
            {
                return;
            }
            // done

            // statistic values // TODO: Maybe we should change this to count each request for a file only once and ignore reasks
            reqfile = MuleApplication.Instance.SharedFiles.GetFileByID(client.UploadFileID);
            if (reqfile != null)
                reqfile.Statistic.AddRequest();

            // emule collection will bypass the queue
            if (reqfile != null &&
                MuleUtilities.HasCollectionExtention(reqfile.FileName) &&
                reqfile.FileSize < (ulong)MuleConstants.MAXPRIORITYCOLL_SIZE
                && !client.IsDownloading &&
                client.ClientSocket != null &&
                client.ClientSocket.IsConnected)
            {
                client.HasCollectionUploadSlot = true;
                RemoveFromWaitingQueue(client, true);
                AddUpNextClient("Collection Priority Slot", client);
                return;
            }
            else
                client.HasCollectionUploadSlot = false;

            // cap the list
            // the queue limit in prefs is only a soft limit. Hard limit is 25% higher, to let in powershare clients and other
            // high ranking clients after soft limit has been reached
            uint softQueueLimit = MuleApplication.Instance.Preference.QueueSize;
            uint hardQueueLimit = MuleApplication.Instance.Preference.QueueSize +
                Math.Max(MuleApplication.Instance.Preference.QueueSize / 4, 200);

            // if soft queue limit has been reached, only let in high ranking clients
            if ((uint)WaitingList.Count >= hardQueueLimit ||
                (uint)WaitingList.Count >= softQueueLimit && // soft queue limit is reached
                (client.IsFriend && client.FriendSlot) == false && // client is not a friend with friend slot
                client.CombinedFilePrioAndCredit < GetAverageCombinedFilePrioAndCredit())
            { // and client has lower Credits/wants lower prio file than average client in queue

                // then block client from getting on queue
                return;
            }
            if (client.IsDownloading)
            {
                // he's already downloading and wants probably only another file
                Packet packet =
                    MuleApplication.Instance.NetworkObjectManager.CreatePacket(OperationCodeEnum.OP_ACCEPTUPLOADREQ, 0);
                MuleApplication.Instance.Statistics.AddUpDataOverheadFileRequest(packet.Size);
                client.SendPacket(packet, true);
                return;
            }
            if (WaitingList.Count == 0 && ForceNewClient(true))
            {
                AddUpNextClient("Direct add with empty queue.", client);
            }
            else
            {
                m_bStatisticsWaitingListDirty = true;
                WaitingList.Add(client);
                client.UploadState = UploadStateEnum.US_ONUPLOADQUEUE;
                client.SendRankingInfo();
            }
        }

        public bool RemoveFromUploadQueue(UpDownClient client)
        {
            return RemoveFromUploadQueue(client, null, true, false);
        }

        public bool RemoveFromUploadQueue(UpDownClient client, string pszReason)
        {
            return RemoveFromUploadQueue(client, pszReason, true, false);
        }

        public bool RemoveFromUploadQueue(UpDownClient client, string pszReason, bool updatewindow)
        {
            return RemoveFromUploadQueue(client, pszReason, updatewindow, false);
        }

        public bool RemoveFromUploadQueue(UpDownClient client, string pszReason, bool updatewindow, bool earlyabort)
        {
            bool result = false;
            uint slotCounter = 1;
            int pos = 0;
            while (pos < UploadingList.Count)
            {
                UpDownClient curClient = UploadingList[pos];
                if (client == curClient)
                {
                    client.DoesAddNextConnect = false;
                    UploadingList.RemoveAt(pos);

                    bool removed =
                        MuleApplication.Instance.UploadBandwidthThrottler.RemoveFromStandardList(client.ClientSocket);
                    bool pcRemoved =
                        MuleApplication.Instance.UploadBandwidthThrottler.RemoveFromStandardList(client.PeerCacheUpSocket);

                    //if(MuleApplication.Instance.Preference.GetLogUlDlEvents() && !(removed || pcRemoved)) {
                    //    AddDebugLogLine(false, _T("UploadQueue: Didn't find ClientSocket to delete. Adress: 0x%x"), client.ClientSocket);
                    //}

                    if (client.SessionUp > 0)
                    {
                        ++successfullupcount;
                        totaluploadtime += client.UpStartTimeDelay / 1000;
                    }
                    else if (earlyabort == false)
                        ++failedupcount;

                    KnownFile requestedFile =
                        MuleApplication.Instance.SharedFiles.GetFileByID(client.UploadFileID);
                    if (requestedFile != null)
                    {
                        requestedFile.UpdatePartsInfo();
                    }
                    MuleApplication.Instance.ClientList.AddTrackClient(client); // Keep track of this client
                    client.UploadState = UploadStateEnum.US_NONE;
                    client.ClearUploadBlockRequests();
                    client.HasCollectionUploadSlot = false;

                    m_iHighestNumberOfFullyActivatedSlotsSinceLastCall = 0;

                    result = true;
                }
                else
                {
                    curClient.SlotNumber = slotCounter;
                    slotCounter++;
                    pos++;
                }
            }
            return result;
        }

        public bool RemoveFromWaitingQueue(UpDownClient client)
        {
            return RemoveFromWaitingQueue(client, true);
        }

        public bool RemoveFromWaitingQueue(UpDownClient client, bool updatewindow)
        {
            int pos = WaitingList.IndexOf(client);
            if (pos >= 0)
            {
                RemoveFromWaitingQueue(pos, updatewindow);
                return true;
            }
            else
                return false;
        }

        public bool IsOnUploadQueue(UpDownClient client)
        {
            return WaitingList.Contains(client);
        }

        public bool IsDownloading(UpDownClient client)
        {
            return UploadingList.Contains(client);
        }

        public void UpdateDataRates()
        {
            // Calculate average datarate
            if (MpdUtilities.GetTickCount() - m_lastCalculatedDataRateTick > 500)
            {
                m_lastCalculatedDataRateTick = MpdUtilities.GetTickCount();

                if (avarage_dr_list.Count >= 2 &&
                    (avarage_tick_list.Last() > avarage_tick_list.First()))
                {
                    datarate = (uint)(((m_avarage_dr_sum - avarage_dr_list.First()) * 1000) /
                        (avarage_tick_list.Last() - avarage_tick_list.First()));
                    friendDatarate = (uint)(((avarage_friend_dr_list.Last() -
                        avarage_friend_dr_list.First()) * 1000) /
                        (avarage_tick_list.Last() - avarage_tick_list.First()));
                }
            }
        }

        public uint DataRate
        {
            get { return datarate; }
        }

        public uint ToNetworkDataRate
        {
            get
            {
                if (datarate > friendDatarate)
                {
                    return datarate - friendDatarate;
                }
                else
                {
                    return 0;
                }
            }
        }

        public bool CheckForTimeOver(UpDownClient client)
        {
            //If we have nobody in the queue, do NOT remove the current uploads..
            //This will save some bandwidth and some unneeded swapping from upload/queue/upload..
            if (WaitingList.Count == 0 || client.FriendSlot)
                return false;

            if (client.HasCollectionUploadSlot)
            {
                KnownFile pDownloadingFile =
                    MuleApplication.Instance.SharedFiles.GetFileByID(client.ReqUpFileId);
                if (pDownloadingFile == null)
                    return true;
                if (MuleUtilities.HasCollectionExtention(pDownloadingFile.FileName) &&
                    pDownloadingFile.FileSize < (ulong)MuleConstants.MAXPRIORITYCOLL_SIZE)
                    return false;
                else
                {
                    return true;
                }
            }

            if (!MuleApplication.Instance.Preference.DoesTransferFullChunks)
            {
                if (client.UpStartTimeDelay > MuleConstants.SESSIONMAXTIME)
                { // Try to keep the clients from downloading for ever
                    return true;
                }

                // Cache current client score
                uint score = client.GetScore(true, true);

                // Check if another client has a bigger score
                if (score < GetMaxClientScore() && m_dwRemovedClientByScore < MpdUtilities.GetTickCount())
                {
                    //Set timer to prevent to many uploadslot getting kick do to score.
                    //Upload slots are delayed by a min of 1 sec and the maxscore is reset every 5 sec.
                    //So, I choose 6 secs to make sure the maxscore it updated before doing this again.
                    m_dwRemovedClientByScore = MpdUtilities.GetTickCount() +
                        MuleConstants.ONE_SEC_MS * 6;
                    return true;
                }
            }
            else
            {
                // Allow the client to download a specified amount per session
                if (client.QueueSessionPayloadUp > MuleConstants.SESSIONMAXTRANS)
                {
                    return true;
                }
            }
            return false;
        }

        public int WaitingUserCount
        {
            get { return WaitingList.Count; }
        }

        public int UploadQueueLength
        {
            get { return UploadingList.Count; }
        }

        public uint ActiveUploadsCount
        {
            get { return m_MaxActiveClientsShortTime; }
        }

        public uint GetWaitingUserForFileCount(List<KnownFile> raFiles, bool bOnlyIfChanged)
        {
            if (bOnlyIfChanged && !m_bStatisticsWaitingListDirty)
                return Convert.ToUInt32(-1);

            m_bStatisticsWaitingListDirty = false;
            uint nResult = 0;

            WaitingList.ForEach(cur_client =>
                {
                    raFiles.ForEach(rfile =>
                        {
                            if (MpdUtilities.Md4Cmp(rfile.FileHash, cur_client.UploadFileID) == 0)
                            {
                                nResult++;
                            }
                        });
                });
            return nResult;
        }

        public uint GetDatarateForFile(List<KnownFile> raFiles)
        {
            uint nResult = 0;
            UploadingList.ForEach(cur_client =>
            {
                raFiles.ForEach(rfile =>
                {
                    if (MpdUtilities.Md4Cmp(rfile.FileHash, cur_client.UploadFileID) == 0)
                    {
                        nResult+=cur_client.DataRate;
                    }
                });
            });
            return nResult;
        }

        public List<UpDownClient> WaitingList
        {
            get;
            set;
        }

        public List<UpDownClient> UploadingList
        {
            get;
            set;
        }

        public UpDownClient GetWaitingClientByIP_UDP(uint dwIP, ushort nUDPPort, bool bIgnorePortOnUniqueIP)
        {
            bool pbMultipleIPs = false;
            return GetWaitingClientByIP_UDP(dwIP, nUDPPort, bIgnorePortOnUniqueIP, ref pbMultipleIPs);
        }

        public UpDownClient GetWaitingClientByIP_UDP(uint dwIP, ushort nUDPPort, bool bIgnorePortOnUniqueIP, ref bool pbMultipleIPs)
        {
            UpDownClient pMatchingIPClient = null;
            uint cMatches = 0;

            foreach (UpDownClient cur_client in WaitingList)
            {
                if (dwIP == cur_client.IP && nUDPPort == cur_client.UDPPort)
                    return cur_client;
                else if (dwIP == cur_client.IP && bIgnorePortOnUniqueIP)
                {
                    pMatchingIPClient = cur_client;
                    cMatches++;
                }
            }

            pbMultipleIPs = cMatches > 1;

            if (pMatchingIPClient != null && cMatches == 1)
                return pMatchingIPClient;
            else
                return null;
        }

        public UpDownClient GetWaitingClientByIP(uint dwIP)
        {
            foreach (UpDownClient cur_client in WaitingList)
            {
                if (dwIP == cur_client.IP)
                    return cur_client;
            }

            return null;
        }

        public UpDownClient GetNextClient(UpDownClient lastclient)
        {
            if (WaitingList.Count == 0)
                return null;
            if (lastclient == null)
                return WaitingList[0];
            int pos = WaitingList.IndexOf(lastclient);
            if (pos < 0)
            {
                return WaitingList[0];
            }

            if (pos + 1 < WaitingList.Count)
                return WaitingList[pos + 1];

            return null;
        }

        public void DeleteAll()
        {
            WaitingList.Clear();
            UploadingList.Clear();
        }

        public uint GetWaitingPosition(UpDownClient client)
        {
            if (!IsOnUploadQueue(client))
                return 0;
            uint rank = 1;
            uint myscore = client.GetScore(false);
            foreach (UpDownClient c in WaitingList)
            {
                if (c.GetScore(false) > myscore)
                    rank++;
            }
            return rank;
        }

        public uint SuccessfullUploadCount
        {
            get;
            set;
        }

        public uint FailedUploadCount
        {
            get;
            set;
        }

        public uint AverageUploadTime
        {
            get
            {
                if (successfullupcount != 0)
                {
                    return totaluploadtime / successfullupcount;
                }
                return 0;
            }
        }

        public UpDownClient FindBestClientInQueue()
        {
            int toadd = -1;
            int toaddlow = -1;
            uint bestscore = 0;
            uint bestlowscore = 0;
            UpDownClient newclient = null;
            UpDownClient lowclient = null;

            int pos1 = 0;
            while (pos1 < WaitingList.Count)
            {
                UpDownClient cur_client = WaitingList[pos1];
                //While we are going through this list.. Lets check if a client appears to have left the network..
                Debug.Assert(cur_client.LastUpRequest != 0);
                if ((MpdUtilities.GetTickCount() - cur_client.LastUpRequest > MuleConstants.MAX_PURGEQUEUETIME) ||
                    MuleApplication.Instance.SharedFiles.GetFileByID(cur_client.UploadFileID) == null)
                {
                    //This client has either not been seen in a long time, or we no longer share the file he wanted anymore..
                    cur_client.ClearWaitStartTime();
                    RemoveFromWaitingQueue(pos1, true);
                    continue;
                }
                else
                {
                    // finished clearing
                    uint cur_score = cur_client.GetScore(false);

                    if (cur_score > bestscore)
                    {
                        // cur_client is more worthy than current best client that is ready to go (connected).
                        if (!cur_client.HasLowID ||
                            (cur_client.ClientSocket != null &&
                            cur_client.ClientSocket.IsConnected))
                        {
                            // this client is a HighID or a lowID client that is ready to go (connected)
                            // and it is more worthy
                            bestscore = cur_score;
                            toadd = pos1;
                            newclient = WaitingList[toadd];
                        }
                        else if (!cur_client.DoesAddNextConnect)
                        {
                            // this client is a lowID client that is not ready to go (not connected)

                            // now that we know this client is not ready to go, compare it to the best not ready client
                            // the best not ready client may be better than the best ready client, so we need to check
                            // against that client
                            if (cur_score > bestlowscore)
                            {
                                // it is more worthy, keep it
                                bestlowscore = cur_score;
                                toaddlow = pos1;
                                lowclient = WaitingList[toaddlow];
                            }
                        }
                    }

                    pos1++;
                }
            }

            if (bestlowscore > bestscore && lowclient != null)
                lowclient.DoesAddNextConnect = true;

            if (toadd == -1)
                return null;
            else
                return WaitingList[toadd];
        }

        public void ResortUploadSlots()
        {
            ResortUploadSlots(false);
        }

        public void ResortUploadSlots(bool force)
        {
            uint curtick = MpdUtilities.GetTickCount();
            if (force || curtick - m_dwLastResortedUploadSlots >= 10 * 1000)
            {
                m_dwLastResortedUploadSlots = curtick;

                MuleApplication.Instance.UploadBandwidthThrottler.Pause(true);

                List<UpDownClient> tempUploadinglist =
                    new List<UpDownClient>();

                // Remove all clients from uploading list and store in tempList
                while (UploadingList.Count > 0)
                {
                    // Get and remove the client from upload list.
                    UpDownClient cur_client = UploadingList[0];

                    UploadingList.RemoveAt(0);

                    // Remove the found Client from UploadBandwidthThrottler
                    MuleApplication.Instance.UploadBandwidthThrottler.RemoveFromStandardList(cur_client.ClientSocket);
                    MuleApplication.Instance.UploadBandwidthThrottler.RemoveFromStandardList(cur_client.PeerCacheUpSocket);

                    tempUploadinglist.Add(cur_client);
                }

                // Remove one at a time from temp list and reinsert in correct position in uploading list
                while (tempUploadinglist.Count > 0)
                {
                    // Get and remove the client from upload list.
                    UpDownClient cur_client = tempUploadinglist[0];

                    tempUploadinglist.RemoveAt(0);

                    // This will insert in correct place
                    InsertInUploadingList(cur_client);
                }

                MuleApplication.Instance.UploadBandwidthThrottler.Pause(false);
            }
        }

        #endregion

        #region Protected
        protected void RemoveFromWaitingQueue(int pos, bool updatewindow)
        {
            m_bStatisticsWaitingListDirty = true;
            UpDownClient todelete = WaitingList[pos];
            WaitingList.RemoveAt(pos);
            todelete.DoesAddNextConnect = false;
            todelete.UploadState = UploadStateEnum.US_NONE;
        }

        protected bool AcceptNewClient()
        {
            return AcceptNewClient(false);
        }

        protected bool AcceptNewClient(bool addOnNextConnect)
        {
            uint curUploadSlots = (uint)UploadingList.Count;

            //We allow ONE extra slot to be created to accommodate lowID users.
            //This is because we skip these users when it was actually their turn
            //to get an upload slot..
            if (addOnNextConnect && curUploadSlots > 0)
                curUploadSlots--;

            return AcceptNewClient(curUploadSlots);
        }

        protected bool AcceptNewClient(uint curUploadSlots)
        {
            // check if we can allow a new client to start downloading from us

            if (curUploadSlots < MuleConstants.MIN_UP_CLIENTS_ALLOWED)
                return true;

            ushort MaxSpeed;
            if (MuleApplication.Instance.Preference.IsDynamicUploadEnabled)
                MaxSpeed = (ushort)(MuleApplication.Instance.LastCommonRouteFinder.Upload / 1024);
            else
                MaxSpeed = MuleApplication.Instance.Preference.MaxUpload;

            if (curUploadSlots >= MuleConstants.MAX_UP_CLIENTS_ALLOWED ||
                curUploadSlots >= 4 &&
                (
                 curUploadSlots >= (datarate / MuleConstants.UPLOAD_CHECK_CLIENT_DR) ||
                 curUploadSlots >= ((uint)MaxSpeed) * 1024 / MuleConstants.UPLOAD_CLIENT_DATARATE ||
                 (
                  MuleApplication.Instance.Preference.MaxUpload == MuleConstants.UNLIMITED &&
                  !MuleApplication.Instance.Preference.IsDynamicUploadEnabled &&
                  MuleApplication.Instance.Preference.GetMaxGraphUploadRate(true) > 0 &&
                  curUploadSlots >=
                  ((uint)MuleApplication.Instance.Preference.GetMaxGraphUploadRate(false)) * 1024 / MuleConstants.UPLOAD_CLIENT_DATARATE
                 )
                )
            ) // Math.Max number of clients to allow for all circumstances
                return false;

            return true;
        }

        protected bool ForceNewClient()
        {
            return ForceNewClient(false);
        }

        protected bool ForceNewClient(bool allowEmptyWaitingQueue)
        {
            if (!allowEmptyWaitingQueue && WaitingList.Count <= 0)
                return false;

            if (MpdUtilities.GetTickCount() - m_nLastStartUpload < 1000 && datarate < 102400)
                return false;

            uint curUploadSlots = (uint)UploadingList.Count;

            if (curUploadSlots < MuleConstants.MIN_UP_CLIENTS_ALLOWED)
                return true;

            if (!AcceptNewClient(curUploadSlots) ||
                !MuleApplication.Instance.LastCommonRouteFinder.IsAcceptNewClient)
            {
                // UploadSpeedSense can veto a new slot if USS enabled
                return false;
            }

            ushort MaxSpeed;
            if (MuleApplication.Instance.Preference.IsDynamicUploadEnabled)
                MaxSpeed = (ushort)(MuleApplication.Instance.LastCommonRouteFinder.Upload / 1024);
            else
                MaxSpeed = MuleApplication.Instance.Preference.MaxUpload;

            uint upPerClient = MuleConstants.UPLOAD_CLIENT_DATARATE;

            // if throttler doesn't require another slot, go with a slightly more restrictive method
            if (MaxSpeed > 20 || MaxSpeed == MuleConstants.UNLIMITED)
                upPerClient += datarate / 43;

            if (upPerClient > 7680)
                upPerClient = 7680;

            //now the final check

            if (MaxSpeed == MuleConstants.UNLIMITED)
            {
                if (curUploadSlots < (datarate / upPerClient))
                    return true;
            }
            else
            {
                uint nMaxSlots;
                if (MaxSpeed > 12)
                    nMaxSlots = (uint)(((float)(MaxSpeed * 1024)) / upPerClient);
                else if (MaxSpeed > 7)
                    nMaxSlots = MuleConstants.MIN_UP_CLIENTS_ALLOWED + 2;
                else if (MaxSpeed > 3)
                    nMaxSlots = MuleConstants.MIN_UP_CLIENTS_ALLOWED + 1;
                else
                    nMaxSlots = MuleConstants.MIN_UP_CLIENTS_ALLOWED;
                //		AddLogLine(true,"maxslots=%u, upPerClient=%u, datarateslot=%u|%u|%u",nMaxSlots,upPerClient,datarate/UPLOAD_CHECK_CLIENT_DR, datarate, UPLOAD_CHECK_CLIENT_DR);

                if (curUploadSlots < nMaxSlots)
                {
                    return true;
                }
            }

            if (m_iHighestNumberOfFullyActivatedSlotsSinceLastCall > (uint)UploadingList.Count)
            {
                // uploadThrottler requests another slot. If throttler says it needs another slot, we will allow more slots
                // than what we require ourself. Never allow more slots than to give each slot high enough average transfer speed, though (checked above).
                //if(MuleApplication.Instance.Preference.GetLogUlDlEvents() && WaitingList.Count > 0)
                //    AddDebugLogLine(false, _T("UploadQueue: Added new slot since throttler needs it. m_iHighestNumberOfFullyActivatedSlotsSinceLastCall: %i UploadingList.Count: %i tick: %i"), m_iHighestNumberOfFullyActivatedSlotsSinceLastCall, UploadingList.Count, ::GetTickCount());
                return true;
            }

            //nope
            return false;
        }

        protected bool AddUpNextClient(string pszReason)
        {
            return AddUpNextClient(pszReason, null);
        }

        protected bool AddUpNextClient(string pszReason, UpDownClient directadd)
        {
            UpDownClient newclient = null;
            // select next client or use given client
            if (directadd == null)
            {
                newclient = FindBestClientInQueue();

                if (newclient != null)
                {
                    RemoveFromWaitingQueue(newclient, true);
                }
            }
            else
                newclient = directadd;

            if (newclient == null)
                return false;

            if (!MuleApplication.Instance.Preference.DoesTransferFullChunks)
                UpdateMaxClientScore(); // refresh score caching, now that the highest score is removed

            if (IsDownloading(newclient))
                return false;

            if (newclient.HasCollectionUploadSlot && directadd == null)
            {
                newclient.HasCollectionUploadSlot = false;
            }

            // tell the client that we are now ready to upload
            if (newclient.ClientSocket == null || !newclient.ClientSocket.IsConnected)
            {
                newclient.UploadState = UploadStateEnum.US_CONNECTING;
                if (!newclient.TryToConnect(true))
                    return false;
            }
            else
            {
                Packet packet =
                    MuleApplication.Instance.NetworkObjectManager.CreatePacket(OperationCodeEnum.OP_ACCEPTUPLOADREQ, 0);
                MuleApplication.Instance.Statistics.AddUpDataOverheadFileRequest(packet.Size);
                newclient.SendPacket(packet, true);
                newclient.UploadState = UploadStateEnum.US_UPLOADING;
            }
            newclient.SetUpStartTime();
            newclient.ResetSessionUp();

            InsertInUploadingList(newclient);

            m_nLastStartUpload = MpdUtilities.GetTickCount();

            // statistic
            KnownFile reqfile = MuleApplication.Instance.SharedFiles.GetFileByID(newclient.UploadFileID);
            if (reqfile != null)
                reqfile.Statistic.AddAccepted();

            return true;
        }

        protected void UseHighSpeedUploadTimer(bool bEnable)
        {
            if (!bEnable)
            {
                if (m_hHighSpeedUploadTimer != null)
                {
                    m_hHighSpeedUploadTimer.Dispose();
                    m_hHighSpeedUploadTimer = null;
                }
            }
            else
            {
                if (m_hHighSpeedUploadTimer == null)
                    m_hHighSpeedUploadTimer = 
                        new Timer(new TimerCallback(HSUploadTimer), this, 0, 1);
            }
        }

        protected void UploadTimer(object state)
        {
            try
            {
                if (!MuleApplication.Instance.IsRunning)
                    return;

                // ZZ:UploadSpeedSense -.
                MuleApplication.Instance.LastCommonRouteFinder.SetPrefs(
                    MuleApplication.Instance.Preference.IsDynamicUploadEnabled,
                    MuleApplication.Instance.UploadQueue.DataRate,
                    (uint)MuleApplication.Instance.Preference.MinUpload * 1024,
                    (MuleApplication.Instance.Preference.MaxUpload != 0) ?
                    (uint)MuleApplication.Instance.Preference.MaxUpload * 1024 :
                    MuleApplication.Instance.Preference.GetMaxGraphUploadRate(false) * 1024,
                    MuleApplication.Instance.Preference.IsDynUpUseMillisecondPingTolerance,
                    (MuleApplication.Instance.Preference.DynUpPingTolerance > 100) ?
                    ((MuleApplication.Instance.Preference.DynUpPingTolerance - 100) / 100.0f) : 0,
                    (uint)MuleApplication.Instance.Preference.DynUpPingToleranceMilliseconds,
                    (uint)MuleApplication.Instance.Preference.DynUpGoingUpDivider,
                    (uint)MuleApplication.Instance.Preference.DynUpGoingDownDivider,
                    (uint)MuleApplication.Instance.Preference.DynUpNumberOfPings,
                    20); // PENDING: Hard coded min pLowestPingAllowed
                // ZZ:UploadSpeedSense <--

                MuleApplication.Instance.UploadQueue.Process();
                MuleApplication.Instance.DownloadQueue.Process();

                if (MuleApplication.Instance.Preference.DoesShowOverhead)
                {
                    MuleApplication.Instance.Statistics.CompUpDatarateOverhead();
                    MuleApplication.Instance.Statistics.CompDownDatarateOverhead();
                }
                counter++;

                // one second
                if (counter >= 10)
                {
                    counter = 0;

                    // try to use different time intervals here to not create any disk-IO bottle necks by saving all files at once
                    MuleApplication.Instance.ClientCredits.Process();	// 13 minutes
                    MuleApplication.Instance.ServerList.Process();		// 17 minutes
                    MuleApplication.Instance.KnownFiles.Process();		// 11 minutes
                    MuleApplication.Instance.FriendList.Process();		// 19 minutes
                    MuleApplication.Instance.ClientList.Process();
                    MuleApplication.Instance.SharedFiles.Process();
                    if (MuleApplication.Instance.KadEngine.IsRunning)
                    {
                        MuleApplication.Instance.KadEngine.Process();
                        if (MuleApplication.Instance.KadEngine.Preference.HasLostConnection)
                        {
                            MuleApplication.Instance.KadEngine.Stop();
                        }
                    }
                    if (MuleApplication.Instance.ServerConnect.IsConnecting &&
                        !MuleApplication.Instance.ServerConnect.IsSingleConnect)
                        MuleApplication.Instance.ServerConnect.TryAnotherConnectionRequest();

                    MuleApplication.Instance.ListenSocket.UpdateConnectionsStatus();
                    if (MuleApplication.Instance.Preference.DoesWatchClipboard4ED2KLinks)
                    {
                        // TODO: Remove this from here. This has to be done with a clipboard chain
                        // and *not* with a timer!!
                    }

                    if (MuleApplication.Instance.ServerConnect.IsConnecting)
                        MuleApplication.Instance.ServerConnect.CheckForTimeout();

                    // 2 seconds
                    i2Secs++;
                    if (i2Secs >= 2)
                    {
                        i2Secs = 0;

                        // Update connection stats...
                        MuleApplication.Instance.Statistics.UpdateConnectionStats((float)MuleApplication.Instance.UploadQueue.DataRate / 1024, (float)MuleApplication.Instance.DownloadQueue.DataRate / 1024);

                    }

                    // display graphs
                    if (MuleApplication.Instance.Preference.TrafficOMeterInterval > 0)
                    {
                        igraph++;

                        if (igraph >= (uint)(MuleApplication.Instance.Preference.TrafficOMeterInterval))
                        {
                            igraph = 0;
                        }
                    }

                    MuleApplication.Instance.UploadQueue.UpdateDataRates();

                    //save rates every second
                    MuleApplication.Instance.Statistics.RecordRate();

                    bool gotEnoughHosts = MuleApplication.Instance.ClientList.GiveClientsForTraceRoute();
                    if (gotEnoughHosts == false)
                    {
                        MuleApplication.Instance.ServerList.GiveServersForTraceRoute();
                    }

                    sec++;
                    // *** 5 seconds **********************************************
                    if (sec >= 5)
                    {
                        sec = 0;
                        MuleApplication.Instance.ListenSocket.Process();
                        MuleApplication.Instance.OnlineSig(); // Added By Bouc7 

                        MuleApplication.Instance.Preference.EstimateMaxUploadCapability(MuleApplication.Instance.UploadQueue.DataRate / 1024);

                        if (!MuleApplication.Instance.Preference.DoesTransferFullChunks)
                            MuleApplication.Instance.UploadQueue.UpdateMaxClientScore();
                    }

                    statsave++;
                    // *** 60 seconds *********************************************
                    if (statsave >= 60)
                    {
                        statsave = 0;

                        MuleApplication.Instance.ServerConnect.KeepConnectionAlive();
                    }

                    s_uSaveStatistics++;
                    if (s_uSaveStatistics >= MuleApplication.Instance.Preference.StatsSaveInterval)
                    {
                        s_uSaveStatistics = 0;
                        MuleApplication.Instance.Statistics.Save();
                    }
                }
            }
            catch (Exception ex)
            {
                MpdUtilities.DebugLogError("UploadTimer error", ex);
            }
        }

        protected void HSUploadTimer(object state)
        {
            try
            {
                foreach (UpDownClient cur_client in MuleApplication.Instance.UploadQueue.UploadingList)
                {
                    if (cur_client.ClientSocket != null)
                        cur_client.CreateNextBlockPackage(true);
                }
            }
            catch (Exception ex)
            {
                MpdUtilities.DebugLogError("High Speed UploadTimer error", ex);
            }
        }
        #endregion

        #region Privates
        public void UpdateMaxClientScore()
        {
            m_imaxscore = 0;
            foreach (UpDownClient c in WaitingList)
            {
                uint score = c.GetScore(true, false);
                if (score > m_imaxscore)
                    m_imaxscore = score;
            }
        }

        private uint GetMaxClientScore() { return m_imaxscore; }

        private void UpdateActiveClientsInfo(uint curTick)
        {
            // Save number of active clients for statistics
            uint tempHighest =
                MuleApplication.Instance.UploadBandwidthThrottler.HighestNumberOfFullyActivatedSlotsSinceLastCallAndReset;

            if (tempHighest > (uint)UploadingList.Count + 1)
            {
                tempHighest = (uint)UploadingList.Count + 1;
            }

            m_iHighestNumberOfFullyActivatedSlotsSinceLastCall = tempHighest;

            // save some data about number of fully active clients
            uint tempMaxRemoved = 0;
            while (activeClients_tick_list.Count > 0 &&
                activeClients_list.Count > 0 &&
                curTick - activeClients_tick_list[0] > 20 * 1000)
            {
                activeClients_tick_list.RemoveAt(0);
                uint removed = (uint)activeClients_list[0];
                activeClients_list.RemoveAt(0);

                if (removed > tempMaxRemoved)
                {
                    tempMaxRemoved = removed;
                }
            }

            activeClients_list.Add((int)m_iHighestNumberOfFullyActivatedSlotsSinceLastCall);
            activeClients_tick_list.Add(curTick);

            if (activeClients_tick_list.Count > 1)
            {
                uint tempMaxActiveClients = m_iHighestNumberOfFullyActivatedSlotsSinceLastCall;
                uint tempMaxActiveClientsShortTime = m_iHighestNumberOfFullyActivatedSlotsSinceLastCall;
                int activeClientsTickPos = activeClients_tick_list.Count - 1;
                int activeClientsListPos = activeClients_list.Count - 1;

                while (activeClientsListPos >= 0 &&
                    (tempMaxRemoved > tempMaxActiveClients &&
                    tempMaxRemoved >= m_MaxActiveClients ||
                    curTick - activeClients_tick_list[activeClientsTickPos] < 10 * 1000))
                {
                    uint activeClientsTickSnapshot =
                        activeClients_tick_list[activeClientsTickPos];
                    uint activeClientsSnapshot =
                        (uint)activeClients_list[activeClientsListPos];

                    if (activeClientsSnapshot > tempMaxActiveClients)
                    {
                        tempMaxActiveClients = activeClientsSnapshot;
                    }

                    if (activeClientsSnapshot > tempMaxActiveClientsShortTime &&
                        curTick - activeClientsTickSnapshot < 10 * 1000)
                    {
                        tempMaxActiveClientsShortTime = activeClientsSnapshot;
                    }

                    activeClientsTickPos--;
                    activeClientsListPos--;
                }

                if (tempMaxRemoved >= m_MaxActiveClients ||
                    tempMaxActiveClients > m_MaxActiveClients)
                {
                    m_MaxActiveClients = tempMaxActiveClients;
                }

                m_MaxActiveClientsShortTime = tempMaxActiveClientsShortTime;
            }
            else
            {
                m_MaxActiveClients = m_iHighestNumberOfFullyActivatedSlotsSinceLastCall;
                m_MaxActiveClientsShortTime = m_iHighestNumberOfFullyActivatedSlotsSinceLastCall;
            }
        }

        private void InsertInUploadingList(UpDownClient newclient)
        {
            //Lets make sure any client that is added to the list has this flag reset!
            newclient.DoesAddNextConnect = false;
            // Add it last
            MuleApplication.Instance.UploadBandwidthThrottler.AddToStandardList((uint)UploadingList.Count,
                newclient.GetFileUploadSocket());
            UploadingList.Add(newclient);
            newclient.SlotNumber = (uint)UploadingList.Count;
        }

        private float GetAverageCombinedFilePrioAndCredit()
        {
            uint curTick = MpdUtilities.GetTickCount();

            if (curTick - m_dwLastCalculatedAverageCombinedFilePrioAndCredit > 5 * 1000)
            {
                m_dwLastCalculatedAverageCombinedFilePrioAndCredit = curTick;

                // TODO: is there a risk of overflow? I don't think so...
                double sum = 0;
                WaitingList.ForEach(cur_client =>
                {
                    sum += cur_client.CombinedFilePrioAndCredit;
                });
                m_fAverageCombinedFilePrioAndCredit = (float)(sum / WaitingList.Count);
            }

            return m_fAverageCombinedFilePrioAndCredit;
        }

        #endregion
    }
}
