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

namespace Mule.Core.ED2K
{
    public enum ED2KFileTypeEnum
    {
        ED2KFT_ANY = 0,
        ED2KFT_AUDIO = 1,	// ED2K protocol value (eserver 17.6+)
        ED2KFT_VIDEO = 2,	// ED2K protocol value (eserver 17.6+)
        ED2KFT_IMAGE = 3,	// ED2K protocol value (eserver 17.6+)
        ED2KFT_PROGRAM = 4,	// ED2K protocol value (eserver 17.6+)
        ED2KFT_DOCUMENT = 5,	// ED2K protocol value (eserver 17.6+)
        ED2KFT_ARCHIVE = 6,	// ED2K protocol value (eserver 17.6+)
        ED2KFT_CDIMAGE = 7,	// ED2K protocol value (eserver 17.6+)
        ED2KFT_EMULECOLLECTION = 8
    };
    
    public interface ED2KFileTypes
    {
        string GetFileTypeByName(string pszFileName);
        ED2KFileTypeEnum GetED2KFileTypeSearchID(ED2KFileTypeEnum iFileID);
        string GetED2KFileTypeSearchTerm(ED2KFileTypeEnum iFileID);
        ED2KFileTypeEnum GetED2KFileTypeID(string pszFileName);
    }
}
