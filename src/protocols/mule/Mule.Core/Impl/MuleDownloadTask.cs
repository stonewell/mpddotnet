using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.File;
using Mule.Definitions;
using Mpd.Generic.Types.IO;
using Mpd.Generic.Types;
using System.IO;
using Mpd.Utilities;
using Mule.AICH;
using Kademlia;
using System.Runtime.InteropServices;
using Mule.ED2K;

namespace Mule.Core.Impl
{
    class MuleDownloadTask : MuleFileTask
    {
        #region Fields
        private UpDownClientList downloadingSourceList_ = new UpDownClientList();
        private UpDownClientList srclist_ = new UpDownClientList();
        private UpDownClientList A4AFsrclist_ = new UpDownClientList(); //<<-- enkeyDEV(Ottavio84) -A4AF-
        private struct MetaTagsStruct
        {
            public MetaTagsStruct(byte name, byte type)
            {
                nName = name;
                nType = type;
            }

            public byte nName;
            public byte nType;
        };

        private static readonly MetaTagsStruct[] MetaTags =
            new MetaTagsStruct[] 
			{
				new MetaTagsStruct( MuleConstants.FT_MEDIA_ARTIST,  2 ),
				new MetaTagsStruct( MuleConstants.FT_MEDIA_ALBUM,   2 ),
				new MetaTagsStruct( MuleConstants.FT_MEDIA_TITLE,   2 ),
				new MetaTagsStruct( MuleConstants.FT_MEDIA_LENGTH,  3 ),
				new MetaTagsStruct( MuleConstants.FT_MEDIA_BITRATE, 3 ),
				new MetaTagsStruct( MuleConstants.FT_MEDIA_CODEC,   2 ),
				new MetaTagsStruct( MuleConstants.FT_FILETYPE,		2 ),
				new MetaTagsStruct( MuleConstants.FT_FILEFORMAT,	2 )
			};

        #endregion

        #region Properties
        public uint KadFileSearchID { get; set; }
        public uint LastSearchTimeKad { get; set; }
        public uint TotalSearchesKad { get; set; }
        public uint LastSearchTime { get; set; }
        public uint ProcessCount { get; set; }
        public PartFile PartFile
        {
            get { return AbstractFile as PartFile; }
            set { AbstractFile = value; }
        }
        #endregion

        public MuleDownloadTask()
        {
            ProcessCount = 0;
        }

        private bool LoadPartFile(string in_directory, string in_filename, bool getsizeonly)
        {
            bool isnewstyle;
            byte version;
            PartFileFormatEnum partmettype = PartFileFormatEnum.PMT_UNKNOWN;

            Dictionary<uint, Gap> gap_map = new Dictionary<uint, Gap>(); // Slugfiller

            PartFile.Transferred = 0;
            PartFile.PartMetFileName = in_filename;
            PartFile.FileDirectory = (in_directory);
            PartFile.FullName = System.IO.Path.Combine(PartFile.FileDirectory, PartFile.PartMetFileName);

            // readfile data form part.met file
            SafeBufferedFile metFile = null;

            try
            {
                metFile =
                    MpdGenericObjectManager.CreateSafeBufferedFile(PartFile.FullName,
                        FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception)
            {
                //TODO:Log
                return false;
            }

            try
            {
                version = metFile.ReadUInt8();

                if (version != Convert.ToByte(VersionsEnum.PARTFILE_VERSION) &&
                    version != Convert.ToByte(VersionsEnum.PARTFILE_SPLITTEDVERSION) &&
                    version != Convert.ToByte(VersionsEnum.PARTFILE_VERSION_LARGEFILE))
                {
                    metFile.Close();
                    if (version == Convert.ToByte(83))
                    {
                        return ImportShareazaTempfile(in_directory, in_filename, getsizeonly);
                    }
                    //TODO:LogError(LOG_STATUSBAR, GetResString(IDS_ERR_BADMETVERSION), partmetfilename_, FileName);
                    return false;
                }

                isnewstyle = (version == Convert.ToByte(VersionsEnum.PARTFILE_SPLITTEDVERSION));
                partmettype = isnewstyle ? PartFileFormatEnum.PMT_SPLITTED : PartFileFormatEnum.PMT_DEFAULTOLD;
                if (!isnewstyle)
                {
                    byte[] test = new byte[4];
                    metFile.Seek(24, SeekOrigin.Begin);
                    metFile.Read(test);

                    metFile.Seek(1, SeekOrigin.Begin);

                    if (test[0] == Convert.ToByte(0) &&
                        test[1] == Convert.ToByte(0) &&
                        test[2] == Convert.ToByte(2) &&
                        test[3] == Convert.ToByte(1))
                    {
                        isnewstyle = true;	// edonkeys so called "old part style"
                        partmettype = PartFileFormatEnum.PMT_NEWOLD;
                    }
                }

                if (isnewstyle)
                {
                    byte[] tmpBuf = new byte[4];
                    metFile.Read(tmpBuf);

                    uint temp = BitConverter.ToUInt32(tmpBuf, 0);

                    if (temp == 0)
                    {	// 0.48 partmets - different again
                        PartFile.LoadHashsetFromFile(metFile, false);
                    }
                    else
                    {
                        byte[] gethash = new byte[16];
                        metFile.Seek(2, SeekOrigin.Begin);
                        PartFile.LoadDateFromFile(metFile);
                        metFile.Read(gethash);
                        MPDUtilities.Md4Cpy(PartFile.FileHash, gethash);
                    }
                }
                else
                {
                    PartFile.LoadDateFromFile(metFile);
                    PartFile.LoadHashsetFromFile(metFile, false);
                }

                uint tagcount = metFile.ReadUInt32();
                for (uint j = 0; j < tagcount; j++)
                {
                    Tag newtag = MpdGenericObjectManager.CreateTag(metFile, false);
                    if (!getsizeonly ||
                        (getsizeonly &&
                        (newtag.NameID == MuleConstants.FT_FILESIZE ||
                        newtag.NameID == MuleConstants.FT_FILENAME)))
                    {
                        switch (newtag.NameID)
                        {
                            case MuleConstants.FT_FILENAME:
                                {
                                    if (!newtag.IsStr)
                                    {
                                        //TODO:Log
                                        return false;
                                    }
                                    if (string.IsNullOrEmpty(PartFile.FileName))
                                        PartFile.FileName = newtag.Str;

                                    break;
                                }
                            case MuleConstants.FT_LASTSEENCOMPLETE:
                                {
                                    if (newtag.IsInt)
                                        PartFile.LastSeenComplete = MPDUtilities.UInt32ToDateTime(newtag.Int);

                                    break;
                                }
                            case MuleConstants.FT_FILESIZE:
                                {
                                    if (newtag.IsInt64(true))
                                        PartFile.FileSize = newtag.Int64;

                                    break;
                                }
                            case MuleConstants.FT_TRANSFERRED:
                                {
                                    if (newtag.IsInt64(true))
                                        PartFile.Transferred = newtag.Int64;

                                    break;
                                }
                            case MuleConstants.FT_COMPRESSION:
                                {
                                    //ASSERT( newtag.IsInt64(true) );
                                    if (newtag.IsInt64(true))
                                        PartFile.CompressionGain = newtag.Int64;

                                    break;
                                }
                            case MuleConstants.FT_CORRUPTED:
                                {
                                    //ASSERT( newtag.IsInt64() );
                                    if (newtag.IsInt64())
                                        PartFile.CorruptionLoss = newtag.Int64;

                                    break;
                                }
                            case MuleConstants.FT_FILETYPE:
                                {
                                    //ASSERT( newtag.IsStr );
                                    if (newtag.IsStr)
                                        PartFile.FileType = newtag.Str;

                                    break;
                                }
                            case MuleConstants.FT_CATEGORY:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                        PartFile.Category = newtag.Int;

                                    break;
                                }
                            case MuleConstants.FT_MAXSOURCES:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                        PartFile.MaxSources = newtag.Int;

                                    break;
                                }
                            case MuleConstants.FT_DLPRIORITY:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                    {
                                        if (!isnewstyle)
                                        {
                                            PartFile.DownPriority = Convert.ToByte(newtag.Int);
                                            if (PartFile.DownPriority == Convert.ToByte(PriorityEnum.PR_AUTO))
                                            {
                                                PartFile.DownPriority = Convert.ToByte(PriorityEnum.PR_HIGH);
                                                PartFile.IsAutoDownPriority = true;
                                            }
                                            else
                                            {
                                                if (PartFile.DownPriority != Convert.ToByte(PriorityEnum.PR_LOW) &&
                                                    PartFile.DownPriority != Convert.ToByte(PriorityEnum.PR_NORMAL) &&
                                                    PartFile.DownPriority != Convert.ToByte(PriorityEnum.PR_HIGH))
                                                    PartFile.DownPriority = Convert.ToByte(PriorityEnum.PR_NORMAL);
                                                PartFile.IsAutoDownPriority = false;
                                            }
                                        }
                                    }

                                    break;
                                }
                            case MuleConstants.FT_STATUS:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                    {
                                        PartFile.Paused = newtag.Int != 0;
                                        PartFile.IsStopped = PartFile.Paused;
                                    }

                                    break;
                                }
                            case MuleConstants.FT_ULPRIORITY:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                    {
                                        if (!isnewstyle)
                                        {
                                            int iUpPriority = Convert.ToInt32(newtag.Int);
                                            if (iUpPriority == Convert.ToInt32(PriorityEnum.PR_AUTO))
                                            {
                                                PartFile.SetUpPriority(Convert.ToByte(PriorityEnum.PR_HIGH), false);
                                                PartFile.IsAutoUpPriority = true;
                                            }
                                            else
                                            {
                                                if (iUpPriority != Convert.ToInt32(PriorityEnum.PR_VERYLOW) &&
                                                    iUpPriority != Convert.ToInt32(PriorityEnum.PR_LOW) &&
                                                    iUpPriority != Convert.ToInt32(PriorityEnum.PR_NORMAL) &&
                                                    iUpPriority != Convert.ToInt32(PriorityEnum.PR_HIGH) &&
                                                    iUpPriority != Convert.ToInt32(PriorityEnum.PR_VERYHIGH))
                                                    iUpPriority = Convert.ToInt32(PriorityEnum.PR_NORMAL);
                                                PartFile.SetUpPriority(Convert.ToByte(iUpPriority), false);
                                                PartFile.IsAutoUpPriority = false;
                                            }
                                        }
                                    }

                                    break;
                                }
                            case MuleConstants.FT_KADLASTPUBLISHSRC:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                    {
                                        PartFile.LastPublishTimeKadSrc = newtag.Int;
                                        PartFile.LastPublishBuddy = 0;

                                        if (PartFile.LastPublishTimeKadSrc > MPDUtilities.Time() + MuleConstants.KADEMLIAREPUBLISHTIMES)
                                        {
                                            //There may be a posibility of an older client that saved a random number here.. This will check for that..
                                            PartFile.LastPublishTimeKadSrc = 0;
                                            PartFile.LastPublishBuddy = 0;
                                        }
                                    }

                                    break;
                                }
                            case MuleConstants.FT_KADLASTPUBLISHNOTES:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                    {
                                        PartFile.LastPublishTimeKadNotes = newtag.Int;
                                    }

