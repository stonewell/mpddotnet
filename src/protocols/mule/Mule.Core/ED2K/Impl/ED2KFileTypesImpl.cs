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
using System.IO;

namespace Mule.Core.ED2K.Impl
{
    class ED2KFileTypeComparer : IComparer<ED2KFileType>
    {
        #region IComparer<ED2KFileType> Members
        public int Compare(ED2KFileType x, ED2KFileType y)
        {
            return x.ExtName.CompareTo(y.ExtName);
        }

        #endregion
    }

    class ED2KFileType
    {
        private string ext_ = null;
        private ED2KFileTypeEnum filetype_ = ED2KFileTypeEnum.ED2KFT_ANY;

        public ED2KFileType(string ext, ED2KFileTypeEnum filetype)
        {
            ext_ = ext;
            filetype_ = filetype;
        }

        public string ExtName
        {
            get { return ext_; }
        }

        public ED2KFileTypeEnum FileType
        {
            get { return filetype_; }
        }
    };

    enum ED2KFileTypeNameEnum
    {
        Audio = 1,
        Video,
        Image,
        Pro,
        Doc,
        Arc,
        Iso,
        EmuleCollection
    }

    class ED2KFileTypesImpl : ED2KFileTypes
    {
        #region Fields
        private static readonly ED2KFileType[] ED2K_FILE_TYPES = null;
        private static readonly Dictionary<string, ED2KFileType> FileTypesMap =
            null;
        #endregion

