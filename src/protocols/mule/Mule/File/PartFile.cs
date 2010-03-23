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
using Mule.File;
using Mule.ED2K;
using Mpd.Generic.IO;
using Mule.Core;
using Mule.Network;
using Mule.AICH;
using System.IO;


namespace Mule.File
{
    public class Gap
    {
        public ulong start;
        public ulong end;
    }

    public class GapList : List<Gap>
    {
    }

    public class PartFileBufferedData
    {
        // Barry - This is the data to be written
        public byte[] data;
        // Barry - This is the start offset of the data
        public ulong start;
        // Barry - This is the end offset of the data
        public ulong end;
        // Barry - This is the requested block that this data relates to
        public RequestedBlock block;
    };

    public class PartFileList : List<PartFile>
    {
    }

    public interface PartFile : KnownFile
    {
        // part.met filename (without path!)
        string PartMetFileName { get; set; }
        DateTime LastSeenComplete { get; set; }
        System.IO.Stream PartFileStream { get; set; }

        // full path to part.met file or completed file
        string FullName { get; set; }
        string TempPath { get; }

        // local file system related properties
        bool IsNormalFile { get; }
        ulong RealFileSize { get; }
        void GetLeftToTransferAndAdditionalNeededSpace(ref ulong ui64LeftToTransfer, ref ulong pui32AdditionalNeededSpace);
        ulong NeededSpace { get; }

        // last file modification time (NT's version of UTC), to be used for stats only!
        DateTime LastModifiedDate { get; }
        uint LastModified { get; set; }

        // file creation time (NT's version of UTC), to be used for stats only!
        DateTime CreatedDate { get; }
        uint Created { get; set; }

        void InitializeFromLink(Mule.ED2K.ED2KFileLink fileLink);
        void InitializeFromLink(Mule.ED2K.ED2KFileLink fileLink, uint cat);
        uint Process(uint reducedownload, uint icounter);

        PartFileLoadResultEnum LoadPartFile(string in_directory, string in_filename);
        PartFileLoadResultEnum LoadPartFile(string in_directory, string in_filename, ref PartFileFormatEnum fileFormat);
        PartFileLoadResultEnum ImportShareazaTempfile(string in_directory,
            string in_filename);
        PartFileLoadResultEnum ImportShareazaTempfile(string in_directory,
            string in_filename, ref PartFileFormatEnum pOutCheckFileFormat);

        bool SavePartFile();
        bool SavePartFile(bool bDontOverrideBak);

        void PartFileHashFinished(KnownFile result);

        // true = ok , false = corrupted
        bool HashSinglePart(uint partnumber);
        bool HashSinglePart(uint partnumber, ref bool pbAICHReportedOK);

        void AddGap(ulong start, ulong end);
        void FillGap(ulong start, ulong end);
        bool IsComplete(ulong start, ulong end);
        bool IsPureGap(ulong start, ulong end);
        bool IsAlreadyRequested(ulong start, ulong end);
        bool ShrinkToAvoidAlreadyRequested(ref ulong start, ref ulong end);
        bool IsCorruptedPart(uint partnumber);
        ulong GetTotalGapSizeInRange(ulong uRangeStart, ulong uRangeEnd);
        ulong GetTotalGapSizeInPart(uint uPart);
        void UpdateCompletedInfos();
        void UpdateCompletedInfos(ulong uTotalGaps);

        bool GetNextRequestedBlock(UpDownClient sender,
            ref RequestedBlock newblocks, ref ushort count);

        void WritePartStatus(SafeMemFile file);
        void WriteCompleteSourcesCount(SafeMemFile file);
        void AddSources(SafeMemFile sources, uint serverip, ushort serverport, bool bWithObfuscationAndHash);
        void AddSource(string pszURL, uint nIP);

        bool CanAddSource(uint userid, ushort port,
            uint serverip, ushort serverport);
        bool CanAddSource(uint userid, ushort port,
            uint serverip, ushort serverport,
            ref uint pdebug_lowiddropped);
        bool CanAddSource(uint userid, ushort port,
            uint serverip, ushort serverport,
            ref uint pdebug_lowiddropped, bool Ed2kID);

        PartFileStatusEnum Status { get; set; }
        PartFileStatusEnum GetStatus(bool ignorePause);

        void NotifyStatusChange();
        bool IsStopped { get; set; }
        bool CompletionError { get; }
        ulong CompletedSize { get; }
        string PartfileStatus { get; }
        int PartfileStatusRang { get; }
        void SetActive(bool bActive);

        PriorityEnum DownPriority { get; set; }
        void SetDownPriority(PriorityEnum priority, bool reort);

        bool IsAutoDownPriority { get; set; }
        void UpdateAutoDownPriority();

        uint SourceCount { get; }
        uint SrcA4AFCount { get; }
        uint GetSrcStatisticsValue(DownloadStateEnum nDLState);
        uint TransferringSrcCount { get; }
        ulong Transferred { get; set; }
        uint DataRate { get; set; }
        float PercentCompleted { get; }
        uint NotCurrentSourcesCount { get; }
        int ValidSourcesCount { get; }
        // Barry - Also want to preview archives
        bool IsArchive();
        bool IsArchive(bool onlyPreviewable);
        bool IsPreviewableFileType { get; }
        ulong TimeRemaining { get; }
        ulong TimeRemainingSimple { get; }
        uint DLActiveTime { get; }

