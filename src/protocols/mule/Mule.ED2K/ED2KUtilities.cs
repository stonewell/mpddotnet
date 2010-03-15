using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Generic;
using Mpd.Generic.IO;
using Mule.Definitions;
using Mpd.Utilities;

namespace Mule.ED2K
{
    public class ED2KUtilities
    {
        public static bool WriteOptED2KUTF8Tag(FileDataIO file,
            string filename, byte uTagName)
        {
            if (!NeedUTF8String(filename))
                return false;
            Tag tag = MpdObjectManager.CreateTag(uTagName, filename);
            tag.WriteTagToFile(file, Utf8StrEnum.utf8strOptBOM);
            return true;
        }

        public static bool NeedUTF8String(string filename)
        {
            for (int i = 0; i < filename.Length; i++)
            {
                if (filename[i] >= 0x100U)
                {
                    return true;
                }
            }

            return false;
        }

        private struct EmuleToED2KMetaTagsMap
        {
            public EmuleToED2KMetaTagsMap(byte id, byte type, string name)
            {
                nID = id;
                nED2KType = type;
                pszED2KName = name;
            }

            public byte nID;
            public byte nED2KType;
            public string pszED2KName;
        };

        public static void ConvertED2KTag(ref Tag pTag)
        {
            if (pTag.NameID == 0 && pTag.Name != null)
            {
                EmuleToED2KMetaTagsMap[] _aEmuleToED2KMetaTagsMap = new EmuleToED2KMetaTagsMap[]
		        {
			        // Artist, Album and Title are disabled because they should be already part of the filename
			        // and would therefore be redundant information sent to the servers.. and the servers count the
			        // amount of sent data!
			        new EmuleToED2KMetaTagsMap ( MuleConstants.FT_MEDIA_ARTIST,  MuleConstants.TAGTYPE_STRING, MuleConstants.FT_ED2K_MEDIA_ARTIST ),
			        new EmuleToED2KMetaTagsMap ( MuleConstants.FT_MEDIA_ALBUM,   MuleConstants.TAGTYPE_STRING, MuleConstants.FT_ED2K_MEDIA_ALBUM ),
			        new EmuleToED2KMetaTagsMap ( MuleConstants.FT_MEDIA_TITLE,   MuleConstants.TAGTYPE_STRING, MuleConstants.FT_ED2K_MEDIA_TITLE ),
			        new EmuleToED2KMetaTagsMap ( MuleConstants.FT_MEDIA_LENGTH,  MuleConstants.TAGTYPE_STRING, MuleConstants.FT_ED2K_MEDIA_LENGTH ),
			        new EmuleToED2KMetaTagsMap ( MuleConstants.FT_MEDIA_LENGTH,  MuleConstants.TAGTYPE_UINT32, MuleConstants.FT_ED2K_MEDIA_LENGTH ),
			        new EmuleToED2KMetaTagsMap ( MuleConstants.FT_MEDIA_BITRATE, MuleConstants.TAGTYPE_UINT32, MuleConstants.FT_ED2K_MEDIA_BITRATE ),
			        new EmuleToED2KMetaTagsMap ( MuleConstants.FT_MEDIA_CODEC,   MuleConstants.TAGTYPE_STRING, MuleConstants.FT_ED2K_MEDIA_CODEC )
		        };

                for (int j = 0; j < _aEmuleToED2KMetaTagsMap.Length; j++)
                {
                    if (string.Compare(pTag.Name, _aEmuleToED2KMetaTagsMap[j].pszED2KName) == 0
                        && ((pTag.IsStr && _aEmuleToED2KMetaTagsMap[j].nED2KType == MuleConstants.TAGTYPE_STRING) ||
                        (pTag.IsInt && _aEmuleToED2KMetaTagsMap[j].nED2KType == MuleConstants.TAGTYPE_UINT32)))
                    {
                        if (pTag.IsStr)
                        {
                            if (_aEmuleToED2KMetaTagsMap[j].nID == MuleConstants.FT_MEDIA_LENGTH)
                            {
                                uint nMediaLength = 0;
                                uint hour = 0, min = 0, sec = 0;
                                DateTime dt = DateTime.Now;

                                if (MpdUtilities.Scan3UInt32(pTag.Str, ref hour, ref min, ref sec) == 3)
                                    nMediaLength = hour * 3600 + min * 60 + sec;
                                else if (MpdUtilities.Scan2UInt32(pTag.Str, ref min, ref sec) == 2)
                                    nMediaLength = min * 60 + sec;
                                else if (MpdUtilities.ScanUInt32(pTag.Str, ref sec) == 1)
                                    nMediaLength = sec;

                                if (nMediaLength == 0)
                                    pTag = null;
                                else
                                    pTag =
                                        MpdObjectManager.CreateTag(_aEmuleToED2KMetaTagsMap[j].nID,
                                            nMediaLength);
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(pTag.Str))
                                {
                                    pTag =
                                        MpdObjectManager.CreateTag(_aEmuleToED2KMetaTagsMap[j].nID, pTag.Str);
                                }
                                else
                                {
                                    pTag = null;
                                }
                            }
                        }
                        else if (pTag.IsInt)
                        {
                            if (pTag.Int != 0)
                            {
                                pTag =
                                    MpdObjectManager.CreateTag(_aEmuleToED2KMetaTagsMap[j].nID, pTag.Int);
                            }
                            else
                            {
                                pTag = null;
                            }
                        }
                        break;
                    }
                }
            }
        }

    }
}
