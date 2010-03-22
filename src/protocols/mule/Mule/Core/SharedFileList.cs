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
using Mpd.Generic;

namespace Mule.Core
{
    public class ByteArray
    {
    }

    public class SharedFileMap : Dictionary<ByteArray, KnownFile>
    {
    }

    public interface UnknownFile
    {
        string Name { get; set; }
        string Directory { get; set; }
    }

    public interface SharedFileList
    {
        void SendListToServer();
        void Reload();
        bool SafeAddKFile(KnownFile toadd/*, bool bOnlyAdd = false*/);
        bool SafeAddKFile(KnownFile toadd, bool bOnlyAdd);
        void RepublishFile(KnownFile pFile);
        bool RemoveFile(KnownFile toremove);
        KnownFile GetFileByID(byte[] filehash);
        KnownFile GetFileByIndex(int index);
        bool IsFilePtrInList(KnownFile file);
        void PublishNextTurn();
        void CreateOfferedFilePacket(KnownFile cur_file,
            SafeMemFile files, ED2KServer pServer/*, UpDownClient pClient = NULL*/);
        void CreateOfferedFilePacket(KnownFile cur_file,
            SafeMemFile files, ED2KServer pServer, UpDownClient pClient);
        ulong GetDatasize(ref ulong pbytesLargest);
        int Count { get; }
        int HashingCount { get; }
        void UpdateFile(KnownFile toupdate);
        void AddFilesFromDirectory(string rstrDirectory);
        void AddFileFromNewlyCreatedCollection(string path, string fileName);
        void HashFailed(UnknownFile hashed);		// SLUGFILLER: SafeHash
        void FileHashingFinished(KnownFile file);
        void ClearED2KPublishInfo();
        void ClearKadSourcePublishInfo();
        void Process();
        void Publish();
        void AddKeywords(KnownFile pFile);
        void RemoveKeywords(KnownFile pFile);
        void DeletePartFileInstances();
        bool IsUnsharedFile(byte[] auFileHash);

        void CopySharedFileMap(SharedFileMap filesMap);

        Dictionary<MapCKey, KnownFile> FilesMap { get; }
    }
}