        #region Constructors
        static ED2KFileTypesImpl()
        {
            ED2K_FILE_TYPES = new ED2KFileType[] 
                 {
                    new ED2KFileType(".669",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".aac",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".aif",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".aiff",  ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".amf",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".ams",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".ape",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".au",    ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".dbm",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".dmf",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".dsm",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".far",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".flac",  ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".it",    ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".m4a",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".mdl",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".med",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".mid",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".midi",  ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".mka",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".mod",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".mol",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".mp1",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".mp2",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".mp3",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".mpa",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".mpc",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".mpp",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".mtm",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".nst",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".ogg",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".okt",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".psm",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".ptm",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".ra",    ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".rmi",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".s3m",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".stm",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".ult",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".umx",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".wav",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".wma",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".wow",   ED2KFileTypeEnum.ED2KFT_AUDIO ),
                    new ED2KFileType(".xm",    ED2KFileTypeEnum.ED2KFT_AUDIO ),

                    new ED2KFileType(".3g2",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".3gp",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".3gp2",  ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".3gpp",  ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".asf",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".avi",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".divx",  ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".m1v",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".m2v",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".m4v",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".mkv",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".mov",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".mp1v",  ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".mp2v",  ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".mp4",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".mpe",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".mpeg",  ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".mpg",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".mps",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".mpv",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".mpv1",  ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".mpv2",  ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".ogm",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".qt",    ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".ram",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".rm",    ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".rmvb",  ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".rv",    ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".rv9",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".swf",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".ts",    ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".vivo",  ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".vob",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".wmv",   ED2KFileTypeEnum.ED2KFT_VIDEO ),
                    new ED2KFileType(".xvid",  ED2KFileTypeEnum.ED2KFT_VIDEO ),

                    new ED2KFileType(".bmp",   ED2KFileTypeEnum.ED2KFT_IMAGE ),
                    new ED2KFileType(".dcx",   ED2KFileTypeEnum.ED2KFT_IMAGE ),
                    new ED2KFileType(".emf",   ED2KFileTypeEnum.ED2KFT_IMAGE ),
                    new ED2KFileType(".gif",   ED2KFileTypeEnum.ED2KFT_IMAGE ),
                    new ED2KFileType(".ico",   ED2KFileTypeEnum.ED2KFT_IMAGE ),
                    new ED2KFileType(".jpeg",  ED2KFileTypeEnum.ED2KFT_IMAGE ),
                    new ED2KFileType(".jpg",   ED2KFileTypeEnum.ED2KFT_IMAGE ),
                    new ED2KFileType(".pct",   ED2KFileTypeEnum.ED2KFT_IMAGE ),
                    new ED2KFileType(".pcx",   ED2KFileTypeEnum.ED2KFT_IMAGE ),
                    new ED2KFileType(".pic",   ED2KFileTypeEnum.ED2KFT_IMAGE ),
                    new ED2KFileType(".pict",  ED2KFileTypeEnum.ED2KFT_IMAGE ),
                    new ED2KFileType(".png",   ED2KFileTypeEnum.ED2KFT_IMAGE ),
                    new ED2KFileType(".psd",   ED2KFileTypeEnum.ED2KFT_IMAGE ),
                    new ED2KFileType(".psp",   ED2KFileTypeEnum.ED2KFT_IMAGE ),
                    new ED2KFileType(".tga",   ED2KFileTypeEnum.ED2KFT_IMAGE ),
                    new ED2KFileType(".tif",   ED2KFileTypeEnum.ED2KFT_IMAGE ),
                    new ED2KFileType(".tiff",  ED2KFileTypeEnum.ED2KFT_IMAGE ),
                    new ED2KFileType(".wmf",   ED2KFileTypeEnum.ED2KFT_IMAGE ),
                    new ED2KFileType(".xif",   ED2KFileTypeEnum.ED2KFT_IMAGE ),

                    new ED2KFileType(".7z",    ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".ace",   ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".alz",   ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".arj",   ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".bz2",   ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".cab",   ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".cbz",   ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".cbr",   ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".gz",    ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".hqx",   ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".lha",   ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".lzh",   ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".msi",   ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".rar",   ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".sea",   ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".sit",   ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".tar",   ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".tgz",   ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".uc2",   ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".z",     ED2KFileTypeEnum.ED2KFT_ARCHIVE ),
                    new ED2KFileType(".zip",   ED2KFileTypeEnum.ED2KFT_ARCHIVE ),

                    new ED2KFileType(".bat",   ED2KFileTypeEnum.ED2KFT_PROGRAM ),
                    new ED2KFileType(".cmd",   ED2KFileTypeEnum.ED2KFT_PROGRAM ),
                    new ED2KFileType(".com",   ED2KFileTypeEnum.ED2KFT_PROGRAM ),
                    new ED2KFileType(".exe",   ED2KFileTypeEnum.ED2KFT_PROGRAM ),

                    new ED2KFileType(".bin",   ED2KFileTypeEnum.ED2KFT_CDIMAGE ),
                    new ED2KFileType(".bwa",   ED2KFileTypeEnum.ED2KFT_CDIMAGE ),
                    new ED2KFileType(".bwi",   ED2KFileTypeEnum.ED2KFT_CDIMAGE ),
                    new ED2KFileType(".bws",   ED2KFileTypeEnum.ED2KFT_CDIMAGE ),
                    new ED2KFileType(".bwt",   ED2KFileTypeEnum.ED2KFT_CDIMAGE ),
                    new ED2KFileType(".ccd",   ED2KFileTypeEnum.ED2KFT_CDIMAGE ),
                    new ED2KFileType(".cue",   ED2KFileTypeEnum.ED2KFT_CDIMAGE ),
                    new ED2KFileType(".dmg",   ED2KFileTypeEnum.ED2KFT_CDIMAGE ),
                    new ED2KFileType(".dmz",   ED2KFileTypeEnum.ED2KFT_CDIMAGE ),
                    new ED2KFileType(".img",   ED2KFileTypeEnum.ED2KFT_CDIMAGE ),
                    new ED2KFileType(".iso",   ED2KFileTypeEnum.ED2KFT_CDIMAGE ),
                    new ED2KFileType(".mdf",   ED2KFileTypeEnum.ED2KFT_CDIMAGE ),
                    new ED2KFileType(".mds",   ED2KFileTypeEnum.ED2KFT_CDIMAGE ),
                    new ED2KFileType(".nrg",   ED2KFileTypeEnum.ED2KFT_CDIMAGE ),
                    new ED2KFileType(".sub",   ED2KFileTypeEnum.ED2KFT_CDIMAGE ),
                    new ED2KFileType(".toast", ED2KFileTypeEnum.ED2KFT_CDIMAGE ),

                    new ED2KFileType(".chm",   ED2KFileTypeEnum.ED2KFT_DOCUMENT ),
                    new ED2KFileType(".css",   ED2KFileTypeEnum.ED2KFT_DOCUMENT ),
                    new ED2KFileType(".diz",   ED2KFileTypeEnum.ED2KFT_DOCUMENT ),
                    new ED2KFileType(".doc",   ED2KFileTypeEnum.ED2KFT_DOCUMENT ),
                    new ED2KFileType(".dot",   ED2KFileTypeEnum.ED2KFT_DOCUMENT ),
                    new ED2KFileType(".hlp",   ED2KFileTypeEnum.ED2KFT_DOCUMENT ),
                    new ED2KFileType(".htm",   ED2KFileTypeEnum.ED2KFT_DOCUMENT ),
                    new ED2KFileType(".html",  ED2KFileTypeEnum.ED2KFT_DOCUMENT ),
                    new ED2KFileType(".nfo",   ED2KFileTypeEnum.ED2KFT_DOCUMENT ),
                    new ED2KFileType(".pdf",   ED2KFileTypeEnum.ED2KFT_DOCUMENT ),
                    new ED2KFileType(".pps",   ED2KFileTypeEnum.ED2KFT_DOCUMENT ),
                    new ED2KFileType(".ppt",   ED2KFileTypeEnum.ED2KFT_DOCUMENT ),
                    new ED2KFileType(".ps",    ED2KFileTypeEnum.ED2KFT_DOCUMENT ),
                    new ED2KFileType(".rtf",   ED2KFileTypeEnum.ED2KFT_DOCUMENT ),
                    new ED2KFileType(".txt",   ED2KFileTypeEnum.ED2KFT_DOCUMENT ),
                    new ED2KFileType(".wri",   ED2KFileTypeEnum.ED2KFT_DOCUMENT ),
                    new ED2KFileType(".xls",   ED2KFileTypeEnum.ED2KFT_DOCUMENT ),
                    new ED2KFileType(".xml",   ED2KFileTypeEnum.ED2KFT_DOCUMENT ),

	                new ED2KFileType(".emulecollection", ED2KFileTypeEnum.ED2KFT_EMULECOLLECTION),
                };

            Array.Sort(ED2K_FILE_TYPES, new ED2KFileTypeComparer());

            FileTypesMap = new Dictionary<string, ED2KFileType>();

            foreach (ED2KFileType type in ED2K_FILE_TYPES)
            {
                FileTypesMap[type.ExtName] = type;
            }
        }

