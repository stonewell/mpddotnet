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
using Kademlia;
using CxImage;
using Mule.Core.AICH;
using Mule.Core.File;

namespace Mule.Core.File
{
    public enum FileTypeEnum
    {
    };

    public class ByteArrayArray : List<byte[]>
    {
    };

    public interface KnownFile : AbstractFile
    {
        // 'bReplaceInvalidFileSystemChars' is set to 'false' for backward compatibility!
        void SetFileName(string pszFileName,
            bool bReplaceInvalidFileSystemChars,
            bool bRemoveControlChars);

        string FileDirectory { get; set;}

        string FilePath { get; set; }

        // create date, hashset and tags from a file
        bool CreateFromFile(string directory, string filename);
        //load date, hashset and tags from a .met file
        bool LoadFromFile(FileDataIO file);
        bool WriteToFile(FileDataIO file);
        bool CreateAICHHashSetOnly();

        FileTypeEnum VerifiedFileType { get; set; }

        // last file modification time in (DST corrected, if NTFS) real UTC format
        // NOTE: this value can *not* be compared with NT's version of the UTC time
        DateTime UtcCFileDate { get; }
        uint UtcFileDate { get; }

        // local available part hashs
        uint HashCount { get; }
        byte[] GetPartHash(uint part);
        ByteArrayArray Hashset { get; set; }

        // nr. of part hashs according the file size wrt ED2K protocol
        UInt16 ED2KPartHashCount { get; }

        // nr. of 9MB parts (file data)
        UInt16 PartCount { get;}

        // nr. of 9MB parts according the file size wrt ED2K protocol (OP_FILESTATUS)
        UInt16 ED2KPartCount { get; }

        // file upload priority
        byte UpPriority { get; set;}
        bool IsAutoUpPriority { get; set; }
        void UpdateAutoUpPriority();

        // This has lost it's meaning here.. This is the total clients we know that want this file..
        // Right now this number is used for auto priorities..
        // This may be replaced with total complete source known in the network..
        uint QueuedCount { get; }

        bool LoadHashsetFromFile(FileDataIO file, bool checkhash);

        void AddUploadingClient(UpDownClient client);
        void RemoveUploadingClient(UpDownClient client);
        void UpdatePartsInfo();

        bool PublishedED2K { get; set; }

        uint KadFileSearchID { get; set; }

        KadWordList KadKeywords { get; }

        uint LastPublishTimeKadSrc { get; set; }
        uint LastPublishBuddy { get; set; }
        uint LastPublishTimeKadNotes { get; set; }

        bool PublishSrc();
        bool PublishNotes();

        // file sharing
        Packet CreateSrcInfoPacket(UpDownClient forClient,
            byte byRequestedVersion,
            UInt16 nRequestedOptions);
        uint MetaDataVer { get; }
        void UpdateMetaDataTags();
        void RemoveMetaDataTags();

        // preview
        bool IsMovie { get; }
        bool GrabImage(byte nFramesToGrab,
            double dStartTime,
            bool bReduceColor,
            UInt16 nMaxWidth,
            object pSender);
        void GrabbingFinished(CxImageList imgResults,
            byte nFramesGrabbed, object pSender);

        // aich
        AICHHashSet AICHHashSet { get; set; }

        // Display / Info / Strings
        string InfoSummary { get;}
        string UpPriorityDisplayString { get; }

        //preview
        bool GrabImage(string strFileName, byte nFramesToGrab,
            double dStartTime, bool bReduceColor,
            UInt16 nMaxWidth, object pSender);
        bool LoadTagsFromFile(FileDataIO file);
        bool LoadDateFromFile(FileDataIO file);

        bool CreateHash(System.IO.Stream pFile,
            ulong uSize, byte[] pucHash);
        bool CreateHash(System.IO.Stream pFile,
            ulong uSize, byte[] pucHash,
            AICHHashTree pShaHashOut);
        bool CreateHash(byte[] pucData,
            ulong uSize,
            byte[] pucHash);
        bool CreateHash(byte[] pucData,
            ulong uSize,
            byte[] pucHash,
            AICHHashTree pShaHashOut);

        void SetUpPriority(byte iUpPriority, bool save);
    }
}
