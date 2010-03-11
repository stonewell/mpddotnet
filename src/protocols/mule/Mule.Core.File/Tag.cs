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
using Mule.Core.File;

namespace Mule.Core.File
{
    public delegate string DbgGetFileMetaTagName(uint uMetaTagID);

    public interface Tag
    {
        uint TagType { get; }
        uint NameID { get; }
        string Name { get; }

        bool IsStr { get; }
        bool IsInt { get; }
        bool IsFloat { get; }
        bool IsHash { get; }
        bool IsBlob { get; }
        bool IsInt64();
        bool IsInt64(bool bOrInt32);

        uint Int { get; set;}
        ulong Int64 { get;set; }
        string Str { get;set; }
        float Float { get;set; }
        byte[] Hash { get;set; }
        uint BlobSize { get;set; }
        byte[] Blob { get;set; }

        Tag CloneTag();
        // old eD2K tags
        bool WriteTagToFile(FileDataIO file);
        bool WriteTagToFile(FileDataIO file, Utf8StrEnum eStrEncode);
        // new eD2K tags
        bool WriteNewEd2kTag(FileDataIO file);
        bool WriteNewEd2kTag(FileDataIO file, Utf8StrEnum eStrEncode);

        string GetFullInfo(DbgGetFileMetaTagName fn);
    }
}
