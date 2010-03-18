#region File Header

//
//  Copyright (C) 2008 Jingnan Si
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
//
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using Mule.ED2K;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using Mule.AICH;
using Mpd.Generic.IO;

using Mpd.Generic;
using Mpd.Utilities;

namespace Mule.File.Impl
{
    class PartFileImpl : KnownFileImpl, PartFile
    {
        #region Fields
        private uint[] anStates_ = new uint[MuleConstants.STATES_COUNT];
        private GapList gaplist_ = new GapList();
        private RequestedBlockList requestedblocks_list_ = new RequestedBlockList();
        private List<ushort> srcpartFrequency_ = new List<ushort>();
        private List<ushort> corrupted_list_ = new List<ushort>();
        private List<PartFileBufferedData> bufferedData_list_ = new List<PartFileBufferedData>();
        private System.Threading.Mutex fileCompleteMutex_ = new Mutex();		// Lord KiRon - Mutex for file completion
        private ushort[] src_stats_ = new ushort[4];
        private ushort[] net_stats_ = new ushort[3];

        public uint Category { get; set; }
        public bool PartFileUpdated { get; set; }
        public uint LastBufferFlushTime { get; set; }
        public ulong TotalBufferData { get; set; }
        public uint[] AnStates { get { return anStates_; } set { anStates_ = value; } }
        public bool Paused { get; set; }
        public PriorityEnum DownPriority { get; set; }
        public uint LastPurgeTime { get; set; }
        public System.IO.FileAttributes FileAttributes { get; set; }
        public uint DownloadActiveTime { get; set; }
        public ushort[] SourceStates { get { return src_stats_; } set { src_stats_ = value; } }
        public ushort[] NetStates { get { return net_stats_; } set { net_stats_ = value; } }
        public bool IsLocalSrcReqQueued { get; set; }
        public bool IsPreviewing { get; set; }
        public System.IO.Stream PartFileStream { get; set; }
        public uint Created { get; set; }
        public DateTime LastSeenComplete { get; set; }
        public List<ushort> CorruptedList { get { return corrupted_list_; } }
        public List<ushort> SourcePartFrequency { get { return srcpartFrequency_; } }
        public bool HashsetNeeded { get; set; }
        public ulong GainDueToCompression { get; set; }
        public RequestedBlockList RequestedBlocks
        {
            get { return requestedblocks_list_; }
            set { requestedblocks_list_ = value; }
        }
        #endregion

        #region Constructors
        public PartFileImpl()
            : this(0)
        {
        }
        public PartFileImpl(uint cat)
        {
            Init();
            Category = cat;
        }
        #endregion

        #region PartFile Members

        public string PartMetFileName { get; set; }

        public string FullName { get; set; }

        public string TempPath
        {
            get { return System.IO.Path.GetDirectoryName(FullName); }
        }

