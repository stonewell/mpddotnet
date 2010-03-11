//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Mule.File;
//using Mule.Definitions;
//using Mpd.Generic.Types.IO;

//namespace Mule.Core.Impl
//{
//    public abstract class MuleDownloadTask
//    {
//        private CorruptionBlackBox corruptionBlackBox_;
//        private UpDownClientList downloadingSourceList_;
//        private UpDownClientList srclist_ = new UpDownClientList();
//        private UpDownClientList A4AFsrclist_ = new UpDownClientList(); //<<-- enkeyDEV(Ottavio84) -A4AF-

//        public PartFile PartFile { get; set; }
//        public bool LoadPartFile(string in_directory, string in_filename, bool getsizeonly)
//        {
//            bool isnewstyle;
//            byte version;
//            PartFileFormatEnum partmettype = PartFileFormatEnum.PMT_UNKNOWN;

//            Dictionary<uint, Gap> gap_map = new Dictionary<uint, Gap>(); // Slugfiller
//            uTransferred_ = 0;
//            partmetfilename_ = in_filename;
//            FileDirectory = (in_directory);
//            fullname_ = System.IO.Path.Combine(FileDirectory, partmetfilename_);

//            // readfile data form part.met file
//            SafeBufferedFile metFile = null;

//            try
//            {
//                metFile =
//                    MpdGenericObjectManager.CreateSafeBufferedFile(fullname_,
//                        FileMode.Open, FileAccess.Read, FileShare.Read);
//            }
//            catch (Exception)
//            {
//                //TODO:Log
//                return false;
//            }

//            try
//            {
//                version = metFile.ReadUInt8();

//                if (version != Convert.ToByte(VersionsEnum.PARTFILE_VERSION) &&
//                    version != Convert.ToByte(VersionsEnum.PARTFILE_SPLITTEDVERSION) &&
//                    version != Convert.ToByte(VersionsEnum.PARTFILE_VERSION_LARGEFILE))
//                {
//                    metFile.Close();
//                    if (version == Convert.ToByte(83))
//                    {
//                        return ImportShareazaTempfile(in_directory, in_filename, getsizeonly);
//                    }
//                    //TODO:LogError(LOG_STATUSBAR, GetResString(IDS_ERR_BADMETVERSION), partmetfilename_, FileName);
//                    return false;
//                }

//                isnewstyle = (version == Convert.ToByte(VersionsEnum.PARTFILE_SPLITTEDVERSION));
//                partmettype = isnewstyle ? PartFileFormatEnum.PMT_SPLITTED : PartFileFormatEnum.PMT_DEFAULTOLD;
//                if (!isnewstyle)
//                {
//                    byte[] test = new byte[4];
//                    metFile.Seek(24, SeekOrigin.Begin);
//                    metFile.Read(test);

//                    metFile.Seek(1, SeekOrigin.Begin);

//                    if (test[0] == Convert.ToByte(0) &&
//                        test[1] == Convert.ToByte(0) &&
//                        test[2] == Convert.ToByte(2) &&
//                        test[3] == Convert.ToByte(1))
//                    {
//                        isnewstyle = true;	// edonkeys so called "old part style"
//                        partmettype = PartFileFormatEnum.PMT_NEWOLD;
//                    }
//                }

//                if (isnewstyle)
//                {
//                    byte[] tmpBuf = new byte[4];
//                    metFile.Read(tmpBuf);

//                    uint temp = BitConverter.ToUInt32(tmpBuf, 0);

//                    if (temp == 0)
//                    {	// 0.48 partmets - different again
//                        LoadHashsetFromFile(metFile, false);
//                    }
//                    else
//                    {
//                        byte[] gethash = new byte[16];
//                        metFile.Seek(2, SeekOrigin.Begin);
//                        LoadDateFromFile(metFile);
//                        metFile.Read(gethash);
//                        MPDUtilities.Md4Cpy(FileHash, gethash);
//                    }
//                }
//                else
//                {
//                    LoadDateFromFile(metFile);
//                    LoadHashsetFromFile(metFile, false);
//                }

//                uint tagcount = metFile.ReadUInt32();
//                for (uint j = 0; j < tagcount; j++)
//                {
//                    Tag newtag = MpdGenericObjectManager.CreateTag(metFile, false);
//                    if (!getsizeonly ||
//                        (getsizeonly &&
//                        (newtag.NameID == MuleConstants.FT_FILESIZE ||
//                        newtag.NameID == MuleConstants.FT_FILENAME)))
//                    {
//                        switch (newtag.NameID)
//                        {
//                            case MuleConstants.FT_FILENAME:
//                                {
//                                    if (!newtag.IsStr)
//                                    {
//                                        //TODO:Log
//                                        return false;
//                                    }
//                                    if (string.IsNullOrEmpty(FileName))
//                                        FileName = newtag.Str;

//                                    break;
//                                }
//                            case MuleConstants.FT_LASTSEENCOMPLETE:
//                                {
//                                    if (newtag.IsInt)
//                                        lastseencomplete_ = MPDUtilities.UInt32ToDateTime(newtag.Int);

//                                    break;
//                                }
//                            case MuleConstants.FT_FILESIZE:
//                                {
//                                    if (newtag.IsInt64(true))
//                                        FileSize = newtag.Int64;

//                                    break;
//                                }
//                            case MuleConstants.FT_TRANSFERRED:
//                                {
//                                    if (newtag.IsInt64(true))
//                                        uTransferred_ = newtag.Int64;

//                                    break;
//                                }
//                            case MuleConstants.FT_COMPRESSION:
//                                {
//                                    //ASSERT( newtag.IsInt64(true) );
//                                    if (newtag.IsInt64(true))
//                                        uCompressionGain_ = newtag.Int64;

//                                    break;
//                                }
//                            case MuleConstants.FT_CORRUPTED:
//                                {
//                                    //ASSERT( newtag.IsInt64() );
//                                    if (newtag.IsInt64())
//                                        uCorruptionLoss_ = newtag.Int64;

//                                    break;
//                                }
//                            case MuleConstants.FT_FILETYPE:
//                                {
//                                    //ASSERT( newtag.IsStr );
//                                    if (newtag.IsStr)
//                                        FileType = newtag.Str;

//                                    break;
//                                }
//                            case MuleConstants.FT_CATEGORY:
//                                {
//                                    //ASSERT( newtag.IsInt );
//                                    if (newtag.IsInt)
//                                        category_ = newtag.Int;

//                                    break;
//                                }
//                            case MuleConstants.FT_MAXSOURCES:
//                                {
//                                    //ASSERT( newtag.IsInt );
//                                    if (newtag.IsInt)
//                                        uMaxSources_ = newtag.Int;

