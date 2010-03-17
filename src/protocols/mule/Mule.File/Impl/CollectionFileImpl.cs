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
using Mule.ED2K;
using Mpd.Generic.IO;
using Mpd.Generic;
using Mpd.Utilities;


namespace Mule.File.Impl
{
    class CollectionFileImpl : AbstractFileImpl, CollectionFile
    {
        #region CollectionFile Members

        public bool InitFromLink(string sLink)
        {
            ED2KLink pLink = null;
            ED2KFileLink pFileLink = null;
            try
            {
                pLink = ED2KObjectManager.CreateLinkFromUrl(sLink);
                if (pLink == null)
                    throw new Exception("Not a Valid file link:" + sLink);

                pFileLink = pLink.FileLink;
                if (pFileLink == null)
                    throw new Exception("Not a Valid file link:" + sLink);
            }
            catch (Exception)
            {
                //TODO:Log
                return false;
            }

            tagList_.Add(MpdObjectManager.CreateTag(MuleConstants.FT_FILEHASH, pFileLink.HashKey));
            MpdUtilities.Md4Cpy(FileHash, pFileLink.HashKey);

            tagList_.Add(MpdObjectManager.CreateTag(MuleConstants.FT_FILESIZE, pFileLink.Size, true));
            FileSize = pFileLink.Size;

            tagList_.Add(MpdObjectManager.CreateTag(MuleConstants.FT_FILENAME, pFileLink.Name));
            SetFileName(pFileLink.Name, false, false, false);

            return true;
        }

        public void WriteCollectionInfo(FileDataIO out_data)
        {
            out_data.WriteUInt32(Convert.ToUInt32(tagList_.Count));

            foreach (Tag tag in tagList_)
            {
                tag.WriteNewEd2kTag(out_data, Utf8StrEnum.utf8strRaw);
            }
        }

        #endregion

        #region Overrides
        #endregion
    }
}
