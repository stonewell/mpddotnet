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
        private uint tCreated_;			// file creation time (NT's version of UTC), to be used for stats only!
        private uint randoupdate_wait_;
        private volatile PartFileOpEnum eFileOp_;
        private volatile uint uFileOpProgress_;
        private uint lastSwapForSourceExchangeTick_; // ZZ:DownloadManaager
        private uint lastSearchTime_;
        private uint lastSearchTimeKad_;
        private ulong iAllocinfo_;
        private DateTime lastseencomplete_;
        private System.IO.Stream hpartfile_;				// permanent opened handle to avoid write conflicts
        private System.Threading.Mutex fileCompleteMutex_ = new Mutex();		// Lord KiRon - Mutex for file completion
        private ushort[] src_stats_ = new ushort[4];
        private ushort[] net_stats_ = new ushort[3];
        private volatile bool bPreviewing_;
        private volatile bool bRecoveringArchive_; // Is archive recovery in progress
        private bool bLocalSrcReqQueued_;
        private bool srcarevisible_;				// used for downloadlistctrl
        private bool hashsetneeded_;
        private byte totalSearchesKad_;

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

        public PartFileImpl(string edonkeylink)
            : this(edonkeylink, 0)
        {
        }
        public PartFileImpl(string edonkeylink, uint cat)
        {
            ED2KLink pLink = null;
            try
            {
                pLink = MuleEngine.CoreObjectManager.CreateLinkFromUrl(edonkeylink);
                ED2KFileLink pFileLink = pLink.FileLink;
                if (pFileLink == null)
                    throw new Exception("not a file link:" + edonkeylink);
                InitializeFromLink(pFileLink, cat);
            }
            catch (Exception)
            {
                //TODO:LOg
                //string strMsg;
                //strMsg.Format(GetResString(IDownloadStateEnum.DS_ERR_INVALIDLINK), error);
                //LogError(LOG_STATUSBAR, GetResString(IDownloadStateEnum.DS_ERR_LINKERROR), strMsg);
                Status = PartFileStatusEnum.PS_ERROR;
            }
        }

        public PartFileImpl(ED2KFileLink fileLink)
            : this(fileLink, 0)
        {
        }

        public PartFileImpl(ED2KFileLink fileLink, uint cat)
        {
            InitializeFromLink(fileLink, cat);
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
                //TODO:LogError(LOG_STATUSBAR, GetResString(IDS_ERR_HASHERRORWARNING), GetFileName());
                hashsetneeded_ = true;
                return true;
            }
            else if (GetPartHash(partnumber) == null && PartCount != 1)
            {
                //TODO:LogError(LOG_STATUSBAR, GetResString(IDS_ERR_INCOMPLETEHASH), GetFileName());
                hashsetneeded_ = true;
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