//                                    break;
//                                }
//                            case MuleConstants.FT_DLPRIORITY:
//                                {
//                                    //ASSERT( newtag.IsInt );
//                                    if (newtag.IsInt)
//                                    {
//                                        if (!isnewstyle)
//                                        {
//                                            iDownPriority_ = Convert.ToByte(newtag.Int);
//                                            if (iDownPriority_ == Convert.ToByte(PriorityEnum.PR_AUTO))
//                                            {
//                                                iDownPriority_ = Convert.ToByte(PriorityEnum.PR_HIGH);
//                                                IsAutoDownPriority = true;
//                                            }
//                                            else
//                                            {
//                                                if (iDownPriority_ != Convert.ToByte(PriorityEnum.PR_LOW) &&
//                                                    iDownPriority_ != Convert.ToByte(PriorityEnum.PR_NORMAL) &&
//                                                    iDownPriority_ != Convert.ToByte(PriorityEnum.PR_HIGH))
//                                                    iDownPriority_ = Convert.ToByte(PriorityEnum.PR_NORMAL);
//                                                IsAutoDownPriority = false;
//                                            }
//                                        }
//                                    }

//                                    break;
//                                }
//                            case MuleConstants.FT_STATUS:
//                                {
//                                    //ASSERT( newtag.IsInt );
//                                    if (newtag.IsInt)
//                                    {
//                                        paused_ = newtag.Int != 0;
//                                        stopped_ = paused_;
//                                    }

//                                    break;
//                                }
//                            case MuleConstants.FT_ULPRIORITY:
//                                {
//                                    //ASSERT( newtag.IsInt );
//                                    if (newtag.IsInt)
//                                    {
//                                        if (!isnewstyle)
//                                        {
//                                            int iUpPriority = Convert.ToInt32(newtag.Int);
//                                            if (iUpPriority == Convert.ToInt32(PriorityEnum.PR_AUTO))
//                                            {
//                                                SetUpPriority(Convert.ToByte(PriorityEnum.PR_HIGH), false);
//                                                IsAutoUpPriority = true;
//                                            }
//                                            else
//                                            {
//                                                if (iUpPriority != Convert.ToInt32(PriorityEnum.PR_VERYLOW) &&
//                                                    iUpPriority != Convert.ToInt32(PriorityEnum.PR_LOW) &&
//                                                    iUpPriority != Convert.ToInt32(PriorityEnum.PR_NORMAL) &&
//                                                    iUpPriority != Convert.ToInt32(PriorityEnum.PR_HIGH) &&
//                                                    iUpPriority != Convert.ToInt32(PriorityEnum.PR_VERYHIGH))
//                                                    iUpPriority = Convert.ToInt32(PriorityEnum.PR_NORMAL);
//                                                SetUpPriority(Convert.ToByte(iUpPriority), false);
//                                                IsAutoUpPriority = false;
//                                            }
//                                        }
//                                    }

//                                    break;
//                                }
//                            case MuleConstants.FT_KADLASTPUBLISHSRC:
//                                {
//                                    //ASSERT( newtag.IsInt );
//                                    if (newtag.IsInt)
//                                    {
//                                        LastPublishTimeKadSrc = newtag.Int;
//                                        LastPublishBuddy = 0;

//                                        if (LastPublishTimeKadSrc > MPDUtilities.Time() + MuleConstants.KADEMLIAREPUBLISHTIMES)
//                                        {
//                                            //There may be a posibility of an older client that saved a random number here.. This will check for that..
//                                            LastPublishTimeKadSrc = 0;
//                                            LastPublishBuddy = 0;
//                                        }
//                                    }

//                                    break;
//                                }
//                            case MuleConstants.FT_KADLASTPUBLISHNOTES:
//                                {
//                                    //ASSERT( newtag.IsInt );
//                                    if (newtag.IsInt)
//                                    {
//                                        LastPublishTimeKadNotes = newtag.Int;
//                                    }

//                                    break;
//                                }
//                            case MuleConstants.FT_DL_PREVIEW:
//                                {
//                                    //ASSERT( newtag.IsInt );
//                                    if (newtag.Int == 1)
//                                    {
//                                        PreviewPrio = true;
//                                    }
//                                    else
//                                    {
//                                        PreviewPrio = false;
//                                    }

//                                    break;
//                                }

//                            // statistics
//                            case MuleConstants.FT_ATTRANSFERRED:
//                                {
//                                    //ASSERT( newtag.IsInt );
//                                    if (newtag.IsInt)
//                                        statistic_.AllTimeTransferred = newtag.Int;

//                                    break;
//                                }
//                            case MuleConstants.FT_ATTRANSFERREDHI:
//                                {
//                                    //ASSERT( newtag.IsInt );
//                                    if (newtag.IsInt)
//                                    {
//                                        uint hi, low;
//                                        low = Convert.ToUInt32(statistic_.AllTimeTransferred & 0xFFFFFFFF);
//                                        hi = newtag.Int;
//                                        ulong hi2;
//                                        hi2 = hi;
//                                        hi2 = hi2 << 32;
//                                        statistic_.AllTimeTransferred = low + hi2;
//                                    }

//                                    break;
//                                }
//                            case MuleConstants.FT_ATREQUESTED:
//                                {
//                                    //ASSERT( newtag.IsInt );
//                                    if (newtag.IsInt)
//                                        statistic_.AllTimeRequests = newtag.Int;

//                                    break;
//                                }
//                            case MuleConstants.FT_ATACCEPTED:
//                                {
//                                    //ASSERT( newtag.IsInt );
//                                    if (newtag.IsInt)
//                                        statistic_.AllTimeAccepts = newtag.Int;

//                                    break;
//                                }

//                            // old tags: as long as they are not needed, take the chance to purge them
//                            case MuleConstants.FT_PERMISSIONS:
//                                //ASSERT( newtag.IsInt );

//                                break;
//                            case MuleConstants.FT_KADLASTPUBLISHKEY:
//                                //ASSERT( newtag.IsInt );

//                                break;
//                            case MuleConstants.FT_DL_ACTIVE_TIME:
//                                //ASSERT( newtag.IsInt );
//                                if (newtag.IsInt)
//                                    nDlActiveTime_ = newtag.Int;

//                                break;
//                            case MuleConstants.FT_CORRUPTEDPARTS:
//                                //ASSERT( newtag.IsStr );
//                                if (newtag.IsStr)
//                                {
//                                    //ASSERT( corrupted_list.GetHeadPosition() == NULL );
//                                    string strCorruptedParts = newtag.Str;
//                                    string[] parts = strCorruptedParts.Split(',');

