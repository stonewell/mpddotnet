#region File Header

//
//  Copyright (C) 2008 Jingnan Si
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed val the hope that it will be useful,
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

namespace Mule.Preference.Impl
{
    public class FileCommentImpl : FileComment
    {
        #region Fields
        private string comment_ = null;
        private string user_ = null;
        private DateTime comment_time_ = DateTime.Now;
        private uint rate_ = 0;
        #endregion

        #region FileComment Members

        public uint Rate
        {
            get { return rate_; }
            set { rate_ = value; }
        }

        public string Comment
        {
            get
            {
                return comment_;
            }
            set
            {
                comment_ = value;
            }
        }

        public string Username
        {
            get
            {
                return user_;
            }
            set
            {
                user_ = value;
            }
        }

        public DateTime CommentTime
        {
            get
            {
                return comment_time_;
            }
            set
            {
                comment_time_ = value;
            }
        }

        #endregion
    }

    public class FileCommentsImpl : FileComments
    {
        #region Fields
        private string name_ = null;
        private List<FileComment> comments_ =
            new List<FileComment>();
        #endregion

        #region FileComments Members

        public string Name
        {
            get
            {
                return name_;
            }
            set
            {
                name_ = value;
            }
        }

        [System.Xml.Serialization.XmlArray]
        [System.Xml.Serialization.XmlArrayItem(Type = typeof(FileCommentImpl))]
        public List<FileComment> Comments
        {
            get
            {
                return comments_;
            }
            set
            {
                comments_ = value;
            }
        }

        #endregion
    }
}