        public bool IsNormalFile
        {
            get
            {
                return (FileAttributes & (System.IO.FileAttributes.Compressed | System.IO.FileAttributes.SparseFile)) == 0;
            }
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetCompressedFileSize(string filepath, out uint filesizehigh);

        public ulong RealFileSize
        {
            get
            {
                uint low = 0, high = 0;
                low = GetCompressedFileSize(FilePath, out high);

                ulong size = high << 32;

                size += low;

                return size;
            }
        }

        public void GetLeftToTransferAndAdditionalNeededSpace(ref ulong rui64LeftToTransfer,
            ref ulong rui64AdditionalNeededSpace)
        {
            ulong uSizeLastGap = 0;
            foreach (Gap cur_gap in gaplist_)
            {
                ulong uGapSize = cur_gap.end - cur_gap.start;
                rui64LeftToTransfer += uGapSize;
                if (cur_gap.end == FileSize - 1)
                    uSizeLastGap = uGapSize;
            }

            if (IsNormalFile)
            {
                // File is not NTFS-Compressed nor NTFS-Sparse
                if (FileSize == RealFileSize) // already fully allocated?
                    rui64AdditionalNeededSpace = 0;
                else
                    rui64AdditionalNeededSpace = uSizeLastGap;
            }
            else
            {
                // File is NTFS-Compressed or NTFS-Sparse
                rui64AdditionalNeededSpace = rui64LeftToTransfer;
            }
        }

        public ulong NeededSpace
        {
            get
            {
                if (Convert.ToUInt64(PartFileStream.Length) > FileSize)
                    return 0;
                return FileSize - Convert.ToUInt64(PartFileStream.Length);
            }
        }

        public DateTime LastModifiedDate
        {
            get { return new DateTime(LastModified); }
        }

        public uint LastModified { get; set; }

        public DateTime CreatedDate
        {
            get { return new DateTime(Created); }
        }

        public bool HashSinglePart(uint partnumber)
        {
            if ((HashCount <= partnumber) && (PartCount > 1))
            {
                return true;
            }
            else if (GetPartHash(partnumber) == null && PartCount != 1)
            {
                return true;
            }
            else
            {
                byte[] hashresult = new byte[16];
                PartFileStream.Seek(Convert.ToInt64(MuleConstants.PARTSIZE * partnumber), SeekOrigin.Begin);
                uint length = Convert.ToUInt32(MuleConstants.PARTSIZE);
                if (Convert.ToInt64(MuleConstants.PARTSIZE * (partnumber + 1)) > PartFileStream.Length)
                {
                    length = Convert.ToUInt32(PartFileStream.Length - Convert.ToInt64(MuleConstants.PARTSIZE * partnumber));
                    //ASSERT( length <= PARTSIZE );
                }
                CreateHash(PartFileStream, length, hashresult, null);

                if (PartCount > 1 || FileSize == MuleConstants.PARTSIZE)
                {
                    if (MpdUtilities.Md4Cmp(hashresult, GetPartHash(partnumber)) != 0)
                        return false;
                    else
                        return true;
                }
                else
                {
                    if (MpdUtilities.Md4Cmp(hashresult, FileHash) != 0)
                        return false;
                    else
                        return true;
                }
            }
        }

        public void AddGap(ulong start, ulong end)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void FillGap(ulong start, ulong end)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool IsComplete(ulong start, ulong end)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool IsPureGap(ulong start, ulong end)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool IsAlreadyRequested(ulong start, ulong end)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool ShrinkToAvoidAlreadyRequested(ref ulong start, ref ulong end)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool IsCorruptedPart(uint partnumber)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public ulong GetTotalGapSizeInRange(ulong uRangeStart, ulong uRangeEnd)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public ulong GetTotalGapSizeInPart(uint uPart)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void UpdateCompletedInfos()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void UpdateCompletedInfos(ulong uTotalGaps)
        {
            throw new Exception("The method or operation is not implemented.");
        }


        public void WritePartStatus(SafeMemFile file)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void WriteCompleteSourcesCount(SafeMemFile file)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void AddSources(SafeMemFile sources, uint serverip, ushort serverport, bool bWithObfuscationAndHash)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void AddSource(string pszURL, uint nIP)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public PartFileStatusEnum Status { get; set; }

        public void NotifyStatusChange()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool IsStopped { get; set; }

        public bool CompletionError
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public ulong CompletedSize
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public string PartfileStatus
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public int PartfileStatusRang
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public void SetActive(bool bActive)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool IsAutoDownPriority { get; set; }

        public void UpdateAutoDownPriority()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public uint SourceCount { get; set; }

        public uint SrcA4AFCount { get; set; }

        public uint GetSrcStatisticsValue(DownloadStateEnum nDLState)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public uint TransferringSrcCount { get; set; }

        public ulong Transferred { get; set; }

        public uint DataRate { get; set; }

        public float PercentCompleted { get; set; }

        public uint NotCurrentSourcesCount { get; set; }

        public int ValidSourcesCount { get; set; }

        public bool IsArchive(bool onlyPreviewable)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool IsPreviewableFileType
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public ulong TimeRemaining { get; set; }

        public ulong TimeRemainingSimple { get; set; }

        public void FlushBuffer()
        {
            FlushBuffer(false, false, false);
        }

        public void FlushBuffer(bool forcewai)
        {
            FlushBuffer(forcewai, false, false);
        }
        public void FlushBuffer(bool forcewait, bool bForceICH)
        {
            FlushBuffer(forcewait, bForceICH, false);
        }

        public void FlushBuffer(bool forcewait, bool bForceICH, bool bNoAICH)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public GapList GapList
        {
            get { return gaplist_; }
        }

        public void RemoveAllRequestedBlocks()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool RemoveBlockFromList(ulong start, ulong end)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool IsInRequestedBlockList(RequestedBlock block)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void RemoveAllSources(bool bTryToSwap)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool CanOpenFile
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool IsReadyForPreview
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool CanStopFile
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool CanPauseFile
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool CanResumeFile
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public void OpenFile()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void PreviewFile()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void DeleteFile()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void StopFile(bool bCancel, bool resort)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void PauseFile(bool bInsufficient, bool resort)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void StopPausedFile()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void ResumeFile(bool resort)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void ResumeFileInsufficient()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public uint AvailablePartCount
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public void UpdateAvailablePartsCount()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public uint LastAnsweredTime
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public void UpdateLastAnsweredTimeTimeout()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public ulong CorruptionLoss { get; set; }

        public ulong CompressionGain { get; set; }

        public uint RecoveredPartsByICH
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public string GetProgressString(ushort size)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public string GetInfoSummary()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void UpdateDisplayedInfo()
        {
            UpdateDisplayedInfo(false);
        }

        public void UpdateDisplayedInfo(bool force)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool CheckShowItemInGivenCat(int inCategory)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public byte[] MMCreatePartStatus()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void PerformFileCompleteEnd(uint dwResult)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public PartFileOpEnum FileOp
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public uint FileOpProgress
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public void RequestAICHRecovery(uint nPart)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void AICHRecoveryDataAvailable(uint nPart)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool AllowSwapForSourceExchange
        {
            get;set;
        }

        public void SetSwapForSourceExchangeTick()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public uint PrivateMaxSources { get; set; }

        public uint MaxSources { get; set; }

        public uint MaxSourcePerFileSoft
        {
            get
            {
                uint temp = (MaxSources * 9) / 10;
                if (temp > MuleConstants.MAX_SOURCES_FILE_SOFT)
                {
                    return MuleConstants.MAX_SOURCES_FILE_SOFT;
                }
                return temp;
            }
        }

        public uint MaxSourcePerFileUDP
        {
            get
            {
                uint temp = (MaxSources * 3) / 4;
                if (temp > MuleConstants.MAX_SOURCES_FILE_UDP)
                {
                    return MuleConstants.MAX_SOURCES_FILE_UDP;
                }

                return temp;
            }
        }

        public bool PreviewPriority { get; set; }

        public bool LoadPartFile(string in_directory, string in_filename, bool getsizeonly)
        {
            bool isnewstyle;
            byte version;
            PartFileFormatEnum partmettype = PartFileFormatEnum.PMT_UNKNOWN;

            Dictionary<uint, Gap> gap_map = new Dictionary<uint, Gap>(); // Slugfiller

            Transferred = 0;
            PartMetFileName = in_filename;
            FileDirectory = (in_directory);
            FullName = System.IO.Path.Combine(FileDirectory, PartMetFileName);

            // readfile data form part.met file
            SafeBufferedFile metFile = null;

            try
            {
                metFile =
                    MpdObjectManager.CreateSafeBufferedFile(FullName,
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
                        LoadHashsetFromFile(metFile, false);
                    }
                    else
                    {
                        byte[] gethash = new byte[16];
                        metFile.Seek(2, SeekOrigin.Begin);
                        LoadDateFromFile(metFile);
                        metFile.Read(gethash);
                        MpdUtilities.Md4Cpy(FileHash, gethash);
                    }
                }
                else
                {
                    LoadDateFromFile(metFile);
                    LoadHashsetFromFile(metFile, false);
                }

                uint tagcount = metFile.ReadUInt32();
                for (uint j = 0; j < tagcount; j++)
                {
                    Tag newtag = MpdObjectManager.CreateTag(metFile, false);
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
                                    if (string.IsNullOrEmpty(FileName))
                                        FileName = newtag.Str;

                                    break;
                                }
                            case MuleConstants.FT_LASTSEENCOMPLETE:
                                {
                                    if (newtag.IsInt)
                                        LastSeenComplete = MpdUtilities.UInt32ToDateTime(newtag.Int);

                                    break;
                                }
                            case MuleConstants.FT_FILESIZE:
                                {
                                    if (newtag.IsInt64(true))
                                        FileSize = newtag.Int64;

                                    break;
                                }
                            case MuleConstants.FT_TRANSFERRED:
                                {
                                    if (newtag.IsInt64(true))
                                        Transferred = newtag.Int64;

                                    break;
                                }
                            case MuleConstants.FT_COMPRESSION:
                                {
                                    //ASSERT( newtag.IsInt64(true) );
                                    if (newtag.IsInt64(true))
                                        CompressionGain = newtag.Int64;

                                    break;
                                }
                            case MuleConstants.FT_CORRUPTED:
                                {
                                    //ASSERT( newtag.IsInt64() );
                                    if (newtag.IsInt64())
                                        CorruptionLoss = newtag.Int64;

                                    break;
                                }
                            case MuleConstants.FT_FILETYPE:
                                {
                                    //ASSERT( newtag.IsStr );
                                    if (newtag.IsStr)
                                        FileType = newtag.Str;

                                    break;
                                }
                            case MuleConstants.FT_CATEGORY:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                        Category = newtag.Int;

                                    break;
                                }
                            case MuleConstants.FT_MAXSOURCES:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                        MaxSources = newtag.Int;

                                    break;
                                }
                            case MuleConstants.FT_DLPRIORITY:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                    {
                                        if (!isnewstyle)
                                        {
                                            if (Enum.IsDefined(typeof(PriorityEnum), newtag.Int))
                                                DownPriority = (PriorityEnum)newtag.Int;
                                            else
                                                DownPriority = PriorityEnum.PR_NORMAL;

                                            if (DownPriority == PriorityEnum.PR_AUTO)
                                            {
                                                DownPriority =PriorityEnum.PR_HIGH;
                                                IsAutoDownPriority = true;
                                            }
                                            else
                                            {
                                                if (DownPriority != PriorityEnum.PR_LOW &&
                                                    DownPriority != PriorityEnum.PR_NORMAL &&
                                                    DownPriority != PriorityEnum.PR_HIGH)
                                                    DownPriority = PriorityEnum.PR_NORMAL;
                                                IsAutoDownPriority = false;
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
                                        Paused = newtag.Int != 0;
                                        IsStopped = Paused;
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
                                                SetUpPriority(PriorityEnum.PR_HIGH, false);
                                                IsAutoUpPriority = true;
                                            }
                                            else
                                            {
                                                if (iUpPriority != Convert.ToInt32(PriorityEnum.PR_VERYLOW) &&
                                                    iUpPriority != Convert.ToInt32(PriorityEnum.PR_LOW) &&
                                                    iUpPriority != Convert.ToInt32(PriorityEnum.PR_NORMAL) &&
                                                    iUpPriority != Convert.ToInt32(PriorityEnum.PR_HIGH) &&
                                                    iUpPriority != Convert.ToInt32(PriorityEnum.PR_VERYHIGH))
                                                    iUpPriority = Convert.ToInt32(PriorityEnum.PR_NORMAL);
                                                SetUpPriority((PriorityEnum)iUpPriority, false);
                                                IsAutoUpPriority = false;
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
                                        LastPublishTimeKadSrc = newtag.Int;
                                        LastPublishBuddy = 0;

                                        if (LastPublishTimeKadSrc > MpdUtilities.Time() + MuleConstants.KADEMLIAREPUBLISHTIMES)
                                        {
                                            //There may be a posibility of an older client that saved a random number here.. This will check for that..
                                            LastPublishTimeKadSrc = 0;
                                            LastPublishBuddy = 0;
                                        }
                                    }

                                    break;
                                }
                            case MuleConstants.FT_KADLASTPUBLISHNOTES:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                    {
                                        LastPublishTimeKadNotes = newtag.Int;
                                    }

                                    break;
                                }
                            case MuleConstants.FT_DL_PREVIEW:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.Int == 1)
                                    {
                                        PreviewPriority = true;
                                    }
                                    else
                                    {
                                        PreviewPriority = false;
                                    }