//                                    foreach (string part in parts)
//                                    {
//                                        uint uPart = 0;
//                                        if (MPDUtilities.ScanUInt32(part, ref uPart) == 1)
//                                        {
//                                            if (uPart < PartCount && !IsCorruptedPart(uPart))
//                                                corrupted_list_.Add(Convert.ToUInt16(uPart));
//                                        }
//                                    }
//                                }

//                                break;
//                            case MuleConstants.FT_AICH_HASH:
//                                {
//                                    //ASSERT( newtag.IsStr );
//                                    AICHHash hash = AICHObjectManager.CreateAICHHash();
//                                    if (MPDUtilities.DecodeBase32(newtag.Str.ToCharArray(), hash.RawHash) ==
//                                        Convert.ToUInt32(MuleConstants.HASHSIZE))
//                                    {
//                                        pAICHHashSet_.SetMasterHash(hash, AICHStatusEnum.AICH_VERIFIED);
//                                    }
//                                    else
//                                    {
//                                        //ASSERT( false );
//                                    }

//                                    break;
//                                }
//                            default:
//                                {
//                                    if (newtag.NameID == 0 &&
//                                        (newtag.Name[0] == Convert.ToChar(MuleConstants.FT_GAPSTART) ||
//                                        newtag.Name[0] == Convert.ToChar(MuleConstants.FT_GAPEND)))
//                                    {
//                                        //ASSERT( newtag.IsInt64(true) );
//                                        if (newtag.IsInt64(true))
//                                        {
//                                            Gap gap;
//                                            uint gapkey = uint.Parse(newtag.Name.Substring(1));
//                                            if (!gap_map.ContainsKey(gapkey))
//                                            {
//                                                gap = new Gap();
//                                                gap_map[gapkey] = gap;
//                                                gap.start = 0xFFFFFFFFFFFFFFFF;
//                                                gap.end = 0xFFFFFFFFFFFFFFFF;
//                                            }
//                                            else
//                                            {
//                                                gap = gap_map[gapkey];
//                                            }
//                                            if (newtag.Name[0] == Convert.ToChar(MuleConstants.FT_GAPSTART))
//                                                gap.start = newtag.Int64;
//                                            if (newtag.Name[0] == Convert.ToChar(MuleConstants.FT_GAPEND))
//                                                gap.end = newtag.Int64 - 1;
//                                        }

//                                    }
//                                    else
//                                        taglist_.Add(newtag);

//                                    break;
//                                }
//                        }
//                    }
//                    else
//                    {
//                    }
//                }

//                // load the hashsets from the hybridstylepartmet
//                if (isnewstyle && !getsizeonly && (metFile.Position < metFile.Length))
//                {
//                    byte[] tempBuf = new byte[1];
//                    metFile.Read(tempBuf);

//                    byte temp = tempBuf[0];

//                    uint parts = PartCount;	// assuming we will get all hashsets

//                    for (uint i = 0; i < parts && (metFile.Position + 16 < metFile.Length); i++)
//                    {
//                        byte[] cur_hash = new byte[16];
//                        metFile.Read(cur_hash);
//                        hashlist_.Add(cur_hash);
//                    }

//                    byte[] checkhash = new byte[16];
//                    if (hashlist_.Count != 0)
//                    {
//                        byte[] buffer = new byte[hashlist_.Count * 16];
//                        for (int i = 0; i < hashlist_.Count; i++)
//                            MPDUtilities.Md4Cpy(buffer, i * 16, hashlist_[i], 0, 16);
//                        CreateHash(buffer, Convert.ToUInt64(hashlist_.Count * 16), checkhash);
//                    }
//                    if (MPDUtilities.Md4Cmp(FileHash, checkhash) != 0)
//                    {
//                        hashlist_.Clear();
//                    }
//                }

//                metFile.Close();
//            }
//            catch (Exception)
//            {
//                //TODO:Logif (error.m_cause == CFileException::endOfFile)
//                //    LogError(LOG_STATUSBAR, GetResString(IDS_ERR_METCORRUPT), partmetfilename_, FileName);
//                //else{
//                //    TCHAR buffer[MAX_CFEXP_ERRORMSG];
//                //    error.GetErrorMessage(buffer,ARRSIZE(buffer));
//                //    LogError(LOG_STATUSBAR, GetResString(IDS_ERR_FILEERROR), partmetfilename_, FileName, buffer);
//                //}
//                //error.Delete();
//                return false;
//            }

//            if (FileSize > MuleConstants.MAX_EMULE_FILE_SIZE)
//            {
//                //TODO:LogError(LOG_STATUSBAR, GetResString(IDS_ERR_FILEERROR), partmetfilename_, FileName, _T("File size exceeds supported limit"));
//                return false;
//            }

//            if (getsizeonly)
//            {
//                // AAARGGGHH!!!....
//                return partmettype != PartFileFormatEnum.PMT_UNKNOWN;
//            }

//            // Now to flush the map into the list (Slugfiller)
//            foreach (uint gapkey in gap_map.Keys)
//            {
//                Gap gap = gap_map[gapkey];
//                // SLUGFILLER: SafeHash - revised code, and extra safety
//                if (gap.start != 0xFFFFFFFFFFFFFFFF &&
//                    gap.end != 0xFFFFFFFFFFFFFFFF &&
//                    gap.start <= gap.end && gap.start < FileSize)
//                {
//                    if (gap.end >= FileSize)
//                        gap.end = FileSize - 1; // Clipping
//                    AddGap(gap.start, gap.end); // All tags accounted for, use safe adding
//                }
//                // SLUGFILLER: SafeHash
//            }

//            // verify corrupted parts list

//            List<ushort> tmpList = new List<ushort>();
//            tmpList.AddRange(corrupted_list_);

//            for (int i = 0; i < tmpList.Count; i++)
//            {
//                uint uCorruptedPart = Convert.ToUInt32(tmpList[i]);

//                if (IsComplete(Convert.ToUInt64(uCorruptedPart) * MuleConstants.PARTSIZE,
//                    Convert.ToUInt64(uCorruptedPart + 1) * MuleConstants.PARTSIZE - 1, true))
//                    corrupted_list_.RemoveAt(i);
//            }

//            //check if this is a backup
//            if (string.Compare(Path.GetExtension(fullname_), MuleConstants.PARTMET_TMP_EXT, true) == 0)
//                fullname_ = Path.GetFileNameWithoutExtension(fullname_);

//            // open permanent handle
//            string searchpath = Path.GetFileNameWithoutExtension(fullname_);
//            try
//            {
//                hpartfile_ = new FileStream(searchpath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
//            }
//            catch (Exception)
//            {
//                //TODO:Log
//                return false;
//            }

