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
        private List<PartFile> filelist = new List<PartFile>();
        private List<PartFile> m_localServerReqQueue = new List<PartFile>();
        private ushort filesrdy;
        private uint datarate;

        private PartFile lastfile;
        private uint lastcheckdiskspacetime;
        private uint lastudpsearchtime;
        private uint lastudpstattime;
        private uint udcounter;
        private uint m_cRequestsSentToServer;
        private uint m_dwNextTCPSrcReq;
        private int m_iSearchedServers;
        private uint lastkademliafilerequest;

        private ulong m_datarateMS;
        private uint m_nUDPFileReasks;
        private uint m_nFailedUDPFileReasks;

        // By BadWolf - Accurate Speed Measurement
        private List<TransferredData> avarage_dr_list = new List<TransferredData>();
        // END By BadWolf - Accurate Speed Measurement

        private uint m_dwLastA4AFtime; // ZZ:DownloadManager

        private SourceHostnameResolver sourceHostnameResolver_;
        #endregion

        #region Constructors
        public DownloadQueueImpl()
        {
            filesrdy = 0;
            datarate = 0;
            CurrentUDPServer = null;
            lastfile = null;
            lastcheckdiskspacetime = 0;
            lastudpsearchtime = 0;
            lastudpstattime = 0;
            SetLastKademliaFileRequest();
            udcounter = 0;
            m_iSearchedServers = 0;
            m_datarateMS = 0;
            m_nUDPFileReasks = 0;
            m_nFailedUDPFileReasks = 0;
            m_dwNextTCPSrcReq = 0;
            m_cRequestsSentToServer = 0;
            m_dwLastA4AFtime = 0; // ZZ:DownloadManager
        }
        #endregion

        #region DownloadQueue Members

        public void Process()
        {
            ProcessLocalRequests(); // send src requests to local server

            uint downspeed = 0;
            ulong maxDownload = MuleApplication.Instance.Preference.GetMaxDownloadInBytesPerSec(true);
            if (maxDownload != MuleConstants.UNLIMITED * 1024 && datarate > 1500)
            {
                downspeed = (uint)((maxDownload * 100) / (datarate + 1));
                if (downspeed < 50)
                    downspeed = 50;
                else if (downspeed > 200)
                    downspeed = 200;
            }

            while (avarage_dr_list.Count > 0 &&
                (MpdUtilities.GetTickCount() - avarage_dr_list[0].timestamp > 10 * 1000))
            {
                m_datarateMS -= avarage_dr_list[0].datalen;
                avarage_dr_list.RemoveAt(0);
            }

            if (avarage_dr_list.Count > 1)
            {
                datarate = (uint)(m_datarateMS / (ulong)avarage_dr_list.Count);
            }
            else
            {
                datarate = 0;
            }

            uint datarateX = 0;
            udcounter++;

            MuleApplication.Instance.Statistics.GlobalDone = 0;
            MuleApplication.Instance.Statistics.GlobalSize = 0;
            MuleApplication.Instance.Statistics.OverallStatus = 0;
            //filelist is already sorted by prio, therefore I removed all the extra loops..
            foreach (PartFile cur_file in filelist)
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
                    datarateX += cur_file.Process(downspeed, udcounter);
                }
                else
                {
                    //This will make sure we don't keep old sources to paused and stoped files..
                    cur_file.StopPausedFile();
                }
            }

            TransferredData newitem = new TransferredData(datarateX, MpdUtilities.GetTickCount());
            avarage_dr_list.Add(newitem);
            m_datarateMS += datarateX;

            if (udcounter == 5)
            {
                if (MuleApplication.Instance.ServerConnect.IsUDPSocketAvailable)
                {
                    if ((lastudpstattime == 0) ||
                        (MpdUtilities.GetTickCount() - lastudpstattime) > MuleConstants.UDPSERVERSTATTIME)
                    {
                        lastudpstattime = MpdUtilities.GetTickCount();
                        MuleApplication.Instance.ServerList.ServerStats();
                    }
                }
            }

            if (udcounter == 10)
            {
                udcounter = 0;
                if (MuleApplication.Instance.ServerConnect.IsUDPSocketAvailable)
                {
                    if ((lastudpsearchtime == 0) ||
                        (MpdUtilities.GetTickCount() - lastudpsearchtime) > MuleConstants.UDPSERVERREASKTIME)
                        SendNextUDPPacket();
                }
            }

            CheckDiskspaceTimed();

            // ZZ:DownloadManager -.
            if ((m_dwLastA4AFtime == 0) || (MpdUtilities.GetTickCount() - m_dwLastA4AFtime) > MuleConstants.ONE_MIN_MS * 8)
            {
                MuleApplication.Instance.ClientList.ProcessA4AFClients();
                m_dwLastA4AFtime = MpdUtilities.GetTickCount();
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
                        filelist.Add(toadd);			// to downloadqueue
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
                        filelist.Add(toadd);			// to downloadqueue
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
            filelist.ForEach(cur =>
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

            filelist.Add(newfile);
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

            foreach (PartFile cur_file in filelist)
            {
                if (toremove == cur_file)
                {
                    filelist.Remove(cur_file);
                    break;
                }
            }

            SortByPriority();
            CheckDiskspace();
            ExportPartMetFilesOverview();
        }

        public void DeleteAll()
        {
            filelist.ForEach(cur_file =>
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
            get { throw new NotImplementedException(); }
        }

        public uint DownloadingFileCount
        {
            get { throw new NotImplementedException(); }
        }

        public uint PausedFileCount
        {
            get { throw new NotImplementedException(); }
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
            foreach (PartFile cur_file in filelist)
                if (cur_file == file)
                    return true;

            return false;
        }

        public PartFile GetFileByID(byte[] filehash)
        {
            foreach (PartFile cur_file in filelist)
            {
                if (MpdUtilities.Md4Cmp(filehash, cur_file.FileHash) == 0)
                    return cur_file;
            }
            return null;
        }

        public PartFile GetFileByIndex(int index)
        {
            if (index < filelist.Count && index >= 0)
                return filelist[index];

            return null;
        }

        public PartFile GetFileByKadFileSearchID(uint id)
        {
            foreach (PartFile cur_file in filelist)
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
                foreach (PartFile cur_file in filelist)
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
                foreach (PartFile cur_file in filelist)
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
            throw new NotImplementedException();
        }

        public UpDownClient GetDownloadClientByIP(uint dwIP)
        {
            foreach (PartFile cur_file in filelist)
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

            foreach (PartFile cur_file in filelist)
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
            foreach (PartFile cur_file in filelist)
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
            foreach (PartFile cur_file in filelist)
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
                //	AddDebugLogLine(DLP_DEFAULT, false, _T("Rejected source because it was found on the DeadSourcesList (%s) for file %s : %s")
                //	,sender.m_DeadSourceList.IsDeadSource(source)? _T("Local") : _T("Global"), sender.GetFileName(), source.DbgGetClientInfo() );
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
                    //	AddDebugLogLine(false, _T("Ignored already known source with IP=%s"), ipstr(nClientIP));
                    return false;
                }
            }

            // use this for client which are already know (downloading for example)
            foreach (PartFile cur_file in filelist)
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
            foreach (PartFile cur_file in filelist)
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
            foreach (PartFile cur_file in filelist)
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

        public int GetDownloadFilesStats(ref ulong ui64TotalFileSize, ref ulong ui64TotalLeftToTransfer, ref ulong ui64TotalAdditionalNeededSpace)
        {
            throw new NotImplementedException();
        }

        public uint Datarate
        {
            get { throw new NotImplementedException(); }
        }

        public void AddUDPFileReasks()
        {
            throw new NotImplementedException();
        }

        public uint UDPFileReasks
        {
            get { throw new NotImplementedException(); }
        }

        public void AddFailedUDPFileReasks()
        {
            throw new NotImplementedException();
        }

        public uint FailedUDPFileReasks
        {
            get { throw new NotImplementedException(); }
        }

        public void ResetCatParts(uint cat)
        {
            foreach (PartFile cur_file in filelist)
            {
                if (cur_file.Category == cat)
                    cur_file.Category = 0;
                else if (cur_file.Category > cat)
                    cur_file.Category = cur_file.Category - 1;
            }
        }

        public void SetCatPrio(uint cat, byte newprio)
        {
            foreach (PartFile cur_file in filelist)
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

        public void RemoveAutoPrioInCat(uint cat, byte newprio)
        {
            foreach (PartFile cur_file in filelist)
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

        public void SetCatStatus(uint cat, int newstatus)
        {
            //bool reset = false;
            //bool resort = false;

            //POSITION pos = filelist.GetHeadPosition();
            //while (pos != 0)
            //{
            //    CPartFile* cur_file = filelist.GetAt(pos);
            //    if (!cur_file)
            //        continue;

            //    if (cat == -1 ||
            //        (cat == -2 && cur_file->GetCategory() == 0) ||
            //        (cat == 0 && cur_file->CheckShowItemInGivenCat(cat)) ||
            //        (cat > 0 && cat == cur_file->GetCategory()))
            //    {
            //        switch (newstatus)
            //        {
            //            case MP_CANCEL:
            //                cur_file->DeleteFile();
            //                reset = true;
            //                break;
            //            case MP_PAUSE:
            //                cur_file->PauseFile(false, false);
            //                resort = true;
            //                break;
            //            case MP_STOP:
            //                cur_file->StopFile(false, false);
            //                resort = true;
            //                break;
            //            case MP_RESUME:
            //                if (cur_file->CanResumeFile())
            //                {
            //                    if (cur_file->GetStatus() == PS_INSUFFICIENT)
            //                        cur_file->ResumeFileInsufficient();
            //                    else
            //                    {
            //                        cur_file->ResumeFile(false);
            //                        resort = true;
            //                    }
            //                }
            //                break;
            //        }
            //    }
            //    filelist.GetNext(pos);
            //    if (reset)
            //    {
            //        reset = false;
            //        pos = filelist.GetHeadPosition();
            //    }
            //}

            //if (resort)
            //{
            //    theApp.downloadqueue->SortByPriority();
            //    theApp.downloadqueue->CheckDiskspace();
            //}
        }

        public void MoveCat(uint from, uint to)
        {
            throw new NotImplementedException();
        }

        public void SetAutoCat(PartFile newfile)
        {
            throw new NotImplementedException();
        }

        public void SendLocalSrcRequest(PartFile sender)
        {
            throw new NotImplementedException();
        }

        public void RemoveLocalServerRequest(PartFile pFile)
        {
            throw new NotImplementedException();
        }

        public void ResetLocalServerRequests()
        {
            throw new NotImplementedException();
        }

        public void SetLastKademliaFileRequest()
        {
            throw new NotImplementedException();
        }

        public bool DoKademliaFileRequest()
        {
            throw new NotImplementedException();
        }

        public void KademliaSearchFile(uint searchID, Mpd.Generic.UInt128 pcontactID, Mpd.Generic.UInt128 pkadID, byte type, uint ip, ushort tcp, ushort udp, uint dwBuddyIP, ushort dwBuddyPort, byte byCryptOptions)
        {
            throw new NotImplementedException();
        }

        public void StopUDPRequests()
        {
            CurrentUDPServer = null;
            lastudpsearchtime = MpdUtilities.GetTickCount();
            lastfile = null;
            m_iSearchedServers = 0;
        }

        public void SortByPriority()
        {
            uint n = (uint)filelist.Count;
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
            lastcheckdiskspacetime = MpdUtilities.GetTickCount();

            // sorting the list could be done here, but I prefer to "see" that function call in the calling functions.
            //SortByPriority();

            // If disabled, resume any previously paused files
            if (!MuleApplication.Instance.Preference.IsCheckDiskspaceEnabled)
            {
                if (!bNotEnoughSpaceLeft) // avoid worse case, if we already had 'disk full'
                {
                    foreach (PartFile cur_file in filelist)
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
                0 : MpdUtilities.GetFreeDiskSpaceX(MuleApplication.Instance.Preference.GetTempDir());

            // 'bNotEnoughSpaceLeft' - avoid worse case, if we already had 'disk full'
            if (MuleApplication.Instance.Preference.MinFreeDiskSpace == 0)
            {
                foreach (PartFile cur_file in filelist)
                {
                    ulong nTotalAvailableSpace = bNotEnoughSpaceLeft ? 0 :
                        ((MuleApplication.Instance.Preference.TempDirCount == 1) ?
                        nTotalAvailableSpaceMain : MpdUtilities.GetFreeDiskSpaceX(cur_file.TempPath));

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
                foreach (PartFile cur_file in filelist)
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
                        nTotalAvailableSpaceMain : MpdUtilities.GetFreeDiskSpaceX(cur_file.TempPath));
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
            if ((lastcheckdiskspacetime == 0) ||
                (MpdUtilities.GetTickCount() - lastcheckdiskspacetime) > MuleConstants.DISKSPACERECHECKTIME)
                CheckDiskspace();
        }

        public void ExportPartMetFilesOverview()
        {
            throw new NotImplementedException();
        }

        public void OnConnectionState(bool bConnected)
        {
            throw new NotImplementedException();
        }

        public void AddToResolved(PartFile pFile, Mule.ED2K.UnresolvedHostname pUH)
        {
            if (pFile != null && pUH != null)
                sourceHostnameResolver_.AddToResolve(pFile.FileHash, pUH.HostName, pUH.Port, pUH.Url);
        }

        public string GetOptimalTempDir(uint nCat, ulong nFileSize)
        {
            throw new NotImplementedException();
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
            if (filelist.Count == 0
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
                m_cRequestsSentToServer = 0;
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
                    if (lastfile == null) // we just started the global source searching or have switched the server
                    {
                        // get first file to search sources for
                        nextfile = filelist[0];
                        lastfile = nextfile;
                    }
                    else
                    {
                        int pos = filelist.IndexOf(lastfile);
                        if (pos == 0) // the last file is no longer in the DL-list (may have been finished or canceld)
                        {
                            // get first file to search sources for
                            nextfile = filelist[0];
                            lastfile = nextfile;
                        }
                        else
                        {
                            pos++;
                            if (pos >= filelist.Count) // finished asking the current server for all files
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
                                m_cRequestsSentToServer = 0;
                                if (CurrentUDPServer == null)
                                {
                                    // finished asking all servers for all files
                                    StopUDPRequests();
                                    return false; // finished (processed all file & all servers)
                                }
                                m_iSearchedServers++;

                                // if we already sent a packet, switch to the next file at next function call
                                if (bSentPacket)
                                {
                                    lastfile = null;
                                    break;
                                }

                                bGetSources2Packet = (CurrentUDPServer.UDPFlags & ED2KServerUdpFlagsEnum.SRV_UDPFLG_EXT_GETSOURCES2) > 0;
                                bServerSupportsLargeFiles = CurrentUDPServer.DoesSupportsLargeFilesUDP;

                                // have selected a new server; get first file to search sources for
                                nextfile = filelist[0];
                                lastfile = nextfile;
                            }
                            else
                            {
                                nextfile = filelist[pos];
                                lastfile = nextfile;
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
            if (m_cRequestsSentToServer >= MAX_REQUESTS_PER_SERVER)
            {
                // move the last 35 files to the head
                if (filelist.Count >= MAX_REQUESTS_PER_SERVER)
                {
                    for (int i = 0; i != MAX_REQUESTS_PER_SERVER; i++)
                    {
                        PartFile f = filelist[filelist.Count - 1];
                        filelist.Remove(f);
                        filelist.Insert(0, f);
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
                m_cRequestsSentToServer = 0;
                if (CurrentUDPServer == null)
                {
                    StopUDPRequests();
                    return false; // finished (processed all file & all servers)
                }
                m_iSearchedServers++;
                lastfile = null;
            }

            return true;
        }

        protected void ProcessLocalRequests()
        {
            throw new Exception();
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

                return (m_cRequestsSentToServer >= MAX_REQUESTS_PER_SERVER) ||
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

                m_cRequestsSentToServer += nFiles;
                bSentPacket = true;
            }

            return bSentPacket;
        }
        #endregion

        #region Private
        private bool CompareParts(int pos1, int pos2)
        {
            PartFile file1 = filelist[(pos1)];
            PartFile file2 = filelist[(pos2)];
            return file1.RightFileHasHigherPrio(file1, file2);
        }

        private void SwapParts(int pos1, int pos2)
        {
            PartFile file1 = filelist[(pos1)];
            PartFile file2 = filelist[(pos2)];

            filelist[pos1] = file2;
            filelist[pos2] = file1;
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
            MpdUtilities.HeapSort(ref filelist, (int)first, (int)last, new PartFileComparer());
        }
        #endregion
    }
}