        public ED2KFileTypesImpl()
        {
        }

        #endregion

        #region ED2KFileTypes Members
        public string GetFileTypeByName(string pszFileName)
        {
            ED2KFileTypeEnum typeId =
                GetED2KFileTypeID(pszFileName);

            if (Enum.IsDefined(typeof(ED2KFileTypeNameEnum), typeId))
            {
                ED2KFileTypeNameEnum typeName = 
                    (ED2KFileTypeNameEnum)typeId;

                return typeName.ToString();
            }

            return string.Empty;
        }

        public ED2KFileTypeEnum GetED2KFileTypeSearchID(ED2KFileTypeEnum iFileID)
        {
            if (iFileID == ED2KFileTypeEnum.ED2KFT_ARCHIVE ||
                iFileID == ED2KFileTypeEnum.ED2KFT_CDIMAGE)
                return ED2KFileTypeEnum.ED2KFT_PROGRAM;

            return iFileID;
        }

        public string GetED2KFileTypeSearchTerm(ED2KFileTypeEnum iFileID)
        {
            if (iFileID == ED2KFileTypeEnum.ED2KFT_ARCHIVE ||
                iFileID == ED2KFileTypeEnum.ED2KFT_CDIMAGE)
            {
                return ED2KFileTypeNameEnum.Pro.ToString();
            }

            if (Enum.IsDefined(typeof(ED2KFileTypeNameEnum), iFileID))
            {
                ED2KFileTypeNameEnum typeName =
                    (ED2KFileTypeNameEnum)iFileID;

                return typeName.ToString();
            }

            return null;
        }

        public ED2KFileTypeEnum GetED2KFileTypeID(string pszFileName)
        {
            string pszExt = Path.GetExtension(pszFileName);

            if (string.IsNullOrEmpty(pszExt))
                return ED2KFileTypeEnum.ED2KFT_ANY;

            pszExt = pszExt.ToLower();

            if (FileTypesMap.ContainsKey(pszExt))
                return FileTypesMap[pszExt].FileType;

            return ED2KFileTypeEnum.ED2KFT_ANY;
        }

        #endregion
    }
}
