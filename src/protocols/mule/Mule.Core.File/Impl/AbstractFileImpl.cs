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
using Mule.Core.Preference;
using Mule.Core.ED2K;
using Mule.Core.Impl;

namespace Mule.Core.File.Impl
{
    abstract class AbstractFileImpl : MuleBaseObjectImpl, AbstractFile
    {
        #region Fields
        private bool is_comment_loaded_ = false;
        protected List<Tag> taglist_ = new List<Tag>();
        #endregion

        #region Properties
        private bool isKadCommentSearchRunning_ = false;
        public bool IsKadCommentSearchRunning
        {
            get { return isKadCommentSearchRunning_; }
            set { isKadCommentSearchRunning_ = value; }
        }

        private string FileName_;
        public string FileName
        {
            get { return FileName_; }
            set { SetFileName(value, false, true, false); }
        }

        // returns the ED2K file type (an ASCII string)
        private string FileType_;
        public string FileType
        {
            get { return FileType_; }
            set { FileType_ = value; }
        }


        private byte[] FileHash_ = new byte[16];
        public byte[] FileHash
        {
            get { return FileHash_; }
            set 
            {
                if (value != null)
                {
                    Array.Copy(value,
                        FileHash_, value.Length > 16 ? 16 : value.Length);
                }
            }
        }

        private ulong filesize_ = 0;
        public virtual ulong FileSize
        {
            get { return filesize_; }
            set { filesize_ = value; }
        }

        public bool IsLargeFile 
        { 
            get 
            {
                return filesize_ > CoreConstants.OLD_MAX_EMULE_FILE_SIZE;
            } 
        }

        private KadEntryList kad_entry_notes_ = new KadEntryList();
        public KadEntryList KadNotes
        {
            get { return kad_entry_notes_; }
        }

        public virtual bool IsPartFile
        {
            get { return false; }
        }

        private List<Tag> tags_ = new List<Tag>();
        public List<Tag> Tags
        {
            get { return tags_; }
        }

        private bool has_comment_ = false;
        public bool HasComment
        {
            get { return has_comment_; }
            set { has_comment_ = value; }
        }

        private uint user_rating_ = 0;
        public uint UserRating
        {
            get { return user_rating_; }
            set { user_rating_ = value; }
        }

        public bool HasRating
        {
            get { return user_rating_ > 0; }
        }

        public bool HasBadRating
        {
            get { return HasRating && (user_rating_ < 2); }
        }

        private string fileComment_ = string.Empty;
        public virtual string FileComment
        {
            get 
            {
                if (!is_comment_loaded_)
                    LoadComment();

                return fileComment_; 
            }

            set
            {
                fileComment_ = value;
            }
        }

        private uint fileRating_ = 0;
        public virtual uint FileRating
        {
            get
            {
                if (!is_comment_loaded_)
                    LoadComment();

                return fileRating_;
            }

            set
            {
                fileRating_ = value;
            }
        }

        public bool HasNullHash
        {
            get { return MuleEngine.CoreUtilities.IsNullMd4(FileHash_); }
        }
        #endregion

        #region AbstractFile Members
        public uint GetIntTagValue(byte nameId)
        {
            foreach (Tag tag in tags_)
            {
                if (tag.NameID == Convert.ToUInt32(nameId) && tag.IsInt)
                {
                    return tag.Int;
                }
            }

            return 0;
        }

        public uint GetIntTagValue(string tagname)
        {
            foreach (Tag tag in tags_)
            {
                if (tag.NameID == 0 && 
                    tag.IsInt &&
                    string.Compare(tag.Name, tagname) == 0)
                {
                    return tag.Int;
                }
            }

            return 0;
        }

        public bool GetIntTagValue(byte nameId, ref uint ruValue)
        {
            foreach (Tag tag in tags_)
            {
                if (tag.NameID == Convert.ToUInt32(nameId) && tag.IsInt)
                {
                    ruValue = tag.Int;

                    return true;
                }
            }

            return false;
        }

        public ulong GetInt64TagValue(byte nameId)
        {
            foreach (Tag tag in tags_)
            {
                if (tag.NameID == nameId &&
                    tag.IsInt64(true))
                {
                    return tag.Int64;
                }
            }

            return 0;
        }

