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
using Mpd.Generic;
using Kademlia;

namespace Mule.File
{
    public interface AbstractFile
    {
        string FileName { get; set; }

        void SetFileName(string pszFileName,
            bool bReplaceInvalidFileSystemChars/* = false */);
        void SetFileName(string pszFileName,
            bool bReplaceInvalidFileSystemChars/* = false */,
            bool bAutoSetFileType/* = true */);
        void SetFileName(string pszFileName,
            bool bReplaceInvalidFileSystemChars/* = false */,
            bool bAutoSetFileType/* = true */,
            bool bRemoveControlChars/* = false*/);

        // returns the ED2K file type (an ASCII string)
        string FileType { get; set; }
        string FileTypeDisplayStr { get; set; }

        FileIdentifier FileIdentifier { get; }

        byte[] FileHash { get; set; }
        bool HasNullHash { get; }

        ulong FileSize { get; set; }
        bool IsLargeFile { get; }

        uint GetIntTagValue(byte tagname);
        uint GetIntTagValue(string tagname);
        bool GetIntTagValue(byte tagname, ref uint ruValue);
        ulong GetInt64TagValue(byte tagname);
        ulong GetInt64TagValue(string tagname);
        bool GetInt64TagValue(byte tagname, ref ulong ruValue);
        void SetIntTagValue(byte tagname, uint uValue);
        void SetInt64TagValue(byte tagname, ulong uValue);
        string GetStrTagValue(byte tagname);
        string GetStrTagValue(string tagname);
        void SetStrTagValue(byte tagname, string val);
        Tag GetTag(byte tagname, byte tagtype);
        Tag GetTag(string tagname, byte tagtype);
        Tag GetTag(byte tagname);
        Tag GetTag(string tagname);
        TagList TagList { get; }
        void AddTagUnique(Tag pTag);
        void DeleteTag(byte tagname);
        void DeleteTag(Tag pTag);
        void ClearTags();
        void CopyTags(TagList tags);
        bool IsPartFile { get; }

        bool HasComment { get; set; }
        uint UserRating { get; set; }
        uint GetUserRating(bool bKadSearchIndicator);
        bool HasRating { get; }
        bool HasBadRating { get; }

        string FileComment { get; set; }
        uint FileRating { get; set; }

        void LoadComment();
        void UpdateFileRatingCommentAvail();
        void UpdateFileRatingCommentAvail(bool bForceUpdate);

        bool AddNote(KadEntry pEntry);
        void RefilterKadNotes();
        void RefilterKadNotes(bool bUpdate);
        KadEntryList KadNotes { get; set; }
        bool IsKadCommentSearchRunning { get; set; }
    }
}