                                    break;
                                }

                            // statistics
                            case MuleConstants.FT_ATTRANSFERRED:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                        Statistic.AllTimeTransferred = newtag.Int;

                                    break;
                                }
                            case MuleConstants.FT_ATTRANSFERREDHI:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                    {
                                        uint hi, low;
                                        low = Convert.ToUInt32(Statistic.AllTimeTransferred & 0xFFFFFFFF);
                                        hi = newtag.Int;
                                        ulong hi2;
                                        hi2 = hi;
                                        hi2 = hi2 << 32;
                                        Statistic.AllTimeTransferred = low + hi2;
                                    }

                                    break;
                                }
                            case MuleConstants.FT_ATREQUESTED:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                        Statistic.AllTimeRequests = newtag.Int;

                                    break;
                                }
                            case MuleConstants.FT_ATACCEPTED:
                                {
                                    //ASSERT( newtag.IsInt );
                                    if (newtag.IsInt)
                                        Statistic.AllTimeAccepts = newtag.Int;

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
                                    DownloadActiveTime = newtag.Int;

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
                                        if (MpdUtilities.ScanUInt32(part, ref uPart) == 1)
                                        {
                                            if (uPart < PartCount && !IsCorruptedPart(uPart))
                                                CorruptedList.Add(Convert.ToUInt16(uPart));
                                        }
                                    }
                                }

                                break;
                            case MuleConstants.FT_AICH_HASH:
                                {
                                    //ASSERT( newtag.IsStr );
                                    AICHHash hash = MuleApplication.Instance.AICHObjectManager.CreateAICHHash();
                                    if (MpdUtilities.DecodeBase32(newtag.Str.ToCharArray(), hash.RawHash) ==
                                        Convert.ToUInt32(MuleConstants.HASHSIZE))
                                    {
                                        AICHHashSet.SetMasterHash(hash, AICHStatusEnum.AICH_VERIFIED);
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
                                        TagList.Add(newtag);

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

                    uint parts = PartCount;	// assuming we will get all hashsets

                    for (uint i = 0; i < parts && (metFile.Position + 16 < metFile.Length); i++)
                    {
                        byte[] cur_hash = new byte[16];
                        metFile.Read(cur_hash);
                        Hashset.Add(cur_hash);
                    }

                    byte[] checkhash = new byte[16];
                    if (Hashset.Count != 0)
                    {
                        byte[] buffer = new byte[Hashset.Count * 16];
                        for (int i = 0; i < Hashset.Count; i++)
                            MpdUtilities.Md4Cpy(buffer, i * 16, Hashset[i], 0, 16);
                        CreateHash(buffer, Convert.ToUInt64(Hashset.Count * 16), checkhash);
                    }
                    if (MpdUtilities.Md4Cmp(FileHash, checkhash) != 0)
                    {
                        Hashset.Clear();
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

            if (FileSize > MuleConstants.MAX_EMULE_FILE_SIZE)
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
                    gap.start <= gap.end && gap.start < FileSize)
                {
                    if (gap.end >= FileSize)
                        gap.end = FileSize - 1; // Clipping
                    AddGap(gap.start, gap.end); // All tags accounted for, use safe adding
                }
                // SLUGFILLER: SafeHash
            }

            // verify corrupted parts list

            List<ushort> tmpList = new List<ushort>();
            tmpList.AddRange(CorruptedList);

            for (int i = 0; i < tmpList.Count; i++)
            {
                uint uCorruptedPart = Convert.ToUInt32(tmpList[i]);

                if (IsComplete(Convert.ToUInt64(uCorruptedPart) * MuleConstants.PARTSIZE,
                    Convert.ToUInt64(uCorruptedPart + 1) * MuleConstants.PARTSIZE - 1))
                    CorruptedList.RemoveAt(i);
            }

            //check if this is a backup
            if (string.Compare(Path.GetExtension(FullName), MuleConstants.PARTMET_TMP_EXT, true) == 0)
                FullName = Path.GetFileNameWithoutExtension(FullName);

            // open permanent handle
            string searchpath = Path.GetFileNameWithoutExtension(FullName);
            try
            {
                PartFileStream = new FileStream(searchpath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            }
            catch (Exception)
            {
                //TODO:Log
                return false;
            }

            // read part file creation time
            LastModified = MpdUtilities.DateTime2UInt32Time(System.IO.File.GetLastWriteTime(searchpath));
            Created = MpdUtilities.DateTime2UInt32Time(System.IO.File.GetCreationTime(searchpath));

            try
            {
                FilePath = (searchpath);

                try
                {
                    FileAttributes = System.IO.File.GetAttributes(searchpath);
                }
                catch
                {
                    FileAttributes = 0;
                }

                // SLUGFILLER: SafeHash - final safety, make sure any missing part of the file is gap
                if (Convert.ToUInt64(PartFileStream.Length) < FileSize)
                {
                    AddGap(Convert.ToUInt64(PartFileStream.Length), FileSize - 1);
                }
                // Goes both ways - Partfile should never be too large
                if (Convert.ToUInt64(PartFileStream.Length) > FileSize)
                {
                    //TODO:LogTRACE(_T("Partfile \"%s\" is too large! Truncating %I64u bytes.\n"), FileName, m_hpartfile.Length) - FileSize);
                    PartFileStream.SetLength(Convert.ToInt64(FileSize));
                }
                // SLUGFILLER: SafeHash

                SourcePartFrequency.Clear();
                for (uint i = 0; i < PartCount; i++)
                    SourcePartFrequency.Add(0);
                Status = PartFileStatusEnum.PS_EMPTY;
                // check hashcount, filesatus etc
                if (HashCount != ED2KPartHashCount)
                {
                    //ASSERT( Hashset.GetSize() == 0 );
                    HashsetNeeded = true;
                    return true;
                }
                else
                {
                    HashsetNeeded = false;
                    for (uint i = 0; i < (uint)Hashset.Count; i++)
                    {
                        if (i < PartCount &&
                            IsComplete((ulong)i * MuleConstants.PARTSIZE,
                            (ulong)(i + 1) * MuleConstants.PARTSIZE - 1))
                        {
                            Status = PartFileStatusEnum.PS_READY;
                            break;
                        }
                    }
                }

                if (GapList.Count == 0)
                {	// is this file complete already?
                    CompleteFile(false);
                    return true;
                }

                if (!isnewstyle) // not for importing
                {
                    // check date of .Part file - if its wrong, rehash file
                    uint fdate = MpdUtilities.DateTime2UInt32Time(System.IO.File.GetLastWriteTimeUtc(searchpath));
                    MpdUtilities.AdjustNTFSDaylightFileTime(ref fdate, searchpath);

                    if (UtcLastModified != fdate)
                    {
                        //TODO:Log
                        //string strFileInfo
                        //strFileInfo.Format(_T("%s (%s)"), GetFilePath(), FileName);
                        //LogError(LOG_STATUSBAR, GetResString(IDS_ERR_REHASH), strFileInfo);
                        // rehash
                        Status = PartFileStatusEnum.PS_WAITINGFORHASH;
                        try
                        {
                            //AddFileThread addfilethread = MuleEngine.CoreObjectManager.CreateAddFileThread();
                            //FileOp = PartFileOpEnum.PFOP_HASHING;
                            //FileOpProgress = 0;
                            //addfilethread.SetValues(null, FileDirectory, searchpath, this);
                            //addfilethread.Start();
                        }
                        catch (Exception)
                        {
                            //TODO:LOG:
                            Status = PartFileStatusEnum.PS_ERROR;
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

            UpdateCompletedInfos();
            return true;
        }

        public bool SavePartFile()
        {
            switch (Status)
            {
                case PartFileStatusEnum.PS_WAITINGFORHASH:
                case PartFileStatusEnum.PS_HASHING:
                    return false;
            }

            // search part file
            if (!System.IO.File.Exists(Path.GetFileNameWithoutExtension(FullName)))
            {
                //TODO:LogError(GetResString(IDS_ERR_SAVEMET) + _T(" - %s"), m_partmetfilename, GetFileName(), GetResString(IDS_ERR_PART_FNF));
                return false;
            }

            // get filedate
            LastModified =
                MpdUtilities.DateTime2UInt32Time(System.IO.File.GetLastWriteTime(Path.GetFileNameWithoutExtension(FullName)));
            if (LastModified == 0)
                LastModified = 0xFFFFFFFF;
            tUtcLastModified_ = LastModified;
            if (tUtcLastModified_ == 0xFFFFFFFF)
            {
                //TODO:Log
                //if (thePrefs.GetVerbose())
                //    AddDebugLogLine(false, _T("Failed to get file date of \"%s\" (%s)"), m_partmetfilename, GetFileName());
            }
            else
                MpdUtilities.AdjustNTFSDaylightFileTime(ref tUtcLastModified_, Path.GetFileNameWithoutExtension(FullName));

            string strTmpFile = FullName;
            strTmpFile += MuleConstants.PARTMET_TMP_EXT;

            // save file data to part.met file
            SafeBufferedFile file = null;

            try
            {
                file =
                    MpdObjectManager.CreateSafeBufferedFile(strTmpFile, FileMode.Create, FileAccess.Write, FileShare.None);
            }
            catch (Exception)
            {
                //TODO:Log
                return false;
            }

            try
            {
                //version
                // only use 64 bit tags, when PARTFILE_VERSION_LARGEFILE is set!
                file.WriteUInt8(IsLargeFile ? Convert.ToByte(VersionsEnum.PARTFILE_VERSION_LARGEFILE) : Convert.ToByte(VersionsEnum.PARTFILE_VERSION));

                //date
                file.WriteUInt32(tUtcLastModified_);

                //hash
                file.WriteHash16(FileHash);
                uint parts = Convert.ToUInt32(hashlist_.Count);
                file.WriteUInt16(Convert.ToUInt16(parts));
                for (uint x = 0; x < parts; x++)
                    file.WriteHash16(hashlist_[Convert.ToInt32(x)]);

                uint uTagCount = 0;
                ulong uTagCountFilePos = Convert.ToUInt64(file.Position);
                file.WriteUInt32(uTagCount);

                if (ED2KUtilities.WriteOptED2KUTF8Tag(file, FileName, MuleConstants.FT_FILENAME))
                    uTagCount++;
                Tag nametag =
                    MpdObjectManager.CreateTag(MuleConstants.FT_FILENAME, FileName);
                nametag.WriteTagToFile(file);
                uTagCount++;

                Tag sizetag =
                    MpdObjectManager.CreateTag(MuleConstants.FT_FILESIZE,
                    FileSize, IsLargeFile);
                sizetag.WriteTagToFile(file);
                uTagCount++;

                if (Transferred > 0)
                {
                    Tag transtag =
                        MpdObjectManager.CreateTag(MuleConstants.FT_TRANSFERRED,
                        Transferred, IsLargeFile);
                    transtag.WriteTagToFile(file);
                    uTagCount++;
                }
                if (CompressionGain > 0)
                {
                    Tag transtag =
                        MpdObjectManager.CreateTag(MuleConstants.FT_COMPRESSION,
                        CompressionGain, IsLargeFile);
                    transtag.WriteTagToFile(file);
                    uTagCount++;
                }
                if (CorruptionLoss > 0)
                {
                    Tag transtag =
                        MpdObjectManager.CreateTag(MuleConstants.FT_CORRUPTED,
                        CorruptionLoss, IsLargeFile);
                    transtag.WriteTagToFile(file);
                    uTagCount++;
                }

                if (Paused)
                {
                    Tag statustag = MpdObjectManager.CreateTag(MuleConstants.FT_STATUS, 1);
                    statustag.WriteTagToFile(file);
                    uTagCount++;
                }

                Tag prioritytag =
                    MpdObjectManager.CreateTag(MuleConstants.FT_DLPRIORITY,
                    IsAutoDownPriority ? PriorityEnum.PR_AUTO : DownPriority);
                prioritytag.WriteTagToFile(file);
                uTagCount++;

                Tag ulprioritytag =
                    MpdObjectManager.CreateTag(MuleConstants.FT_ULPRIORITY,
                    IsAutoUpPriority ? PriorityEnum.PR_AUTO : UpPriority);
                ulprioritytag.WriteTagToFile(file);
                uTagCount++;

                if (MpdUtilities.DateTime2UInt32Time(LastSeenComplete) > 0)
                {
                    Tag lsctag =
                        MpdObjectManager.CreateTag(MuleConstants.FT_LASTSEENCOMPLETE,
                        MpdUtilities.DateTime2UInt32Time(LastSeenComplete));
                    lsctag.WriteTagToFile(file);
                    uTagCount++;
                }

                if (Category > 0)
                {
                    Tag categorytag =
                        MpdObjectManager.CreateTag(MuleConstants.FT_CATEGORY, Category);
                    categorytag.WriteTagToFile(file);
                    uTagCount++;
                }

                if (LastPublishTimeKadSrc > 0)
                {
                    Tag kadLastPubSrc =
                        MpdObjectManager.CreateTag(MuleConstants.FT_KADLASTPUBLISHSRC,
                        LastPublishTimeKadSrc);
                    kadLastPubSrc.WriteTagToFile(file);
                    uTagCount++;
                }

                if (LastPublishTimeKadNotes > 0)
                {
                    Tag kadLastPubNotes =
                        MpdObjectManager.CreateTag(MuleConstants.FT_KADLASTPUBLISHNOTES,
                        LastPublishTimeKadNotes);
                    kadLastPubNotes.WriteTagToFile(file);
                    uTagCount++;
                }

                if (DownloadActiveTime > 0)
                {
                    Tag tagDownloadActiveTime =
                        MpdObjectManager.CreateTag(MuleConstants.FT_DL_ACTIVE_TIME,
                        DownloadActiveTime);
                    tagDownloadActiveTime.WriteTagToFile(file);
                    uTagCount++;
                }

                if (PreviewPriority)
                {
                    Tag tagDlPreview =
                        MpdObjectManager.CreateTag(MuleConstants.FT_DL_PREVIEW,
                        PreviewPriority ? (uint)1 : (uint)0);
                    tagDlPreview.WriteTagToFile(file);
                    uTagCount++;
                }

                // statistics
                if (statistic_.AllTimeTransferred > 0)
                {
                    Tag attag1 =
                        MpdObjectManager.CreateTag(MuleConstants.FT_ATTRANSFERRED,
                        Convert.ToUInt32(statistic_.AllTimeTransferred & 0xFFFFFFFF));
                    attag1.WriteTagToFile(file);
                    uTagCount++;

                    Tag attag4 =
                        MpdObjectManager.CreateTag(MuleConstants.FT_ATTRANSFERREDHI,
                        Convert.ToUInt32((statistic_.AllTimeTransferred >> 32) & 0xFFFFFFFF));
                    attag4.WriteTagToFile(file);
                    uTagCount++;
                }

                if (statistic_.AllTimeRequests > 0)
                {
                    Tag attag2 =
                        MpdObjectManager.CreateTag(MuleConstants.FT_ATREQUESTED,
                        statistic_.AllTimeRequests);
                    attag2.WriteTagToFile(file);
                    uTagCount++;
                }

                if (statistic_.AllTimeAccepts > 0)
                {
                    Tag attag3 =
                        MpdObjectManager.CreateTag(MuleConstants.FT_ATACCEPTED,
                        statistic_.AllTimeAccepts);
                    attag3.WriteTagToFile(file);
                    uTagCount++;
                }

                if (MaxSources > 0)
                {
                    Tag attag3 =
                        MpdObjectManager.CreateTag(MuleConstants.FT_MAXSOURCES,
                        MaxSources);
                    attag3.WriteTagToFile(file);
                    uTagCount++;
                }

                // currupt part infos
                if (corrupted_list_.Count > 0)
                {
                    StringBuilder strCorruptedParts = new StringBuilder();
                    foreach (ushort tmpPart in corrupted_list_)
                    {
                        uint uCorruptedPart = Convert.ToUInt32(tmpPart);
                        if (strCorruptedParts.Length > 0)
                            strCorruptedParts.Append(",");
                        strCorruptedParts.Append(uCorruptedPart);
                    }
                    Tag tagCorruptedParts = MpdObjectManager.CreateTag(MuleConstants.FT_CORRUPTEDPARTS, strCorruptedParts);
                    tagCorruptedParts.WriteTagToFile(file);
                    uTagCount++;
                }

                //AICH Filehash
                if (pAICHHashSet_.HasValidMasterHash && (pAICHHashSet_.Status == AICHStatusEnum.AICH_VERIFIED))
                {
                    Tag aichtag = MpdObjectManager.CreateTag(MuleConstants.FT_AICH_HASH, pAICHHashSet_.GetMasterHash().HashString);
                    aichtag.WriteTagToFile(file);
                    uTagCount++;
                }
                for (int j = 0; j < tagList_.Count; j++)
                {
                    if (tagList_[j].IsStr || tagList_[j].IsInt)
                    {
                        tagList_[j].WriteTagToFile(file);
                        uTagCount++;
                    }
                }

                //gaps
                byte[] namebuffer = new byte[10];
                uint i_pos = 0;
                foreach (Gap gap in gaplist_)
                {
                    byte[] tmpNumber = Encoding.Default.GetBytes(i_pos.ToString());
                    namebuffer[0] = MuleConstants.FT_GAPSTART;
                    Array.Copy(tmpNumber, 0, namebuffer, 1, tmpNumber.Length > 9 ? 9 : tmpNumber.Length);

                    Tag gapstarttag = MpdObjectManager.CreateTag(namebuffer, gap.start, IsLargeFile);
                    gapstarttag.WriteTagToFile(file);
                    uTagCount++;

                    // gap start = first missing byte but gap ends = first non-missing byte in edonkey
                    // but I think its easier to user the real limits
                    namebuffer[0] = MuleConstants.FT_GAPEND;
                    Tag gapendtag = MpdObjectManager.CreateTag(namebuffer, gap.end + 1, IsLargeFile);
                    gapendtag.WriteTagToFile(file);
                    uTagCount++;

                    i_pos++;
                }

                file.Seek(Convert.ToInt64(uTagCountFilePos), SeekOrigin.Begin);
                file.WriteUInt32(uTagCount);
                file.Seek(0, SeekOrigin.End);
                file.Flush();
                file.Close();
            }
            catch (Exception)
            {
                //CString strError;
                //strError.Format(GetResString(IDS_ERR_SAVEMET), m_partmetfilename, GetFileName());
                //TCHAR szError[MAX_CFEXP_ERRORMSG];
                //if (error.GetErrorMessage(szError, ARRSIZE(szError))){
                //    strError += _T(" - ");
                //    strError += szError;
                //}
                //LogError(_T("%s"), strError);
                //error.Delete();

                // remove the partially written or otherwise damaged temporary file
                // // need to close the file before removing it. call 'Abort' instead of 'Close', just to avoid an ASSERT.
                file.Abort();
                try
                {
                    System.IO.File.Delete(strTmpFile);
                }
                catch
                {
                    //TODO:Log
                }
                return false;
            }

            // after successfully writing the temporary part.met file...
            try
            {
                System.IO.File.Delete(FullName);
            }
            catch (Exception)
            {
                //TODO:Log
            }

            try
            {
                System.IO.File.Move(strTmpFile, FullName);
            }
            catch (Exception)
            {
                //int iErrno = errno;
                //if (thePrefs.GetVerbose())
                //    DebugLogError(_T("Failed to move temporary part.met file \"%s\" to \"%s\" - %s"), strTmpFile, m_fullname, _tcserror(iErrno));

                //CString strError;
                //strError.Format(GetResString(IDS_ERR_SAVEMET), m_partmetfilename, GetFileName());
                //strError += _T(" - ");
                //strError += _tcserror(iErrno);
                //LogError(_T("%s"), strError);
                //TODO:Log
                return false;
            }

            // create a backup of the successfully written part.met file
            string bakname = FullName + MuleConstants.PARTMET_BAK_EXT;

            try
            {
                System.IO.File.Copy(FullName, bakname, false);
            }
            catch (Exception)
            {
                //TODO:Log
            }

            return true;
        }

        public bool GetNextEmptyBlockInPart(uint partNumber, RequestedBlock result)
        {
            Gap firstGap;
            ulong end;
            ulong blockLimit;

            // Find start of this part
            ulong partStart = (MuleConstants.PARTSIZE * partNumber);
            ulong start = partStart;

            // What is the end limit of this block, i.e. can't go outside part (or filesize)
            ulong partEnd = (MuleConstants.PARTSIZE * ((ulong)partNumber + 1)) - 1;
            if (partEnd >= FileSize)
            {
                partEnd = FileSize - 1;
            }
            // Loop until find a suitable gap and return true, or no more gaps and return false
            while (true)
            {
                firstGap = null;

                // Find the first gap from the start position
                foreach (Gap currentGap in GapList)
                {

                    // Want gaps that overlap start<.partEnd
                    if ((currentGap.start <= partEnd) && (currentGap.end >= start))
                    {
                        // Is this the first gap?
                        if ((firstGap == null) || (currentGap.start < firstGap.start))
                        {
                            firstGap = currentGap;
                        }
                    }
                }

                // If no gaps after start, exit
                if (firstGap == null)
                {
                    return false;
                }
                // Update start position if gap starts after current pos
                if (start < firstGap.start)
                {
                    start = firstGap.start;
                }
                // If this is not within part, exit
                if (start > partEnd)
                {
                    return false;
                }
                // Find end, keeping within the max block size and the part limit
                end = firstGap.end;
                blockLimit = partStart + (MuleConstants.EMBLOCKSIZE * (((start - partStart) / MuleConstants.EMBLOCKSIZE) + 1)) - 1;
                if (end > blockLimit)
                {
                    end = blockLimit;
                }
                if (end > partEnd)
                {
                    end = partEnd;
                }
                // If this gap has not already been requested, we have found a valid entry
                if (!IsAlreadyRequested(start, end))
                {
                    // Was this block to be returned
                    if (result != null)
                    {
                        result.StartOffset = start;
                        result.EndOffset = end;
                        MpdUtilities.Md4Cpy(result.FileID, FileHash);
                        result.Transferred = 0;
                    }
                    return true;
                }
                else
                {
                    // Reposition to end of that gap
                    start = end + 1;
                }
                // If tried all gaps then break out of the loop
                if (end == partEnd)
                {
                    break;
                }
            }
            // No suitable gap found
            return false;
        }

        public void CompleteFile(bool hashingdone)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void CreatePartFile()
        {
            CreatePartFile(0);
        }

        public void CreatePartFile(uint cat)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public uint WriteToBuffer(ulong transize,
            byte[] data,
            ulong start,
            ulong end,
            RequestedBlock block)
        {
            // Increment transferred bytes counter for this file
            Transferred += transize;

            // This is needed a few times
            // Kry - should not need a ulong here - no block is larger than
            // 2GB even after uncompressed.
            uint lenData = (uint)(end - start + 1);

            if (lenData > transize)
            {
                GainDueToCompression += lenData - transize;
            }

            // Occasionally packets are duplicated, no point writing it twice
            if (IsComplete(start, end))
            {
                //AddDebugLogLineM(false, logPartFile,	
                //			CFormat(wxT("File '%s' has already been written from %u to %u"))
                //				% GetFileName() % start % end);
                return 0;
            }

            // security sanitize check to make sure we do not write anything into an already hashed complete chunk
            ulong nStartChunk = start / MuleConstants.PARTSIZE;
            ulong nEndChunk = end / MuleConstants.PARTSIZE;
            if (IsComplete(MuleConstants.PARTSIZE * (ulong)nStartChunk,
                (MuleConstants.PARTSIZE * (ulong)(nStartChunk + 1)) - 1))
            {
                //AddDebugLogLineM(false, logPartFile, CFormat(wxT("Received data touches already hashed chunk - ignored (start): %u-%u; File=%s")) % start % end % GetFileName());
                return 0;
            }
            else if (nStartChunk != nEndChunk)
            {
                if (IsComplete(MuleConstants.PARTSIZE * (ulong)nEndChunk,
                    (MuleConstants.PARTSIZE * (ulong)(nEndChunk + 1)) - 1))
                {
                    //AddDebugLogLineM(false, logPartFile, CFormat(wxT("Received data touches already hashed chunk - ignored (end): %u-%u; File=%s")) % start % end % GetFileName());
                    return 0;
                }
                else
                {
                    //AddDebugLogLineM(false, logPartFile, CFormat(wxT("Received data crosses chunk boundaries: %u-%u; File=%s")) % start % end % GetFileName());
                }
            }

            // Create a new buffered queue entry
            PartFileBufferedData item = new PartFileBufferedData();
            item.data = data;
            item.start = start;
            item.end = end;
            item.block = block;

            // Add to the queue in the correct position (most likely the end)
            foreach (PartFileBufferedData queueItem in bufferedData_list_)
            {
                if (item.end <= queueItem.end)
                {
                    bufferedData_list_.Insert(bufferedData_list_.IndexOf(queueItem), item);

                    break;
                }
            }

            // Increment buffer size marker
            TotalBufferData += lenData;

            // Mark this small section of the file as filled
            FillGap(item.start, item.end);

            // Update the flushed mark on the requested block 
            // The loop here is unfortunate but necessary to detect deleted blocks.

            foreach (RequestedBlock rb in requestedblocks_list_)
            {
                if (rb.Equals(item.block))
                {
                    item.block.Transferred += lenData;
                }
            }

            if (gaplist_.Count == 0)
            {
                FlushBuffer(true);
            }

            // Return the length of data written to the buffer
            return lenData;
        }
        #endregion

        #region Protected
        protected void Init()
        {
            throw new Exception("The method or operation is not implemented.");
        }
        #endregion

        #region Private
        private bool ImportShareazaTempfile(string in_directory, string in_filename, bool getsizeonly)
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
                FileName = sRemoteName;

                ulong lSize = br.ReadUInt64();
                ulong nSize = lSize;

                FileSize = lSize;

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
                    MpdUtilities.Md4Cpy(FileHash, pED2K);
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
                if (MpdUtilities.GotoString(sdFile,
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
                                AddGap(Convert.ToUInt64(begin),
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
                                AddGap(begin, begin + length - 1);
                            }
                        }
                        else
                        {
                            badGapList = true;
                        }
                    }

                    if (badGapList)
                    {
                        GapList.Clear();
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
                if (MpdUtilities.GotoString(sdFile, FileHash, 16)) // search the hashset
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
                        Hashset.Add(curhash);
                    }

                    byte[] checkhash = new byte[16];
                    if (Hashset.Count != 0)
                    {
                        byte[] buffer = new byte[Hashset.Count * 16];
                        for (int i = 0; i < Hashset.Count; i++)
                            MpdUtilities.Md4Cpy(buffer, (i * 16), Hashset[i], 0, 16);
                        CreateHash(buffer, Convert.ToUInt64(Hashset.Count * 16), checkhash);
                    }
                    if (MpdUtilities.Md4Cmp(pMD4, checkhash) != 0)
                    {
                        Hashset.Clear();
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
            if (!SavePartFile())
                return false;

            Hashset.Clear();
            GapList.Clear();

            return LoadPartFile(in_directory, in_filename, false);
        }
        #endregion
    }
}