//            // read part file creation time
//            tLastModified_ = MPDUtilities.DateTime2UInt32Time(System.IO.File.GetLastWriteTime(searchpath));
//            tCreated_ = MPDUtilities.DateTime2UInt32Time(System.IO.File.GetCreationTime(searchpath));

//            try
//            {
//                FilePath = (searchpath);

//                try
//                {
//                    dwFileAttributes_ = System.IO.File.GetAttributes(searchpath);
//                }
//                catch
//                {
//                    dwFileAttributes_ = 0;
//                }

//                // SLUGFILLER: SafeHash - final safety, make sure any missing part of the file is gap
//                if (Convert.ToUInt64(hpartfile_.Length) < FileSize)
//                {
//                    AddGap(Convert.ToUInt64(hpartfile_.Length), FileSize - 1);
//                }
//                // Goes both ways - Partfile should never be too large
//                if (Convert.ToUInt64(hpartfile_.Length) > FileSize)
//                {
//                    //TODO:LogTRACE(_T("Partfile \"%s\" is too large! Truncating %I64u bytes.\n"), FileName, m_hpartfile.Length) - FileSize);
//                    hpartfile_.SetLength(Convert.ToInt64(FileSize));
//                }
//                // SLUGFILLER: SafeHash

//                srcpartFrequency_.Clear();
//                for (uint i = 0; i < PartCount; i++)
//                    srcpartFrequency_.Add(0);
//                Status = PartFileStatusEnum.PS_EMPTY;
//                // check hashcount, filesatus etc
//                if (HashCount != ED2KPartHashCount)
//                {
//                    //ASSERT( hashlist_.GetSize() == 0 );
//                    hashsetneeded_ = true;
//                    return true;
//                }
//                else
//                {
//                    hashsetneeded_ = false;
//                    for (uint i = 0; i < (uint)hashlist_.Count; i++)
//                    {
//                        if (i < PartCount &&
//                            IsComplete((ulong)i * MuleConstants.PARTSIZE, (ulong)(i + 1) * MuleConstants.PARTSIZE - 1, true))
//                        {
//                            Status = PartFileStatusEnum.PS_READY;
//                            break;
//                        }
//                    }
//                }

//                if (gaplist_.Count == 0)
//                {	// is this file complete already?
//                    CompleteFile(false);
//                    return true;
//                }

//                if (!isnewstyle) // not for importing
//                {
//                    // check date of .part file - if its wrong, rehash file
//                    uint fdate = MPDUtilities.DateTime2UInt32Time(System.IO.File.GetLastWriteTimeUtc(searchpath));
//                    MPDUtilities.AdjustNTFSDaylightFileTime(ref fdate, searchpath);

//                    if (tUtcLastModified_ != fdate)
//                    {
//                        //TODO:Log
//                        //string strFileInfo
//                        //strFileInfo.Format(_T("%s (%s)"), GetFilePath(), FileName);
//                        //LogError(LOG_STATUSBAR, GetResString(IDS_ERR_REHASH), strFileInfo);
//                        // rehash
//                        Status = PartFileStatusEnum.PS_WAITINGFORHASH;
//                        try
//                        {
//                            AddFileThread addfilethread = MuleEngine.CoreObjectManager.CreateAddFileThread();
//                            FileOp = PartFileOpEnum.PFOP_HASHING;
//                            FileOpProgress = 0;
//                            addfilethread.SetValues(null, FileDirectory, searchpath, this);
//                            addfilethread.Start();
//                        }
//                        catch (Exception)
//                        {
//                            //TODO:LOG:
//                            Status = PartFileStatusEnum.PS_ERROR;
//                        }
//                    }
//                }
//            }
//            catch (Exception)
//            {
//                //TODO:Log
//                //string strError;
//                //strError.Format(_T("Failed to initialize part file \"%s\" (%s)"), m_hpartfile.GetFilePath(), FileName);
//                //TCHAR szError[MAX_CFEXP_ERRORMSG];
//                //if (error.GetErrorMessage(szError, ARRSIZE(szError))){
//                //    strError += _T(" - ");
//                //    strError += szError;
//                //}
//                //LogError(LOG_STATUSBAR, _T("%s"), strError);
//                //error.Delete();
//                return false;
//            }

//            UpdateCompletedInfos();
//            return true;
//        }

//        public abstract bool GetNextRequestedBlock(UpDownClient sender, RequestedBlock newblocks, ref ushort count);
//        public abstract uint WriteToBuffer(ulong transize,
//            byte[] data,
//            ulong start,
//            ulong end,
//            RequestedBlock block,
//            UpDownClient client);
//        public abstract void AddClientSources(SafeMemFile sources,
//            byte sourceexchangeversion,
//            bool bSourceExchange2,
//            UpDownClient pClient);
//        public abstract void AddDownloadingSource(UpDownClient client);
//        public abstract void RemoveDownloadingSource(UpDownClient client);

//        public void InitializeFromLink(Mule.ED2K.ED2KFileLink fileLink, uint cat)
//        {
//            Init();
//            try
//            {
//                SetFileName(fileLink.Name, true, true);
//                FileSize = fileLink.Size;
//                MuleEngine.CoreUtilities.Md4Cpy(FileHash, fileLink.HashKey);
//                if (!MuleEngine.DownloadQueue.IsFileExisting(FileHash))
//                {
//                    if (fileLink.HashSet != null && fileLink.HashSet.Length > 0)
//                    {
//                        try
//                        {
//                            if (!LoadHashsetFromFile(fileLink.HashSet, true))
//                            {
//                                //TODO:Log
//                                ////ASSERT( hashlist_.Count == 0 );
//                                //AddDebugLogLine(false, _T("eD2K link \"%s\" specified with invalid hashset"), fileLink.Name);
//                            }
//                            else
//                                hashsetneeded_ = false;
//                        }
//                        catch (Exception)
//                        {
//                            //TODO:LOG
//                            //TCHAR szError[MAX_CFEXP_ERRORMSG];
//                            //e.GetErrorMessage(szError, ARRSIZE(szError));
//                            //AddDebugLogLine(false, _T("Error: Failed to process hashset for eD2K link \"%s\" - %s"), fileLink.Name, szError);
//                            //e.Delete();
//                        }
//                    }
//                    CreatePartFile(cat);
//                    category_ = cat;
//                }
//                else
//                    Status = PartFileStatusEnum.PS_ERROR;
//            }
//            catch (Exception)
//            {
//                //TODO:Log
//                //string strMsg;
//                //strMsg.Format(GetResString(IDownloadStateEnum.DS_ERR_INVALIDLINK), error);
//                //LogError(LOG_STATUSBAR, GetResString(IDownloadStateEnum.DS_ERR_LINKERROR), strMsg);
//                Status = PartFileStatusEnum.PS_ERROR;
//            }
//        }
//        public uint Process(uint reducedownload, uint icounter)
//        {
//            uint nOldTransSourceCount = GetSrcStatisticsValue(DownloadStateEnum.DS_DOWNLOADING);
//            uint dwCurTick = GetTickCount();