        // Barry - Added as replacement for BlockReceived to buffer data before writing to disk
        uint WriteToBuffer(ulong transize,
            byte[] data, ulong start, ulong end,
            RequestedBlock block, UpDownClient client);
        void FlushBuffer();
        void FlushBuffer(bool forcewai);
        void FlushBuffer(bool forcewait, bool bForceICH);
        void FlushBuffer(bool forcewait, bool bForceICH, bool bNoAICH);
        // Barry - This will invert the gap list, up to caller to delete gaps when done
        // 'Gaps' returned are really the filled areas, and guaranteed to be in order
        GapList GapList { get; }

        // Barry - Added to prevent list containing deleted blocks on shutdown
        void RemoveAllRequestedBlocks();
        bool RemoveBlockFromList(ulong start, ulong end);
        bool IsInRequestedBlockList(RequestedBlock block);
        void RemoveAllSources(bool bTryToSwap);

        bool CanOpenFile { get; }
        bool IsReadyForPreview { get; }
        bool CanStopFile { get; }
        bool CanPauseFile { get; }
        bool CanResumeFile { get; }
        bool IsPausingOnPrevie { get; }

        void OpenFile();
        void PreviewFile();
        void DeleteFile();
        void StopFile();
        void StopFile(bool bCancel);
        void StopFile(bool bCancel, bool resort);
        void PauseFile();
        void PauseFile(bool bInsufficient);
        void PauseFile(bool bInsufficient, bool resort);
        void StopPausedFile();
        void ResumeFile();
        void ResumeFile(bool resort);
        void ResumeFileInsufficient();
        void SetPauseOnPreview(bool bVal);
        Packet CreateSrcInfoPacket(UpDownClient forClient,
            byte byRequestedVersion, ushort nRequestedOptions);
        void AddClientSources(SafeMemFile sources, byte sourceexchangeversion,
            bool bSourceExchange2);
        void AddClientSources(SafeMemFile sources, byte sourceexchangeversion,
            bool bSourceExchange2, UpDownClient pClient);

        uint AvailablePartCount { get; }
        void UpdateAvailablePartsCount();

        uint LastAnsweredTime { get; set; }
        void UpdateLastAnsweredTimeTimeout();

        ulong CorruptionLoss { get; set; }
        ulong CompressionGain { get; set; }
        uint RecoveredPartsByICH { get; }

        void AddDownloadingSource(UpDownClient client);
        void RemoveDownloadingSource(UpDownClient client);

        string GetProgressString(ushort size);
        string GetInfoSummary();

        void UpdateDisplayedInfo();
        void UpdateDisplayedInfo(bool force);

        uint Category { get; set; }
        bool HasDefaultCategory { get; }
        bool CheckShowItemInGivenCat(int inCategory);

        byte[] MMCreatePartStatus();

        void PerformFileCompleteEnd(uint dwResult);

        PartFileOpEnum FileOp { get; set; }
        uint FileOpProgress { get; set; }

        AICHRecoveryHashSet AICHRecoveryHashSet { get; }
        void RequestAICHRecovery(uint nPart);
        void AICHRecoveryDataAvailable(uint nPart);
        bool IsAICHPartHashSetNeeded { get; set; }

        bool AllowSwapForSourceExchange { get; }
        void SetSwapForSourceExchangeTick();

        uint PrivateMaxSources { get; set; }
        uint MaxSources { get; set; }
        uint MaxSourcePerFileSoft { get; }
        uint MaxSourcePerFileUDP { get; }

        bool PreviewPriority { get; set; }
        bool Paused { get; set; }

        void CreatePartFile();
        void CreatePartFile(uint cat);
        void CompleteFile(bool hashingdone);
        uint DownloadActiveTime { get; set; }
        List<ushort> CorruptedList { get; }
        System.IO.FileAttributes FileAttributes { get; set; }
        List<ushort> SourcePartFrequency { get; }
        bool HashsetNeeded { get; set; }
        uint[] AnStates { get; set; }
        ushort[] SourceStates { get; set; }
        ushort[] NetStates { get; set; }
        uint LastPurgeTime { get; set; }
        ulong TotalBufferData { get; set; }
        uint LastBufferFlushTime { get; set; }
        bool PartFileUpdated { get; set; }
        bool IsLocalSrcReqQueued { get; set; }
        bool IsPreviewing { get; set; }
        RequestedBlockList RequestedBlocks { get; }
        bool GetNextEmptyBlockInPart(uint partnumber, RequestedBlock result);
        uint LastSearchTime { get; set; }
        uint LastSearchTimeKad { get; set; }
        ulong AllocInfo { get; set; }
        UpDownClientList SourceList { get; set; }
        //<<-- enkeyDEV(Ottavio84) -A4AF-
        UpDownClientList A4AFSourceList { get; set; }
        bool Previewing { get; set; }
        bool RecoveringArchive { get; set; } // Is archive recovery in progress
        bool LocalSrcReqQueued { get; set; }
        bool SourceAreVisible { get; set; }				// used for downloadlistctrl
        bool MD4HashsetNeeded { get; set; }
        byte TotalSearchesKad { get; set; }

        bool PreviewPrio { get; set; }

        bool RightFileHasHigherPrio(PartFile left, PartFile right);

        DeadSourceList DeadSourceList { get; set; }

        void RefilterFileComments();
    }
}
