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
using Mule.ED2K;

using Mpd.Utilities;

namespace Mule.File.Impl
{
    abstract class AbstractFileImpl : AbstractFile
    {
        #region Fields
        protected TagList tagList_ = new TagList();
        #endregion

        #region Properties
        public object KadNotes { get; set; }

        private string FileName_;
        public string FileName
        {
            get { return FileName_; }
            set { SetFileName(value, false, true, false); }
        }

        // returns the ED2K file type (an ASCII string)
        public string FileType {get;set;}


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

        public virtual ulong FileSize { get;set;}

        public bool IsLargeFile 
        { 
            get 
            {
                return FileSize > MuleConstants.OLD_MAX_EMULE_FILE_SIZE;
            } 
        }

        public virtual bool IsPartFile
        {
            get { return false; }
        }

        public TagList TagList
        {
            get { return tagList_; }
        }

        public bool HasComment {get;set;}
        public uint UserRating {get;set;}

        public bool HasRating
        {
            get { return UserRating > 0; }
        }

        public bool HasBadRating
        {
            get { return HasRating && (UserRating < 2); }
        }

        public virtual string FileComment { get;set;}
        public virtual uint FileRating { get;set;}

        public bool HasNullHash
        {
            get { return MpdUtilities.IsNullMd4(FileHash_); }
        }
        #endregion

        #region AbstractFile Members
        public uint GetIntTagValue(byte nameId)
        {
            foreach (Tag tag in tagList_)
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
            foreach (Tag tag in tagList_)
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
            foreach (Tag tag in tagList_)
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
            foreach (Tag tag in tagList_)
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
            foreach (Tag tag in tagList_)
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
            foreach (Tag tag in tagList_)
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
            foreach (Tag tag in tagList_)
            {
                if (tag.NameID == nameId &&
                    tag.IsInt)
                {
                    tag.Int = uValue;
                    return;
                }
            }

            Tag newTag = MpdObjectManager.CreateTag(nameId, uValue);

            tagList_.Add(newTag);
        }

        public void SetInt64TagValue(byte nameId, ulong uValue)
        {
            foreach (Tag tag in tagList_)
            {
                if (tag.NameID == nameId &&
                    tag.IsInt64(true))
                {
                    tag.Int64 = uValue;
                    return;
                }
            }

            Tag newTag = MpdObjectManager.CreateTag(nameId, uValue);

            tagList_.Add(newTag);
        }

        public string GetStrTagValue(byte nameId)
        {
            foreach (Tag tag in tagList_)
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
            foreach (Tag tag in tagList_)
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
            foreach (Tag tag in tagList_)
            {
                if (tag.NameID == nameId &&
                    tag.IsStr)
                {
                    tag.Str = val;
                    return;
                }
            }

            Tag newTag = MpdObjectManager.CreateTag(nameId, val);

            tagList_.Add(newTag);
        }

        public Tag GetTag(byte nameId, byte tagtype)
        {
            foreach (Tag tag in tagList_)
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
            foreach (Tag tag in tagList_)
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
            foreach (Tag tag in tagList_)
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
            foreach (Tag tag in tagList_)
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
            foreach (Tag tag in tagList_)
            {
                if ((tag.NameID != 0 && tag.NameID == pTag.NameID ||
                    tag.Name != null && string.Compare(tag.Name, pTag.Name) == 0) &&
                    tag.TagType == pTag.TagType)
                {
                    int index = tagList_.IndexOf(tag);
                    tagList_.RemoveAt(index);
                    tagList_.Insert(index, pTag);
                    return;
                }
            }

            tagList_.Add(pTag);
        }

        public void DeleteTag(byte tagname)
        {
            foreach (Tag tag in tagList_)
            {
                if (tag.NameID == tagname)
                {
                    tagList_.Remove(tag);
                    return;
                }
            }
        }

        public void DeleteTag(Tag pTag)
        {
            tagList_.Remove(pTag);
        }

        public void ClearTags()
        {
            tagList_.Clear();
        }

        public void CopyTags(TagList tags)
        {
            foreach (Tag tag in tags)
            {
                tagList_.Add(MpdObjectManager.CreateTag(tag));
            }
        }

        public uint GetUserRating(bool bKadSearchIndicator)
        {
            return (bKadSearchIndicator) ? 6 : UserRating;
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

            ED2KFileTypes ed2kFileTypes = ED2KObjectManager.CreateED2KFileTypes();

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
