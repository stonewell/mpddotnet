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
using Mule.Core;
using Kademlia;

namespace Mule.File.Impl
{
    class PartFileImpl : KnownFileImpl, PartFile
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

        private uint[] anStates_ = new uint[MuleConstants.STATES_COUNT];
        private GapList gaplist_ = new GapList();
        private RequestedBlockList requestedblocks_list_ = new RequestedBlockList();
        private List<ushort> srcpartFrequency_ = new List<ushort>();
        private List<ushort> corrupted_list_ = new List<ushort>();
        private List<PartFileBufferedData> bufferedData_list_ = new List<PartFileBufferedData>();
        private System.Threading.Mutex fileCompleteMutex_ = new Mutex();		// Lord KiRon - Mutex for file completion
        private ushort[] src_stats_ = new ushort[4];
        private ushort[] net_stats_ = new ushort[3];
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
        public uint LastSearchTimeKad { get; set; }
        public byte TotalSearchesKad { get; set; }
        public uint LastSearchTime { get; set; }
        public uint ProcessCount { get; set; }

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

        public ulong RealFileSize
        {
            get
            {
                uint low = 0, high = 0;
                low = MpdUtilities.GetCompressedFileSize(FilePath, out high);

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

        private PartFileStatusEnum status_;

        public PartFileStatusEnum Status
        {
            get
            {
                return GetStatus(false);
            }
            set
            {
                status_ = value;
            }
        }

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
            get;
            set;
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
            get;set;
        }

        public uint FileOpProgress
        {
            get;
            set;
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

        public uint Process(uint reducedownload, uint icounter)
        {
            uint nOldTransSourceCount = GetSrcStatisticsValue(DownloadStateEnum.DS_DOWNLOADING);
            uint dwCurTick = MpdUtilities.GetTickCount();

            // If buffer size exceeds limit, or if not written within time limit, flush data
            if ((TotalBufferData > MuleApplication.Instance.Preference.FileBufferSize) ||
                (dwCurTick > (LastBufferFlushTime + MuleConstants.BUFFER_TIME_LIMIT)))
            {
                // Avoid flushing while copying preview file
                if (!IsPreviewing)
                    FlushBuffer();
            }

            DataRate = 0;

            // calculate datarate, set limit etc.
            if (icounter < 10)
            {
                uint cur_datarate;
                foreach (UpDownClient cur_src in downloadingSourceList_)
                {
                    if (cur_src != null && cur_src.DownloadState == DownloadStateEnum.DS_DOWNLOADING)
                    {
                        if (cur_src.ClientSocket != null)
                        {
                            cur_src.CheckDownloadTimeout();
                            cur_datarate = cur_src.CalculateDownloadRate();
                            DataRate += cur_datarate;
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
                                cur_src.ClientSocket.DownloadLimit = limit;
                                //if (cur_src.IsDownloadingFromPeerCache &&
                                //    cur_src.PeerCacheDownloadSocket != null &&
                                //    cur_src.PeerCacheDownloadSocket.IsConnected)
                                //    cur_src.PeerCacheDownloadSocket.DownloadLimit = limit;
                            }
                        }
                    }
                }
            }
            else
            {
                bool downloadingbefore = AnStates[Convert.ToInt32(DownloadStateEnum.DS_DOWNLOADING)] > 0;
                // -khaos--+++> Moved this here, otherwise we were setting our permanent variables to 0 every tenth of a second...
                Array.Clear(AnStates, 0, AnStates.Length);
                Array.Clear(SourceStates, 0, SourceStates.Length);
                Array.Clear(NetStates, 0, NetStates.Length);
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
                        SourceStates[Convert.ToInt32(cur_src.SourceFrom)] =
                            SourceStates[Convert.ToInt32(cur_src.SourceFrom)]++;

                    if (cur_src.ServerIP != 0 && cur_src.ServerPort != 0)
                    {
                        NetStates[0] = NetStates[0]++;
                        if (cur_src.KadPort != 0)
                            NetStates[2] = NetStates[2]++;
                    }
                    if (cur_src.KadPort != 0)
                        NetStates[1] = NetStates[1]++;

                    AnStates[nCountForState] = AnStates[nCountForState]++;

                    switch (cur_src.DownloadState)
                    {
                        case DownloadStateEnum.DS_DOWNLOADING:
                            {
                                if (cur_src.ClientSocket != null)
                                {
                                    cur_src.CheckDownloadTimeout();
                                    uint cur_datarate = cur_src.CalculateDownloadRate();
                                    DataRate += cur_datarate;
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
                                        cur_src.ClientSocket.DownloadLimit = limit;
                                        //if (cur_src.IsDownloadingFromPeerCache &&
                                        //    cur_src.PeerCacheDownloadSocket != null &&
                                        //    cur_src.PeerCacheDownloadSocket.IsConnected)
                                        //    cur_src.PeerCacheDownloadSocket.DownloadLimit = limit;

                                    }
                                    else
                                    {
                                        cur_src.ClientSocket.EnableDownloadLimit = false;
                                        //if (cur_src.IsDownloadingFromPeerCache &&
                                        //    cur_src.PeerCacheDownloadSocket != null &&
                                        //    cur_src.PeerCacheDownloadSocket.IsConnected)
                                        //    cur_src.PeerCacheDownloadSocket.EnableDownloadLimit = false;
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
                                    if (!MuleApplication.Instance.CanDoCallback(cur_src))
                                    {
                                        //If we are almost maxed on sources, slowly remove these client to see if we can find a better source.
                                        if (((dwCurTick - LastPurgeTime) > 30 * 1000) &&
                                            (SourceCount >= (MaxSources * .8)))
                                        {
                                            MuleApplication.Instance.DownloadQueue.RemoveSource(cur_src);
                                            LastPurgeTime = dwCurTick;
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
                                if ((dwCurTick - LastPurgeTime) > 40 * 1000)
                                {
                                    LastPurgeTime = dwCurTick;
                                    // we only delete them if reaching the limit
                                    if (SourceCount >= (MaxSources * .8))
                                    {
                                        MuleApplication.Instance.DownloadQueue.RemoveSource(cur_src);
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
                                    if (((dwCurTick - LastPurgeTime) > 1 * 60 * 1000) &&
                                        (SourceCount >= (MaxSources * .8)))
                                    {
                                        MuleApplication.Instance.DownloadQueue.RemoveSource(cur_src);
                                        LastPurgeTime = dwCurTick;
                                        break;
                                    }
                                }
                                //Give up to 1 min for UDP to respond.. If we are within one min of TCP reask, do not try..
                                if (MuleApplication.Instance.IsConnected &&
                                    cur_src.GetTimeUntilReask() < 2 * 60 * 1000 &&
                                    cur_src.GetTimeUntilReask() > 1 * 1000 &&
                                    MpdUtilities.GetTickCount() - cur_src.LastTriedToConnectTime > 20 * 60 * 1000) // ZZ:DownloadManager (one resk timestamp for each file)
                                {
                                    cur_src.UDPReaskForDownload();
                                }

                                if (MuleApplication.Instance.IsConnected &&
                                    cur_src.GetTimeUntilReask() == 0 &&
                                    MpdUtilities.GetTickCount() - cur_src.LastTriedToConnectTime > 20 * 60 * 1000) // ZZ:DownloadManager (one resk timestamp for each file)
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
                                if (MuleApplication.Instance.IsConnected &&
                                    cur_src.GetTimeUntilReask() == 0 &&
                                    MpdUtilities.GetTickCount() - cur_src.LastTriedToConnectTime > 20 * 60 * 1000) // ZZ:DownloadManager (one resk timestamp for each file)
                                {
                                    if (!cur_src.DoesAskForDownload) // NOTE: This may *delete* the client!!
                                        break; //I left this break here just as a reminder just in case re rearange things..
                                }
                                break;
                            }
                    }
                }

                if (downloadingbefore != (AnStates[Convert.ToInt32(DownloadStateEnum.DS_DOWNLOADING)] > 0))
                    NotifyStatusChange();

                if (MaxSourcePerFileUDP > SourceCount)
                {
                    if (MuleApplication.Instance.DownloadQueue.DoKademliaFileRequest() &&
                        (MuleApplication.Instance.KadEngine.TotalFile < MuleConstants.KADEMLIATOTALFILE) &&
                        (dwCurTick > LastSearchTimeKad) &&
                        MuleApplication.Instance.KadEngine.IsConnected &&
                        MuleApplication.Instance.IsConnected &&
                        !IsStopped)
                    {
                        //Once we can handle lowID users in Kad, we remove the second IsConnected
                        //Kademlia
                        MuleApplication.Instance.DownloadQueue.SetLastKademliaFileRequest();
                        if (KadFileSearchID == 0)
                        {
                            KadSearch pSearch =
                                MuleApplication.Instance.KadEngine.SearchManager.PrepareLookup(Convert.ToUInt32(KadSearchTypeEnum.FILE),
                                    true,
                                    MuleApplication.Instance.KadEngine.ObjectManager.CreateUInt128(FileHash));
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
                        MuleApplication.Instance.KadEngine.SearchManager.StopSearch(KadFileSearchID, true);
                    }
                }

                // check if we want new sources from server
                if (!IsLocalSrcReqQueued &&
                    ((LastSearchTime == 0) ||
                    (dwCurTick - LastSearchTime) > MuleConstants.SERVERREASKTIME) &&
                    MuleApplication.Instance.ServerConnect.IsConnected &&
                    MaxSourcePerFileSoft > SourceCount &&
                    !IsStopped &&
                    (!IsLargeFile ||
                    (MuleApplication.Instance.ServerConnect.CurrentServer != null &&
                    MuleApplication.Instance.ServerConnect.CurrentServer.DoesSupportsLargeFilesTCP)))
                {
                    IsLocalSrcReqQueued = true;
                    MuleApplication.Instance.DownloadQueue.SendLocalSrcRequest(this);
                }

                ProcessCount++;
                if (ProcessCount == 3)
                {
                    ProcessCount = 0;
                    UpdateAutoDownPriority();
                    UpdateDisplayedInfo();
                    UpdateCompletedInfos();
                }
            }

            if (GetSrcStatisticsValue(DownloadStateEnum.DS_DOWNLOADING) != nOldTransSourceCount)
            {
                UpdateDisplayedInfo(true);
            }

            return DataRate;
        }

        public void InitializeFromLink(Mule.ED2K.ED2KFileLink fileLink)
        {
            InitializeFromLink(fileLink, 0);
        }

        public void InitializeFromLink(Mule.ED2K.ED2KFileLink fileLink, uint cat)
        {
            try
            {
                SetFileName(fileLink.Name, true, true, false);
                FileSize = fileLink.Size;
                MpdUtilities.Md4Cpy(FileHash, fileLink.HashKey);
                if (!MuleApplication.Instance.DownloadQueue.IsFileExisting(FileHash))
                {
                    if (fileLink.HashSet != null && fileLink.HashSet.Length > 0)
                    {
                        try
                        {
                            if (!LoadHashsetFromFile(fileLink.HashSet, true))
                            {
                                //TODO:Log
                                ////ASSERT( Hashset.Count == 0 );
                                //AddDebugLogLine(false, _T("eD2K link \"%s\" specified with invalid hashset"), fileLink.Name);
                            }
                            else
                                HashsetNeeded = false;
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
                    CreatePartFile(cat);
                    Category = cat;
                }
                else
                    Status = PartFileStatusEnum.PS_ERROR;
            }
            catch (Exception)
            {
                //TODO:Log
                //string strMsg;
                //strMsg.Format(GetResString(IDownloadStateEnum.DS_ERR_INVALIDLINK), error);
                //LogError(LOG_STATUSBAR, GetResString(IDownloadStateEnum.DS_ERR_LINKERROR), strMsg);
                Status = PartFileStatusEnum.PS_ERROR;
            }
        }

        public PartFileStatusEnum GetStatus(bool ignorePause)
        {
            throw new NotImplementedException();
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
            ushort partCount = PartCount;
            ChunkList chunksList = new ChunkList();

            // Main loop
            ushort newBlockCount = 0;
            while (newBlockCount != count)
            {
                // Create a request block stucture if a chunk has been previously selected
                if (sender.LastPartAsked != 0xffff)
                {
                    RequestedBlock pBlock = new RequestedBlock();
                    if (GetNextEmptyBlockInPart(sender.LastPartAsked, pBlock) == true)
                    {
                        // Keep a track of all pending requested blocks
                        RequestedBlocks.Add(pBlock);
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
                            if (sender.IsPartAvailable(i) == true && GetNextEmptyBlockInPart(i, null) == true)
                            {
                                // Create a new entry for this chunk and add it to the list
                                Chunk newEntry = new Chunk();
                                newEntry.Part = i;
                                newEntry.Frequency = SourcePartFrequency[i];
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
                        if (SourceCount > 800)
                        {
                            modif = 2;
                        }
                        else if (SourceCount > 200)
                        {
                            modif = 5;
                        }
                        ushort limit = (ushort)(modif * SourceCount / 100);
                        if (limit == 0)
                        {
                            limit = 1;
                        }
                        ushort veryRareBound = limit;
                        ushort rareBound = (ushort)(2 * limit);

                        // Cache Preview state (Criterion 2)
                        ED2KFileTypeEnum type =
                            MuleApplication.Instance.ED2KObjectManager.CreateED2KFileTypes().GetED2KFileTypeID(FileName);
                        bool isPreviewEnable =
                            PreviewPriority &&
                            (type == ED2KFileTypeEnum.ED2KFT_ARCHIVE || type == ED2KFileTypeEnum.ED2KFT_VIDEO);

                        // Collect and calculate criteria for all chunks
                        foreach (Chunk cur_chunk in chunksList)
                        {
                            // Offsets of chunk
                            ulong uStart = cur_chunk.Part * MuleConstants.PARTSIZE;
                            ulong uEnd =
                                ((FileSize - 1) < (uStart + MuleConstants.PARTSIZE - 1)) ?
                                    (FileSize - 1) : (uStart + MuleConstants.PARTSIZE - 1);
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
                                    uint sizeOfLastChunk = (uint)(FileSize - uEnd);
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
                                IsAlreadyRequested(uStart, uEnd);

                            // Criterion 4. Completion
                            ulong partSize = MuleConstants.PARTSIZE;

                            foreach (Gap cur_gap in GapList)
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
                        ushort randomness = Convert.ToUInt16(1 + (int)(((float)(chunkCount - 1)) * new Random().NextDouble() / (MpdUtilities.RAND_UINT16_MAX + 1.0)));

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
            if (MuleApplication.Instance.ServerConnect.IsConnected)
            {
                if (MuleUtilities.IsLowID(MuleApplication.Instance.ServerConnect.ClientID))
                {
                    if (MuleApplication.Instance.ServerConnect.ClientID == userid &&
                        MuleApplication.Instance.ServerConnect.CurrentServer.IP == serverip &&
                        MuleApplication.Instance.ServerConnect.CurrentServer.Port == serverport)
                    {
                        return false;
                    }
                    if (MuleApplication.Instance.PublicIP == userid)
                    {
                        return false;
                    }
                }
                else
                {
                    if (MuleApplication.Instance.ServerConnect.ClientID == userid &&
                        MuleApplication.Instance.Preference.Port == port)
                    {
                        return false;
                    }
                }
            }

            if (MuleApplication.Instance.KadEngine.IsConnected)
            {
                if (!MuleApplication.Instance.KadEngine.IsFirewalled)
                {
                    if (MuleApplication.Instance.KadEngine.IPAddress == hybridID &&
                        MuleApplication.Instance.Preference.Port == port)
                    {
                        return false;
                    }
                }
            }

            //This allows *.*.*.0 clients to not be removed if Ed2kID == false
            if (MuleUtilities.IsLowID(hybridID) && MuleApplication.Instance.IsFirewalled)
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

            if (IsStopped)
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
                    if (MuleApplication.Instance.ClientList.IsBannedClient(dwIDED2K))
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

                if (MuleApplication.Instance.Preference.MaxSourcePerFileDefault > SourceCount)
                {
                    UpDownClient newsource =
                        MuleApplication.Instance.CoreObjectManager.CreateUpDownClient(nPort, dwID,
                        dwServerIP, nServerPort, this,
                        (uPacketSXVersion < 3), true);

                    if (uPacketSXVersion > 1)
                    {
                        newsource.UserHash = userHash;
                    }

                    if (uPacketSXVersion >= 4)
                    {
                        newsource.SetConnectOptions(byCryptOptions, true, false);
                    }

                    newsource.SourceFrom = nSourceFrom;
                    MuleApplication.Instance.DownloadQueue.CheckAndAddSource(this, newsource);

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

            MpdUtilities.Md4Cpy(FileHash, searchresult.FileHash);
            foreach (Tag pTag in searchresult.TagList)
            {
                switch (pTag.NameID)
                {
                    case MuleConstants.FT_FILENAME:
                        {
                            if (pTag.IsStr)
                            {
                                if (string.IsNullOrEmpty(FileName))
                                    SetFileName(pTag.Str, true, true, false);
                            }
                            break;
                        }
                    case MuleConstants.FT_FILESIZE:
                        {
                            if (pTag.IsInt64(true))
                                FileSize = pTag.Int64;
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

                                        Tag newtag = MpdObjectManager.CreateTag(pTag);
                                        TagList.Add(newtag);
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
            CreatePartFile(cat);
            Category = cat;
        }

        public void PartFileHashFinished(KnownFile result)
        {
            PartFileUpdated = true;
            bool errorfound = false;
            if (ED2KPartHashCount == 0 || HashCount == 0)
            {
                if (IsComplete(0, FileSize - (ulong)1))
                {
                    if (MpdUtilities.Md4Cmp(result.FileHash, FileHash) != 0)
                    {
                        //TODO:LogWarning(GetResString(IDS_ERR_FOUNDCORRUPTION), 1, GetFileName());
                        AddGap(0, FileSize - (ulong)1);
                        errorfound = true;
                    }
                    else
                    {
                        if (ED2KPartHashCount != HashCount)
                        {
                            //ASSERT(result.ED2KPartHashCount == ED2KPartHashCount);
                            Hashset = result.Hashset;
                        }
                    }
                }
            }
            else
            {
                for (uint i = 0; i < (uint)Hashset.Count; i++)
                {
                    if (i < PartCount && IsComplete((ulong)i * MuleConstants.PARTSIZE,
                        (ulong)(i + 1) * MuleConstants.PARTSIZE - 1))
                    {
                        if (!(result.GetPartHash(i) != null &&
                            MpdUtilities.Md4Cmp(result.GetPartHash(i), GetPartHash(i)) == 0))
                        {
                            //TODO:LogWarning(GetResString(IDS_ERR_FOUNDCORRUPTION), i + 1, GetFileName());
                            AddGap((ulong)i * MuleConstants.PARTSIZE,
                                ((ulong)((ulong)(i + 1) * MuleConstants.PARTSIZE - 1) >= FileSize) ? ((ulong)FileSize - 1) : ((ulong)(i + 1) * MuleConstants.PARTSIZE - 1));
                            errorfound = true;
                        }
                    }
                }
            }
            if (!errorfound && result.AICHHashSet.Status == AICHStatusEnum.AICH_HASHSETCOMPLETE &&
                Status == PartFileStatusEnum.PS_COMPLETING)
            {
                AICHHashSet = result.AICHHashSet;
            }
            else if (Status == PartFileStatusEnum.PS_COMPLETING)
            {
                //TODO:AddDebugLogLine(false, _T("Failed to store new AICH Hashset for completed file %s"), GetFileName());
            }

            if (!errorfound)
            {
                if (Status == PartFileStatusEnum.PS_COMPLETING)
                {
                    //TODO:Log
                    //if (thePrefs.GetVerbose())
                    //    AddDebugLogLine(true, _T("Completed file-hashing for \"%s\""), GetFileName());
                    if (MuleApplication.Instance.SharedFiles.GetFileByID(FileHash) == null)
                        MuleApplication.Instance.SharedFiles.SafeAddKFile(this);
                    CompleteFile(true);
                    return;
                }
                else
                {
                    //TODO:AddLogLine(false, GetResString(IDS_HASHINGDONE), GetFileName());
                }
            }
            else
            {
                Status = PartFileStatusEnum.PS_READY;
                //TODO:Log
                //if (thePrefs.GetVerbose())
                //    DebugLogError(LOG_STATUSBAR, _T("File-hashing failed for \"%s\""), GetFileName());
                SavePartFile();
                return;
            }
            //TODO:Log
            //if (thePrefs.GetVerbose())
            //    AddDebugLogLine(true, _T("Completed file-hashing for \"%s\""), GetFileName());
            Status = PartFileStatusEnum.PS_READY;
            SavePartFile();
            MuleApplication.Instance.SharedFiles.SafeAddKFile(this);
        }
        #endregion

        #region PartFile Members
        public PartFileLoadResultEnum LoadPartFile(string in_directory, string in_filename)
        {
            throw new NotImplementedException();
        }

        public PartFileLoadResultEnum LoadPartFile(string in_directory, string in_filename, ref PartFileFormatEnum fileFormat)
        {
            throw new NotImplementedException();
        }

        public PartFileLoadResultEnum ImportShareazaTempfile(string in_directory, string in_filename)
        {
            throw new NotImplementedException();
        }

        public PartFileLoadResultEnum ImportShareazaTempfile(string in_directory, string in_filename, ref PartFileFormatEnum pOutCheckFileFormat)
        {
            throw new NotImplementedException();
        }

        public bool SavePartFile(bool bDontOverrideBak)
        {
            throw new NotImplementedException();
        }

        public bool HashSinglePart(uint partnumber, ref bool pbAICHReportedOK)
        {
            throw new NotImplementedException();
        }

        public bool GetNextRequestedBlock(UpDownClient sender, ref RequestedBlock newblocks, ref ushort count)
        {
            throw new NotImplementedException();
        }

        public bool CanAddSource(uint userid, ushort port, uint serverip, ushort serverport)
        {
            throw new NotImplementedException();
        }

        public bool CanAddSource(uint userid, ushort port, uint serverip, ushort serverport, ref uint pdebug_lowiddropped)
        {
            throw new NotImplementedException();
        }

        public bool CanAddSource(uint userid, ushort port, uint serverip, ushort serverport, ref uint pdebug_lowiddropped, bool Ed2kID)
        {
            throw new NotImplementedException();
        }

        public void SetDownPriority(PriorityEnum priority, bool reort)
        {
            throw new NotImplementedException();
        }

        public bool IsArchive()
        {
            throw new NotImplementedException();
        }

        public uint DLActiveTime
        {
            get { throw new NotImplementedException(); }
        }

        public uint WriteToBuffer(ulong transize, byte[] data, ulong start, ulong end, RequestedBlock block, UpDownClient client)
        {
            throw new NotImplementedException();
        }

        public bool IsPausingOnPrevie
        {
            get { throw new NotImplementedException(); }
        }

        public void StopFile()
        {
            throw new NotImplementedException();
        }

        public void StopFile(bool bCancel)
        {
            throw new NotImplementedException();
        }

        public void PauseFile()
        {
            throw new NotImplementedException();
        }

        public void PauseFile(bool bInsufficient)
        {
            throw new NotImplementedException();
        }

        public void ResumeFile()
        {
            throw new NotImplementedException();
        }

        public void SetPauseOnPreview(bool bVal)
        {
            throw new NotImplementedException();
        }

        public void AddClientSources(SafeMemFile sources, byte sourceexchangeversion, bool bSourceExchange2)
        {
            throw new NotImplementedException();
        }

        public void AddClientSources(SafeMemFile sources, byte sourceexchangeversion, bool bSourceExchange2, UpDownClient pClient)
        {
            throw new NotImplementedException();
        }

        public bool HasDefaultCategory
        {
            get { throw new NotImplementedException(); }
        }

        public AICHRecoveryHashSet AICHRecoveryHashSet
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsAICHPartHashSetNeeded
        {
            get;
            set;
        }

        public ulong AllocInfo
        {
            get;
            set;
        }

        public UpDownClientList SourceList
        {
            get
            {
                return downloadingSourceList_;
            }
            set
            {
                downloadingSourceList_ = value;
            }
        }

        public UpDownClientList A4AFSourceList
        {
            get
            {
                return this.A4AFsrclist_;
            }
            set
            {
                this.A4AFsrclist_ = value;
            }
        }

        public bool Previewing
        {
            get;
            set;
        }

        public bool RecoveringArchive
        {
            get;
            set;
        }

        public bool LocalSrcReqQueued
        {
            get;
            set;
        }

        public bool SourceAreVisible
        {
            get;
            set;
        }

        public bool MD4HashsetNeeded
        {
            get;
            set;
        }

        public bool PreviewPrio
        {
            get;set;
        }

        public bool RightFileHasHigherPrio(PartFile left, PartFile right)
        {
            throw new NotImplementedException();
        }

        public DeadSourceList DeadSourceList
        {
            get;set;
        }

        public override void UpdateFileRatingCommentAvail(bool bForceUpdate)
        {
            base.UpdateFileRatingCommentAvail(bForceUpdate);
        }

        public override void RefilterKadNotes(bool bUpdate)
        {
            base.RefilterKadNotes(bUpdate);
        }

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


        public void RefilterFileComments()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
