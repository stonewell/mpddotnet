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
using System.Security.Cryptography;

namespace Mule.Core
{
    public interface MuleCollection
    {
        bool InitCollectionFromFile(string sFilePath, string sFileName);
        CollectionFile AddFileToCollection(AbstractFile pAbstractFile, bool bCreateClone);
        void RemoveFileFromCollection(AbstractFile pAbstractFile);
        void WriteToFileAddShared();
        void WriteToFileAddShared(RSAPKCS1SignatureFormatter pSignkey);
        void SetCollectionAuthorKey(byte[] abyCollectionAuthorKey, uint nSize);
        string GetCollectionAuthorKeyString();
        string GetAuthorKeyHashString();

        string CollectionName { get; set;}
        string CollectionAuthorName { get; set;}

        bool TextFormat { get; set; }
    }
}
