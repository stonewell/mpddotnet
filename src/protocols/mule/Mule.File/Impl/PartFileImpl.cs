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
using Mpd.Generic.Types.IO;
using Mule.Definitions;
using Mpd.Generic.Types;
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
        public byte DownPriority { get; set; }
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
                    if (MPDUtilities.Md4Cmp(hashresult, GetPartHash(partnumber)) != 0)
                        return false;
                    else
                        return true;
                }
                else
                {
                    if (MPDUtilities.Md4Cmp(hashresult, FileHash) != 0)
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
            get { throw new Exception("The method or operation is not implemented."); }
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
                MPDUtilities.DateTime2UInt32Time(System.IO.File.GetLastWriteTime(Path.GetFileNameWithoutExtension(FullName)));
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
                MPDUtilities.AdjustNTFSDaylightFileTime(ref tUtcLastModified_, Path.GetFileNameWithoutExtension(FullName));

            string strTmpFile = FullName;
            strTmpFile += MuleConstants.PARTMET_TMP_EXT;

            // save file data to part.met file
            SafeBufferedFile file = null;

            try
            {
                file =
                    MpdGenericObjectManager.CreateSafeBufferedFile(strTmpFile, FileMode.Create, FileAccess.Write, FileShare.None);
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
                    MpdGenericObjectManager.CreateTag(MuleConstants.FT_FILENAME, FileName);
                nametag.WriteTagToFile(file);
                uTagCount++;

                Tag sizetag =
                    MpdGenericObjectManager.CreateTag(MuleConstants.FT_FILESIZE,
                    FileSize, IsLargeFile);
                sizetag.WriteTagToFile(file);
                uTagCount++;

                if (Transferred > 0)
                {
                    Tag transtag =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_TRANSFERRED,
                        Transferred, IsLargeFile);
                    transtag.WriteTagToFile(file);
                    uTagCount++;
                }
                if (CompressionGain > 0)
                {
                    Tag transtag =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_COMPRESSION,
                        CompressionGain, IsLargeFile);
                    transtag.WriteTagToFile(file);
                    uTagCount++;
                }
                if (CorruptionLoss > 0)
                {
                    Tag transtag =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_CORRUPTED,
                        CorruptionLoss, IsLargeFile);
                    transtag.WriteTagToFile(file);
                    uTagCount++;
                }

                if (Paused)
                {
                    Tag statustag = MpdGenericObjectManager.CreateTag(MuleConstants.FT_STATUS, 1);
                    statustag.WriteTagToFile(file);
                    uTagCount++;
                }

                Tag prioritytag =
                    MpdGenericObjectManager.CreateTag(MuleConstants.FT_DLPRIORITY,
                    IsAutoDownPriority ? Convert.ToByte(PriorityEnum.PR_AUTO) : DownPriority);
                prioritytag.WriteTagToFile(file);
                uTagCount++;

                Tag ulprioritytag =
                    MpdGenericObjectManager.CreateTag(MuleConstants.FT_ULPRIORITY,
                    IsAutoUpPriority ? Convert.ToByte(PriorityEnum.PR_AUTO) : UpPriority);
                ulprioritytag.WriteTagToFile(file);
                uTagCount++;

                if (MPDUtilities.DateTime2UInt32Time(LastSeenComplete) > 0)
                {
                    Tag lsctag =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_LASTSEENCOMPLETE,
                        MPDUtilities.DateTime2UInt32Time(LastSeenComplete));
                    lsctag.WriteTagToFile(file);
                    uTagCount++;
                }

                if (Category > 0)
                {
                    Tag categorytag =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_CATEGORY, Category);
                    categorytag.WriteTagToFile(file);
                    uTagCount++;
                }

                if (LastPublishTimeKadSrc > 0)
                {
                    Tag kadLastPubSrc =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_KADLASTPUBLISHSRC,
                        LastPublishTimeKadSrc);
                    kadLastPubSrc.WriteTagToFile(file);
                    uTagCount++;
                }

                if (LastPublishTimeKadNotes > 0)
                {
                    Tag kadLastPubNotes =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_KADLASTPUBLISHNOTES,
                        LastPublishTimeKadNotes);
                    kadLastPubNotes.WriteTagToFile(file);
                    uTagCount++;
                }

                if (DownloadActiveTime > 0)
                {
                    Tag tagDownloadActiveTime =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_DL_ACTIVE_TIME,
                        DownloadActiveTime);
                    tagDownloadActiveTime.WriteTagToFile(file);
                    uTagCount++;
                }

                if (PreviewPriority)
                {
                    Tag tagDlPreview =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_DL_PREVIEW,
                        PreviewPriority ? (uint)1 : (uint)0);
                    tagDlPreview.WriteTagToFile(file);
                    uTagCount++;
                }

                // statistics
                if (statistic_.AllTimeTransferred > 0)
                {
                    Tag attag1 =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_ATTRANSFERRED,
                        Convert.ToUInt32(statistic_.AllTimeTransferred & 0xFFFFFFFF));
                    attag1.WriteTagToFile(file);
                    uTagCount++;

                    Tag attag4 =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_ATTRANSFERREDHI,
                        Convert.ToUInt32((statistic_.AllTimeTransferred >> 32) & 0xFFFFFFFF));
                    attag4.WriteTagToFile(file);
                    uTagCount++;
                }

                if (statistic_.AllTimeRequests > 0)
                {
                    Tag attag2 =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_ATREQUESTED,
                        statistic_.AllTimeRequests);
                    attag2.WriteTagToFile(file);
                    uTagCount++;
                }

                if (statistic_.AllTimeAccepts > 0)
                {
                    Tag attag3 =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_ATACCEPTED,
                        statistic_.AllTimeAccepts);
                    attag3.WriteTagToFile(file);
                    uTagCount++;
                }

                if (MaxSources > 0)
                {
                    Tag attag3 =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_MAXSOURCES,
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
                    Tag tagCorruptedParts = MpdGenericObjectManager.CreateTag(MuleConstants.FT_CORRUPTEDPARTS, strCorruptedParts);
                    tagCorruptedParts.WriteTagToFile(file);
                    uTagCount++;
                }

                //AICH Filehash
                if (pAICHHashSet_.HasValidMasterHash && (pAICHHashSet_.Status == AICHStatusEnum.AICH_VERIFIED))
                {
                    Tag aichtag = MpdGenericObjectManager.CreateTag(MuleConstants.FT_AICH_HASH, pAICHHashSet_.GetMasterHash().HashString);
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

                    Tag gapstarttag = MpdGenericObjectManager.CreateTag(namebuffer, gap.start, IsLargeFile);
                    gapstarttag.WriteTagToFile(file);
                    uTagCount++;

                    // gap start = first missing byte but gap ends = first non-missing byte in edonkey
                    // but I think its easier to user the real limits
                    namebuffer[0] = MuleConstants.FT_GAPEND;
                    Tag gapendtag = MpdGenericObjectManager.CreateTag(namebuffer, gap.end + 1, IsLargeFile);
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
        #endregion

        #region Protected
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
                        MPDUtilities.Md4Cpy(result.FileID, FileHash);
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

        protected void Init()
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
            bool added = false;

            foreach (PartFileBufferedData queueItem in bufferedData_list_)
            {
                if (item.end <= queueItem.end)
                {

                    added = true;

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

        #region Private
        private bool PerformFileComplete()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        private static uint CompleteThreadProc(object pvParams)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        private static uint AllocateSpaceThread(object lpParam)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        private void CharFillRange(ref string buffer, uint start, uint end, byte color)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
