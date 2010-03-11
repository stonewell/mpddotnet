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
using Mpd.Generic.Types.IO;
using Mule.Definitions;

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
        string PartMetFileName { get; }

        // full path to part.met file or completed file
        string FullName { get; set;}
        string TempPath { get; }

        // local file system related properties
        bool IsNormalFile { get; }
        bool IsAllocating { get; }
        ulong RealFileSize { get; }
        void GetLeftToTransferAndAdditionalNeededSpace(ref ulong ui64LeftToTransfer, ref ulong pui32AdditionalNeededSpace);
        ulong NeededSpace { get; }

        // last file modification time (NT's version of UTC), to be used for stats only!
        DateTime CFileDate { get; }
        uint FileDate { get; }

        // file creation time (NT's version of UTC), to be used for stats only!
        DateTime CrCFileDate { get; }
        uint CrFileDate { get; }

        // true = ok , false = corrupted
        bool HashSinglePart(uint partnumber);

        void AddGap(ulong start, ulong end);
        void FillGap(ulong start, ulong end);
        bool IsComplete(ulong start, ulong end, bool bIgnoreBufferedData);
        bool IsPureGap(ulong start, ulong end);
        bool IsAlreadyRequested(ulong start, ulong end, bool bCheckBuffers);
        bool ShrinkToAvoidAlreadyRequested(ref ulong start, ref ulong end);
        bool IsCorruptedPart(uint partnumber);
        ulong GetTotalGapSizeInRange(ulong uRangeStart, ulong uRangeEnd);
        ulong GetTotalGapSizeInPart(uint uPart);
        void UpdateCompletedInfos();
        void UpdateCompletedInfos(ulong uTotalGaps);

        void WritePartStatus(SafeMemFile file);
        void WriteCompleteSourcesCount(SafeMemFile file);
        void AddSources(SafeMemFile sources, uint serverip, ushort serverport, bool bWithObfuscationAndHash);
        void AddSource(string pszURL, uint nIP);

        PartFileStatusEnum Status { get; set;}
        void NotifyStatusChange();
        bool IsStopped { get; }
        bool CompletionError { get; }
        ulong CompletedSize { get; }
        string PartfileStatus { get; }
        int PartfileStatusRang { get; }
        void SetActive(bool bActive);

        byte DownPriority { get; set;}
        bool IsAutoDownPriority { get;set; }
        void UpdateAutoDownPriority();

        uint SourceCount { get; }
        uint SrcA4AFCount { get; }
        uint GetSrcStatisticsValue(DownloadStateEnum nDLState);
        uint TransferringSrcCount { get; }
        ulong Transferred { get; }
        uint Datarate { get; }
        float PercentCompleted { get; }
        uint NotCurrentSourcesCount { get; }
        int ValidSourcesCount { get; }
        // Barry - Also want to preview archives
        bool IsArchive(bool onlyPreviewable);
        bool IsPreviewableFileType { get; }
        ulong TimeRemaining { get; }
        ulong TimeRemainingSimple { get; }
        uint DlActiveTime { get; }

        // Barry - Added as replacement for BlockReceived to buffer data before writing to disk
        void FlushBuffer();
        void FlushBuffer(bool forcewai);
        void FlushBuffer(bool forcewait, bool bForceICH);
        void FlushBuffer(bool forcewait, bool bForceICH, bool bNoAICH);
        // Barry - This will invert the gap list, up to caller to delete gaps when done
        // 'Gaps' returned are really the filled areas, and guaranteed to be in order
        GapList FilledList { get;}

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

        void OpenFile();
        void PreviewFile();
        void DeleteFile();
        void StopFile(bool bCancel, bool resort);
        void PauseFile(bool bInsufficient, bool resort);
        void StopPausedFile();
        void ResumeFile(bool resort);
        void ResumeFileInsufficient();

        uint AvailablePartCount { get; }
        void UpdateAvailablePartsCount();

        uint LastAnsweredTime { get; set;}
        void UpdateLastAnsweredTimeTimeout();

        ulong CorruptionLoss { get; }
        ulong CompressionGain { get; }
        uint RecoveredPartsByICH { get; }

        string GetProgressString(ushort size);
        string GetInfoSummary();

        void UpdateDisplayedInfo();
        void UpdateDisplayedInfo(bool force);

        uint Category { get; set; }
        bool CheckShowItemInGivenCat(int inCategory);

        byte[] MMCreatePartStatus();

        void PerformFileCompleteEnd(uint dwResult);

        PartFileOpEnum FileOp { get;set;}
        uint FileOpProgress { get;set;}

        void RequestAICHRecovery(uint nPart);
        void AICHRecoveryDataAvailable(uint nPart);

        bool AllowSwapForSourceExchange { get;}
        void SetSwapForSourceExchangeTick();

        uint PrivateMaxSources { get;set;}
        uint MaxSources { get;}
        uint MaxSourcePerFileSoft { get;}
        uint MaxSourcePerFileUDP { get;}

        bool PreviewPrio { get;set;}

        /* Protected 
        bool GetNextEmptyBlockInPart(uint partnumber, RequestedBlock result);
        void CompleteFile(bool hashingdone);
        void CreatePartFile(uint cat);
        void Init();
        */

        /* Private
        bool PerformFileComplete();
        void CharFillRange(ref string buffer, uint start, uint end, char color);
         */
    }
}