//            // If buffer size exceeds limit, or if not written within time limit, flush data
//            if ((nTotalBufferData_ > MuleEngine.CoreObjectManager.Preference.FileBufferSize) ||
//                (dwCurTick > (nLastBufferFlushTime_ + MuleConstants.BUFFER_TIME_LIMIT)))
//            {
//                // Avoid flushing while copying preview file
//                if (!bPreviewing_)
//                    FlushBuffer();
//            }

//            datarate_ = 0;

//            // calculate datarate, set limit etc.
//            if (icounter < 10)
//            {
//                uint cur_datarate;
//                foreach (UpDownClient cur_src in downloadingSourceList_)
//                {
//                    if (cur_src != null && cur_src.DownloadState == DownloadStateEnum.DS_DOWNLOADING)
//                    {
//                        if (cur_src.ClientRequestSocket != null)
//                        {
//                            cur_src.CheckDownloadTimeout();
//                            cur_datarate = cur_src.CalculateDownloadRate();
//                            datarate_ += cur_datarate;
//                            if (reducedownload != 0)
//                            {
//                                uint limit = reducedownload * cur_datarate / 1000;
//                                if (limit < 1000 && reducedownload == 200)
//                                    limit += 1000;
//                                else if (limit < 200 && cur_datarate == 0 && reducedownload >= 100)
//                                    limit = 200;
//                                else if (limit < 60 && cur_datarate < 600 && reducedownload >= 97)
//                                    limit = 60;
//                                else if (limit < 20 && cur_datarate < 200 && reducedownload >= 93)
//                                    limit = 20;
//                                else if (limit < 1)
//                                    limit = 1;
//                                cur_src.ClientRequestSocket.SetDownloadLimit(limit);
//                                if (cur_src.IsDownloadingFromPeerCache &&
//                                    cur_src.PeerCacheDownloadSocket != null &&
//                                    cur_src.PeerCacheDownloadSocket.IsConnected)
//                                    cur_src.PeerCacheDownloadSocket.SetDownloadLimit(limit);
//                            }
//                        }
//                    }
//                }
//            }
//            else
//            {
//                bool downloadingbefore = anStates_[Convert.ToInt32(DownloadStateEnum.DS_DOWNLOADING)] > 0;
//                // -khaos--+++> Moved this here, otherwise we were setting our permanent variables to 0 every tenth of a second...
//                Array.Clear(anStates_, 0, anStates_.Length);
//                Array.Clear(src_stats_, 0, src_stats_.Length);
//                Array.Clear(net_stats_, 0, net_stats_.Length);
//                uint nCountForState;

//                foreach (UpDownClient cur_src in srclist_)
//                {
//                    // BEGIN -rewritten- refreshing statistics (no need for temp vars since it is not multithreaded)
//                    nCountForState = Convert.ToUInt32(cur_src.DownloadState);
//                    //special case which is not yet set as downloadstate
//                    if (nCountForState == Convert.ToUInt32(DownloadStateEnum.DS_ONQUEUE))
//                    {
//                        if (cur_src.IsRemoteQueueFull)
//                            nCountForState = Convert.ToUInt32(DownloadStateEnum.DS_REMOTEQUEUEFULL);
//                    }

//                    // this is a performance killer . avoid calling 'IsBanned' for gathering stats
//                    //if (cur_src.IsBanned())
//                    //	nCountForState = DownloadStateEnum.DS_BANNED;
//                    if (cur_src.UploadState == UploadStateEnum.US_BANNED) // not as accurate as 'IsBanned', but way faster and good enough for stats.
//                        nCountForState = Convert.ToUInt32(DownloadStateEnum.DS_BANNED);

//                    if (cur_src.SourceFrom >= SourceFromEnum.SF_SERVER &&
//                        cur_src.SourceFrom <= SourceFromEnum.SF_PASSIVE)
//                        src_stats_[Convert.ToInt32(cur_src.SourceFrom)] =
//                            src_stats_[Convert.ToInt32(cur_src.SourceFrom)]++;

//                    if (cur_src.ServerIP != 0 && cur_src.ServerPort != 0)
//                    {
//                        net_stats_[0] = net_stats_[0]++;
//                        if (cur_src.KadPort != 0)
//                            net_stats_[2] = net_stats_[2]++;
//                    }
//                    if (cur_src.KadPort != 0)
//                        net_stats_[1] = net_stats_[1]++;

//                    anStates_[nCountForState] = anStates_[nCountForState]++;

//                    switch (cur_src.DownloadState)
//                    {
//                        case DownloadStateEnum.DS_DOWNLOADING:
//                            {
//                                if (cur_src.ClientRequestSocket != null)
//                                {
//                                    cur_src.CheckDownloadTimeout();
//                                    uint cur_datarate = cur_src.CalculateDownloadRate();
//                                    datarate_ += cur_datarate;
//                                    if (reducedownload != 0 && cur_src.DownloadState == DownloadStateEnum.DS_DOWNLOADING)
//                                    {
//                                        uint limit = reducedownload * cur_datarate / 1000; //(uint)(((float)reducedownload/100)*cur_datarate)/10;		
//                                        if (limit < 1000 && reducedownload == 200)
//                                            limit += 1000;
//                                        else if (limit < 200 && cur_datarate == 0 && reducedownload >= 100)
//                                            limit = 200;
//                                        else if (limit < 60 && cur_datarate < 600 && reducedownload >= 97)
//                                            limit = 60;
//                                        else if (limit < 20 && cur_datarate < 200 && reducedownload >= 93)
//                                            limit = 20;
//                                        else if (limit < 1)
//                                            limit = 1;
//                                        cur_src.ClientRequestSocket.SetDownloadLimit(limit);
//                                        if (cur_src.IsDownloadingFromPeerCache &&
//                                            cur_src.PeerCacheDownloadSocket != null &&
//                                            cur_src.PeerCacheDownloadSocket.IsConnected)
//                                            cur_src.PeerCacheDownloadSocket.SetDownloadLimit(limit);