                                    break;
                                }
                            case MuleConstants.FT_DL_PREVIEW:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.Int == 1)
                                    {
                                        PartFile.PreviewPriority = true;
                                    }
                                    else
                                    {
                                        PartFile.PreviewPriority = false;
                                    }

                                    break;
                                }

                            // statistics
                            case MuleConstants.FT_ATTRANSFERRED:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                        PartFile.Statistic.AllTimeTransferred = newtag.Int;

                                    break;
                                }
                            case MuleConstants.FT_ATTRANSFERREDHI:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                    {
                                        uint hi, low;
                                        low = Convert.ToUInt32(PartFile.Statistic.AllTimeTransferred & 0xFFFFFFFF);
                                        hi = newtag.Int;
                                        ulong hi2;
                                        hi2 = hi;
                                        hi2 = hi2 << 32;
                                        PartFile.Statistic.AllTimeTransferred = low + hi2;
                                    }

                                    break;
                                }
                            case MuleConstants.FT_ATREQUESTED:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                        PartFile.Statistic.AllTimeRequests = newtag.Int;

                                    break;
                                }
                            case MuleConstants.FT_ATACCEPTED:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                        PartFile.Statistic.AllTimeAccepts = newtag.Int;

                                    break;
                                }

                            // old tags: as long as they are not needed, take the chance to purge them
                            case MuleConstants.FT_PERMISSIONS:
                                //ASSERT( newtag.IsInt );

                                break;
                            case MuleConstants.FT_KADLASTPUBLISHKEY:
                                //ASSERT( newtag.IsInt );

                                break;
                            case MuleConstants.FT_DL_ACTIVE_TIME:
                                //ASSERT( newtag.IsInt );
                                if (newtag.IsInt)
                                    PartFile.DownloadActiveTime = newtag.Int;

                                break;
                            case MuleConstants.FT_CORRUPTEDPARTS:
                                //ASSERT( newtag.IsStr );
                                if (newtag.IsStr)
                                {
                                    //ASSERT( corrupted_list.GetHeadPosition() == NULL );
                                    string strCorruptedParts = newtag.Str;
                                    string[] parts = strCorruptedParts.Split(',');

                                    foreach (string part in parts)
                                    {
                                        uint uPart = 0;
                                        if (MPDUtilities.ScanUInt32(part, ref uPart) == 1)
                                        {
                                            if (uPart < PartFile.PartCount && !PartFile.IsCorruptedPart(uPart))
                                                PartFile.CorruptedList.Add(Convert.ToUInt16(uPart));
                                        }
                                    }
                                }

                                break;
                            case MuleConstants.FT_AICH_HASH:
                                {
                                    //ASSERT( newtag.IsStr );
                                    AICHHash hash = AICHObjectManager.CreateAICHHash();
                                    if (MPDUtilities.DecodeBase32(newtag.Str.ToCharArray(), hash.RawHash) ==
                                        Convert.ToUInt32(MuleConstants.HASHSIZE))
                                    {
                                        PartFile.AICHHashSet.SetMasterHash(hash, AICHStatusEnum.AICH_VERIFIED);
                                    }
                                    else
                                    {
                                        //ASSERT( false );
                                    }

                                    break;
                                }
                            default:
                                {
                                    if (newtag.NameID == 0 &&
                                        (newtag.Name[0] == Convert.ToChar(MuleConstants.FT_GAPSTART) ||
                                        newtag.Name[0] == Convert.ToChar(MuleConstants.FT_GAPEND)))
                                    {
                                        //ASSERT( newtag.IsInt64(true) );
                                        if (newtag.IsInt64(true))
                                        {
                                            Gap gap;
                                            uint gapkey = uint.Parse(newtag.Name.Substring(1));
                                            if (!gap_map.ContainsKey(gapkey))
                                            {
                                                gap = new Gap();
                                                gap_map[gapkey] = gap;
                                                gap.start = 0xFFFFFFFFFFFFFFFF;
                                                gap.end = 0xFFFFFFFFFFFFFFFF;
                                            }
                                            else
                                            {
                                                gap = gap_map[gapkey];
                                            }
                                            if (newtag.Name[0] == Convert.ToChar(MuleConstants.FT_GAPSTART))
                                                gap.start = newtag.Int64;
                                            if (newtag.Name[0] == Convert.ToChar(MuleConstants.FT_GAPEND))
                                                gap.end = newtag.Int64 - 1;
                                        }

                                    }
                                    else
                                        PartFile.TagList.Add(newtag);

                                    break;
                                }
                        }
                    }
                    else
                    {
                    }
                }

                // load the hashsets from the hybridstylepartmet
                if (isnewstyle && !getsizeonly && (metFile.Position < metFile.Length))
                {
                    byte[] tempBuf = new byte[1];
                    metFile.Read(tempBuf);

                    byte temp = tempBuf[0];

                    uint parts = PartFile.PartCount;	// assuming we will get all hashsets

                    for (uint i = 0; i < parts && (metFile.Position + 16 < metFile.Length); i++)
                    {
                        byte[] cur_hash = new byte[16];
                        metFile.Read(cur_hash);
                        PartFile.Hashset.Add(cur_hash);
                    }

                    byte[] checkhash = new byte[16];
                    if (PartFile.Hashset.Count != 0)
                    {
                        byte[] buffer = new byte[PartFile.Hashset.Count * 16];
                        for (int i = 0; i < PartFile.Hashset.Count; i++)
                            MPDUtilities.Md4Cpy(buffer, i * 16, PartFile.Hashset[i], 0, 16);
                        PartFile.CreateHash(buffer, Convert.ToUInt64(PartFile.Hashset.Count * 16), checkhash);
                    }
                    if (MPDUtilities.Md4Cmp(PartFile.FileHash, checkhash) != 0)
                    {
                        PartFile.Hashset.Clear();
                    }
                }

                metFile.Close();
            }
            catch (Exception)
            {
                //TODO:Logif (error.m_cause == CFileException::endOfFile)
                //    LogError(LOG_STATUSBAR, GetResString(IDS_ERR_METCORRUPT), partmetfilename_, FileName);
                //else{
                //    TCHAR buffer[MAX_CFEXP_ERRORMSG];
                //    error.GetErrorMessage(buffer,ARRSIZE(buffer));
                //    LogError(LOG_STATUSBAR, GetResString(IDS_ERR_FILEERROR), partmetfilename_, FileName, buffer);
                //}
                //error.Delete();
                return false;
            }

            if (PartFile.FileSize > MuleConstants.MAX_EMULE_FILE_SIZE)
            {
                //TODO:LogError(LOG_STATUSBAR, GetResString(IDS_ERR_FILEERROR), partmetfilename_, FileName, _T("File size exceeds supported limit"));
                return false;
            }

            if (getsizeonly)
            {
                // AAARGGGHH!!!....
                return partmettype != PartFileFormatEnum.PMT_UNKNOWN;
            }

            // Now to flush the map into the list (Slugfiller)
            foreach (uint gapkey in gap_map.Keys)
            {
                Gap gap = gap_map[gapkey];
                // SLUGFILLER: SafeHash - revised code, and extra safety
                if (gap.start != 0xFFFFFFFFFFFFFFFF &&
                    gap.end != 0xFFFFFFFFFFFFFFFF &&
                    gap.start <= gap.end && gap.start < PartFile.FileSize)
                {
                    if (gap.end >= PartFile.FileSize)
                        gap.end = PartFile.FileSize - 1; // Clipping
                    PartFile.AddGap(gap.start, gap.end); // All tags accounted for, use safe adding
                }
                // SLUGFILLER: SafeHash
            }

            // verify corrupted parts list

            List<ushort> tmpList = new List<ushort>();
            tmpList.AddRange(PartFile.CorruptedList);

            for (int i = 0; i < tmpList.Count; i++)
            {
                uint uCorruptedPart = Convert.ToUInt32(tmpList[i]);

                if (PartFile.IsComplete(Convert.ToUInt64(uCorruptedPart) * MuleConstants.PARTSIZE,
                    Convert.ToUInt64(uCorruptedPart + 1) * MuleConstants.PARTSIZE - 1))
                    PartFile.CorruptedList.RemoveAt(i);
            }

            //check if this is a backup
            if (string.Compare(Path.GetExtension(PartFile.FullName), MuleConstants.PARTMET_TMP_EXT, true) == 0)
                PartFile.FullName = Path.GetFileNameWithoutExtension(PartFile.FullName);

            // open permanent handle
            string searchpath = Path.GetFileNameWithoutExtension(PartFile.FullName);
            try
            {
                PartFile.PartFileStream = new FileStream(searchpath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            }
            catch (Exception)
            {
                //TODO:Log
                return false;
            }

            // read part file creation time
            PartFile.LastModified = MPDUtilities.DateTime2UInt32Time(System.IO.File.GetLastWriteTime(searchpath));
            PartFile.Created = MPDUtilities.DateTime2UInt32Time(System.IO.File.GetCreationTime(searchpath));

            try
            {
                PartFile.FilePath = (searchpath);

                try
                {
                    PartFile.FileAttributes = System.IO.File.GetAttributes(searchpath);
                }
                catch
                {
                    PartFile.FileAttributes = 0;
                }

                // SLUGFILLER: SafeHash - final safety, make sure any missing part of the file is gap
                if (Convert.ToUInt64(PartFile.PartFileStream.Length) < PartFile.FileSize)
                {
                    PartFile.AddGap(Convert.ToUInt64(PartFile.PartFileStream.Length), PartFile.FileSize - 1);
                }
                // Goes both ways - Partfile should never be too large
                if (Convert.ToUInt64(PartFile.PartFileStream.Length) > PartFile.FileSize)
                {
                    //TODO:LogTRACE(_T("Partfile \"%s\" is too large! Truncating %I64u bytes.\n"), FileName, m_hpartfile.Length) - FileSize);
                    PartFile.PartFileStream.SetLength(Convert.ToInt64(PartFile.FileSize));
                }
                // SLUGFILLER: SafeHash

                PartFile.SourcePartFrequency.Clear();
                for (uint i = 0; i < PartFile.PartCount; i++)
                    PartFile.SourcePartFrequency.Add(0);
                PartFile.Status = PartFileStatusEnum.PS_EMPTY;
                // check hashcount, filesatus etc
                if (PartFile.HashCount != PartFile.ED2KPartHashCount)
                {
                    //ASSERT( PartFile.Hashset.GetSize() == 0 );
                    PartFile.HashsetNeeded = true;
                    return true;
                }
                else
                {
                    PartFile.HashsetNeeded = false;
                    for (uint i = 0; i < (uint)PartFile.Hashset.Count; i++)
                    {
                        if (i < PartFile.PartCount &&
                            PartFile.IsComplete((ulong)i * MuleConstants.PARTSIZE,
                            (ulong)(i + 1) * MuleConstants.PARTSIZE - 1))
                        {
                            PartFile.Status = PartFileStatusEnum.PS_READY;
                            break;
                        }
                    }
                }

                if (PartFile.GapList.Count == 0)
                {	// is this file complete already?
                    PartFile.CompleteFile(false);
                    return true;
                }

                if (!isnewstyle) // not for importing
                {
                    // check date of .Part file - if its wrong, rehash file
                    uint fdate = MPDUtilities.DateTime2UInt32Time(System.IO.File.GetLastWriteTimeUtc(searchpath));
                    MPDUtilities.AdjustNTFSDaylightFileTime(ref fdate, searchpath);

                    if (PartFile.UtcLastModified != fdate)
                    {
                        //TODO:Log
                        //string strFileInfo
                        //strFileInfo.Format(_T("%s (%s)"), GetFilePath(), FileName);
                        //LogError(LOG_STATUSBAR, GetResString(IDS_ERR_REHASH), strFileInfo);
                        // rehash
                        PartFile.Status = PartFileStatusEnum.PS_WAITINGFORHASH;
                        try
                        {
                            //AddFileThread addfilethread = MuleEngine.CoreObjectManager.CreateAddFileThread();
                            //PartFile.FileOp = PartFileOpEnum.PFOP_HASHING;
                            //PartFile.FileOpProgress = 0;
                            //addfilethread.SetValues(null, FileDirectory, searchpath, this);
                            //addfilethread.Start();
                        }
                        catch (Exception)
                        {
                            //TODO:LOG:
                            PartFile.Status = PartFileStatusEnum.PS_ERROR;
                        }
                    }
                }
            }
            catch (Exception)
            {
                //TODO:Log
                //string strError;
                //strError.Format(_T("Failed to initialize part file \"%s\" (%s)"), m_hpartfile.GetFilePath(), FileName);
                //TCHAR szError[MAX_CFEXP_ERRORMSG];
                //if (error.GetErrorMessage(szError, ARRSIZE(szError))){
                //    strError += _T(" - ");
                //    strError += szError;
                //}
                //LogError(LOG_STATUSBAR, _T("%s"), strError);
                //error.Delete();
                return false;
            }

            PartFile.UpdateCompletedInfos();
            return true;
        }

        public bool GetNextRequestedBlock(UpDownClient sender,
            RequestedBlockList toAdd, ref ushort count)
        {
            // Check input parameters
            if (sender.PartStatus.Count == 0)
            {
                return false;
            }
            // Define and create the list of the chunks to download
            ushort partCount = PartFile.PartCount;
            ChunkList chunksList = new ChunkList();

            // Main loop
            ushort newBlockCount = 0;
            while (newBlockCount != count)
            {
                // Create a request block stucture if a chunk has been previously selected
                if (sender.LastPartAsked != 0xffff)
                {
                    RequestedBlock pBlock = new RequestedBlock();
                    if (PartFile.GetNextEmptyBlockInPart(sender.LastPartAsked, pBlock) == true)
                    {
                        // Keep a track of all pending requested blocks
                        PartFile.RequestedBlocks.Add(pBlock);
                        // Update list of blocks to return
                        toAdd.Add(pBlock);
                        newBlockCount++;
                        // Skip end of loop (=> CPU load)
                        continue;
                    }
                    else
                    {
                        // => Try to select another chunk
                        sender.LastPartAsked = 0xffff;
                    }
                }

                // Check if a new chunk must be selected (e.g. download starting, previous chunk complete)
                if (sender.LastPartAsked == 0xffff)
                {
                    // Quantify all chunks (create list of chunks to download) 
                    // This is done only one time and only if it is necessary (=> CPU load)
                    if (chunksList.Count == 0)
                    {
                        // Indentify the locally missing part(s) that this source has
                        for (ushort i = 0; i < partCount; ++i)
                        {
                            if (sender.IsPartAvailable(i) == true && PartFile.GetNextEmptyBlockInPart(i, null) == true)
                            {
                                // Create a new entry for this chunk and add it to the list
                                Chunk newEntry = new Chunk();
                                newEntry.Part = i;
                                newEntry.Frequency = PartFile.SourcePartFrequency[i];
                                chunksList.Add(newEntry);
                            }
                        }

                        // Check if any bloks(s) could be downloaded
                        if (chunksList.Count == 0)
                        {
                            break; // Exit main loop while()
                        }

                        // Define the bounds of the three zones (very rare, rare)
                        // more depending on available sources
                        byte modif = 10;
                        if (PartFile.SourceCount > 800)
                        {
                            modif = 2;
                        }
                        else if (PartFile.SourceCount > 200)
                        {
                            modif = 5;
                        }
                        ushort limit = (ushort)(modif * PartFile.SourceCount / 100);
                        if (limit == 0)
                        {
                            limit = 1;
                        }
                        ushort veryRareBound = limit;
                        ushort rareBound = (ushort)(2 * limit);

                        // Cache Preview state (Criterion 2)
                        ED2KFileTypeEnum type =
                            ED2KObjectManager.CreateED2KFileTypes().GetED2KFileTypeID(PartFile.FileName);
                        bool isPreviewEnable =
                            PartFile.PreviewPriority &&
                            (type == ED2KFileTypeEnum.ED2KFT_ARCHIVE || type == ED2KFileTypeEnum.ED2KFT_VIDEO);

                        // Collect and calculate criteria for all chunks
                        foreach (Chunk cur_chunk in chunksList)
                        {
                            // Offsets of chunk
                            ulong uStart = cur_chunk.Part * MuleConstants.PARTSIZE;
                            ulong uEnd =
                                ((PartFile.FileSize - 1) < (uStart + MuleConstants.PARTSIZE - 1)) ?
                                    (PartFile.FileSize - 1) : (uStart + MuleConstants.PARTSIZE - 1);
                            // Criterion 2. Parts used for preview
                            // Remark: - We need to download the first part and the last part(s).
                            //        - When the last part is very small, it's necessary to 
                            //          download the two last parts.
                            bool critPreview = false;
                            if (isPreviewEnable == true)
                            {
                                if (cur_chunk.Part == 0)
                                {
                                    critPreview = true; // First chunk
                                }
                                else if (cur_chunk.Part == partCount - 1)
                                {
                                    critPreview = true; // Last chunk
                                }
                                else if (cur_chunk.Part == partCount - 2)
                                {
                                    // Last chunk - 1 (only if last chunk is too small)
                                    uint sizeOfLastChunk = (uint)(PartFile.FileSize - uEnd);
                                    if (sizeOfLastChunk < MuleConstants.PARTSIZE / 3)
                                    {
                                        critPreview = true; // Last chunk - 1
                                    }
                                }
                            }

                            // Criterion 3. Request state (downloading in process from other source(s))
                            // => CPU load
                            bool critRequested =
                                cur_chunk.Frequency > veryRareBound &&
                                PartFile.IsAlreadyRequested(uStart, uEnd);

                            // Criterion 4. Completion
                            ulong partSize = MuleConstants.PARTSIZE;

                            foreach (Gap cur_gap in PartFile.GapList)
                            {
                                // Check if Gap is into the limit
                                if (cur_gap.start < uStart)
                                {
                                    if (cur_gap.end > uStart && cur_gap.end < uEnd)
                                    {
                                        partSize -= cur_gap.end - uStart + 1;
                                    }
                                    else if (cur_gap.end >= uEnd)
                                    {
                                        partSize = 0;
                                        break; // exit loop for()
                                    }
                                }
                                else if (cur_gap.start <= uEnd)
                                {
                                    if (cur_gap.end < uEnd)
                                    {
                                        partSize -= cur_gap.end - cur_gap.start + 1;
                                    }
                                    else
                                    {
                                        partSize -= uEnd - cur_gap.start + 1;
                                    }
                                }
                            }
                            ushort critCompletion = (ushort)(partSize / (MuleConstants.PARTSIZE / 100)); // in [%]

                            // Calculate priority with all criteria
                            if (cur_chunk.Frequency <= veryRareBound)
                            {
                                // 0..xxxx unrequested + requested very rare chunks
                                cur_chunk.Rank = Convert.ToUInt16((25 * cur_chunk.Frequency) + // Criterion 1
                                ((critPreview == true) ? 0 : 1) + // Criterion 2
                                (100 - critCompletion)); // Criterion 4
                            }
                            else if (critPreview == true)
                            {
                                // 10000..10100  unrequested preview chunks
                                // 30000..30100  requested preview chunks
                                cur_chunk.Rank = Convert.ToUInt16(((critRequested == false) ? 10000 : 30000) + // Criterion 3
                                (100 - critCompletion)); // Criterion 4
                            }
                            else if (cur_chunk.Frequency <= rareBound)
                            {
                                // 10101..1xxxx  unrequested rare chunks
                                // 30101..3xxxx  requested rare chunks
                                cur_chunk.Rank = Convert.ToUInt16((25 * cur_chunk.Frequency) +                 // Criterion 1 
                                ((critRequested == false) ? 10101 : 30101) + // Criterion 3
                                (100 - critCompletion)); // Criterion 4
                            }
                            else
                            {
                                // common chunk
                                if (critRequested == false)
                                { // Criterion 3
                                    // 20000..2xxxx  unrequested common chunks
                                    cur_chunk.Rank = Convert.ToUInt16(20000 + // Criterion 3
                                    (100 - critCompletion)); // Criterion 4
                                }
                                else
                                {
                                    // 40000..4xxxx  requested common chunks
                                    // Remark: The weight of the completion criterion is inversed
                                    //         to spead the requests over the completing chunks.
                                    //         Without this, the chunk closest to completion will
                                    //         received every new sources.
                                    cur_chunk.Rank = Convert.ToUInt16(40000 + // Criterion 3
                                    (critCompletion)); // Criterion 4
                                }
                            }
                        }
                    }

                    // Select the next chunk to download
                    if (chunksList.Count != 0)
                    {
                        // Find and count the chunck(s) with the highest priority
                        ushort chunkCount = 0; // Number of found chunks with same priority
                        ushort rank = 0xffff; // Highest priority found

                        // Collect and calculate criteria for all chunks
                        foreach (Chunk cur_chunk in chunksList)
                        {
                            if (cur_chunk.Rank < rank)
                            {
                                chunkCount = 1;
                                rank = cur_chunk.Rank;
                            }
                            else if (cur_chunk.Rank == rank)
                            {
                                ++chunkCount;
                            }
                        }

                        // Use a random access to avoid that everybody tries to download the 
                        // same chunks at the same time (=> spread the selected chunk among clients)
                        ushort randomness = Convert.ToUInt16(1 + (int)(((float)(chunkCount - 1)) * new Random().NextDouble() / (MPDUtilities.RAND_MAX + 1.0)));

                        foreach (Chunk cur_chunk in chunksList)
                        {
                            if (cur_chunk.Rank == rank)
                            {
                                randomness--;
                                if (randomness == 0)
                                {
                                    // Selection process is over
                                    sender.LastPartAsked = cur_chunk.Part;
                                    // Remark: this list might be reused up to *count times
                                    chunksList.Remove(cur_chunk);
                                    break; // exit loop for()
                                }
                            }
                        }
                    }
                    else
                    {
                        // There is no remaining chunk to download
                        break; // Exit main loop while()
                    }
                }
            }
            // Return the number of the blocks 
            count = newBlockCount;
            // Return
            return (newBlockCount > 0);
        }

        public bool CanAddSource(uint userid,
            ushort port, uint serverip, ushort serverport,
            ref byte pdebug_lowiddropped, bool ed2kID)
        {

            //The incoming ID could have the userid in the Hybrid format.. 
            uint hybridID = 0;
            if (ed2kID)
            {
                if (MuleUtilities.IsLowID(userid))
                {
                    hybridID = userid;
                }
                else
                {
                    hybridID = MuleUtilities.SwapAlways(userid);
                }
            }
            else
            {
                hybridID = userid;
                if (!MuleUtilities.IsLowID(userid))
                {
                    userid = MuleUtilities.SwapAlways(userid);
                }
            }

            // MOD Note: Do not change this part - Merkur
            if (MuleEngine.ServerConnect.IsConnected)
            {
                if (MuleUtilities.IsLowID(MuleEngine.ServerConnect.ClientID))
                {
                    if (MuleEngine.ServerConnect.ClientID == userid &&
                        MuleEngine.ServerConnect.CurrentServer.IP == serverip &&
                        MuleEngine.ServerConnect.CurrentServer.Port == serverport)
                    {
                        return false;
                    }
                    if (MuleEngine.PublicIP == userid)
                    {
                        return false;
                    }
                }
                else
                {
                    if (MuleEngine.ServerConnect.ClientID == userid &&
                        MuleEngine.CoreObjectManager.Preference.Port == port)
                    {
                        return false;
                    }
                }
            }

            if (MuleEngine.KadEngine.IsConnected)
            {
                if (!MuleEngine.KadEngine.IsFirewalled)
                {
                    if (MuleEngine.KadEngine.IPAddress == hybridID &&
                        MuleEngine.CoreObjectManager.Preference.Port == port)
                    {
                        return false;
                    }
                }
            }

            //This allows *.*.*.0 clients to not be removed if Ed2kID == false
            if (MuleUtilities.IsLowID(hybridID) && MuleEngine.IsFirewalled)
            {
                pdebug_lowiddropped++;
                return false;
            }
            // MOD Note - end
            return true;
        }

        public void AddClientSources(SafeMemFile sources,
            SourceFromEnum nSourceFrom,
            byte uClientSXVersion,
            bool bSourceExchange2,
            UpDownClient pClient)
        {
            // Kad reviewed

            if (PartFile.IsStopped)
            {
                return;
            }

            uint nCount = 0;
            byte uPacketSXVersion = 0;
            if (!bSourceExchange2)
            {
                nCount = sources.ReadUInt16();

                // Check if the data size matches the 'nCount' for v1 or v2 and eventually correct the source
                // exchange version while reading the packet data. Otherwise we could experience a higher
                // chance in dealing with wrong source data, userhashs and finally duplicate sources.
                long uDataSize = sources.Length - sources.Position;

                if ((uint)(nCount * (4 + 2 + 4 + 2)) == uDataSize)
                { //Checks if version 1 packet is correct size
                    if (uClientSXVersion != 1)
                    {
                        return;
                    }
                    uPacketSXVersion = 1;
                }
                else if ((uint)(nCount * (4 + 2 + 4 + 2 + 16)) == uDataSize)
                { // Checks if version 2&3 packet is correct size
                    if (uClientSXVersion == 2)
                    {
                        uPacketSXVersion = 2;
                    }
                    else if (uClientSXVersion > 2)
                    {
                        uPacketSXVersion = 3;
                    }
                    else
                    {
                        return;
                    }
                }
                else if (nCount * (4 + 2 + 4 + 2 + 16 + 1) == uDataSize)
                {
                    if (uClientSXVersion != 4)
                    {
                        return;
                    }
                    uPacketSXVersion = 4;
                }
                else
                {
                    // If v5 inserts additional data (like v2), the above code will correctly filter those packets.
                    // If v5 appends additional data after <count>(<Sources>)[count], we are in trouble with the 
                    // above code. Though a client which does not understand v5+ should never receive such a packet.
                    //AddDebugLogLineM(false, logClient, CFormat(wxT("Received invalid source exchange packet (v%u) of data size %u for %s")) % uClientSXVersion % uDataSize % GetFileName());
                    return;
                }
            }
            else
            {
                // for SX2:
                // We only check if the version is known by us and do a quick sanitize check on known version
                // other then SX1, the packet will be ignored if any error appears, sicne it can't be a "misunderstanding" anymore
                if (uClientSXVersion > Convert.ToByte(VersionsEnum.SOURCEEXCHANGE2_VERSION) || uClientSXVersion == 0)
                {
                    //AddDebugLogLineM(false, logPartFile, CFormat(wxT("Invalid source exchange type version: %i")) % uClientSXVersion);
                    return;
                }

                // all known versions use the first 2 bytes as count and unknown version are already filtered above
                nCount = sources.ReadUInt16();
                uint uDataSize = (uint)(sources.Length - sources.Position);
                bool bError = false;
                switch (uClientSXVersion)
                {
                    case 1:
                        bError = nCount * (4 + 2 + 4 + 2) != uDataSize;
                        break;
                    case 2:
                    case 3:
                        bError = nCount * (4 + 2 + 4 + 2 + 16) != uDataSize;
                        break;
                    case 4:
                        bError = nCount * (4 + 2 + 4 + 2 + 16 + 1) != uDataSize;
                        break;
                    default:
                        break;
                }

                if (bError)
                {
                    //AddDebugLogLineM(false, logPartFile, wxT("Invalid source exchange data size."));
                    return;
                }
                uPacketSXVersion = uClientSXVersion;
            }

            for (ushort i = 0; i != nCount; ++i)
            {

                uint dwID = sources.ReadUInt32();
                ushort nPort = sources.ReadUInt16();
                uint dwServerIP = sources.ReadUInt32();
                ushort nServerPort = sources.ReadUInt16();

                byte[] userHash = new byte[16];
                if (uPacketSXVersion > 1)
                {
                    sources.ReadHash16(userHash);
                }

                byte byCryptOptions = 0;
                if (uPacketSXVersion >= 4)
                {
                    byCryptOptions = sources.ReadUInt8();
                }

                //Clients send ID's the the Hyrbid format so highID clients with *.*.*.0 won't be falsely switched to a lowID..
                uint dwIDED2K;
                if (uPacketSXVersion >= 3)
                {
                    dwIDED2K = MuleUtilities.SwapAlways(dwID);
                }
                else
                {
                    dwIDED2K = dwID;
                }

                // check the HighID(IP) - "Filter LAN IPs" and "IPfilter" the received sources IP addresses
                if (!MuleUtilities.IsLowID(dwID))
                {
                    //TODO:
                    //if (!IsGoodIP(dwIDED2K, thePrefs::FilterLanIPs())) {
                    //    // check for 0-IP, localhost and optionally for LAN addresses
                    //    //AddDebugLogLineM(false, logIPFilter, CFormat(wxT("Ignored source (IP=%s) received via %s - bad IP")) % Uint32toStringIP(dwIDED2K) % OriginToText(nSourceFrom));
                    //    continue;
                    //}
                    //if (theApp.ipfilter.IsFiltered(dwIDED2K)) {
                    //    //AddDebugLogLineM(false, logIPFilter, CFormat(wxT("Ignored source (IP=%s) received via %s - IPFilter")) % Uint32toStringIP(dwIDED2K) % OriginToText(nSourceFrom));
                    //    continue;
                    //}
                    if (MuleEngine.ClientList.IsBannedClient(dwIDED2K))
                    {
                        continue;
                    }
                }

                // additionally check for LowID and own IP
                byte lowIdDrop = 0;

                if (!CanAddSource(dwID, nPort, dwServerIP, nServerPort, ref lowIdDrop, false))
                {
                    //AddDebugLogLineM(false, logIPFilter, CFormat(wxT("Ignored source (IP=%s) received via source exchange")) % Uint32toStringIP(dwIDED2K));
                    continue;
                }

                if (MuleEngine.CoreObjectManager.Preference.MaxSourcePerFileDefault > PartFile.SourceCount)
                {
                    UpDownClient newsource =
                        MuleEngine.CoreObjectManager.CreateUpDownClient(nPort, dwID, dwServerIP, nServerPort, this, (uPacketSXVersion < 3), true);
                    if (uPacketSXVersion > 1)
                    {
                        newsource.UserHash = userHash;
                    }

                    if (uPacketSXVersion >= 4)
                    {
                        newsource.SetConnectOptions(byCryptOptions, true, false);
                    }

                    newsource.SourceFrom = nSourceFrom;
                    MuleEngine.DownloadQueue.CheckAndAddSource(PartFile, newsource);

                }
                else
                {
                    break;
                }
            }
        }

        public void AddDownloadingSource(UpDownClient client)
        {
            if (!downloadingSourceList_.Contains(client))
                downloadingSourceList_.Add(client);
        }

        public void RemoveDownloadingSource(UpDownClient client)
        {
            if (downloadingSourceList_.Contains(client))
                downloadingSourceList_.Remove(client);
        }

        public void InitFromSearchFile(SearchFile searchresult)
        {
            InitFromSearchFile(searchresult, 0);
        }

        public void InitFromSearchFile(SearchFile searchresult, uint cat)
        {
            PartFile = FileObjectManager.CreatePartFile();

            if (searchresult.KadNotes is KadEntryList)
            {
                KadEntryList searchKadNotes =
                    searchresult.KadNotes as KadEntryList;

                foreach (KadEntry entry in searchKadNotes)
                {
                    KadNotes.Add(entry.Copy());
                }
            }

            UpdateFileRatingCommentAvail();

            MPDUtilities.Md4Cpy(PartFile.FileHash, searchresult.FileHash);
            foreach (Tag pTag in searchresult.TagList)
            {
                switch (pTag.NameID)
                {
                    case MuleConstants.FT_FILENAME:
                        {
                            if (pTag.IsStr)
                            {
                                if (string.IsNullOrEmpty(PartFile.FileName))
                                    PartFile.SetFileName(pTag.Str, true, true, false);
                            }
                            break;
                        }
                    case MuleConstants.FT_FILESIZE:
                        {
                            if (pTag.IsInt64(true))
                                PartFile.FileSize = pTag.Int64;
                            break;
                        }
                    default:
                        {
                            bool bTagAdded = false;
                            if (pTag.NameID != 0 && pTag.Name == null && (pTag.IsStr || pTag.IsInt))
                            {
                                for (int t = 0; t < MetaTags.Length; t++)
                                {
                                    if (pTag.TagType == MetaTags[t].nType &&
                                        pTag.NameID == MetaTags[t].nName)
                                    {
                                        // skip string tags with empty string values
                                        if (pTag.IsStr && string.IsNullOrEmpty(pTag.Str))
                                            break;

                                        // skip integer tags with '0' values
                                        if (pTag.IsInt && pTag.Int == 0)
                                            break;

                                        Tag newtag = MpdGenericObjectManager.CreateTag(pTag);
                                        PartFile.TagList.Add(newtag);
                                        bTagAdded = true;
                                        break;
                                    }
                                }
                            }

                            if (!bTagAdded)
                            {
                                //TODO:    TRACE(_T("CPartFile::CPartFile(CSearchFile*): ignored tag %s\n"), pTag.GetFullInfo(DbgGetFileMetaTagName));
                            }
                        }
                        break;
                }
            }
            PartFile.CreatePartFile(cat);
            PartFile.Category = cat;
        }

        public void InitializeFromLink(Mule.ED2K.ED2KFileLink fileLink, uint cat)
        {
            PartFile = FileObjectManager.CreatePartFile();

            try
            {
                PartFile.SetFileName(fileLink.Name, true, true, false);
                PartFile.FileSize = fileLink.Size;
                MPDUtilities.Md4Cpy(PartFile.FileHash, fileLink.HashKey);
                if (!MuleEngine.DownloadQueue.IsFileExisting(PartFile.FileHash))
                {
                    if (fileLink.HashSet != null && fileLink.HashSet.Length > 0)
                    {
                        try
                        {
                            if (!PartFile.LoadHashsetFromFile(fileLink.HashSet, true))
                            {
                                //TODO:Log
                                ////ASSERT( PartFile.Hashset.Count == 0 );
                                //AddDebugLogLine(false, _T("eD2K link \"%s\" specified with invalid hashset"), fileLink.Name);
                            }
                            else
                                PartFile.HashsetNeeded = false;
                        }
                        catch (Exception)
                        {
                            //TODO:LOG
                            //TCHAR szError[MAX_CFEXP_ERRORMSG];
                            //e.GetErrorMessage(szError, ARRSIZE(szError));
                            //AddDebugLogLine(false, _T("Error: Failed to process hashset for eD2K link \"%s\" - %s"), fileLink.Name, szError);
                            //e.Delete();
                        }
                    }
                    PartFile.CreatePartFile(cat);
                    PartFile.Category = cat;
                }
                else
                    PartFile.Status = PartFileStatusEnum.PS_ERROR;
            }
            catch (Exception)
            {
                //TODO:Log
                //string strMsg;
                //strMsg.Format(GetResString(IDownloadStateEnum.DS_ERR_INVALIDLINK), error);
                //LogError(LOG_STATUSBAR, GetResString(IDownloadStateEnum.DS_ERR_LINKERROR), strMsg);
                PartFile.Status = PartFileStatusEnum.PS_ERROR;
            }
        }

        public uint Process(uint reducedownload, uint icounter)
        {
            uint nOldTransSourceCount = PartFile.GetSrcStatisticsValue(DownloadStateEnum.DS_DOWNLOADING);
            uint dwCurTick = MPDUtilities.GetTickCount();

            // If buffer size exceeds limit, or if not written within time limit, flush data
            if ((PartFile.TotalBufferData > MuleEngine.CoreObjectManager.Preference.FileBufferSize) ||
                (dwCurTick > (PartFile.LastBufferFlushTime + MuleConstants.BUFFER_TIME_LIMIT)))
            {
                // Avoid flushing while copying preview file
                if (!PartFile.IsPreviewing)
                    PartFile.FlushBuffer();
            }

            PartFile.DataRate = 0;

            // calculate datarate, set limit etc.
            if (icounter < 10)
            {
                uint cur_datarate;
                foreach (UpDownClient cur_src in downloadingSourceList_)
                {
                    if (cur_src != null && cur_src.DownloadState == DownloadStateEnum.DS_DOWNLOADING)
                    {
                        if (cur_src.ClientRequestSocket != null)
                        {
                            cur_src.CheckDownloadTimeout();
                            cur_datarate = cur_src.CalculateDownloadRate();
                            PartFile.DataRate += cur_datarate;
                            if (reducedownload != 0)
                            {
                                uint limit = reducedownload * cur_datarate / 1000;
                                if (limit < 1000 && reducedownload == 200)
                                    limit += 1000;
                                else if (limit < 200 && cur_datarate == 0 && reducedownload >= 100)
                                    limit = 200;
                                else if (limit < 60 && cur_datarate < 600 && reducedownload >= 97)
                                    limit = 60;
                                else if (limit < 20 && cur_datarate < 200 && reducedownload >= 93)
                                    limit = 20;
                                else if (limit < 1)
                                    limit = 1;
                                cur_src.ClientRequestSocket.SetDownloadLimit(limit);
                                if (cur_src.IsDownloadingFromPeerCache &&
                                    cur_src.PeerCacheDownloadSocket != null &&
                                    cur_src.PeerCacheDownloadSocket.IsConnected)
                                    cur_src.PeerCacheDownloadSocket.SetDownloadLimit(limit);
                            }
                        }
                    }
                }
            }
            else
            {
                bool downloadingbefore = PartFile.AnStates[Convert.ToInt32(DownloadStateEnum.DS_DOWNLOADING)] > 0;
                // -khaos--+++> Moved this here, otherwise we were setting our permanent variables to 0 every tenth of a second...
                Array.Clear(PartFile.AnStates, 0, PartFile.AnStates.Length);
                Array.Clear(PartFile.SourceStates, 0, PartFile.SourceStates.Length);
                Array.Clear(PartFile.NetStates, 0, PartFile.NetStates.Length);
                uint nCountForState;

                foreach (UpDownClient cur_src in srclist_)
                {
                    // BEGIN -rewritten- refreshing statistics (no need for temp vars since it is not multithreaded)
                    nCountForState = Convert.ToUInt32(cur_src.DownloadState);
                    //special case which is not yet set as downloadstate
                    if (nCountForState == Convert.ToUInt32(DownloadStateEnum.DS_ONQUEUE))
                    {
                        if (cur_src.IsRemoteQueueFull)
                            nCountForState = Convert.ToUInt32(DownloadStateEnum.DS_REMOTEQUEUEFULL);
                    }

                    // this is a performance killer . avoid calling 'IsBanned' for gathering stats
                    //if (cur_src.IsBanned())
                    //	nCountForState = DownloadStateEnum.DS_BANNED;
                    if (cur_src.UploadState == UploadStateEnum.US_BANNED) // not as accurate as 'IsBanned', but way faster and good enough for stats.
                        nCountForState = Convert.ToUInt32(DownloadStateEnum.DS_BANNED);

                    if (cur_src.SourceFrom >= SourceFromEnum.SF_SERVER &&
                        cur_src.SourceFrom <= SourceFromEnum.SF_PASSIVE)
                        PartFile.SourceStates[Convert.ToInt32(cur_src.SourceFrom)] =
                            PartFile.SourceStates[Convert.ToInt32(cur_src.SourceFrom)]++;

                    if (cur_src.ServerIP != 0 && cur_src.ServerPort != 0)
                    {
                        PartFile.NetStates[0] = PartFile.NetStates[0]++;
                        if (cur_src.KadPort != 0)
                            PartFile.NetStates[2] = PartFile.NetStates[2]++;
                    }
                    if (cur_src.KadPort != 0)
                        PartFile.NetStates[1] = PartFile.NetStates[1]++;

                    PartFile.AnStates[nCountForState] = PartFile.AnStates[nCountForState]++;

                    switch (cur_src.DownloadState)
                    {
                        case DownloadStateEnum.DS_DOWNLOADING:
                            {
                                if (cur_src.ClientRequestSocket != null)
                                {
                                    cur_src.CheckDownloadTimeout();
                                    uint cur_datarate = cur_src.CalculateDownloadRate();
                                    PartFile.DataRate += cur_datarate;
                                    if (reducedownload != 0 && cur_src.DownloadState == DownloadStateEnum.DS_DOWNLOADING)
                                    {
                                        uint limit = reducedownload * cur_datarate / 1000; //(uint)(((float)reducedownload/100)*cur_datarate)/10;		
                                        if (limit < 1000 && reducedownload == 200)
                                            limit += 1000;
                                        else if (limit < 200 && cur_datarate == 0 && reducedownload >= 100)
                                            limit = 200;
                                        else if (limit < 60 && cur_datarate < 600 && reducedownload >= 97)
                                            limit = 60;
                                        else if (limit < 20 && cur_datarate < 200 && reducedownload >= 93)
                                            limit = 20;
                                        else if (limit < 1)
                                            limit = 1;
                                        cur_src.ClientRequestSocket.SetDownloadLimit(limit);
                                        if (cur_src.IsDownloadingFromPeerCache &&
                                            cur_src.PeerCacheDownloadSocket != null &&
                                            cur_src.PeerCacheDownloadSocket.IsConnected)
                                            cur_src.PeerCacheDownloadSocket.SetDownloadLimit(limit);

                                    }
                                    else
                                    {
                                        cur_src.ClientRequestSocket.DisableDownloadLimit();
                                        if (cur_src.IsDownloadingFromPeerCache &&
                                            cur_src.PeerCacheDownloadSocket != null &&
                                            cur_src.PeerCacheDownloadSocket.IsConnected)
                                            cur_src.PeerCacheDownloadSocket.DisableDownloadLimit();
                                    }
                                }
                                break;
                            }
                        // Do nothing with this client..
                        case DownloadStateEnum.DS_BANNED:
                            break;
                        // Check if something has changed with our or their ID state..
                        case DownloadStateEnum.DS_LOWTOLOWIP:
                            {
                                // To Mods, please stop instantly removing these sources..
                                // This causes sources to pop in and out creating extra overhead!
                                //Make sure this source is still a LowID Client..
                                if (cur_src.HasLowID)
                                {
                                    //Make sure we still cannot callback to this Client..
                                    if (!MuleEngine.CanDoCallback(cur_src))
                                    {
                                        //If we are almost maxed on sources, slowly remove these client to see if we can find a better source.
                                        if (((dwCurTick - PartFile.LastPurgeTime) > 30 * 1000) &&
                                            (PartFile.SourceCount >= (PartFile.MaxSources * .8)))
                                        {
                                            MuleEngine.DownloadQueue.RemoveSource(cur_src);
                                            PartFile.LastPurgeTime = dwCurTick;
                                        }
                                        break;
                                    }
                                }
                                // This should no longer be a LOWTOLOWIP..
                                cur_src.DownloadState = DownloadStateEnum.DS_ONQUEUE;
                                break;
                            }
                        case DownloadStateEnum.DS_NONEEDEDPARTS:
                            {
                                // To Mods, please stop instantly removing these sources..
                                // This causes sources to pop in and out creating extra overhead!
                                if ((dwCurTick - PartFile.LastPurgeTime) > 40 * 1000)
                                {
                                    PartFile.LastPurgeTime = dwCurTick;
                                    // we only delete them if reaching the limit
                                    if (PartFile.SourceCount >= (PartFile.MaxSources * .8))
                                    {
                                        MuleEngine.DownloadQueue.RemoveSource(cur_src);
                                        break;
                                    }
                                }
                                // doubled reasktime for no needed parts - save connections and traffic
                                if (cur_src.GetTimeUntilReask() > 0)
                                    break;

                                cur_src.SwapToAnotherFile("A4AF for NNP file. CPartFile::Process()",
                                    true, false, false, null, true, true); // ZZ:DownloadManager
                                // Recheck this client to see if still NNP.. Set to DownloadStateEnum.DS_NONE so that we force a TCP reask next time..
                                cur_src.DownloadState = DownloadStateEnum.DS_NONE;
                                break;
                            }
                        case DownloadStateEnum.DS_ONQUEUE:
                            {
                                // To Mods, please stop instantly removing these sources..
                                // This causes sources to pop in and out creating extra overhead!
                                if (cur_src.IsRemoteQueueFull)
                                {
                                    if (((dwCurTick - PartFile.LastPurgeTime) > 1 * 60 * 1000) &&
                                        (PartFile.SourceCount >= (PartFile.MaxSources * .8)))
                                    {
                                        MuleEngine.DownloadQueue.RemoveSource(cur_src);
                                        PartFile.LastPurgeTime = dwCurTick;
                                        break;
                                    }
                                }
                                //Give up to 1 min for UDP to respond.. If we are within one min of TCP reask, do not try..
                                if (MuleEngine.IsConnected &&
                                    cur_src.GetTimeUntilReask() < 2 * 60 * 1000 &&
                                    cur_src.GetTimeUntilReask() > 1 * 1000 &&
                                    MPDUtilities.GetTickCount() - cur_src.LastTriedToConnectTime > 20 * 60 * 1000) // ZZ:DownloadManager (one resk timestamp for each file)
                                {
                                    cur_src.UDPReaskForDownload();
                                }

                                if (MuleEngine.IsConnected &&
                                    cur_src.GetTimeUntilReask() == 0 &&
                                    MPDUtilities.GetTickCount() - cur_src.LastTriedToConnectTime > 20 * 60 * 1000) // ZZ:DownloadManager (one resk timestamp for each file)
                                {
                                    if (!cur_src.DoesAskForDownload) // NOTE: This may *delete* the client!!
                                        break; //I left this break here just as a reminder just in case re rearange things..
                                }
                                break;
                            }
                        case DownloadStateEnum.DS_CONNECTING:
                        case DownloadStateEnum.DS_TOOMANYCONNS:
                        case DownloadStateEnum.DS_TOOMANYCONNSKAD:
                        case DownloadStateEnum.DS_NONE:
                        case DownloadStateEnum.DS_WAITCALLBACK:
                        case DownloadStateEnum.DS_WAITCALLBACKKAD:
                            {
                                if (MuleEngine.IsConnected &&
                                    cur_src.GetTimeUntilReask() == 0 &&
                                    MPDUtilities.GetTickCount() - cur_src.LastTriedToConnectTime > 20 * 60 * 1000) // ZZ:DownloadManager (one resk timestamp for each file)
                                {
                                    if (!cur_src.DoesAskForDownload) // NOTE: This may *delete* the client!!
                                        break; //I left this break here just as a reminder just in case re rearange things..
                                }
                                break;
                            }
                    }
                }

                if (downloadingbefore != (PartFile.AnStates[Convert.ToInt32(DownloadStateEnum.DS_DOWNLOADING)] > 0))
                    PartFile.NotifyStatusChange();

                if (PartFile.MaxSourcePerFileUDP > PartFile.SourceCount)
                {
                    if (MuleEngine.DownloadQueue.DoKademliaFileRequest() &&
                        (MuleEngine.KadEngine.TotalFile < MuleConstants.KADEMLIATOTALFILE) &&
                        (dwCurTick > LastSearchTimeKad) &&
                        MuleEngine.KadEngine.IsConnected &&
                        MuleEngine.IsConnected &&
                        !PartFile.IsStopped)
                    {
                        //Once we can handle lowID users in Kad, we remove the second IsConnected
                        //Kademlia
                        MuleEngine.DownloadQueue.SetLastKademliaFileRequest();
                        if (KadFileSearchID == 0)
                        {
                            KadSearch pSearch =
                                MuleEngine.KadEngine.SearchManager.PrepareLookup(Convert.ToUInt32(KadSearchTypeEnum.FILE),
                                    true,
                                    MuleEngine.KadEngine.ObjectManager.CreateUInt128(PartFile.FileHash));
                            if (pSearch != null)
                            {
                                if (TotalSearchesKad < 7)
                                    TotalSearchesKad++;
                                LastSearchTimeKad = dwCurTick +
                                    (MuleConstants.KADEMLIAREASKTIME * TotalSearchesKad);
                                KadFileSearchID = pSearch.SearchID;
                            }
                            else
                                KadFileSearchID = 0;
                        }
                    }
                }
                else
                {
                    if (KadFileSearchID != 0)
                    {
                        MuleEngine.KadEngine.SearchManager.StopSearch(KadFileSearchID, true);
                    }
                }

                // check if we want new sources from server
                if (!PartFile.IsLocalSrcReqQueued &&
                    ((LastSearchTime == 0) ||
                    (dwCurTick - LastSearchTime) > MuleConstants.SERVERREASKTIME) &&
                    MuleEngine.ServerConnect.IsConnected &&
                    PartFile.MaxSourcePerFileSoft > PartFile.SourceCount &&
                    !PartFile.IsStopped &&
                    (!PartFile.IsLargeFile ||
                    (MuleEngine.ServerConnect.CurrentServer != null &&
                    MuleEngine.ServerConnect.CurrentServer.DoesSupportsLargeFilesTCP)))
                {
                    PartFile.IsLocalSrcReqQueued = true;
                    MuleEngine.DownloadQueue.SendLocalSrcRequest(PartFile);
                }

                ProcessCount++;
                if (ProcessCount == 3)
                {
                    ProcessCount = 0;
                    PartFile.UpdateAutoDownPriority();
                    PartFile.UpdateDisplayedInfo();
                    PartFile.UpdateCompletedInfos();
                }
            }

            if (PartFile.GetSrcStatisticsValue(DownloadStateEnum.DS_DOWNLOADING) != nOldTransSourceCount)
            {
                PartFile.UpdateDisplayedInfo(true);
            }

            return PartFile.DataRate;
        }

        public bool ImportShareazaTempfile(string in_directory, string in_filename, bool getsizeonly)
        {
            string fullname = System.IO.Path.Combine(in_directory, in_filename);

            // open the file
            Stream sdFile = null;

            try
            {
                sdFile = new FileStream(fullname, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception)
            {
                //TODO:Log
                return false;
            }
            //	setvbuf(sdFile.m_pStream, NULL, _IOFBF, 16384);

            try
            {
                BinaryReader br = new BinaryReader(sdFile);

                // Is it a valid Shareaza temp file?
                byte[] szID = new byte[3];
                br.Read(szID, 0, szID.Length);

                string tmpStrID = Encoding.Default.GetString(szID);

                if (!"SDL".Equals(tmpStrID))
                {
                    br.Close();
                    sdFile.Close();
                    return false;
                }

                // Get the version
                int nVersion = br.ReadInt32();

                // Get the File Name
                string sRemoteName = br.ReadString();
                PartFile.FileName = sRemoteName;

                ulong lSize = br.ReadUInt64();
                ulong nSize = lSize;

                PartFile.FileSize = lSize;

                // Get the ed2k hash
                bool bSHA1, bTiger, bMD5, bED2K, Trusted; bMD5 = false; bED2K = false;
                byte[] pSHA1 = new byte[20];
                byte[] pTiger = new byte[24];
                byte[] pMD5 = new byte[16];
                byte[] pED2K = new byte[16];

                bSHA1 = br.ReadBoolean();
                if (bSHA1) br.Read(pSHA1, 0, pSHA1.Length);
                if (nVersion >= 31) Trusted = br.ReadBoolean();

                bTiger = br.ReadBoolean();
                if (bTiger) br.Read(pTiger, 0, pTiger.Length);
                if (nVersion >= 31) Trusted = br.ReadBoolean();

                if (nVersion >= 22) bMD5 = br.ReadBoolean();
                if (bMD5) br.Read(pMD5, 0, pMD5.Length);
                if (nVersion >= 31) Trusted = br.ReadBoolean();

                if (nVersion >= 13) bED2K = br.ReadBoolean();
                if (bED2K) br.Read(pED2K, 0, pED2K.Length);
                if (nVersion >= 31) Trusted = br.ReadBoolean();

                br.Close();

                if (bED2K)
                {
                    MPDUtilities.Md4Cpy(PartFile.FileHash, pED2K);
                }
                else
                {
                    //TODO:Log(LOG_ERROR,GetResString(IDS_X_SHAREAZA_IMPORT_NO_HASH),in_filename);
                    sdFile.Close();
                    return false;
                }

                if (getsizeonly)
                {
                    sdFile.Close();
                    return true;
                }

                // Now the tricky part
                long basePos = sdFile.Position;

                // Try to to get the gap list
                if (MPDUtilities.GotoString(sdFile,
                    nVersion >= 29 ? BitConverter.GetBytes(lSize) : BitConverter.GetBytes(nSize),
                    nVersion >= 29 ? 8 : 4)) // search the gap list
                {
                    sdFile.Seek(sdFile.Position - (nVersion >= 29 ? 8 : 4), SeekOrigin.Begin); // - file size
                    br = new BinaryReader(sdFile);

                    bool badGapList = false;

                    if (nVersion >= 29)
                    {
                        long nTotal, nRemaining;
                        uint nFragments;

                        nTotal = br.ReadInt64();
                        nRemaining = br.ReadInt64();
                        nFragments = br.ReadUInt32();

                        if (nTotal >= nRemaining)
                        {
                            long begin, length;
                            for (; nFragments-- > 0; )
                            {
                                begin = br.ReadInt64();
                                length = br.ReadInt64();

                                if (begin + length > nTotal)
                                {
                                    badGapList = true;
                                    break;
                                }
                                PartFile.AddGap(Convert.ToUInt64(begin),
                                    Convert.ToUInt64((begin + length - 1)));
                            }
                        }
                        else
                        {
                            badGapList = true;
                        }
                    }
                    else
                    {
                        uint nTotal, nRemaining;
                        uint nFragments;
                        nTotal = br.ReadUInt32();
                        nRemaining = br.ReadUInt32();
                        nFragments = br.ReadUInt32();

                        if (nTotal >= nRemaining)
                        {
                            uint begin, length;
                            for (; nFragments-- > 0; )
                            {
                                begin = br.ReadUInt32();
                                length = br.ReadUInt32();
                                if (begin + length > nTotal)
                                {
                                    badGapList = true;
                                    break;
                                }
                                PartFile.AddGap(begin, begin + length - 1);
                            }
                        }
                        else
                        {
                            badGapList = true;
                        }
                    }

                    if (badGapList)
                    {
                        PartFile.GapList.Clear();
                        //TODO:Log(LOG_WARNING,GetResString(IDS_X_SHAREAZA_IMPORT_GAP_LIST_CORRUPT),in_filename);
                    }

                    br.Close();
                }
                else
                {
                    //TODO:Log(LOG_WARNING,GetResString(IDS_X_SHAREAZA_IMPORT_NO_GAP_LIST),in_filename);
                    sdFile.Seek(basePos, SeekOrigin.Begin); // not found, reset start position
                }

                // Try to get the complete hashset
                if (MPDUtilities.GotoString(sdFile, PartFile.FileHash, 16)) // search the hashset
                {
                    sdFile.Seek(sdFile.Position - 16 - 4, SeekOrigin.Begin); // - list size - hash length
                    br = new BinaryReader(sdFile);

                    uint nCount = br.ReadUInt32();

                    byte[] pMD4 = new byte[16];
                    br.Read(pMD4, 0, pMD4.Length); // read the hash again

                    // read the hashset
                    for (uint i = 0; i < nCount; i++)
                    {
                        byte[] curhash = new byte[16];
                        br.Read(curhash, 0, 16);
                        PartFile.Hashset.Add(curhash);
                    }

                    byte[] checkhash = new byte[16];
                    if (PartFile.Hashset.Count != 0)
                    {
                        byte[] buffer = new byte[PartFile.Hashset.Count * 16];
                        for (int i = 0; i < PartFile.Hashset.Count; i++)
                            MPDUtilities.Md4Cpy(buffer, (i * 16), PartFile.Hashset[i], 0, 16);
                        PartFile.CreateHash(buffer, Convert.ToUInt64(PartFile.Hashset.Count * 16), checkhash);
                    }
                    if (MPDUtilities.Md4Cmp(pMD4, checkhash) != 0)
                    {
                        PartFile.Hashset.Clear();
                        //TODOLog(LOG_WARNING,GetResString(IDS_X_SHAREAZA_IMPORT_HASH_SET_CORRUPT),in_filename);
                    }

                    br.Close();
                }
                else
                {
                    //TODO:Log(LOG_WARNING,GetResString(IDS_X_SHAREAZA_IMPORT_NO_HASH_SET),in_filename);
                    //sdFile.Seek(basePos,CFile::begin); // not found, reset start position
                }

                // Close the file
                sdFile.Close();
            }
            catch (Exception)
            {
                //TODO:Log
                //TCHAR buffer[MAX_CFEXP_ERRORMSG];
                //error.GetErrorMessage(buffer,ARRSIZE(buffer));
                //LogError(LOG_STATUSBAR, GetResString(IDS_ERR_FILEERROR), in_filename, GetFileName(), buffer);
                //error.Delete();
                return false;
            }

            // The part below would be a copy of the CPartFile::LoadPartFile, 
            // so it is smarter to save and reload the file insta dof dougling the whole stuff
            if (!PartFile.SavePartFile())
                return false;

            PartFile.Hashset.Clear();
            PartFile.GapList.Clear();

            return LoadPartFile(in_directory, in_filename, false);
        }

        public void PartFileHashFinished(KnownFile result)
        {
            PartFile.PartFileUpdated = true;
            bool errorfound = false;
            if (PartFile.ED2KPartHashCount == 0 || PartFile.HashCount == 0)
            {
                if (PartFile.IsComplete(0, PartFile.FileSize - (ulong)1))
                {
                    if (MPDUtilities.Md4Cmp(result.FileHash, PartFile.FileHash) != 0)
                    {
                        //TODO:LogWarning(GetResString(IDS_ERR_FOUNDCORRUPTION), 1, GetFileName());
                        PartFile.AddGap(0, PartFile.FileSize - (ulong)1);
                        errorfound = true;
                    }
                    else
                    {
                        if (PartFile.ED2KPartHashCount != PartFile.HashCount)
                        {
                            //ASSERT(result.ED2KPartHashCount == ED2KPartHashCount);
                            PartFile.Hashset = result.Hashset;
                        }
                    }
                }
            }
            else
            {
                for (uint i = 0; i < (uint)PartFile.Hashset.Count; i++)
                {
                    if (i < PartFile.PartCount && PartFile.IsComplete((ulong)i * MuleConstants.PARTSIZE,
                        (ulong)(i + 1) * MuleConstants.PARTSIZE - 1))
                    {
                        if (!(result.GetPartHash(i) != null &&
                            MPDUtilities.Md4Cmp(result.GetPartHash(i), PartFile.GetPartHash(i)) == 0))
                        {
                            //TODO:LogWarning(GetResString(IDS_ERR_FOUNDCORRUPTION), i + 1, GetFileName());
                            PartFile.AddGap((ulong)i * MuleConstants.PARTSIZE,
                                ((ulong)((ulong)(i + 1) * MuleConstants.PARTSIZE - 1) >= PartFile.FileSize) ? ((ulong)PartFile.FileSize - 1) : ((ulong)(i + 1) * MuleConstants.PARTSIZE - 1));
                            errorfound = true;
                        }
                    }
                }
            }
            if (!errorfound && result.AICHHashSet.Status == AICHStatusEnum.AICH_HASHSETCOMPLETE &&
                PartFile.Status == PartFileStatusEnum.PS_COMPLETING)
            {
                PartFile.AICHHashSet = result.AICHHashSet;
            }
            else if (PartFile.Status == PartFileStatusEnum.PS_COMPLETING)
            {
                //TODO:AddDebugLogLine(false, _T("Failed to store new AICH Hashset for completed file %s"), GetFileName());
            }

            if (!errorfound)
            {
                if (PartFile.Status == PartFileStatusEnum.PS_COMPLETING)
                {
                    //TODO:Log
                    //if (thePrefs.GetVerbose())
                    //    AddDebugLogLine(true, _T("Completed file-hashing for \"%s\""), GetFileName());
                    if (MuleEngine.SharedFiles.GetFileByID(PartFile.FileHash) == null)
                        MuleEngine.SharedFiles.SafeAddKFile(PartFile);
                    PartFile.CompleteFile(true);
                    return;
                }
                else
                {
                    //TODO:AddLogLine(false, GetResString(IDS_HASHINGDONE), GetFileName());
                }
            }
            else
            {
                PartFile.Status = PartFileStatusEnum.PS_READY;
                //TODO:Log
                //if (thePrefs.GetVerbose())
                //    DebugLogError(LOG_STATUSBAR, _T("File-hashing failed for \"%s\""), GetFileName());
                PartFile.SavePartFile();
                return;
            }
            //TODO:Log
            //if (thePrefs.GetVerbose())
            //    AddDebugLogLine(true, _T("Completed file-hashing for \"%s\""), GetFileName());
            PartFile.Status = PartFileStatusEnum.PS_READY;
            PartFile.SavePartFile();
            MuleEngine.SharedFiles.SafeAddKFile(PartFile);
        }
    }
}
