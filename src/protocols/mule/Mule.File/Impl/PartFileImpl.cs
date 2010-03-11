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
        private uint iLastPausePurge_;
        private ushort count_;
        private uint[] anStates_ = new uint[MuleConstants.STATES_COUNT];
        private ulong completedsize_;
        private ulong uCorruptionLoss_;
        private ulong uCompressionGain_;
        private uint uPartsSavedDueICH_;
        private uint datarate_;
        private string fullname_;
        private string partmetfilename_;
        private ulong uTransferred_;
        private uint uMaxSources_;
        private bool paused_;
        private bool stopped_;
        private bool insufficient_;
        private bool bCompletionError_;
        private byte iDownPriority_;
        private bool bAutoDownPriority_;
        private PartFileStatusEnum status_;
        private bool newdate_;	// indicates if there was a writeaccess to the .part file
        private uint lastpurgetime_;
        private uint LastNoNeededCheck_;
        private List<Gap> gaplist_ = new List<Gap>();
        private List<RequestedBlock> requestedblocks_list_ = new List<RequestedBlock>();
        private List<ushort> srcpartFrequency_ = new List<ushort>();
        private float percentcompleted_;
        private List<ushort> corrupted_list_ = new List<ushort>();
        private uint clientSrcAnswered_;
        private uint availablePartsCount_;
        private Thread allocateThread_;
        private uint lastRefreshedDLDisplay_;
        private bool bDeleteAfterAlloc_;
        private bool bpreviewprio_;
        private List<PartFileBufferedData> bufferedData_list_ = new List<PartFileBufferedData>();
        private ulong nTotalBufferData_;
        private uint nLastBufferFlushTime_;
        private uint category_;
        private System.IO.FileAttributes dwFileAttributes_;
        private ulong tActivated_;
        private uint nDlActiveTime_;
        private uint tLastModified_;	// last file modification time (NT's version of UTC), to be used for stats only!
        private System.Threading.Mutex fileCompleteMutex_ = new Mutex();		// Lord KiRon - Mutex for file completion
        private ushort[] src_stats_ = new ushort[4];
        private ushort[] net_stats_ = new ushort[3];

        private System.IO.Stream hpartfile_;
        private uint tCreated_;
        private DateTime lastseencomplete_;

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

        private static readonly MetaTagsStruct[] _aMetaTags =
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

        #region Constructors
        public PartFileImpl()
            : this(0)
        {
        }
        public PartFileImpl(uint cat)
        {
            Init();
            category_ = cat;
        }

        public PartFileImpl(SearchFile searchresult)
            : this(searchresult, 0)
        {
        }

        public PartFileImpl(SearchFile searchresult, uint cat)
        {
            Init();

            //foreach (KadEntry entry in searchresult.KadNotes)
            //{
            //    KadNotes.Add(entry.Copy());
            //}
            //UpdateFileRatingCommentAvail();

            MPDUtilities.Md4Cpy(FileHash, searchresult.FileHash);
            foreach (Tag pTag in searchresult.Tags)
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
                                for (int t = 0; t < _aMetaTags.Length; t++)
                                {
                                    if (pTag.TagType == _aMetaTags[t].nType && pTag.NameID == _aMetaTags[t].nName)
                                    {
                                        // skip string tags with empty string values
                                        if (pTag.IsStr && string.IsNullOrEmpty(pTag.Str))
                                            break;

                                        // skip integer tags with '0' values
                                        if (pTag.IsInt && pTag.Int == 0)
                                            break;

                                        Tag newtag = MpdGenericObjectManager.CreateTag(pTag);
                                        taglist_.Add(newtag);
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
            category_ = cat;
        }

        #endregion

        #region PartFile Members

        public string PartMetFileName
        {
            get { return partmetfilename_; }
        }

        public string FullName
        {
            get
            {
                return fullname_;
            }
            set
            {
                fullname_ = value;
            }
        }

        public string TempPath
        {
            get { return System.IO.Path.GetDirectoryName(fullname_); }
        }

        public bool IsNormalFile
        {
            get
            {
                return (dwFileAttributes_ & (System.IO.FileAttributes.Compressed | System.IO.FileAttributes.SparseFile)) == 0;
            }
        }

        public bool IsAllocating
        {
            get { return allocateThread_ != null; }
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
                if (Convert.ToUInt64(hpartfile_.Length) > FileSize)
                    return 0;
                return FileSize - Convert.ToUInt64(hpartfile_.Length);
            }
        }

        public DateTime CFileDate
        {
            get { return new DateTime(tLastModified_); }
        }

        public uint FileDate
        {
            get { return tLastModified_; }
        }

        public DateTime CrCFileDate
        {
            get { return new DateTime(tCreated_); }
        }

        public uint CrFileDate
        {
            get { return tCreated_; }
        }

        [DllImport("Kernel32.dll")]
        private static extern uint GetTickCount();

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
                hpartfile_.Seek(Convert.ToInt64(MuleConstants.PARTSIZE * partnumber), SeekOrigin.Begin);
                uint length = Convert.ToUInt32(MuleConstants.PARTSIZE);
                if (Convert.ToInt64(MuleConstants.PARTSIZE * (partnumber + 1)) > hpartfile_.Length)
                {
                    length = Convert.ToUInt32(hpartfile_.Length - Convert.ToInt64(MuleConstants.PARTSIZE * partnumber));
                    //ASSERT( length <= PARTSIZE );
                }
                CreateHash(hpartfile_, length, hashresult, null);

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

        public bool IsComplete(ulong start, ulong end, bool bIgnoreBufferedData)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool IsPureGap(ulong start, ulong end)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool IsAlreadyRequested(ulong start, ulong end, bool bCheckBuffers)
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

        public PartFileStatusEnum Status
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

        public void NotifyStatusChange()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool IsStopped
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

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

        public byte DownPriority
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

        public bool IsAutoDownPriority
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

        public void UpdateAutoDownPriority()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public uint SourceCount
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public uint SrcA4AFCount
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public uint GetSrcStatisticsValue(DownloadStateEnum nDLState)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public uint TransferringSrcCount
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public ulong Transferred
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public uint Datarate
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public float PercentCompleted
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public uint NotCurrentSourcesCount
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public int ValidSourcesCount
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool IsArchive(bool onlyPreviewable)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool IsPreviewableFileType
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public ulong TimeRemaining
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public ulong TimeRemainingSimple
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public uint DlActiveTime
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

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

        public GapList FilledList
        {
            get { throw new Exception("The method or operation is not implemented."); }
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

        public ulong CorruptionLoss
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public ulong CompressionGain
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

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

        public uint Category
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

        public uint PrivateMaxSources
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

        public uint MaxSources
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public uint MaxSourcePerFileSoft
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public uint MaxSourcePerFileUDP
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool PreviewPrio
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

        public bool SavePartFile()
        {
            switch (status_)
            {
                case PartFileStatusEnum.PS_WAITINGFORHASH:
                case PartFileStatusEnum.PS_HASHING:
                    return false;
            }

            // search part file
            if (!System.IO.File.Exists(Path.GetFileNameWithoutExtension(fullname_)))
            {
                //TODO:LogError(GetResString(IDS_ERR_SAVEMET) + _T(" - %s"), m_partmetfilename, GetFileName(), GetResString(IDS_ERR_PART_FNF));
                return false;
            }

            // get filedate
            tLastModified_ =
                MPDUtilities.DateTime2UInt32Time(System.IO.File.GetLastWriteTime(Path.GetFileNameWithoutExtension(fullname_)));
            if (tLastModified_ == 0)
                tLastModified_ = 0xFFFFFFFF;
            tUtcLastModified_ = tLastModified_;
            if (tUtcLastModified_ == 0xFFFFFFFF)
            {
                //TODO:Log
                //if (thePrefs.GetVerbose())
                //    AddDebugLogLine(false, _T("Failed to get file date of \"%s\" (%s)"), m_partmetfilename, GetFileName());
            }
            else
                MPDUtilities.AdjustNTFSDaylightFileTime(ref tUtcLastModified_, Path.GetFileNameWithoutExtension(fullname_));

            string strTmpFile = fullname_;
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

                if (uTransferred_ > 0)
                {
                    Tag transtag =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_TRANSFERRED,
                        uTransferred_, IsLargeFile);
                    transtag.WriteTagToFile(file);
                    uTagCount++;
                }
                if (uCompressionGain_ > 0)
                {
                    Tag transtag =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_COMPRESSION,
                        uCompressionGain_, IsLargeFile);
                    transtag.WriteTagToFile(file);
                    uTagCount++;
                }
                if (uCorruptionLoss_ > 0)
                {
                    Tag transtag =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_CORRUPTED,
                        uCorruptionLoss_, IsLargeFile);
                    transtag.WriteTagToFile(file);
                    uTagCount++;
                }

                if (paused_)
                {
                    Tag statustag = MpdGenericObjectManager.CreateTag(MuleConstants.FT_STATUS, 1);
                    statustag.WriteTagToFile(file);
                    uTagCount++;
                }

                Tag prioritytag =
                    MpdGenericObjectManager.CreateTag(MuleConstants.FT_DLPRIORITY,
                    IsAutoDownPriority ? Convert.ToByte(PriorityEnum.PR_AUTO) : iDownPriority_);
                prioritytag.WriteTagToFile(file);
                uTagCount++;

                Tag ulprioritytag =
                    MpdGenericObjectManager.CreateTag(MuleConstants.FT_ULPRIORITY,
                    IsAutoUpPriority ? Convert.ToByte(PriorityEnum.PR_AUTO) : UpPriority);
                ulprioritytag.WriteTagToFile(file);
                uTagCount++;

                if (MPDUtilities.DateTime2UInt32Time(lastseencomplete_) > 0)
                {
                    Tag lsctag =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_LASTSEENCOMPLETE,
                        MPDUtilities.DateTime2UInt32Time(lastseencomplete_));
                    lsctag.WriteTagToFile(file);
                    uTagCount++;
                }

                if (category_ > 0)
                {
                    Tag categorytag =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_CATEGORY, category_);
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

                if (DlActiveTime > 0)
                {
                    Tag tagDlActiveTime =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_DL_ACTIVE_TIME,
                        DlActiveTime);
                    tagDlActiveTime.WriteTagToFile(file);
                    uTagCount++;
                }

                if (PreviewPrio)
                {
                    Tag tagDlPreview =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_DL_PREVIEW,
                        PreviewPrio ? (uint)1 : (uint)0);
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

                if (uMaxSources_ > 0)
                {
                    Tag attag3 =
                        MpdGenericObjectManager.CreateTag(MuleConstants.FT_MAXSOURCES,
                        uMaxSources_);
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
                for (int j = 0; j < taglist_.Count; j++)
                {
                    if (taglist_[j].IsStr || taglist_[j].IsInt)
                    {
                        taglist_[j].WriteTagToFile(file);
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
                System.IO.File.Delete(fullname_);
            }
            catch (Exception)
            {
                //TODO:Log
            }

            try
            {
                System.IO.File.Move(strTmpFile, fullname_);
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
            string bakname = fullname_ + MuleConstants.PARTMET_BAK_EXT;

            try
            {
                System.IO.File.Copy(fullname_, bakname, false);
            }
            catch (Exception)
            {
                //TODO:Log
            }

            return true;
        }
        #endregion

        #region Protected
        protected bool GetNextEmptyBlockInPart(uint partnumber, RequestedBlock result)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        protected void CompleteFile(bool hashingdone)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        protected void CreatePartFile()
        {
            CreatePartFile(0);
        }

        protected void CreatePartFile(uint cat)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        protected void Init()
        {
            throw new Exception("The method or operation is not implemented.");
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
