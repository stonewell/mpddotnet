using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.File;
using Mpd.Generic.IO;
using System.IO;
using Mpd.Generic;
using Mule.AICH;
using Mpd.Utilities;
using Mule.Preference;
using System.Net;
using Mule.ED2K;
using Mule.Network;
using System.Diagnostics;
using System.Collections;

namespace Mule.Core.Impl
{
    struct TransferredData
    {
        public TransferredData(uint datalen, uint timestamp)
        {
            this.datalen = datalen;
            this.timestamp = timestamp;
        }

        public uint datalen;
        public uint timestamp;
    };

    class DownloadQueueImpl : DownloadQueue
    {
        #region Fields
        private List<PartFile> filelist_ = new List<PartFile>();
        private List<PartFile> localServerReqQueue_ = new List<PartFile>();

        private PartFile lastfile_;
        private uint lastcheckdiskspacetime_;
        private uint lastudpsearchtime_;
        private uint lastudpstattime_;
        private uint udcounter_;
        private uint requestsSentToServer_;
        private uint nextTCPSrcReq_;
        private int searchedServers_;
        private uint lastkademliafilerequest_;

        private ulong datarateMS_;

        // By BadWolf - Accurate Speed Measurement
        private List<TransferredData> avarage_dr_list_ = new List<TransferredData>();
        // END By BadWolf - Accurate Speed Measurement

        private uint lastA4AFtime_; // ZZ:DownloadManager

        private SourceHostnameResolver sourceHostnameResolver_;
        #endregion

        #region Constructors
        public DownloadQueueImpl()
        {
            DataRate = 0;
            CurrentUDPServer = null;
            lastfile_ = null;
            lastcheckdiskspacetime_ = 0;
            lastudpsearchtime_ = 0;
            lastudpstattime_ = 0;
            SetLastKademliaFileRequest();
            udcounter_ = 0;
            searchedServers_ = 0;
            datarateMS_ = 0;
            UDPFileReasks = 0;
            FailedUDPFileReasks = 0;
            nextTCPSrcReq_ = 0;
            requestsSentToServer_ = 0;
            lastA4AFtime_ = 0; // ZZ:DownloadManager
        }
        #endregion

        #region DownloadQueue Members

        public void Process()
        {
            ProcessLocalRequests(); // send src requests to local server

            uint downspeed = 0;
            ulong maxDownload = MuleApplication.Instance.Preference.GetMaxDownloadInBytesPerSec(true);
            if (maxDownload != MuleConstants.UNLIMITED * 1024 && DataRate > 1500)
            {
                downspeed = (uint)((maxDownload * 100) / (DataRate + 1));
                if (downspeed < 50)
                    downspeed = 50;
                else if (downspeed > 200)
                    downspeed = 200;
            }

            while (avarage_dr_list_.Count > 0 &&
                (MpdUtilities.GetTickCount() - avarage_dr_list_[0].timestamp > 10 * 1000))
            {
                datarateMS_ -= avarage_dr_list_[0].datalen;
                avarage_dr_list_.RemoveAt(0);
            }

            if (avarage_dr_list_.Count > 1)
            {
                DataRate = (uint)(datarateMS_ / (ulong)avarage_dr_list_.Count);
            }
            else
            {
                DataRate = 0;
            }

            uint datarateX = 0;
            udcounter_++;

            MuleApplication.Instance.Statistics.GlobalDone = 0;
            MuleApplication.Instance.Statistics.GlobalSize = 0;
            MuleApplication.Instance.Statistics.OverallStatus = 0;
            //filelist is already sorted by prio, therefore I removed all the extra loops.
            foreach (PartFile cur_file in filelist_)
            {
                // maintain global download stats
                MuleApplication.Instance.Statistics.GlobalDone += (ulong)cur_file.CompletedSize;
                MuleApplication.Instance.Statistics.GlobalSize += (ulong)cur_file.FileSize;

                if (cur_file.TransferringSrcCount > 0)
                    MuleApplication.Instance.Statistics.OverallStatus |= (uint)TBPSTATES.STATE_DOWNLOADING;
                if (cur_file.Status == PartFileStatusEnum.PS_ERROR)
                    MuleApplication.Instance.Statistics.OverallStatus |= (uint)TBPSTATES.STATE_ERROROUS;


                if (cur_file.Status == PartFileStatusEnum.PS_READY ||
                    cur_file.Status == PartFileStatusEnum.PS_EMPTY)
                {
                    datarateX += cur_file.Process(downspeed, udcounter_);
                }
                else
                {
                    //This will make sure we don't keep old sources to paused and stoped files.
                    cur_file.StopPausedFile();
                }
            }

            TransferredData newitem = new TransferredData(datarateX, MpdUtilities.GetTickCount());
            avarage_dr_list_.Add(newitem);
            datarateMS_ += datarateX;

            if (udcounter_ == 5)
            {
                if (MuleApplication.Instance.ServerConnect.IsUDPSocketAvailable)
                {
                    if ((lastudpstattime_ == 0) ||
                        (MpdUtilities.GetTickCount() - lastudpstattime_) > MuleConstants.UDPSERVERSTATTIME)
                    {
                        lastudpstattime_ = MpdUtilities.GetTickCount();
                        MuleApplication.Instance.ServerList.ServerStats();
                    }
                }
            }

            if (udcounter_ == 10)
            {
                udcounter_ = 0;
                if (MuleApplication.Instance.ServerConnect.IsUDPSocketAvailable)
                {
                    if ((lastudpsearchtime_ == 0) ||
                        (MpdUtilities.GetTickCount() - lastudpsearchtime_) > MuleConstants.UDPSERVERREASKTIME)
                        SendNextUDPPacket();
                }
            }

            CheckDiskspaceTimed();

            // ZZ:DownloadManager -.
            if ((lastA4AFtime_ == 0) || (MpdUtilities.GetTickCount() - lastA4AFtime_) > MuleConstants.ONE_MIN_MS * 8)
            {
                MuleApplication.Instance.ClientList.ProcessA4AFClients();
                lastA4AFtime_ = MpdUtilities.GetTickCount();
            }
            // <-- ZZ:DownloadManager
        }

        public void Init()
        {
            // find all part files, read & hash them if needed and store into a list
            int count = 0;

            for (int i = 0; i < MuleApplication.Instance.Preference.TempDirCount; i++)
            {
                string searchPath = MuleApplication.Instance.Preference.GetTempDir(i);

                string[] files = System.IO.Directory.GetFiles(searchPath, "*.part.met");

                //check all part.met files
                foreach (string filename in files)
                {
                    if (!filename.ToLower().EndsWith(".part.met"))
                        continue;

                    PartFile toadd =
                        MuleApplication.Instance.FileObjectManager.CreatePartFile();
                    PartFileLoadResultEnum eResult =
                        toadd.LoadPartFile(Path.GetDirectoryName(filename),
                            Path.GetFileName(filename));

                    if (eResult == PartFileLoadResultEnum.PLR_FAILED_METFILE_CORRUPT)
                    {
                        // .met file is corrupted, try to load the latest backup of this file
                        toadd =
                            MuleApplication.Instance.FileObjectManager.CreatePartFile();

                        eResult = toadd.LoadPartFile(Path.GetDirectoryName(filename),
                            Path.GetFileName(filename) + ".backup");

                        if (eResult == PartFileLoadResultEnum.PLR_LOADSUCCESS)
                        {
                            toadd.SavePartFile(true); // don't override our just used .bak file yet
                        }
                    }

                    if (eResult == PartFileLoadResultEnum.PLR_LOADSUCCESS)
                    {
                        count++;
                        filelist_.Add(toadd);			// to downloadqueue
                        if (toadd.GetStatus(true) == PartFileStatusEnum.PS_READY)
                            MuleApplication.Instance.SharedFiles.SafeAddKFile(toadd); // part files are always shared files
                    }
                }

                //try recovering any part.met files
                files = System.IO.Directory.GetFiles(searchPath, "*.part.met.backup");

                foreach (string filename in files)
                {
                    if (!filename.ToLower().EndsWith(".part.met.backup"))
                        continue;
                    PartFile toadd =
                        MuleApplication.Instance.FileObjectManager.CreatePartFile();
                    if (toadd.LoadPartFile(Path.GetDirectoryName(filename),
                            Path.GetFileName(filename)) == PartFileLoadResultEnum.PLR_LOADSUCCESS)
                    {
                        toadd.SavePartFile(true); // resave backup, don't overwrite existing bak files yet
                        count++;
                        filelist_.Add(toadd);			// to downloadqueue
                        if (toadd.GetStatus(true) == PartFileStatusEnum.PS_READY)
                            MuleApplication.Instance.SharedFiles.SafeAddKFile(toadd); // part files are always shared files
                    }
                }
            }
            if (count >= 0)
            {
                SortByPriority();
                CheckDiskspace();
            }

            sourceHostnameResolver_ =
                MuleApplication.Instance.CoreObjectManager.CreateSourceHostnameResolver();
            ExportPartMetFilesOverview();
        }