//                                    }
//                                    else
//                                    {
//                                        cur_src.ClientRequestSocket.DisableDownloadLimit();
//                                        if (cur_src.IsDownloadingFromPeerCache &&
//                                            cur_src.PeerCacheDownloadSocket != null &&
//                                            cur_src.PeerCacheDownloadSocket.IsConnected)
//                                            cur_src.PeerCacheDownloadSocket.DisableDownloadLimit();
//                                    }
//                                }
//                                break;
//                            }
//                        // Do nothing with this client..
//                        case DownloadStateEnum.DS_BANNED:
//                            break;
//                        // Check if something has changed with our or their ID state..
//                        case DownloadStateEnum.DS_LOWTOLOWIP:
//                            {
//                                // To Mods, please stop instantly removing these sources..
//                                // This causes sources to pop in and out creating extra overhead!
//                                //Make sure this source is still a LowID Client..
//                                if (cur_src.HasLowID)
//                                {
//                                    //Make sure we still cannot callback to this Client..
//                                    if (!MuleEngine.CanDoCallback(cur_src))
//                                    {
//                                        //If we are almost maxed on sources, slowly remove these client to see if we can find a better source.
//                                        if (((dwCurTick - lastpurgetime_) > 30 * 1000) &&
//                                            (SourceCount >= (MaxSources * .8)))
//                                        {
//                                            MuleEngine.DownloadQueue.RemoveSource(cur_src);
//                                            lastpurgetime_ = dwCurTick;
//                                        }
//                                        break;
//                                    }
//                                }
//                                // This should no longer be a LOWTOLOWIP..
//                                cur_src.DownloadState = DownloadStateEnum.DS_ONQUEUE;
//                                break;
//                            }
//                        case DownloadStateEnum.DS_NONEEDEDPARTS:
//                            {
//                                // To Mods, please stop instantly removing these sources..
//                                // This causes sources to pop in and out creating extra overhead!
//                                if ((dwCurTick - lastpurgetime_) > 40 * 1000)
//                                {
//                                    lastpurgetime_ = dwCurTick;
//                                    // we only delete them if reaching the limit
//                                    if (SourceCount >= (MaxSources * .8))
//                                    {
//                                        MuleEngine.DownloadQueue.RemoveSource(cur_src);
//                                        break;
//                                    }
//                                }
//                                // doubled reasktime for no needed parts - save connections and traffic
//                                if (cur_src.GetTimeUntilReask() > 0)
//                                    break;

//                                cur_src.SwapToAnotherFile("A4AF for NNP file. CPartFile::Process()",
//                                    true, false, false, null, true, true); // ZZ:DownloadManager
//                                // Recheck this client to see if still NNP.. Set to DownloadStateEnum.DS_NONE so that we force a TCP reask next time..
//                                cur_src.DownloadState = DownloadStateEnum.DS_NONE;
//                                break;
//                            }
//                        case DownloadStateEnum.DS_ONQUEUE:
//                            {
//                                // To Mods, please stop instantly removing these sources..
//                                // This causes sources to pop in and out creating extra overhead!
//                                if (cur_src.IsRemoteQueueFull)
//                                {
//                                    if (((dwCurTick - lastpurgetime_) > 1 * 60 * 1000) &&
//                                        (SourceCount >= (MaxSources * .8)))
//                                    {
//                                        MuleEngine.DownloadQueue.RemoveSource(cur_src);
//                                        lastpurgetime_ = dwCurTick;
//                                        break;
//                                    }
//                                }
//                                //Give up to 1 min for UDP to respond.. If we are within one min of TCP reask, do not try..
//                                if (MuleEngine.IsConnected &&
//                                    cur_src.GetTimeUntilReask() < 2 * 60 * 1000 &&
//                                    cur_src.GetTimeUntilReask() > 1 * 1000 &&
//                                    GetTickCount() - cur_src.LastTriedToConnectTime > 20 * 60 * 1000) // ZZ:DownloadManager (one resk timestamp for each file)
//                                {
//                                    cur_src.UDPReaskForDownload();
//                                }

//                                if (MuleEngine.IsConnected &&
//                                    cur_src.GetTimeUntilReask() == 0 &&
//                                    GetTickCount() - cur_src.LastTriedToConnectTime > 20 * 60 * 1000) // ZZ:DownloadManager (one resk timestamp for each file)
//                                {
//                                    if (!cur_src.DoesAskForDownload) // NOTE: This may *delete* the client!!
//                                        break; //I left this break here just as a reminder just in case re rearange things..
//                                }
//                                break;
//                            }
//                        case DownloadStateEnum.DS_CONNECTING:
//                        case DownloadStateEnum.DS_TOOMANYCONNS:
//                        case DownloadStateEnum.DS_TOOMANYCONNSKAD:
//                        case DownloadStateEnum.DS_NONE:
//                        case DownloadStateEnum.DS_WAITCALLBACK:
//                        case DownloadStateEnum.DS_WAITCALLBACKKAD:
//                            {
//                                if (MuleEngine.IsConnected &&
//                                    cur_src.GetTimeUntilReask() == 0 &&
//                                    GetTickCount() - cur_src.LastTriedToConnectTime > 20 * 60 * 1000) // ZZ:DownloadManager (one resk timestamp for each file)
//                                {
//                                    if (!cur_src.DoesAskForDownload) // NOTE: This may *delete* the client!!
//                                        break; //I left this break here just as a reminder just in case re rearange things..
//                                }
//                                break;
//                            }
//                    }
//                }

//                if (downloadingbefore != (anStates_[Convert.ToInt32(DownloadStateEnum.DS_DOWNLOADING)] > 0))
//                    NotifyStatusChange();

//                if (MaxSourcePerFileUDP > SourceCount)
//                {
//                    if (MuleEngine.DownloadQueue.DoKademliaFileRequest() &&
//                        (MuleEngine.KadEngine.TotalFile < MuleConstants.KADEMLIATOTALFILE) &&
//                        (dwCurTick > lastSearchTimeKad_) &&
//                        MuleEngine.KadEngine.IsConnected &&
//                        MuleEngine.IsConnected &&
//                        !stopped_)
//                    {
//                        //Once we can handle lowID users in Kad, we remove the second IsConnected
//                        //Kademlia
//                        MuleEngine.DownloadQueue.SetLastKademliaFileRequest();
//                        if (KadFileSearchID == 0)
//                        {
//                            KadSearch pSearch =
//                                MuleEngine.KadEngine.SearchManager.PrepareLookup(Convert.ToUInt32(KadSearchTypeEnum.FILE),
//                                    true,
//                                    MuleEngine.KadEngine.ObjectManager.CreateUInt128(FileHash));
//                            if (pSearch != null)
//                            {
//                                if (totalSearchesKad_ < 7)
//                                    totalSearchesKad_++;
//                                lastSearchTimeKad_ = dwCurTick + (MuleConstants.KADEMLIAREASKTIME * totalSearchesKad_);
//                                KadFileSearchID = pSearch.SearchID;
//                            }
//                            else
//                                KadFileSearchID = 0;
//                        }
//                    }
//                }
//                else
//                {
//                    if (KadFileSearchID != 0)
//                    {
//                        MuleEngine.KadEngine.SearchManager.StopSearch(KadFileSearchID, true);
//                    }
//                }


