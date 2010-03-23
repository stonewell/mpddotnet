using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core.Impl
{
    class SharedFileListImpl : SharedFileList
    {
        #region SharedFileList Members

        public void SendListToServer()
        {
            throw new NotImplementedException();
        }

        public void Reload()
        {
            throw new NotImplementedException();
        }

        public bool SafeAddKFile(Mule.File.KnownFile toadd)
        {
            throw new NotImplementedException();
        }

        public bool SafeAddKFile(Mule.File.KnownFile toadd, bool bOnlyAdd)
        {
            throw new NotImplementedException();
        }

        public void RepublishFile(Mule.File.KnownFile pFile)
        {
            throw new NotImplementedException();
        }

        public bool RemoveFile(Mule.File.KnownFile toremove)
        {
            throw new NotImplementedException();
        }

        public Mule.File.KnownFile GetFileByID(byte[] filehash)
        {
            throw new NotImplementedException();
        }

        public Mule.File.KnownFile GetFileByIndex(int index)
        {
            throw new NotImplementedException();
        }

        public bool IsFilePtrInList(Mule.File.KnownFile file)
        {
            throw new NotImplementedException();
        }

        public void PublishNextTurn()
        {
            throw new NotImplementedException();
        }

        public void CreateOfferedFilePacket(Mule.File.KnownFile cur_file, Mpd.Generic.IO.SafeMemFile files, Mule.ED2K.ED2KServer pServer)
        {
            throw new NotImplementedException();
        }

        public void CreateOfferedFilePacket(Mule.File.KnownFile cur_file, Mpd.Generic.IO.SafeMemFile files, Mule.ED2K.ED2KServer pServer, UpDownClient pClient)
        {
            throw new NotImplementedException();
        }

        public ulong GetDatasize(ref ulong pbytesLargest)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public int HashingCount
        {
            get { throw new NotImplementedException(); }
        }

        public void UpdateFile(Mule.File.KnownFile toupdate)
        {
            throw new NotImplementedException();
        }

        public void AddFilesFromDirectory(string rstrDirectory)
        {
            throw new NotImplementedException();
        }

        public void AddFileFromNewlyCreatedCollection(string path, string fileName)
        {
            throw new NotImplementedException();
        }

        public void HashFailed(UnknownFile hashed)
        {
            throw new NotImplementedException();
        }

        public void FileHashingFinished(Mule.File.KnownFile file)
        {
            throw new NotImplementedException();
        }

        public void ClearED2KPublishInfo()
        {
            throw new NotImplementedException();
        }

        public void ClearKadSourcePublishInfo()
        {
            throw new NotImplementedException();
        }

        public void Process()
        {
            throw new NotImplementedException();
        }

        public void Publish()
        {
            throw new NotImplementedException();
        }

        public void AddKeywords(Mule.File.KnownFile pFile)
        {
            throw new NotImplementedException();
        }

        public void RemoveKeywords(Mule.File.KnownFile pFile)
        {
            throw new NotImplementedException();
        }

        public void DeletePartFileInstances()
        {
            throw new NotImplementedException();
        }

        public bool IsUnsharedFile(byte[] auFileHash)
        {
            throw new NotImplementedException();
        }

        public void CopySharedFileMap(SharedFileMap filesMap)
        {
            throw new NotImplementedException();
        }

        public Dictionary<Mpd.Generic.MapCKey, Mule.File.KnownFile> FilesMap
        {
            get { throw new NotImplementedException(); }
        }

        #endregion
    }
}