        public ulong GetInt64TagValue(string tagname)
        {
            foreach (Tag tag in tags_)
            {
                if (tag.NameID == 0 &&
                    tag.IsInt64(true) &&
                    string.Compare(tag.Name, tagname) == 0)
                {
                    return tag.Int64;
                }
            }

            return 0;
        }

        public bool GetInt64TagValue(byte nameId, ref ulong ruValue)
        {
            foreach (Tag tag in tags_)
            {
                if (tag.NameID == Convert.ToUInt32(nameId) && tag.IsInt64(true))
                {
                    ruValue = tag.Int64;

                    return true;
                }
            }

            return false;
        }

        public void SetIntTagValue(byte nameId, uint uValue)
        {
            foreach (Tag tag in tags_)
            {
                if (tag.NameID == nameId &&
                    tag.IsInt)
                {
                    tag.Int = uValue;
                    return;
                }
            }

            Tag newTag = MuleEngine.CoreObjectManager.CreateTag(nameId, uValue);

            tags_.Add(newTag);
        }

        public void SetInt64TagValue(byte nameId, ulong uValue)
        {
            foreach (Tag tag in tags_)
            {
                if (tag.NameID == nameId &&
                    tag.IsInt64(true))
                {
                    tag.Int64 = uValue;
                    return;
                }
            }

            Tag newTag = MuleEngine.CoreObjectManager.CreateTag(nameId, uValue);

            tags_.Add(newTag);
        }

        public string GetStrTagValue(byte nameId)
        {
            foreach (Tag tag in tags_)
            {
                if (tag.NameID == nameId &&
                    tag.IsStr)
                {
                    return tag.Str;
                }
            }

            return string.Empty;
        }

        public string GetStrTagValue(string tagname)
        {
            foreach (Tag tag in tags_)
            {
                if (tag.NameID == 0 &&
                    tag.IsStr &&
                    string.Compare(tag.Name, tagname) == 0)
                {
                    return tag.Str;
                }
            }

            return string.Empty;
        }

        public void SetStrTagValue(byte nameId, string val)
        {
            foreach (Tag tag in tags_)
            {
                if (tag.NameID == nameId &&
                    tag.IsStr)
                {
                    tag.Str = val;
                    return;
                }
            }

            Tag newTag = MuleEngine.CoreObjectManager.CreateTag(nameId, val);

            tags_.Add(newTag);
        }

        public Tag GetTag(byte nameId, byte tagtype)
        {
            foreach (Tag tag in tags_)
            {
                if (tag.NameID == nameId &&
                    tag.TagType == tagtype)
                {
                    return tag;
                }
            }

            return null;
        }

        public Tag GetTag(string tagname, byte tagtype)
        {
            foreach (Tag tag in tags_)
            {
                if (tag.NameID == 0 &&
                    tag.TagType == tagtype &&
                    string.Compare(tag.Name, tagname) == 0)
                {
                    return tag;
                }
            }

            return null;
        }

        public Tag GetTag(byte nameId)
        {
            foreach (Tag tag in tags_)
            {
                if (tag.NameID == nameId)
                {
                    return tag;
                }
            }

            return null;
        }

        public Tag GetTag(string tagname)
        {
            foreach (Tag tag in tags_)
            {
                if (tag.NameID == 0 &&
                    string.Compare(tag.Name, tagname) == 0)
                {
                    return tag;
                }
            }

            return null;
        }

        public void AddTagUnique(Tag pTag)
        {
            foreach (Tag tag in tags_)
            {
                if ((tag.NameID != 0 && tag.NameID == pTag.NameID ||
                    tag.Name != null && string.Compare(tag.Name, pTag.Name) == 0) &&
                    tag.TagType == pTag.TagType)
                {
                    int index = tags_.IndexOf(tag);
                    tags_.RemoveAt(index);
                    tags_.Insert(index, pTag);
                    return;
                }
            }

            tags_.Add(pTag);
        }