        public void AddPartFilesToShare()
        {
            filelist_.ForEach(cur =>
                {
                    if (cur.GetStatus(true) == PartFileStatusEnum.PS_READY)
                        MuleApplication.Instance.SharedFiles.SafeAddKFile(cur, true);
                }
            );
        }

        public void AddDownload(PartFile newfile, bool paused)
        {
            // Barry - Add in paused mode if required
            if (paused)
                newfile.PauseFile();

            SetAutoCat(newfile);// HoaX_69 / Slugfiller: AutoCat

            filelist_.Add(newfile);
            SortByPriority();
            CheckDiskspace();
            ExportPartMetFilesOverview();
        }

        public void AddSearchToDownload(SearchFile toadd)
        {
            AddSearchToDownload(toadd, 2, 0);
        }

        public void AddSearchToDownload(SearchFile toadd, byte paused)
        {
            AddSearchToDownload(toadd, paused, 0);
        }

        public void AddSearchToDownload(SearchFile toadd, byte paused, int cat)
        {
            if (toadd.FileSize == (ulong)0 || IsFileExisting(toadd.FileHash))
                return;

            if (toadd.FileSize > MuleConstants.OLD_MAX_EMULE_FILE_SIZE &&
                !MuleApplication.Instance.Preference.CanFSHandleLargeFiles(cat))
            {
                return;
            }

            PartFile newfile = MuleApplication.Instance.FileObjectManager.CreatePartFile(toadd, cat);
            if (newfile.Status == PartFileStatusEnum.PS_ERROR)
            {
                return;
            }

            if (paused == 2)
                paused = (byte)MuleApplication.Instance.Preference.AddNewFilesPaused();

            AddDownload(newfile, (paused == 1));

            // If the search result is from OP_GLOBSEARCHRES there may also be a source
            if (toadd.ClientID != 0 && toadd.ClientPort != 0)
            {
                SafeMemFile sources = MpdObjectManager.CreateSafeMemFile(1 + 4 + 2);
                try
                {
                    sources.WriteUInt8(1);
                    sources.WriteUInt32(toadd.ClientID);
                    sources.WriteUInt16(toadd.ClientPort);
                    sources.SeekToBegin();
                    newfile.AddSources(sources, toadd.ClientServerIP,
                        toadd.ClientServerPort, false);
                }
                catch
                {

                }
            }

            // Add more sources which were found via global UDP search
            List<SearchClient> aClients = toadd.SearchClients;
            for (int i = 0; i < aClients.Count; i++)
            {
                SafeMemFile sources = MpdObjectManager.CreateSafeMemFile(1 + 4 + 2);
                try
                {
                    sources.WriteUInt8(1);
                    sources.WriteUInt32(aClients[i].IP);
                    sources.WriteUInt16(aClients[i].Port);
                    sources.SeekToBegin();
                    newfile.AddSources(sources, aClients[i].ServerIP,
                        aClients[i].ServerPort, false);
                }
                catch
                {
                    break;
                }
            }
        }

        public void AddSearchToDownload(string link)
        {
            AddSearchToDownload(link, 2, 0);
        }

        public void AddSearchToDownload(string link, byte paused)
        {
            AddSearchToDownload(link, paused, 0);
        }

        public void AddSearchToDownload(string link, byte paused, int cat)
        {
            PartFile newfile = MuleApplication.Instance.FileObjectManager.CreatePartFile(link, cat);
            if (newfile.Status == PartFileStatusEnum.PS_ERROR)
            {
                return;
            }

            if (paused == 2)
                paused = (byte)MuleApplication.Instance.Preference.AddNewFilesPaused();
            AddDownload(newfile, (paused == 1));
        }

        public void AddFileLinkToDownload(Mule.ED2K.ED2KFileLink pLink)
        {
            AddFileLinkToDownload(pLink, 0);
        }

        public void AddFileLinkToDownload(Mule.ED2K.ED2KFileLink pLink, int cat)
        {
            PartFile newfile = MuleApplication.Instance.FileObjectManager.CreatePartFile(pLink, cat);
            if (newfile.Status == PartFileStatusEnum.PS_ERROR)
            {
                newfile = null;
            }
            else
            {
                AddDownload(newfile, MuleApplication.Instance.Preference.AddNewFilesPaused() != 0);
            }

            PartFile partfile = newfile;
            if (partfile == null)
                partfile = GetFileByID(pLink.HashKey);
            if (partfile != null)
            {
                // match the fileidentifier and only if the are the same add possible sources
                FileIdentifier tmpFileIdent =
                    MuleApplication.Instance.FileObjectManager.CreateFileIdentifier(pLink.HashKey,
                    pLink.Size, pLink.AICHHash, pLink.HasValidAICHHash);
                if (partfile.FileIdentifier.CompareRelaxed(tmpFileIdent))
                {
                    if (pLink.HasValidSources)
                        partfile.AddClientSources(pLink.SourcesList, 1, false);
                    if (!partfile.FileIdentifier.HasAICHHash && tmpFileIdent.HasAICHHash)
                    {
                        partfile.FileIdentifier.AICHHash = tmpFileIdent.AICHHash;
                        partfile.AICHRecoveryHashSet.SetMasterHash(tmpFileIdent.AICHHash,
                            AICHStatusEnum.AICH_VERIFIED);
                        partfile.AICHRecoveryHashSet.FreeHashSet();

                    }
                }
            }

            if (pLink.HasHostnameSources)
            {
                pLink.HostnameSourcesList.ForEach(pUnresHost =>
                {
                    sourceHostnameResolver_.AddToResolve(pLink.HashKey, pUnresHost.HostName, pUnresHost.Port, pUnresHost.Url);
                });
            }

        }

        public void RemoveFile(PartFile toremove)
        {
            RemoveLocalServerRequest(toremove);

            foreach (PartFile cur_file in filelist_)
            {
                if (toremove == cur_file)
                {
                    filelist_.Remove(cur_file);
                    break;
                }
            }

            SortByPriority();
            CheckDiskspace();
            ExportPartMetFilesOverview();
        }

        public void DeleteAll()
        {
            filelist_.ForEach(cur_file =>
            {
                cur_file.SourceList.Clear();
                // Barry - Should also remove all requested blocks
                // Don't worry about deleting the blocks, that gets handled 
                // when CUpDownClient is deleted in CClientList::DeleteAll()
                cur_file.RemoveAllRequestedBlocks();
            });
        }

        public int FileCount
        {
            get { return filelist_.Count; }
        }

        public uint DownloadingFileCount
        {
            get
            {
                uint result = 0;
                foreach (PartFile cur_file in filelist_)
                {
                    if (cur_file.Status == PartFileStatusEnum.PS_READY ||
                        cur_file.Status == PartFileStatusEnum.PS_EMPTY)
                        result++;
                }
                return result;
            }
        }

        public uint PausedFileCount
        {
            get
            {
                uint result = 0;
                foreach (PartFile cur_file in filelist_)
                {
                    if (cur_file.Status == PartFileStatusEnum.PS_PAUSED)
                        result++;
                }
                return result;
            }
        }

        public bool IsFileExisting(byte[] fileid)
        {
            return IsFileExisting(fileid, true);
        }

        public bool IsFileExisting(byte[] fileid, bool bLogWarnings)
        {
            KnownFile file = MuleApplication.Instance.SharedFiles.GetFileByID(fileid);
            if (file != null)
            {
                return true;
            }
            else if ((file = GetFileByID(fileid)) != null)
            {
                return true;
            }
            return false;
        }

        public bool IsPartFile(KnownFile file)
        {
            foreach (PartFile cur_file in filelist_)
                if (cur_file == file)
                    return true;

            return false;
        }

        public PartFile GetFileByID(byte[] filehash)
        {
            foreach (PartFile cur_file in filelist_)
            {
                if (MpdUtilities.Md4Cmp(filehash, cur_file.FileHash) == 0)
                    return cur_file;
            }
            return null;
        }

        public PartFile GetFileByIndex(int index)
        {
            if (index < filelist_.Count && index >= 0)
                return filelist_[index];

            return null;
        }

        public PartFile GetFileByKadFileSearchID(uint id)
        {
            foreach (PartFile cur_file in filelist_)
            {
                if (id == cur_file.KadFileSearchID)
                    return cur_file;
            }
            return null;
        }

        public void StartNextFileIfPrefs(int cat)
        {
            if (MuleApplication.Instance.Preference.StartNextFile() > 0)
                StartNextFile((MuleApplication.Instance.Preference.StartNextFile() > 1 ? cat : -1),
                    (MuleApplication.Instance.Preference.StartNextFile() != 3));
        }

        public void StartNextFile()
        {
            StartNextFile(-1, false);
        }

        public void StartNextFile(int cat)
        {
            StartNextFile(cat, false);
        }

