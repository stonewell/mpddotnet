using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.File;
using Mpd.Generic.IO;
using System.IO;
using Mpd.Generic;

namespace Mule.Core.Impl
{
    struct TransferredData
    {
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
    //PartFile newfile = MuleApplication.Instance.FileObjectManager.CreatePartFile(pLink, cat);
    //if (newfile.Status == PartFileStatusEnum.PS_ERROR){
    //    newfile=null;
    //}
    //else {
    //    AddDownload(newfile,MuleApplication.Instance.Preference.AddNewFilesPaused() != 0);
    //}

    //PartFile partfile = newfile;
    //if (partfile == null)
    //    partfile = GetFileByID(pLink.HashKey);
    //if (partfile != null)
    //{
    //    // match the fileidentifier and only if the are the same add possible sources
    //    CFileIdentifierSA tmpFileIdent(pLink.GetHashKey(), pLink.GetSize(), pLink.GetAICHHash(), pLink.HasValidAICHHash());
    //    if (partfile.GetFileIdentifier().CompareRelaxed(tmpFileIdent))
    //    {
    //        if (pLink.HasValidSources())
    //            partfile.AddClientSources(pLink.SourcesList, 1, false);
    //        if (!partfile.GetFileIdentifier().HasAICHHash() && tmpFileIdent.HasAICHHash())
    //        {
    //            partfile.GetFileIdentifier().SetAICHHash(tmpFileIdent.GetAICHHash());
    //            partfile.GetAICHRecoveryHashSet().SetMasterHash(tmpFileIdent.GetAICHHash(), AICH_VERIFIED);
    //            partfile.GetAICHRecoveryHashSet().FreeHashSet();

    //        }
    //    }
    //}

    //if (pLink.HasHostnameSources())
    //{
    //    POSITION pos = pLink.m_HostnameSourcesList.GetHeadPosition();
    //    while (pos != null)
    //    {
    //        const SUnresolvedHostname* pUnresHost = pLink.m_HostnameSourcesList.GetNext(pos);
    //        m_srcwnd.AddToResolve(pLink.GetHashKey(), pUnresHost.strHostname, pUnresHost.nPort, pUnresHost.strURL);
    //    }
    //}
    //    
        }

        public void RemoveFile(PartFile toremove)
        {
            throw new NotImplementedException();
        }

        public void DeleteAll()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public bool IsFileExisting(byte[] fileid, bool bLogWarnings)
        {
            throw new NotImplementedException();
        }

        public bool IsPartFile(KnownFile file)
        {
            throw new NotImplementedException();
        }

        public PartFile GetFileByID(byte[] filehash)
        {
            throw new NotImplementedException();
        }

        public PartFile GetFileByIndex(int index)
        {
            throw new NotImplementedException();
        }

        public PartFile GetFileByKadFileSearchID(uint ID)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public UpDownClient GetDownloadClientByIP_UDP(uint dwIP, ushort nUDPPort, bool bIgnorePortOnUniqueIP)
        {
            throw new NotImplementedException();
        }

        public UpDownClient GetDownloadClientByIP_UDP(uint dwIP, ushort nUDPPort, bool bIgnorePortOnUniqueIP, ref bool pbMultipleIPs)
        {
            throw new NotImplementedException();
        }

        public bool IsInList(UpDownClient client)
        {
            throw new NotImplementedException();
        }

        public bool CheckAndAddSource(PartFile sender, UpDownClient source)
        {
            throw new NotImplementedException();
        }

        public bool CheckAndAddKnownSource(PartFile sender, UpDownClient source)
        {
            return CheckAndAddKnownSource(sender, source, false);
        }

        public bool CheckAndAddKnownSource(PartFile sender, UpDownClient source, bool bIgnoreGlobDeadList)
        {
            throw new NotImplementedException();
        }

        public bool RemoveSource(UpDownClient toremove)
        {
            return RemoveSource(toremove, true);
        }

        public bool RemoveSource(UpDownClient toremove, bool bDoStatsUpdate)
        {
            throw new NotImplementedException();
        }

        public void GetDownloadSourcesStats(DownloadStatsStruct results)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public void SetCatPrio(uint cat, byte newprio)
        {
            throw new NotImplementedException();
        }

        public void RemoveAutoPrioInCat(uint cat, byte newprio)
        {
            throw new NotImplementedException();
        }

        public void SetCatStatus(uint cat, int newstatus)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public void SortByPriority()
        {
            throw new NotImplementedException();
        }

        public void CheckDiskspace()
        {
            throw new NotImplementedException();
        }

        public void CheckDiskspace(bool bNotEnoughSpaceLeft)
        {
            throw new NotImplementedException();
        }

        public void CheckDiskspaceTimed()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new Exception();
        }

        protected void ProcessLocalRequests()
        {
            throw new Exception();
        }

        protected bool IsMaxFilesPerUDPServerPacketReached(uint nFiles, uint nIncludedLargeFiles)
        {
            throw new Exception();
        }

        protected bool SendGlobGetSourcesUDPPacket(SafeMemFile data, bool bExt2Packet, uint nFiles, uint nIncludedLargeFiles)
        {
            throw new Exception();
        }
        #endregion

        #region Private
        private bool CompareParts(int pos1, int pos2)
        {
            throw new Exception();
        }

        private void SwapParts(int pos1, int pos2)
        {
            throw new Exception();
        }

        private void HeapSort(uint first, uint last)
        {
            throw new Exception();
        }
        #endregion
    }
}
