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

namespace Mule.Core.File.Impl
{
    class TagImpl : Tag
    {
        #region Tag Members

        public uint TagType
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public uint NameID
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public string Name
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool IsStr
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool IsInt
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool IsFloat
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool IsHash
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool IsBlob
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool IsInt64()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool IsInt64(bool bOrInt32)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public uint Int
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public ulong Int64
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public string Str
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public float Float
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public byte[] Hash
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public uint BlobSize
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public byte[] Blob
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public Tag CloneTag()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool WriteTagToFile(FileDataIO file)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool WriteTagToFile(FileDataIO file, Utf8StrEnum eStrEncode)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool WriteNewEd2kTag(FileDataIO file)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool WriteNewEd2kTag(FileDataIO file, Utf8StrEnum eStrEncode)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public string GetFullInfo(DbgGetFileMetaTagName fn)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
