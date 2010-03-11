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
using Kademlia;
using Mpd.Generic.Types;

namespace Mule.Core
{
    // statistics
    public class DownloadStatsStruct
    {
        public DownloadStatsStruct()
        {
            a = new int[23];
        }

        public int[] a = null;
    };

    public interface DownloadQueue
    {
        void Process();
        void Init();

        // add/remove entries
        void AddPartFilesToShare();
        void AddDownload(PartFile newfile, bool paused);
        void AddSearchToDownload(SearchFile toadd/*, byte paused = 2, int cat = 0*/);
        void AddSearchToDownload(SearchFile toadd, byte paused/* = 2, int cat = 0*/);
        void AddSearchToDownload(SearchFile toadd, byte paused, int cat);
        void AddSearchToDownload(string link/*, byte paused = 2, int cat = 0*/);
        void AddSearchToDownload(string link, byte paused/* = 2, int cat = 0*/);
        void AddSearchToDownload(string link, byte paused, int cat);
        void AddFileLinkToDownload(ED2KFileLink pLink/*, int cat = 0*/);
        void AddFileLinkToDownload(ED2KFileLink pLink, int cat);

        void RemoveFile(PartFile toremove);
        void DeleteAll();

        int FileCount { get;}
        uint DownloadingFileCount { get;}
        uint PausedFileCount { get;}

        bool IsFileExisting(byte[] fileid/*, bool bLogWarnings = true*/);
        bool IsFileExisting(byte[] fileid, bool bLogWarnings);
        bool IsPartFile(KnownFile file);

        PartFile GetFileByID(byte[] filehash);
        PartFile GetFileByIndex(int index);
        PartFile GetFileByKadFileSearchID(uint ID);

        void StartNextFileIfPrefs(int cat);
        void StartNextFile(/*int cat=-1,bool force=false*/);
        void StartNextFile(int cat/*=-1,bool force=false*/);
        void StartNextFile(int cat, bool force);

        void RefilterAllComments();

        // sources
        UpDownClient GetDownloadClientByIP(uint dwIP);
        UpDownClient GetDownloadClientByIP_UDP(uint dwIP, ushort nUDPPort,
            bool bIgnorePortOnUniqueIP/*, bool* pbMultipleIPs = NULL*/);
        UpDownClient GetDownloadClientByIP_UDP(uint dwIP, ushort nUDPPort,
            bool bIgnorePortOnUniqueIP, ref bool pbMultipleIPs);
        bool IsInList(UpDownClient client);

        bool CheckAndAddSource(PartFile sender, UpDownClient source);
        bool CheckAndAddKnownSource(PartFile sender, UpDownClient source/*, bool bIgnoreGlobDeadList = false*/);
        bool CheckAndAddKnownSource(PartFile sender, UpDownClient source, bool bIgnoreGlobDeadList);
        bool RemoveSource(UpDownClient toremove/*, bool bDoStatsUpdate = true*/);
        bool RemoveSource(UpDownClient toremove, bool bDoStatsUpdate);

        void GetDownloadSourcesStats(DownloadStatsStruct results);
        int GetDownloadFilesStats(ref ulong ui64TotalFileSize,
            ref ulong ui64TotalLeftToTransfer,
            ref ulong ui64TotalAdditionalNeededSpace);
        uint Datarate { get;}

        void AddUDPFileReasks();
        uint UDPFileReasks { get;}
        void AddFailedUDPFileReasks();
        uint FailedUDPFileReasks { get;}

        // categories
        void ResetCatParts(uint cat);
        void SetCatPrio(uint cat, byte newprio);
        void RemoveAutoPrioInCat(uint cat, byte newprio); // ZZ:DownloadManager
        void SetCatStatus(uint cat, int newstatus);
        void MoveCat(uint from, uint to);
        void SetAutoCat(PartFile newfile);

        // searching on local server
        void SendLocalSrcRequest(PartFile sender);
        void RemoveLocalServerRequest(PartFile pFile);
        void ResetLocalServerRequests();

        // searching in Kad
        void SetLastKademliaFileRequest();
        bool DoKademliaFileRequest();
        void KademliaSearchFile(uint searchID, UInt128 pcontactID,
            UInt128 pkadID,
            byte type, uint ip, ushort tcp, ushort udp,
            uint dwBuddyIP, ushort dwBuddyPort, byte byCryptOptions);

        // searching on global servers
        void StopUDPRequests();

        // check diskspace
        void SortByPriority();
        void CheckDiskspace(/*bool bNotEnoughSpaceLeft = false*/);
        void CheckDiskspace(bool bNotEnoughSpaceLeft);
        void CheckDiskspaceTimed();

        void ExportPartMetFilesOverview();
        void OnConnectionState(bool bConnected);

        void AddToResolved(PartFile pFile, UnresolvedHostname pUH);

        string GetOptimalTempDir(uint nCat, ulong nFileSize);

        ED2KServer CurrentUDPServer { get; set;}
    }
}