        public void StartNextFile(int cat, bool force)
        {
            PartFile pfile = null;

            if (cat != -1)
            {
                // try to find in specified category
                foreach (PartFile cur_file in filelist_)
                {
                    if (cur_file.Status == PartFileStatusEnum.PS_PAUSED &&
                        (
                         cur_file.Category == (uint)cat ||
                         cat == 0 &&
                         MuleApplication.Instance.Preference.GetCategory(0).Filter == 0 &&
                         cur_file.Category > 0
                        ) &&
                        cur_file.RightFileHasHigherPrio(pfile, cur_file)
                       )
                    {
                        pfile = cur_file;
                    }
                }
                if (pfile == null && !force)
                    return;
            }

            if (cat == -1 || pfile == null && force)
            {
                foreach (PartFile cur_file in filelist_)
                {
                    if (cur_file.Status == PartFileStatusEnum.PS_PAUSED &&
                        cur_file.RightFileHasHigherPrio(pfile, cur_file))
                    {
                        // pick first found matching file, since they are sorted in prio order with most important file first.
                        pfile = cur_file;
                    }
                }
            }
            if (pfile != null) pfile.ResumeFile();
        }

        public void RefilterAllComments()
        {
            filelist_.ForEach(cur_file =>
            {
                cur_file.RefilterFileComments();
            });
        }

        public UpDownClient GetDownloadClientByIP(uint dwIP)
        {
            foreach (PartFile cur_file in filelist_)
            {
                foreach (UpDownClient cur_client in cur_file.SourceList)
                {
                    if (dwIP == cur_client.IP)
                    {
                        return cur_client;
                    }
                }
            }
            return null;
        }

        public UpDownClient GetDownloadClientByIP_UDP(uint dwIP, ushort nUDPPort, bool bIgnorePortOnUniqueIP)
        {
            bool p = false;

            return GetDownloadClientByIP_UDP(dwIP, nUDPPort, bIgnorePortOnUniqueIP, ref p);
        }

        public UpDownClient GetDownloadClientByIP_UDP(uint dwIP, ushort nUDPPort, bool bIgnorePortOnUniqueIP, ref bool pbMultipleIPs)
        {
            UpDownClient pMatchingIPClient = null;
            uint cMatches = 0;

            foreach (PartFile cur_file in filelist_)
            {
                foreach (UpDownClient cur_client in cur_file.SourceList)
                {
                    if (dwIP == cur_client.IP && nUDPPort == cur_client.UDPPort)
                    {
                        return cur_client;
                    }
                    else if (dwIP == cur_client.IP && bIgnorePortOnUniqueIP && cur_client != pMatchingIPClient)
                    {
                        pMatchingIPClient = cur_client;
                        cMatches++;
                    }
                }
            }

            pbMultipleIPs = cMatches > 1;

            if (pMatchingIPClient != null && cMatches == 1)
                return pMatchingIPClient;
            else
                return null;
        }

        public bool IsInList(UpDownClient client)
        {
            foreach (PartFile cur_file in filelist_)
            {
                foreach (UpDownClient cur_client in cur_file.SourceList)
                {
                    if (cur_client == client)
                        return true;
                }
            }
            return false;
        }

        public bool CheckAndAddSource(PartFile sender, UpDownClient source)
        {
            if (sender.IsStopped)
            {

                return false;
            }

            if (source.HasValidHash)
            {
                if (0 == MpdUtilities.Md4Cmp(source.UserHash, MuleApplication.Instance.Preference.UserHash))
                {
                    return false;
                }
            }
            // filter sources which are known to be temporarily dead/useless
            if (MuleApplication.Instance.ClientList.DeadSourceList.IsDeadSource(source) ||
                sender.DeadSourceList.IsDeadSource(source))
            {
                return false;
            }

            // filter sources which are incompatible with our encryption setting (one requires it, and the other one doesn't supports it)
            if ((source.RequiresCryptLayer &&
                (!MuleApplication.Instance.Preference.IsClientCryptLayerSupported ||
                !source.HasValidHash)) ||
                (MuleApplication.Instance.Preference.IsClientCryptLayerRequired &&
                (!source.SupportsCryptLayer || !source.HasValidHash)))
            {

                return false;
            }

            // "Filter LAN IPs" and/or "IPfilter" is not required here, because it was already done in parent functions

            // uses this only for temp. clients
            foreach (PartFile cur_file in filelist_)
            {
                foreach (UpDownClient cur_client in cur_file.SourceList)
                {
                    if (cur_client.Compare(source, true) || cur_client.Compare(source, false))
                    {
                        if (cur_file == sender)
                        { // this file has already this source

                            return false;
                        }
                        // set request for this source
                        if (cur_client.AddRequestForAnotherFile(sender))
                        {

                            if (cur_client.DownloadState != DownloadStateEnum.DS_CONNECTED)
                            {
                                cur_client.SwapToAnotherFile(("New A4AF source found. CDownloadQueue::CheckAndAddSource()"), false, false, false, null, true, false); // ZZ:DownloadManager
                            }
                            return false;
                        }
                        else
                        {

                            return false;
                        }
                    }
                }
            }
            //our new source is real new but maybe it is already uploading to us?
            //if yes the known client will be attached to the var "source"
            //and the old sourceclient will be deleted
            if (MuleApplication.Instance.ClientList.AttachToAlreadyKnown(out source, null))
            {
                source.RequestFile = sender;
            }
            else
            {
                // here we know that the client instance 'source' is a new created client instance (see callers) 
                // which is therefor not already in the clientlist, we can avoid the check for duplicate client list entries 
                // when adding this client
                MuleApplication.Instance.ClientList.AddClient(source, true);
            }

            sender.SourceList.Add(source);
            return true;
        }

        public bool CheckAndAddKnownSource(PartFile sender, UpDownClient source)
        {
            return CheckAndAddKnownSource(sender, source, false);
        }

        public bool CheckAndAddKnownSource(PartFile sender, UpDownClient source, bool bIgnoreGlobDeadList)
        {
            if (sender.IsStopped)
                return false;

            // filter sources which are known to be temporarily dead/useless
            if ((MuleApplication.Instance.ClientList.DeadSourceList.IsDeadSource(source) &&
                !bIgnoreGlobDeadList) ||
                sender.DeadSourceList.IsDeadSource(source))
            {
                //if (MuleApplication.Instance.Preference.GetLogFilteredIPs())
                //	AddDebugLogLine(DLP_DEFAULT, false, ("Rejected source because it was found on the DeadSourcesList (%s) for file %s : %s")
                //	,sender.m_DeadSourceList.IsDeadSource(source)? ("Local") : ("Global"), sender.GetFileName(), source.DbgGetClientInfo() );
                return false;
            }

            // filter sources which are incompatible with our encryption setting (one requires it, and the other one doesn't supports it)
            if ((source.RequiresCryptLayer &&
                (!MuleApplication.Instance.Preference.IsClientCryptLayerSupported ||
                !source.HasValidHash)) ||
                (MuleApplication.Instance.Preference.IsClientCryptLayerRequired &&
                (!source.SupportsCryptLayer || !source.HasValidHash)))
            {
                return false;
            }

            // "Filter LAN IPs" -- this may be needed here in case we are connected to the internet and are also connected
            // to a LAN and some client from within the LAN connected to us. Though this situation may be supported in future
            // by adding that client to the source list and filtering that client's LAN IP when sending sources to
            // a client within the internet.
            //
            // "IPfilter" is not needed here, because that "known" client was already IPfiltered when receiving OP_HELLO.
            if (!source.HasLowID)
            {
                uint nClientIP = (uint)IPAddress.NetworkToHostOrder((long)source.UserIDHybrid);
                if (!MpdUtilities.IsGoodIP(nClientIP))
                { // check for 0-IP, localhost and LAN addresses
                    //if (MuleApplication.Instance.Preference.GetLogFilteredIPs())
                    //	AddDebugLogLine(false, ("Ignored already known source with IP=%s"), ipstr(nClientIP));
                    return false;
                }
            }

            // use this for client which are already know (downloading for example)
            foreach (PartFile cur_file in filelist_)
            {
                if (cur_file.SourceList.Contains(source))
                {
                    if (cur_file == sender)
                        return false;
                    if (source.AddRequestForAnotherFile(sender))
                        if (source.DownloadState != DownloadStateEnum.DS_CONNECTED)
                        {
                            source.SwapToAnotherFile(
                                "New A4AF source found. CDownloadQueue::CheckAndAddKnownSource()",
                                false, false, false, null, true, false); // ZZ:DownloadManager
                        }
                    return false;
                }
            }

            source.RequestFile = sender;
            sender.SourceList.Add(source);
            source.SourceFrom = SourceFromEnum.SF_PASSIVE;
            //UpdateDisplayedInfo();
            return true;
        }

        public bool RemoveSource(UpDownClient toremove)
        {
            return RemoveSource(toremove, true);
        }