//                // check if we want new sources from server
//                if (!bLocalSrcReqQueued_ &&
//                    ((lastSearchTime_ == 0) ||
//                    (dwCurTick - lastSearchTime_) > MuleConstants.SERVERREASKTIME) &&
//                    MuleEngine.ServerConnect.IsConnected &&
//                    MaxSourcePerFileSoft > SourceCount &&
//                    !stopped_ &&
//                    (!IsLargeFile ||
//                    (MuleEngine.ServerConnect.CurrentServer != null &&
//                    MuleEngine.ServerConnect.CurrentServer.DoesSupportsLargeFilesTCP)))
//                {
//                    bLocalSrcReqQueued_ = true;
//                    MuleEngine.DownloadQueue.SendLocalSrcRequest(this);
//                }

//                count_++;
//                if (count_ == 3)
//                {
//                    count_ = 0;
//                    UpdateAutoDownPriority();
//                    UpdateDisplayedInfo();
//                    UpdateCompletedInfos();
//                }
//            }

//            if (GetSrcStatisticsValue(DownloadStateEnum.DS_DOWNLOADING) != nOldTransSourceCount)
//            {
//                UpdateDisplayedInfo(true);
//            }

//            return datarate_;
//        }
//        public bool ImportShareazaTempfile(string in_directory, string in_filename, bool getsizeonly)
//        {
//            string fullname = System.IO.Path.Combine(in_directory, in_filename);

//            // open the file
//            Stream sdFile = null;

//            try
//            {
//                sdFile = new FileStream(fullname, FileMode.Open, FileAccess.Read, FileShare.Read);
//            }
//            catch (Exception)
//            {
//                //TODO:Log
//                return false;
//            }
//            //	setvbuf(sdFile.m_pStream, NULL, _IOFBF, 16384);

//            try
//            {
//                BinaryReader br = new BinaryReader(sdFile);

//                // Is it a valid Shareaza temp file?
//                byte[] szID = new byte[3];
//                br.Read(szID, 0, szID.Length);

//                string tmpStrID = Encoding.Default.GetString(szID);

//                if (!"SDL".Equals(tmpStrID))
//                {
//                    br.Close();
//                    sdFile.Close();
//                    return false;
//                }

//                // Get the version
//                int nVersion = br.ReadInt32();

//                // Get the File Name
//                string sRemoteName = br.ReadString();
//                FileName = sRemoteName;

//                ulong lSize = br.ReadUInt64();
//                ulong nSize = lSize;

//                FileSize = lSize;

//                // Get the ed2k hash
//                bool bSHA1, bTiger, bMD5, bED2K, Trusted; bMD5 = false; bED2K = false;
//                byte[] pSHA1 = new byte[20];
//                byte[] pTiger = new byte[24];
//                byte[] pMD5 = new byte[16];
//                byte[] pED2K = new byte[16];

//                bSHA1 = br.ReadBoolean();
//                if (bSHA1) br.Read(pSHA1, 0, pSHA1.Length);
//                if (nVersion >= 31) Trusted = br.ReadBoolean();

//                bTiger = br.ReadBoolean();
//                if (bTiger) br.Read(pTiger, 0, pTiger.Length);
//                if (nVersion >= 31) Trusted = br.ReadBoolean();

//                if (nVersion >= 22) bMD5 = br.ReadBoolean();
//                if (bMD5) br.Read(pMD5, 0, pMD5.Length);
//                if (nVersion >= 31) Trusted = br.ReadBoolean();

//                if (nVersion >= 13) bED2K = br.ReadBoolean();
//                if (bED2K) br.Read(pED2K, 0, pED2K.Length);
//                if (nVersion >= 31) Trusted = br.ReadBoolean();

//                br.Close();

//                if (bED2K)
//                {
//                    MPDUtilities.Md4Cpy(FileHash, pED2K);
//                }
//                else
//                {
//                    //TODO:Log(LOG_ERROR,GetResString(IDS_X_SHAREAZA_IMPORT_NO_HASH),in_filename);
//                    sdFile.Close();
//                    return false;
//                }

//                if (getsizeonly)
//                {
//                    sdFile.Close();
//                    return true;
//                }

//                // Now the tricky part
//                long basePos = sdFile.Position;

//                // Try to to get the gap list
//                if (MPDUtilities.GotoString(sdFile,
//                    nVersion >= 29 ? BitConverter.GetBytes(lSize) : BitConverter.GetBytes(nSize),
//                    nVersion >= 29 ? 8 : 4)) // search the gap list
//                {
//                    sdFile.Seek(sdFile.Position - (nVersion >= 29 ? 8 : 4), SeekOrigin.Begin); // - file size
//                    br = new BinaryReader(sdFile);

//                    bool badGapList = false;

//                    if (nVersion >= 29)
//                    {
//                        long nTotal, nRemaining;
//                        uint nFragments;

//                        nTotal = br.ReadInt64();
//                        nRemaining = br.ReadInt64();
//                        nFragments = br.ReadUInt32();

//                        if (nTotal >= nRemaining)
//                        {
//                            long begin, length;
//                            for (; nFragments-- > 0; )
//                            {
//                                begin = br.ReadInt64();
//                                length = br.ReadInt64();

//                                if (begin + length > nTotal)
//                                {
//                                    badGapList = true;
//                                    break;
//                                }
//                                AddGap(Convert.ToUInt64(begin),
//                                    Convert.ToUInt64((begin + length - 1)));
//                            }
//                        }
//                        else
//                        {
//                            badGapList = true;
//                        }
//                    }
//                    else
//                    {
//                        uint nTotal, nRemaining;
//                        uint nFragments;
//                        nTotal = br.ReadUInt32();
//                        nRemaining = br.ReadUInt32();
//                        nFragments = br.ReadUInt32();

//                        if (nTotal >= nRemaining)
//                        {
//                            uint begin, length;
//                            for (; nFragments-- > 0; )
//                            {
//                                begin = br.ReadUInt32();
//                                length = br.ReadUInt32();
//                                if (begin + length > nTotal)
//                                {
//                                    badGapList = true;
//                                    break;
//                                }
//                                AddGap(begin, begin + length - 1);
//                            }
//                        }
//                        else
//                        {
//                            badGapList = true;
//                        }
//                    }

//                    if (badGapList)
//                    {
//                        gaplist_.Clear();
//                        //TODO:Log(LOG_WARNING,GetResString(IDS_X_SHAREAZA_IMPORT_GAP_LIST_CORRUPT),in_filename);
//                    }