        public void DeleteTag(byte tagname)
        {
            foreach (Tag tag in tags_)
            {
                if (tag.NameID == tagname)
                {
                    tags_.Remove(tag);
                    return;
                }
            }
        }

        public void DeleteTag(Tag pTag)
        {
            tags_.Remove(pTag);
        }

        public void ClearTags()
        {
            tags_.Clear();
        }

        public void CopyTags(List<Tag> tags)
        {
            foreach (Tag tag in tags)
            {
                tags_.Add(MuleEngine.CoreObjectManager.CreateTag(tag));
            }
        }

        public uint GetUserRating(bool bKadSearchIndicator)
        {
            return (bKadSearchIndicator && isKadCommentSearchRunning_) ? 6 : user_rating_;
        }

        public void LoadComment()
        {
            fileComment_ = MuleEngine.CoreObjectManager.Preference.GetFileComment(FileHash);

            fileRating_ = MuleEngine.CoreObjectManager.Preference.GetFileRating(FileHash);

            is_comment_loaded_ = true;
        }

        public virtual void UpdateFileRatingCommentAvail()
        {
            UpdateFileRatingCommentAvail(true);
        }

        public abstract void UpdateFileRatingCommentAvail(bool bForceUpdate);

        public bool AddNote(Kademlia.KadEntry pEntry)
        {
            foreach (Kademlia.KadEntry entry in kad_entry_notes_)
            {
                if (entry.SourceID.Equals(pEntry.SourceID))
                {
                    return false;
                }
            }

            kad_entry_notes_.Insert(0, pEntry);

            UpdateFileRatingCommentAvail();

            return true;
        }

        public void RefilterKadNotes()
        {
            RefilterKadNotes(true);
        }

        public void RefilterKadNotes(bool bUpdate)
        {
	        // check all availabe comments against our filter again
            if (string.IsNullOrEmpty(MuleEngine.CoreObjectManager.Preference.CommentFilter))
            {
		        return;
            }

            KadEntryList removed = new KadEntryList();

            string[] filters =
                MuleEngine.CoreObjectManager.Preference.CommentFilter.Split('|');

            if (filters == null || filters.Length == 0)
                return;

            foreach(KadEntry entry in kad_entry_notes_)
            {
                string desc =
                    entry.GetStrTagValue(CoreConstants.TAG_DESCRIPTION);

                if (!string.IsNullOrEmpty(desc))
                {
                    string strCommentLower = desc.ToLower();

                    foreach(string filter in filters)
                    {
                        if (strCommentLower.IndexOf(filter) >= 0)
                        {
                            removed.Add(entry);
                            break;
                        }
                    }
                }
            }

            foreach (KadEntry entry in removed)
                kad_entry_notes_.Remove(entry);

            // untill updated rating and m_bHasComment might be wrong
            if (bUpdate)
            {
                UpdateFileRatingCommentAvail();
            }
        }
        
        public virtual void SetFileName(string pszFileName,
            bool bReplaceInvalidFileSystemChars/* = false */,
            bool bAutoSetFileType/* = true */,
            bool bRemoveControlChars/* = false*/)
        {
            FileName_ = pszFileName;

            if (bReplaceInvalidFileSystemChars)
            {
                FileName_ = FileName_.Replace('/', '-');
                FileName_ = FileName_.Replace('>', '-');
                FileName_ = FileName_.Replace('<', '-');
                FileName_ = FileName_.Replace('*', '-');
                FileName_ = FileName_.Replace(':', '-');
                FileName_ = FileName_.Replace('?', '-');
                FileName_ = FileName_.Replace('\"', '-');
                FileName_ = FileName_.Replace('\\', '-');
                FileName_ = FileName_.Replace('|', '-');
            }

            ED2KFileTypes ed2kFileTypes = MuleEngine.CoreObjectManager.CreateED2KFileTypes();

            if (bAutoSetFileType)
                FileType = ed2kFileTypes.GetFileTypeByName(FileName_);

            if (bRemoveControlChars)
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < FileName_.Length; i++)
                {
                    if (FileName_[i] > '\x1F')
                    {
                        sb.Append(FileName_[i]);
                    }
                }

                FileName_ = sb.ToString();
            }
        }
        #endregion
    }
}