        public bool RemoveSource(UpDownClient toremove, bool bDoStatsUpdate)
        {
            bool bRemovedSrcFromPartFile = false;
            foreach (PartFile cur_file in filelist_)
            {
                foreach (UpDownClient src in cur_file.SourceList)
                {
                    if (toremove == src)
                    {
                        cur_file.SourceList.Remove(src);
                        bRemovedSrcFromPartFile = true;
                        if (bDoStatsUpdate)
                        {
                            cur_file.RemoveDownloadingSource(toremove);
                            cur_file.UpdatePartsInfo();
                        }
                        break;
                    }
                }
                if (bDoStatsUpdate)
                    cur_file.UpdateAvailablePartsCount();
            }

            // remove this source on all files in the downloadqueue who link this source
            // pretty slow but no way arround, maybe using a Map is better, but that's slower on other parts
            int i = 0;
            while (i < toremove.OtherRequestsList.Count)
            {
                if (toremove.OtherRequestsList[i].A4AFSourceList.Contains(toremove))
                {
                    toremove.OtherRequestsList[i].A4AFSourceList.Remove(toremove);
                    toremove.OtherRequestsList.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
            i = 0;

            while (i < toremove.OtherNoNeededList.Count)
            {
                if (toremove.OtherNoNeededList[i].A4AFSourceList.Contains(toremove))
                {
                    toremove.OtherNoNeededList[i].A4AFSourceList.Remove(toremove);
                    toremove.OtherNoNeededList.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            if (bRemovedSrcFromPartFile &&
                (toremove.HasFileRating || toremove.FileComment.Length != 0))
            {
                PartFile pFile = toremove.RequestFile;
                if (pFile != null)
                    pFile.UpdateFileRatingCommentAvail();
            }

            toremove.DownloadState = DownloadStateEnum.DS_NONE;
            toremove.RequestFile = null;
            return bRemovedSrcFromPartFile;
        }

        public void GetDownloadSourcesStats(DownloadStatsStruct results)
        {
            foreach (PartFile cur_file in filelist_)
            {
                results.a[0] += (int)cur_file.SourceCount;
                results.a[1] += (int)cur_file.TransferringSrcCount;
                results.a[2] += (int)cur_file.GetSrcStatisticsValue(DownloadStateEnum.DS_ONQUEUE);
                results.a[3] += (int)cur_file.GetSrcStatisticsValue(DownloadStateEnum.DS_REMOTEQUEUEFULL);
                results.a[4] += (int)cur_file.GetSrcStatisticsValue(DownloadStateEnum.DS_NONEEDEDPARTS);
                results.a[5] += (int)cur_file.GetSrcStatisticsValue(DownloadStateEnum.DS_CONNECTED);
                results.a[6] += (int)cur_file.GetSrcStatisticsValue(DownloadStateEnum.DS_REQHASHSET);
                results.a[7] += (int)cur_file.GetSrcStatisticsValue(DownloadStateEnum.DS_CONNECTING);
                results.a[8] += (int)cur_file.GetSrcStatisticsValue(DownloadStateEnum.DS_WAITCALLBACK);
                results.a[8] += (int)cur_file.GetSrcStatisticsValue(DownloadStateEnum.DS_WAITCALLBACKKAD);
                results.a[9] += (int)cur_file.GetSrcStatisticsValue(DownloadStateEnum.DS_TOOMANYCONNS);
                results.a[9] += (int)cur_file.GetSrcStatisticsValue(DownloadStateEnum.DS_TOOMANYCONNSKAD);
                results.a[10] += (int)cur_file.GetSrcStatisticsValue(DownloadStateEnum.DS_LOWTOLOWIP);
                results.a[11] += (int)cur_file.GetSrcStatisticsValue(DownloadStateEnum.DS_NONE);
                results.a[12] += (int)cur_file.GetSrcStatisticsValue(DownloadStateEnum.DS_ERROR);
                results.a[13] += (int)cur_file.GetSrcStatisticsValue(DownloadStateEnum.DS_BANNED);
                results.a[14] += (int)cur_file.SourceStates[3];
                results.a[15] += (int)cur_file.SrcA4AFCount;
                results.a[16] += (int)cur_file.SourceStates[0];
                results.a[17] += (int)cur_file.SourceStates[1];
                results.a[18] += (int)cur_file.SourceStates[2];
                results.a[19] += (int)cur_file.NetStates[0];
                results.a[20] += (int)cur_file.NetStates[1];
                results.a[21] += (int)cur_file.NetStates[2];
                results.a[22] += (int)cur_file.DeadSourceList.DeadSourcesCount;
            }
        }

        public int GetDownloadFilesStats(ref ulong ui64TotalFileSize,
            ref ulong ui64TotalLeftToTransfer, ref ulong ui64TotalAdditionalNeededSpace)
        {
            int iActiveFiles = 0;
            foreach (PartFile cur_file in filelist_)
            {
                if (cur_file.Status == PartFileStatusEnum.PS_READY ||
                    cur_file.Status == PartFileStatusEnum.PS_EMPTY)
                {
                    ulong ui64LeftToTransfer = 0;
                    ulong ui64AdditionalNeededSpace = 0;
                    cur_file.GetLeftToTransferAndAdditionalNeededSpace(ref ui64LeftToTransfer,
                        ref ui64AdditionalNeededSpace);
                    ui64TotalFileSize += cur_file.FileSize;
                    ui64TotalLeftToTransfer += ui64LeftToTransfer;
                    ui64TotalAdditionalNeededSpace += ui64AdditionalNeededSpace;
                    iActiveFiles++;
                }
            }
            return iActiveFiles;
        }

        public uint DataRate
        {
            get;
            private set;
        }

        public void AddUDPFileReasks()
        {
            UDPFileReasks++;
        }

        public uint UDPFileReasks
        {
            get;
            private set;
        }

        public void AddFailedUDPFileReasks()
        {
            FailedUDPFileReasks++;
        }

        public uint FailedUDPFileReasks
        {
            get;
            private set;
        }

        public void ResetCatParts(int cat)
        {
            foreach (PartFile cur_file in filelist_)
            {
                if (cur_file.Category == cat)
                    cur_file.Category = 0;
                else if (cur_file.Category > cat)
                    cur_file.Category = cur_file.Category - 1;
            }
        }

        public void SetCatPrio(int cat, byte newprio)
        {
            foreach (PartFile cur_file in filelist_)
            {
                if (cat == 0 || cur_file.Category == cat)
                    if (newprio == (byte)PriorityEnum.PR_AUTO)
                    {
                        cur_file.IsAutoDownPriority = true;
                        cur_file.SetDownPriority(PriorityEnum.PR_HIGH, false);
                    }
                    else
                    {
                        cur_file.IsAutoDownPriority = (false);
                        cur_file.SetDownPriority((PriorityEnum)newprio, false);
                    }
            }

            MuleApplication.Instance.DownloadQueue.SortByPriority();
            MuleApplication.Instance.DownloadQueue.CheckDiskspaceTimed();
        }

        public void RemoveAutoPrioInCat(int cat, byte newprio)
        {
            foreach (PartFile cur_file in filelist_)
            {
                if (cur_file.IsAutoDownPriority && (cat == 0 || cur_file.Category == cat))
                {
                    cur_file.IsAutoDownPriority = false;
                    cur_file.SetDownPriority((PriorityEnum)newprio, false);
                }
            }

            MuleApplication.Instance.DownloadQueue.SortByPriority();
            MuleApplication.Instance.DownloadQueue.CheckDiskspaceTimed();
        }

        public void SetCatStatus(int cat, int newstatus)
        {
            bool reset = false;
            bool resort = false;

            List<PartFile>.Enumerator it = filelist_.GetEnumerator();
            while (it.MoveNext())
            {
                PartFile cur_file = it.Current;
                if (cur_file == null)
                    continue;

                if (cat == -1 ||
                    (cat == -2 && cur_file.Category == 0) ||
                    (cat == 0 && cur_file.CheckShowItemInGivenCat(cat)) ||
                    (cat > 0 && cat == cur_file.Category))
                {
                    switch ((CategoryStatusEnum)newstatus)
                    {
                        case CategoryStatusEnum.MP_CANCEL:
                            cur_file.DeleteFile();
                            reset = true;
                            break;
                        case CategoryStatusEnum.MP_PAUSE:
                            cur_file.PauseFile(false, false);
                            resort = true;
                            break;
                        case CategoryStatusEnum.MP_STOP:
                            cur_file.StopFile(false, false);
                            resort = true;
                            break;
                        case CategoryStatusEnum.MP_RESUME:
                            if (cur_file.CanResumeFile)
                            {
                                if (cur_file.Status == PartFileStatusEnum.PS_INSUFFICIENT)
                                    cur_file.ResumeFileInsufficient();
                                else
                                {
                                    cur_file.ResumeFile(false);
                                    resort = true;
                                }
                            }
                            break;
                    }
                }

                if (reset)
                {
                    reset = false;
                    it = filelist_.GetEnumerator();
                }
            }

            if (resort)
            {
                MuleApplication.Instance.DownloadQueue.SortByPriority();
                MuleApplication.Instance.DownloadQueue.CheckDiskspace();
            }
        }

        public void MoveCat(uint from, uint to)
        {
            if (from < to)
                --to;

            List<PartFile>.Enumerator it = filelist_.GetEnumerator();
            while (it.MoveNext())
            {
                PartFile cur_file = it.Current;
                if (cur_file == null)
                    continue;

                uint mycat = cur_file.Category;
                if ((mycat >= Math.Min(from, to) && mycat <= Math.Max(from, to)))
                {
                    //if ((from<to && (mycat<from || mycat>to)) || (from>to && (mycat>from || mycat<to)) )	continue; //not affected

                    if (mycat == from)
                        cur_file.Category = to;
                    else
                    {
                        if (from < to)
                            cur_file.Category = mycat - 1;
                        else
                            cur_file.Category = mycat + 1;
                    }
                }
            }
        }

        public void SetAutoCat(PartFile newfile)
        {
            if (MuleApplication.Instance.Preference.CategoryCount == 1)
                return;
            string catExt;

            for (int ix = 1; ix < MuleApplication.Instance.Preference.CategoryCount; ix++)
            {
                catExt = MuleApplication.Instance.Preference.GetCategory(ix).AutoCategory;
                if (string.IsNullOrEmpty(catExt))
                    continue;

                if (!MuleApplication.Instance.Preference.GetCategory(ix).RegExpEval)
                {
                    // simple string comparison

                    catExt.ToLower();

                    string fullname = newfile.FileName;
                    fullname.ToLower();
                    string[] cmpExts = catExt.Split('|');

                    foreach (string cmpExt in cmpExts)
                    {
                        if (!string.IsNullOrEmpty(cmpExt))
                        {
                            // HoaX_69: Allow wildcards in autocat string
                            //  thanks to: bluecow, khaos and SlugFiller
                            if (cmpExt.IndexOf("*") != -1 || cmpExt.IndexOf("?") != -1)
                            {
                                // Use wildcards
                                if (MpdUtilities.PathMatchSpec(fullname, cmpExt))
                                {
                                    newfile.Category = (uint)ix;
                                    return;
                                }
                            }
                            else
                            {
                                if (fullname.IndexOf(cmpExt) != -1)
                                {
                                    newfile.Category = (uint)ix;
                                    return;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // regular expression evaluation
                    if (MpdUtilities.RegularExpressionMatch(catExt, newfile.FileName))
                        newfile.Category = (uint)ix;
                }
            }
        }

        public void SendLocalSrcRequest(PartFile sender)
        {
            if (localServerReqQueue_.Contains(sender))
                return;
            localServerReqQueue_.Add(sender);
        }

        public void RemoveLocalServerRequest(PartFile pFile)
        {
            int pos = 0;

            while (pos < localServerReqQueue_.Count)
            {
                if (localServerReqQueue_[pos] == pFile)
                {
                    localServerReqQueue_.RemoveAt(pos);
                    pFile.LocalSrcReqQueued = false;
                    // could 'break' here. fail safe: go through entire list.
                }
                else
                {
                    pos++;
                }
            }
        }

        public void ResetLocalServerRequests()
        {
            nextTCPSrcReq_ = 0;
            localServerReqQueue_.Clear();

            foreach (PartFile pFile in filelist_)
            {
                if (pFile.Status == PartFileStatusEnum.PS_READY ||
                    pFile.Status == PartFileStatusEnum.PS_EMPTY)
                    pFile.ResumeFile();
                pFile.LocalSrcReqQueued = false;
            }
        }

        public void SetLastKademliaFileRequest()
        {
            lastkademliafilerequest_ = MpdUtilities.GetTickCount();
        }

        public bool DoKademliaFileRequest()
        {
            return ((MpdUtilities.GetTickCount() - lastkademliafilerequest_) > MuleConstants.KADEMLIAASKTIME);
        }

        public void KademliaSearchFile(uint searchID,
            Mpd.Generic.UInt128 pcontactID,
            Mpd.Generic.UInt128 pkadID,
            byte type, uint ip, ushort tcp,
            ushort udp, uint dwBuddyIP,
            ushort dwBuddyPort, byte byCryptOptions)
        {
            //Safty measure to make sure we are looking for these sources
            PartFile temp = GetFileByKadFileSearchID(searchID);
            if (temp == null)
                return;
            //Do we need more sources?
            if (!(!temp.IsStopped && temp.MaxSources > temp.SourceCount))
                return;

            uint ED2Kip = (uint)IPAddress.NetworkToHostOrder(ip);
            if (MuleApplication.Instance.IPFilter.IsFiltered(ED2Kip))
            {
                return;
            }
            if ((ip == MuleApplication.Instance.KadEngine.IPAddress ||
                ED2Kip == MuleApplication.Instance.ServerConnect.ClientID) &&
                tcp == MuleApplication.Instance.Preference.Port)
                return;
            UpDownClient ctemp = null;
            //DEBUG_ONLY( DebugLog(("Kadsource received, type %u, IP %s"), type, ipstr(ED2Kip)) );
            switch (type)
            {
                case 4:
                case 1:
                    {
                        //NonFirewalled users
                        if (tcp == 0)
                        {
                            return;
                        }
                        ctemp = MuleApplication.Instance.CoreObjectManager.CreateUpDownClient(temp, tcp, ip, 0, 0, false);
                        ctemp.SourceFrom = SourceFromEnum.SF_KADEMLIA;
                        // not actually sent or needed for HighID sources
                        //ctemp.SetServerIP(serverip);
                        //ctemp.SetServerPort(serverport);
                        ctemp.KadPort = udp;
                        ctemp.UserHash = pcontactID.Bytes;
                        break;
                    }
                case 2:
                    {
                        //Don't use this type... Some clients will process it wrong..
                        break;
                    }
                case 5:
                case 3:
                    {
                        //This will be a firewaled client connected to Kad only.
                        // if we are firewalled ourself, the source is useless to us
                        if (MuleApplication.Instance.IsFirewalled)
                            break;

                        //We set the clientID to 1 as a Kad user only has 1 buddy.
                        ctemp = MuleApplication.Instance.CoreObjectManager.CreateUpDownClient(temp, tcp, 1, 0, 0, false);
                        //The only reason we set the real IP is for when we get a callback
                        //from this firewalled source, the compare method will match them.
                        ctemp.SourceFrom = SourceFromEnum.SF_KADEMLIA;
                        ctemp.KadPort = udp;
                        ctemp.UserHash = pcontactID.Bytes;
                        ctemp.BuddyID = pkadID.Bytes;
                        ctemp.BuddyIP = dwBuddyIP;
                        ctemp.BuddyPort = dwBuddyPort;
                        break;
                    }
                case 6:
                    {
                        // firewalled source which supports direct udp callback
                        // if we are firewalled ourself, the source is useless to us
                        if (MuleApplication.Instance.IsFirewalled)
                            break;

                        if ((byCryptOptions & 0x08) == 0)
                        {
                            break;
                        }
                        ctemp = MuleApplication.Instance.CoreObjectManager.CreateUpDownClient(temp, tcp, 1, 0, 0, false);
                        ctemp.SourceFrom = SourceFromEnum.SF_KADEMLIA;
                        ctemp.KadPort = udp;
                        ctemp.IP = ED2Kip; // need to set the Ip address, which cannot be used for TCP but for UDP
                        ctemp.UserHash = pcontactID.Bytes;
                        break;
                    }
            }

            if (ctemp != null)
            {
                // add encryption settings
                ctemp.SetConnectOptions(byCryptOptions);
                CheckAndAddSource(temp, ctemp);
            }
        }

        public void StopUDPRequests()
        {
            CurrentUDPServer = null;
            lastudpsearchtime_ = MpdUtilities.GetTickCount();
            lastfile_ = null;
            searchedServers_ = 0;
        }

        public void SortByPriority()
        {
            uint n = (uint)filelist_.Count;
            if (n == 0)
                return;
            uint i;
            for (i = n / 2; i-- > 0; )
                HeapSort(i, n - 1);
            for (i = n; --i > 0; )
            {
                SwapParts(0, (int)i);
                HeapSort(0, i - 1);
            }
        }

        public void CheckDiskspace()
        {
            CheckDiskspace(false);
        }

        public void CheckDiskspace(bool bNotEnoughSpaceLeft)
        {
            lastcheckdiskspacetime_ = MpdUtilities.GetTickCount();

            // sorting the list could be done here, but I prefer to "see" that function call in the calling functions.
            //SortByPriority();

            // If disabled, resume any previously paused files
            if (!MuleApplication.Instance.Preference.IsCheckDiskspaceEnabled)
            {
                if (!bNotEnoughSpaceLeft) // avoid worse case, if we already had 'disk full'
                {
                    foreach (PartFile cur_file in filelist_)
                    {
                        switch (cur_file.Status)
                        {
                            case PartFileStatusEnum.PS_PAUSED:
                            case PartFileStatusEnum.PS_ERROR:
                            case PartFileStatusEnum.PS_COMPLETING:
                            case PartFileStatusEnum.PS_COMPLETE:
                                continue;
                        }
                        cur_file.ResumeFileInsufficient();
                    }
                }
                return;
            }

            ulong nTotalAvailableSpaceMain = bNotEnoughSpaceLeft ?
                0 : MpdUtilities.GetFreeDiskSpace(MuleApplication.Instance.Preference.GetTempDir());

            // 'bNotEnoughSpaceLeft' - avoid worse case, if we already had 'disk full'
            if (MuleApplication.Instance.Preference.MinFreeDiskSpace == 0)
            {
                foreach (PartFile cur_file in filelist_)
                {
                    ulong nTotalAvailableSpace = bNotEnoughSpaceLeft ? 0 :
                        ((MuleApplication.Instance.Preference.TempDirCount == 1) ?
                        nTotalAvailableSpaceMain : MpdUtilities.GetFreeDiskSpace(cur_file.TempPath));

                    switch (cur_file.Status)
                    {
                        case PartFileStatusEnum.PS_PAUSED:
                        case PartFileStatusEnum.PS_ERROR:
                        case PartFileStatusEnum.PS_COMPLETING:
                        case PartFileStatusEnum.PS_COMPLETE:
                            continue;
                    }

                    // Pause the file only if it would grow in size and would exceed the currently available free space
                    ulong nSpaceToGo = cur_file.NeededSpace;
                    if (nSpaceToGo <= nTotalAvailableSpace)
                    {
                        nTotalAvailableSpace -= nSpaceToGo;
                        cur_file.ResumeFileInsufficient();
                    }
                    else
                        cur_file.PauseFile(true/*bInsufficient*/);
                }
            }
            else
            {
                foreach (PartFile cur_file in filelist_)
                {
                    switch (cur_file.Status)
                    {
                        case PartFileStatusEnum.PS_PAUSED:
                        case PartFileStatusEnum.PS_ERROR:
                        case PartFileStatusEnum.PS_COMPLETING:
                        case PartFileStatusEnum.PS_COMPLETE:
                            continue;
                    }

                    ulong nTotalAvailableSpace = bNotEnoughSpaceLeft ? 0 :
                        ((MuleApplication.Instance.Preference.TempDirCount == 1) ?
                        nTotalAvailableSpaceMain : MpdUtilities.GetFreeDiskSpace(cur_file.TempPath));
                    if (nTotalAvailableSpace < MuleApplication.Instance.Preference.MinFreeDiskSpace)
                    {
                        if (cur_file.IsNormalFile)
                        {
                            // Normal files: pause the file only if it would still grow
                            ulong nSpaceToGrow = cur_file.NeededSpace;
                            if (nSpaceToGrow > 0)
                                cur_file.PauseFile(true/*bInsufficient*/);
                        }
                        else
                        {
                            // Compressed/sparse files: always pause the file
                            cur_file.PauseFile(true/*bInsufficient*/);
                        }
                    }
                    else
                    {
                        // doesn't work this way. resuming the file without checking if there is a chance to successfully
                        // flush any available buffered file data will pause the file right after it was resumed and disturb
                        // the StopPausedFile function.
                        //cur_file.ResumeFileInsufficient();
                    }
                }
            }
        }

        public void CheckDiskspaceTimed()
        {
            if ((lastcheckdiskspacetime_ == 0) ||
                (MpdUtilities.GetTickCount() - lastcheckdiskspacetime_) > MuleConstants.DISKSPACERECHECKTIME)
                CheckDiskspace();
        }

        public void ExportPartMetFilesOverview()
        {
            string strFileListPath =
                Path.Combine(MuleApplication.Instance.Preference.GetMuleDirectory(DefaultDirectoryEnum.EMULE_CONFIGDIR),
                "downloads.txt");

            string strTmpFileListPath =
                Path.Combine(MuleApplication.Instance.Preference.GetMuleDirectory(DefaultDirectoryEnum.EMULE_CONFIGDIR),
                    Path.GetFileNameWithoutExtension(strFileListPath) + ".tmp");

            SafeBufferedFile file =
                MpdObjectManager.CreateSafeBufferedFile(strTmpFileListPath, FileMode.Create, FileAccess.Write, FileShare.Write);

            // write Unicode byte-order mark 0xFEFF
            file.Write(new byte[] { 0xFE, 0xFF });

            try
            {
                file.WriteString(string.Format("Date:      {0}\r\n", DateTime.Now));
                if (MuleApplication.Instance.Preference.TempDirCount == 1)
                    file.WriteString(string.Format("Directory: {0}\r\n",
                        MuleApplication.Instance.Preference.GetTempDir()));
                file.WriteString(("\r\n"));
                file.WriteString(("Part file\teD2K link\r\n"));
                file.WriteString(("--------------------------------------------------------------------------------\r\n"));
                foreach (PartFile pPartFile in filelist_)
                {
                    if (pPartFile.GetStatus(true) != PartFileStatusEnum.PS_COMPLETE)
                    {
                        string strPartFilePath = pPartFile.FilePath;
                        if (MuleApplication.Instance.Preference.TempDirCount == 1)
                            file.WriteString(string.Format("{0}{1}\t{2}\r\n",
                                Path.GetFileNameWithoutExtension(strPartFilePath),
                                Path.GetExtension(strPartFilePath),
                                MuleApplication.Instance.ED2KObjectManager.CreateED2KLink(pPartFile)));
                        else
                            file.WriteString(string.Format("{0}\t{1}\r\n",
                                pPartFile.FullName,
                                MuleApplication.Instance.ED2KObjectManager.CreateED2KLink(pPartFile)));
                    }
                }

                if (MuleApplication.Instance.Preference.CommitFiles >= 2 ||
                    (MuleApplication.Instance.Preference.CommitFiles >= 1 && !
                    MuleApplication.Instance.IsRunning))
                {
                    file.Flush(); // flush file stream buffers to disk buffers
                }
                file.Close();

                string strBakFileListPath = strFileListPath;
                Path.Combine(Path.GetDirectoryName(strFileListPath),
                Path.GetFileNameWithoutExtension(strFileListPath) + ".bak");

                System.IO.File.Delete(strBakFileListPath);
                System.IO.File.Move(strFileListPath, strBakFileListPath);
                System.IO.File.Move(strTmpFileListPath, strFileListPath);
            }
            catch
            {
                file.Abort();
                System.IO.File.Delete(strTmpFileListPath);
            }
        }

        public void OnConnectionState(bool bConnected)
        {
            foreach (PartFile pPartFile in filelist_)
            {
                if (pPartFile.Status == PartFileStatusEnum.PS_READY ||
                    pPartFile.Status == PartFileStatusEnum.PS_EMPTY)
                    pPartFile.SetActive(bConnected);
            }
        }

        public void AddToResolved(PartFile pFile, Mule.ED2K.UnresolvedHostname pUH)
        {
            if (pFile != null && pUH != null)
                sourceHostnameResolver_.AddToResolve(pFile.FileHash, pUH.HostName, pUH.Port, pUH.Url);
        }

        public string GetOptimalTempDir(uint nCat, ulong nFileSize)
        {
            // shortcut
            if (MuleApplication.Instance.Preference.TempDirCount == 1)
                return MuleApplication.Instance.Preference.GetTempDir();

            Dictionary<int, long> mapNeededSpaceOnDrive = new Dictionary<int, long>();
            Dictionary<int, long> mapFreeSpaceOnDrive = new Dictionary<int, long>();

            long llBuffer = 0;
            long llHighestFreeSpace = 0;
            int nHighestFreeSpaceDrive = -1;
            // first collect the free space on drives
            for (int i = 0; i < MuleApplication.Instance.Preference.TempDirCount; i++)
            {
                int nDriveNumber = MpdUtilities.GetPathDriveNumber(MuleApplication.Instance.Preference.GetTempDir(i));
                if (mapFreeSpaceOnDrive.ContainsKey(nDriveNumber))
                    continue;
                llBuffer = (long)(MpdUtilities.GetFreeDiskSpace(MuleApplication.Instance.Preference.GetTempDir(i)) -
                    MuleApplication.Instance.Preference.MinFreeDiskSpace);
                mapFreeSpaceOnDrive[nDriveNumber] = llBuffer;
                if (llBuffer > llHighestFreeSpace)
                {
                    nHighestFreeSpaceDrive = nDriveNumber;
                    llHighestFreeSpace = llBuffer;
                }

            }

            // now get the space we would need to download all files in the current queue
            foreach (PartFile pCurFile in filelist_)
            {
                int nDriveNumber = MpdUtilities.GetPathDriveNumber(pCurFile.TempPath);

                long llNeededForCompletion = 0;
                switch (pCurFile.GetStatus(false))
                {
                    case PartFileStatusEnum.PS_READY:
                        goto case PartFileStatusEnum.PS_INSUFFICIENT;
                    case PartFileStatusEnum.PS_EMPTY:
                        goto case PartFileStatusEnum.PS_INSUFFICIENT;
                    case PartFileStatusEnum.PS_WAITINGFORHASH:
                        goto case PartFileStatusEnum.PS_INSUFFICIENT;
                    case PartFileStatusEnum.PS_INSUFFICIENT:
                        llNeededForCompletion = (long)(pCurFile.FileSize - pCurFile.RealFileSize);
                        if (llNeededForCompletion < 0)
                            llNeededForCompletion = 0;
                        break;
                }
                llBuffer =
                    mapNeededSpaceOnDrive[nDriveNumber];
                llBuffer += llNeededForCompletion;
                mapNeededSpaceOnDrive[nDriveNumber] = llBuffer;
            }

            long llHighestTotalSpace = 0;
            int nHighestTotalSpaceDir = -1;
            int nHighestFreeSpaceDir = -1;
            int nAnyAvailableDir = -1;
            // first round (0): on same drive as incomming and enough space for all downloading
            // second round (1): enough space for all downloading
            // third round (2): most actual free space
            for (int i = 0; i < MuleApplication.Instance.Preference.TempDirCount; i++)
            {
                int nDriveNumber = MpdUtilities.GetPathDriveNumber(MuleApplication.Instance.Preference.GetTempDir(i));
                llBuffer = 0;

                long llAvailableSpace =
                    mapFreeSpaceOnDrive[nDriveNumber];
                llBuffer =
                    mapNeededSpaceOnDrive[nDriveNumber];
                llAvailableSpace -= llBuffer;

                // no condition can be met for a large file on a FAT volume
                if (nFileSize <= MuleConstants.OLD_MAX_EMULE_FILE_SIZE ||
                    !MpdUtilities.IsFileOnFATVolume(MuleApplication.Instance.Preference.GetTempDir(i)))
                {
                    // condition 0
                    // needs to be same drive and enough space
                    if (MpdUtilities.GetPathDriveNumber(MuleApplication.Instance.Preference.GetCategoryPath(nCat)) == nDriveNumber &&
                        llAvailableSpace > (long)nFileSize)
                    {
                        //this one is perfect
                        return MuleApplication.Instance.Preference.GetTempDir(i);
                    }
                    // condition 1
                    // needs to have enough space for downloading
                    if (llAvailableSpace > (long)nFileSize && llAvailableSpace > llHighestTotalSpace)
                    {
                        llHighestTotalSpace = llAvailableSpace;
                        nHighestTotalSpaceDir = i;
                    }
                    // condition 2
                    // first one which has the highest actualy free space
                    if (nDriveNumber == nHighestFreeSpaceDrive && nHighestFreeSpaceDir == (-1))
                    {
                        nHighestFreeSpaceDir = i;
                    }
                    // condition 3
                    // any directory which can be used for this file (ak not FAT for large files)
                    if (nAnyAvailableDir == (-1))
                    {
                        nAnyAvailableDir = i;
                    }
                }
            }

            if (nHighestTotalSpaceDir != (-1))
            {	 //condtion 0 was apperently too much, take 1
                return MuleApplication.Instance.Preference.GetTempDir(nHighestTotalSpaceDir);
            }
            else if (nHighestFreeSpaceDir != (-1))
            { // condtion 1 could not be met too, take 2
                return MuleApplication.Instance.Preference.GetTempDir(nHighestFreeSpaceDir);
            }
            else if (nAnyAvailableDir != (-1))
            {
                return MuleApplication.Instance.Preference.GetTempDir(nAnyAvailableDir);
            }
            else
            { // so was condtion 2 and 3, take 4.. wait there is no 3 - this must be a bug
                Debug.Assert(false);
                return MuleApplication.Instance.Preference.GetTempDir();
            }
        }

        public Mule.ED2K.ED2KServer CurrentUDPServer
        {
            get;
            set;
        }

        #endregion

        #region Protected
        protected bool SendNextUDPPacket()
        {
            if (filelist_.Count == 0
                || !MuleApplication.Instance.ServerConnect.IsUDPSocketAvailable
                || !MuleApplication.Instance.ServerConnect.IsConnected
                || MuleApplication.Instance.Preference.IsClientCryptLayerRequired) // we cannot use sources received without userhash, so dont ask
                return false;

            ED2KServer pConnectedServer = MuleApplication.Instance.ServerConnect.CurrentServer;
            if (pConnectedServer != null)
                pConnectedServer =
                    MuleApplication.Instance.ServerList.GetServerByAddress(pConnectedServer.Address,
                    pConnectedServer.Port);

            if (CurrentUDPServer == null)
            {
                while ((CurrentUDPServer = MuleApplication.Instance.ServerList.GetSuccServer(CurrentUDPServer)) != null)
                {
                    if (CurrentUDPServer == pConnectedServer)
                        continue;
                    if (CurrentUDPServer.FailedCount >= MuleApplication.Instance.Preference.DeadServerRetries)
                        continue;
                    break;
                }
                if (CurrentUDPServer == null)
                {
                    StopUDPRequests();
                    return false;
                }
                requestsSentToServer_ = 0;
            }

            bool bGetSources2Packet = (CurrentUDPServer.UDPFlags & ED2KServerUdpFlagsEnum.SRV_UDPFLG_EXT_GETSOURCES2) > 0;
            bool bServerSupportsLargeFiles = CurrentUDPServer.DoesSupportsLargeFilesUDP;

            // loop until the packet is filled or a packet was sent
            bool bSentPacket = false;
            SafeMemFile dataGlobGetSources =
                MpdObjectManager.CreateSafeMemFile(20);

            uint iFiles = 0;
            uint iLargeFiles = 0;
            while (!IsMaxFilesPerUDPServerPacketReached(iFiles, iLargeFiles) && !bSentPacket)
            {
                // get next file to search sources for
                PartFile nextfile = null;
                while (!bSentPacket && !(nextfile != null &&
                    (nextfile.Status == PartFileStatusEnum.PS_READY ||
                    nextfile.Status == PartFileStatusEnum.PS_EMPTY)))
                {
                    if (lastfile_ == null) // we just started the global source searching or have switched the server
                    {
                        // get first file to search sources for
                        nextfile = filelist_[0];
                        lastfile_ = nextfile;
                    }
                    else
                    {
                        int pos = filelist_.IndexOf(lastfile_);
                        if (pos == 0) // the last file is no longer in the DL-list (may have been finished or canceld)
                        {
                            // get first file to search sources for
                            nextfile = filelist_[0];
                            lastfile_ = nextfile;
                        }
                        else
                        {
                            pos++;
                            if (pos >= filelist_.Count) // finished asking the current server for all files
                            {
                                // if there are pending requests for the current server, send them
                                if (dataGlobGetSources.Length > 0)
                                {
                                    if (SendGlobGetSourcesUDPPacket(dataGlobGetSources,
                                            bGetSources2Packet, iFiles, iLargeFiles))
                                        bSentPacket = true;
                                    dataGlobGetSources.SetLength(0);
                                    iFiles = 0;
                                    iLargeFiles = 0;
                                }

                                // get next server to ask
                                while ((CurrentUDPServer = MuleApplication.Instance.ServerList.GetSuccServer(CurrentUDPServer)) != null)
                                {
                                    if (CurrentUDPServer == pConnectedServer)
                                        continue;
                                    if (CurrentUDPServer.FailedCount >= MuleApplication.Instance.Preference.DeadServerRetries)
                                        continue;
                                    break;
                                }
                                requestsSentToServer_ = 0;
                                if (CurrentUDPServer == null)
                                {
                                    // finished asking all servers for all files
                                    StopUDPRequests();
                                    return false; // finished (processed all file & all servers)
                                }
                                searchedServers_++;

                                // if we already sent a packet, switch to the next file at next function call
                                if (bSentPacket)
                                {
                                    lastfile_ = null;
                                    break;
                                }

                                bGetSources2Packet = (CurrentUDPServer.UDPFlags & ED2KServerUdpFlagsEnum.SRV_UDPFLG_EXT_GETSOURCES2) > 0;
                                bServerSupportsLargeFiles = CurrentUDPServer.DoesSupportsLargeFilesUDP;

                                // have selected a new server; get first file to search sources for
                                nextfile = filelist_[0];
                                lastfile_ = nextfile;
                            }
                            else
                            {
                                nextfile = filelist_[pos];
                                lastfile_ = nextfile;
                            }
                        }
                    }
                }

                if (!bSentPacket &&
                    nextfile != null && nextfile.SourceCount < nextfile.MaxSourcePerFileUDP &&
                    (bServerSupportsLargeFiles || !nextfile.IsLargeFile))
                {
                    if (bGetSources2Packet)
                    {
                        if (nextfile.IsLargeFile)
                        {
                            // GETSOURCES2 Packet Large File (<HASH_16><IND_4 = 0><SIZE_8> *)
                            iLargeFiles++;
                            dataGlobGetSources.WriteHash16(nextfile.FileHash);
                            dataGlobGetSources.WriteUInt32(0);
                            dataGlobGetSources.WriteUInt64(nextfile.FileSize);
                        }
                        else
                        {
                            // GETSOURCES2 Packet (<HASH_16><SIZE_4> *)
                            dataGlobGetSources.WriteHash16(nextfile.FileHash);
                            dataGlobGetSources.WriteUInt32((uint)nextfile.FileSize);
                        }
                    }
                    else
                    {
                        // GETSOURCES Packet (<HASH_16> *)
                        dataGlobGetSources.WriteHash16(nextfile.FileHash);
                    }
                    iFiles++;
                }
            }

            if (!bSentPacket && dataGlobGetSources.Length > 0)
                SendGlobGetSourcesUDPPacket(dataGlobGetSources, bGetSources2Packet, iFiles, iLargeFiles);

            // send max 35 UDP request to one server per interval
            // if we have more than 35 files, we rotate the list and use it as queue
            if (requestsSentToServer_ >= MAX_REQUESTS_PER_SERVER)
            {
                // move the last 35 files to the head
                if (filelist_.Count >= MAX_REQUESTS_PER_SERVER)
                {
                    for (int i = 0; i != MAX_REQUESTS_PER_SERVER; i++)
                    {
                        PartFile f = filelist_[filelist_.Count - 1];
                        filelist_.Remove(f);
                        filelist_.Insert(0, f);
                    }
                }

                // and next server
                while ((CurrentUDPServer = MuleApplication.Instance.ServerList.GetSuccServer(CurrentUDPServer)) != null)
                {
                    if (CurrentUDPServer == pConnectedServer)
                        continue;
                    if (CurrentUDPServer.FailedCount >= MuleApplication.Instance.Preference.DeadServerRetries)
                        continue;
                    break;
                }
                requestsSentToServer_ = 0;
                if (CurrentUDPServer == null)
                {
                    StopUDPRequests();
                    return false; // finished (processed all file & all servers)
                }
                searchedServers_++;
                lastfile_ = null;
            }

            return true;
        }

        protected void ProcessLocalRequests()
        {
            if ((localServerReqQueue_.Count > 0) && (nextTCPSrcReq_ < MpdUtilities.GetTickCount()))
            {
                SafeMemFile dataTcpFrame = MpdObjectManager.CreateSafeMemFile(22);
                int iMaxFilesPerTcpFrame = 15;
                int iFiles = 0;
                while (localServerReqQueue_.Count > 0 && iFiles < iMaxFilesPerTcpFrame)
                {
                    // find the file with the longest waitingtime
                    int pos = 0;
                    uint dwBestWaitTime = 0xFFFFFFFF;
                    int posNextRequest = -1;
                    PartFile cur_file;
                    while (pos < localServerReqQueue_.Count)
                    {
                        cur_file = localServerReqQueue_[pos];
                        if (cur_file.Status == PartFileStatusEnum.PS_READY ||
                            cur_file.Status == PartFileStatusEnum.PS_EMPTY)
                        {
                            PriorityEnum nPriority = cur_file.DownPriority;
                            if (nPriority > PriorityEnum.PR_HIGH)
                            {
                                Debug.Assert(false);
                                nPriority = PriorityEnum.PR_HIGH;
                            }

                            if (cur_file.LastSearchTime + (PriorityEnum.PR_HIGH - nPriority) < dwBestWaitTime)
                            {
                                dwBestWaitTime = (uint)(cur_file.LastSearchTime + (PriorityEnum.PR_HIGH - nPriority));
                                posNextRequest = pos;
                            }

                            pos++;
                        }
                        else
                        {
                            localServerReqQueue_.RemoveAt(pos);
                            cur_file.LocalSrcReqQueued = false;
                        }
                    }

                    if (posNextRequest != -1)
                    {
                        cur_file = localServerReqQueue_[posNextRequest];
                        cur_file.LocalSrcReqQueued = false;
                        cur_file.LastSearchTime = MpdUtilities.GetTickCount();
                        localServerReqQueue_.RemoveAt(posNextRequest);

                        if (cur_file.IsLargeFile &&
                            (MuleApplication.Instance.ServerConnect.CurrentServer == null ||
                            !MuleApplication.Instance.ServerConnect.CurrentServer.DoesSupportsLargeFilesTCP))
                        {
                            Debug.Assert(false);
                            continue;
                        }

                        iFiles++;

                        // create request packet
                        SafeMemFile smPacket = MpdObjectManager.CreateSafeMemFile();
                        smPacket.WriteHash16(cur_file.FileHash);
                        if (!cur_file.IsLargeFile)
                        {
                            smPacket.WriteUInt32((uint)cur_file.FileSize);
                        }
                        else
                        {
                            smPacket.WriteUInt32(0); // indicates that this is a large file and a ulong follows
                            smPacket.WriteUInt64(cur_file.FileSize);
                        }

                        OperationCodeEnum byOpcode = 0;
                        if (MuleApplication.Instance.Preference.IsClientCryptLayerSupported &&
                            MuleApplication.Instance.ServerConnect.CurrentServer != null &&
                            MuleApplication.Instance.ServerConnect.CurrentServer.DoesSupportsGetSourcesObfuscation)
                            byOpcode = OperationCodeEnum.OP_GETSOURCES_OBFU;
                        else
                            byOpcode = OperationCodeEnum.OP_GETSOURCES;

                        Packet packet = MuleApplication.Instance.NetworkObjectManager.CreatePacket(smPacket,
                            MuleConstants.OP_EDONKEYPROT, byOpcode);
                        dataTcpFrame.Write(packet.Packet, 0, (int)packet.RealPacketSize);
                    }
                }

                int iSize = (int)dataTcpFrame.Length;
                if (iSize > 0)
                {
                    // create one 'packet' which contains all buffered OP_GETSOURCES eD2K packets to be sent with one TCP frame
                    // server credits: 16*iMaxFilesPerTcpFrame+1 = 241
                    Packet packet = MuleApplication.Instance.NetworkObjectManager.CreatePacket(new byte[iSize],
                        (uint)dataTcpFrame.Length, true, false);
                    dataTcpFrame.Seek(0, SeekOrigin.Begin);
                    dataTcpFrame.Read(packet.Packet, 0, iSize);
                    MuleApplication.Instance.Statistics.AddUpDataOverheadServer(packet.Size);
                    MuleApplication.Instance.ServerConnect.SendPacket(packet, true);
                }

                // next TCP frame with up to 15 source requests is allowed to be sent in.
                nextTCPSrcReq_ = (uint)(MpdUtilities.GetTickCount() + MuleConstants.ONE_SEC_MS * iMaxFilesPerTcpFrame * (16 + 4));
            }
        }

        private const int MAX_UDP_PACKET_DATA = 510;
        private const int BYTES_PER_FILE_G1 = 16;
        private const int BYTES_PER_FILE_G2 = 20;
        private const int ADDITIONAL_BYTES_PER_LARGEFILE = 8;

        private const int MAX_REQUESTS_PER_SERVER = 35;

        protected bool IsMaxFilesPerUDPServerPacketReached(uint nFiles, uint nIncludedLargeFiles)
        {
            if (CurrentUDPServer != null &&
                (CurrentUDPServer.UDPFlags & ED2KServerUdpFlagsEnum.SRV_UDPFLG_EXT_GETSOURCES) != 0)
            {

                int nBytesPerNormalFile =
                    ((CurrentUDPServer.UDPFlags & ED2KServerUdpFlagsEnum.SRV_UDPFLG_EXT_GETSOURCES2) > 0) ?
                        BYTES_PER_FILE_G2 : BYTES_PER_FILE_G1;
                int nUsedBytes = (int)(nFiles * nBytesPerNormalFile +
                    nIncludedLargeFiles * ADDITIONAL_BYTES_PER_LARGEFILE);

                return (requestsSentToServer_ >= MAX_REQUESTS_PER_SERVER) ||
                    (nUsedBytes >= MAX_UDP_PACKET_DATA);
            }
            else
            {
                return nFiles != 0;
            }
        }

        protected bool SendGlobGetSourcesUDPPacket(SafeMemFile data,
            bool bExt2Packet, uint nFiles, uint nIncludedLargeFiles)
        {
            bool bSentPacket = false;

            if (CurrentUDPServer != null)
            {
                Packet packet = MuleApplication.Instance.NetworkObjectManager.CreatePacket(data);
                data = null;

                if (bExt2Packet)
                {
                    packet.OperationCode = OperationCodeEnum.OP_GLOBGETSOURCES2;
                }
                else
                {
                    packet.OperationCode = OperationCodeEnum.OP_GLOBGETSOURCES;
                }
                MuleApplication.Instance.Statistics.AddUpDataOverheadServer(packet.Size);
                MuleApplication.Instance.ServerConnect.SendUDPPacket(packet, CurrentUDPServer, false);

                requestsSentToServer_ += nFiles;
                bSentPacket = true;
            }

            return bSentPacket;
        }
        #endregion

        #region Private
        private bool CompareParts(int pos1, int pos2)
        {
            PartFile file1 = filelist_[(pos1)];
            PartFile file2 = filelist_[(pos2)];
            return file1.RightFileHasHigherPrio(file1, file2);
        }

        private void SwapParts(int pos1, int pos2)
        {
            PartFile file1 = filelist_[(pos1)];
            PartFile file2 = filelist_[(pos2)];

            filelist_[pos1] = file2;
            filelist_[pos2] = file1;
        }

        class PartFileComparer : Comparer<PartFile>
        {
            public override int Compare(PartFile x, PartFile y)
            {
                return x.RightFileHasHigherPrio(x, y) ? 1 : -1;
            }
        }

        private void HeapSort(uint first, uint last)
        {
            MpdUtilities.HeapSort(ref filelist_, (int)first, (int)last, new PartFileComparer());
        }
        #endregion
    }
}