//                    br.Close();
//                }
//                else
//                {
//                    //TODO:Log(LOG_WARNING,GetResString(IDS_X_SHAREAZA_IMPORT_NO_GAP_LIST),in_filename);
//                    sdFile.Seek(basePos, SeekOrigin.Begin); // not found, reset start position
//                }

//                // Try to get the complete hashset
//                if (MPDUtilities.GotoString(sdFile, FileHash, 16)) // search the hashset
//                {
//                    sdFile.Seek(sdFile.Position - 16 - 4, SeekOrigin.Begin); // - list size - hash length
//                    br = new BinaryReader(sdFile);

//                    uint nCount = br.ReadUInt32();

//                    byte[] pMD4 = new byte[16];
//                    br.Read(pMD4, 0, pMD4.Length); // read the hash again

//                    // read the hashset
//                    for (uint i = 0; i < nCount; i++)
//                    {
//                        byte[] curhash = new byte[16];
//                        br.Read(curhash, 0, 16);
//                        hashlist_.Add(curhash);
//                    }

//                    byte[] checkhash = new byte[16];
//                    if (hashlist_.Count != 0)
//                    {
//                        byte[] buffer = new byte[hashlist_.Count * 16];
//                        for (int i = 0; i < hashlist_.Count; i++)
//                            MPDUtilities.Md4Cpy(buffer, (i * 16), hashlist_[i], 0, 16);
//                        CreateHash(buffer, Convert.ToUInt64(hashlist_.Count * 16), checkhash);
//                    }
//                    if (MPDUtilities.Md4Cmp(pMD4, checkhash) != 0)
//                    {
//                        hashlist_.Clear();
//                        //TODOLog(LOG_WARNING,GetResString(IDS_X_SHAREAZA_IMPORT_HASH_SET_CORRUPT),in_filename);
//                    }

//                    br.Close();
//                }
//                else
//                {
//                    //TODO:Log(LOG_WARNING,GetResString(IDS_X_SHAREAZA_IMPORT_NO_HASH_SET),in_filename);
//                    //sdFile.Seek(basePos,CFile::begin); // not found, reset start position
//                }

//                // Close the file
//                sdFile.Close();
//            }
//            catch (Exception)
//            {
//                //TODO:Log
//                //TCHAR buffer[MAX_CFEXP_ERRORMSG];
//                //error.GetErrorMessage(buffer,ARRSIZE(buffer));
//                //LogError(LOG_STATUSBAR, GetResString(IDS_ERR_FILEERROR), in_filename, GetFileName(), buffer);
//                //error.Delete();
//                return false;
//            }

//            // The part below would be a copy of the CPartFile::LoadPartFile, 
//            // so it is smarter to save and reload the file insta dof dougling the whole stuff
//            if (!SavePartFile())
//                return false;

//            hashlist_.Clear();
//            gaplist_.Clear();

//            return LoadPartFile(in_directory, in_filename, false);
//        }

//        public void PartFileHashFinished(KnownFile result)
//        {
//            newdate_ = true;
//            bool errorfound = false;
//            if (ED2KPartHashCount == 0 || HashCount == 0)
//            {
//                if (IsComplete(0, FileSize - (ulong)1, false))
//                {
//                    if (MPDUtilities.Md4Cmp(result.FileHash, FileHash) != 0)
//                    {
//                        //TODO:LogWarning(GetResString(IDS_ERR_FOUNDCORRUPTION), 1, GetFileName());
//                        AddGap(0, FileSize - (ulong)1);
//                        errorfound = true;
//                    }
//                    else
//                    {
//                        if (ED2KPartHashCount != HashCount)
//                        {
//                            Hashset = null;

//                            //ASSERT(result.ED2KPartHashCount == ED2KPartHashCount);
//                            if (SetHashSet(result.Hashset))
//                                hashsetneeded_ = false;
//                        }
//                    }
//                }
//            }
//            else
//            {
//                for (uint i = 0; i < (uint)hashlist_.Count; i++)
//                {
//                    if (i < PartCount && IsComplete((ulong)i * MuleConstants.PARTSIZE,
//                        (ulong)(i + 1) * MuleConstants.PARTSIZE - 1, false))
//                    {
//                        if (!(result.GetPartHash(i) != null &&
//                            MPDUtilities.Md4Cmp(result.GetPartHash(i), GetPartHash(i)) == 0))
//                        {
//                            //TODO:LogWarning(GetResString(IDS_ERR_FOUNDCORRUPTION), i + 1, GetFileName());
//                            AddGap((ulong)i * MuleConstants.PARTSIZE,
//                                ((ulong)((ulong)(i + 1) * MuleConstants.PARTSIZE - 1) >= FileSize) ? ((ulong)FileSize - 1) : ((ulong)(i + 1) * MuleConstants.PARTSIZE - 1));
//                            errorfound = true;
//                        }
//                    }
//                }
//            }
//            if (!errorfound && result.AICHHashSet.Status == AICHStatusEnum.AICH_HASHSETCOMPLETE &&
//                Status == PartFileStatusEnum.PS_COMPLETING)
//            {
//                pAICHHashSet_ = result.AICHHashSet;
//                result.AICHHashSet = null;
//                pAICHHashSet_.Owner = this;
//            }
//            else if (Status == PartFileStatusEnum.PS_COMPLETING)
//            {
//                //TODO:AddDebugLogLine(false, _T("Failed to store new AICH Hashset for completed file %s"), GetFileName());
//            }

//            if (!errorfound)
//            {
//                if (Status == PartFileStatusEnum.PS_COMPLETING)
//                {
//                    //TODO:Log
//                    //if (thePrefs.GetVerbose())
//                    //    AddDebugLogLine(true, _T("Completed file-hashing for \"%s\""), GetFileName());
//                    if (MuleEngine.SharedFiles.GetFileByID(FileHash) == null)
//                        MuleEngine.SharedFiles.SafeAddKFile(this);
//                    CompleteFile(true);
//                    return;
//                }
//                else
//                {
//                    //TODO:AddLogLine(false, GetResString(IDS_HASHINGDONE), GetFileName());
//                }
//            }
//            else
//            {
//                Status = PartFileStatusEnum.PS_READY;
//                //TODO:Log
//                //if (thePrefs.GetVerbose())
//                //    DebugLogError(LOG_STATUSBAR, _T("File-hashing failed for \"%s\""), GetFileName());
//                SavePartFile();
//                return;
//            }
//            //TODO:Log
//            //if (thePrefs.GetVerbose())
//            //    AddDebugLogLine(true, _T("Completed file-hashing for \"%s\""), GetFileName());
//            Status = PartFileStatusEnum.PS_READY;
//            SavePartFile();
//            MuleEngine.SharedFiles.SafeAddKFile(this);
//        }
//    }
//}
