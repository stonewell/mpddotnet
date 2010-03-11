using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Mule.ED2K.Impl;
using Mule.Definitions;
using Mpd.Utilities;
using Mpd.Generic.Types;

namespace Mule.ED2K
{
    public class ED2KObjectManager
    {
        public static ED2KServerLink CreateED2KServerLink(string ip, string port)
        {
            return CreateObject(typeof(ED2KServerLinkImpl), ip, port) as ED2KServerLink;
        }

        public static ED2KNodesListLink CreateED2KNodesListLink(string address)
        {
            return CreateObject(typeof(ED2KNodesListLinkImpl), address) as ED2KNodesListLink;
        }

        public static ED2KFileLink CreateED2KFileLink(string pszName,
            string pszSize,
            string pszHash,
            string[] allParams,
            string pszSources)
        {
            return CreateObject(typeof(ED2KFileLinkImpl), pszName, pszSize, pszHash, allParams, pszSources) as ED2KFileLink;
        }

        public static UnresolvedHostname CreateUnresolvedHostname()
        {
            return CreateObject(typeof(UnresolvedHostnameImpl)) as UnresolvedHostname;
        }

        public static ED2KServerListLink CreateED2KServerListLink(string address)
        {
            return CreateObject(typeof(ED2KServerListLinkImpl), address) as ED2KServerListLink;
        }
        public static ED2KFileTypes CreateED2KFileTypes()
        {
            return CreateObject(typeof(ED2KFileTypesImpl)) as ED2KFileTypes;
        }

        public static ED2KLink CreateLinkFromUrl(string strURI)
        {
            strURI.Trim(); // This function is used for various sources, trim the string again.
            int iPos = 0;

            string[] tokens = strURI.Split('|');

            if (tokens.Length == 0)
                throw new Exception("Not a valid ed2k link:" + strURI);

            string strTok = GetNextToken(ref iPos, tokens);
            if (string.Compare(strTok, "ed2k://", true) == 0)
            {
                strTok = GetNextToken(ref iPos, tokens);
                if (string.Compare(strTok, "file") == 0)
                {
                    string strName = GetNextToken(ref iPos, tokens);
                    if (!string.IsNullOrEmpty(strName))
                    {
                        string strSize = GetNextToken(ref iPos, tokens);
                        if (!string.IsNullOrEmpty(strSize))
                        {
                            string strHash = GetNextToken(ref iPos, tokens);
                            if (!string.IsNullOrEmpty(strHash))
                            {
                                List<string> astrEd2kParams = new List<string>();
                                bool bEmuleExt = false;
                                string strEmuleExt = null;

                                string strLastTok = null;
                                strTok = GetNextToken(ref iPos, tokens);
                                while (!string.IsNullOrEmpty(strTok))
                                {
                                    strLastTok = strTok;
                                    if (string.Compare(strTok, "/") == 0)
                                    {
                                        if (bEmuleExt)
                                            break;
                                        bEmuleExt = true;
                                    }
                                    else
                                    {
                                        if (bEmuleExt)
                                        {
                                            if (!string.IsNullOrEmpty(strEmuleExt))
                                                strEmuleExt += '|';
                                            strEmuleExt += strTok;
                                        }
                                        else
                                            astrEd2kParams.Add(strTok);
                                    }
                                    strTok = GetNextToken(ref iPos, tokens);
                                }

                                if (string.Compare(strLastTok, "/") == 0)
                                {
                                    ED2KFileLink fileLink =
                                        CreateED2KFileLink(strName, strSize, strHash,
                                            astrEd2kParams.ToArray(),
                                            string.IsNullOrEmpty(strEmuleExt) ? null : strEmuleExt);

                                    return fileLink;
                                }
                            }
                        }
                    }
                }
                else if (string.Compare(strTok, "serverlist") == 0)
                {
                    string strURL = GetNextToken(ref iPos, tokens);
                    if (!string.IsNullOrEmpty(strURL) &&
                        string.Compare(GetNextToken(ref iPos, tokens), "/") == 0)
                        return CreateED2KServerListLink(strURL);
                }
                else if (string.Compare(strTok, "server") == 0)
                {
                    string strServer = GetNextToken(ref iPos, tokens);
                    if (!string.IsNullOrEmpty(strServer))
                    {
                        string strPort = GetNextToken(ref iPos, tokens);
                        if (!string.IsNullOrEmpty(strPort) &&
                            string.Compare(GetNextToken(ref iPos, tokens), "/") == 0)
                            return CreateED2KServerLink(strServer, strPort);
                    }
                }
                else if (string.Compare(strTok, "nodeslist") == 0)
                {
                    string strURL = GetNextToken(ref iPos, tokens);
                    if (!string.IsNullOrEmpty(strURL) &&
                        string.Compare(GetNextToken(ref iPos, tokens), "/") == 0)
                        return CreateED2KNodesListLink(strURL);
                }
            }

            throw new Exception("Not a ed2k link:" + strURI);
        }

        private static string GetNextToken(ref int iPos, string[] tokens)
        {
            if (tokens.Length > iPos)
                return tokens[iPos++];

            return null;
        }
        private static object CreateObject(Type t, params object[] parameters)
        {
            object obj = t.Assembly.CreateInstance(t.FullName,
                true,
                BindingFlags.CreateInstance,
                null,
                parameters,
                null,
                null);

            return obj;
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

                                if (MPDUtilities.Scan3UInt32(pTag.Str, ref hour, ref min, ref sec) == 3)
                                    nMediaLength = hour * 3600 + min * 60 + sec;
                                else if (MPDUtilities.Scan2UInt32(pTag.Str, ref min, ref sec) == 2)
                                    nMediaLength = min * 60 + sec;
                                else if (MPDUtilities.ScanUInt32(pTag.Str, ref sec) == 1)
                                    nMediaLength = sec;

                                if (nMediaLength == 0)
                                    pTag = null;
                                else
                                    pTag =
                                        MpdGenericObjectManager.CreateTag(_aEmuleToED2KMetaTagsMap[j].nID,
                                            nMediaLength);
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(pTag.Str))
                                {
                                    pTag =
                                        MpdGenericObjectManager.CreateTag(_aEmuleToED2KMetaTagsMap[j].nID, pTag.Str);
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
                                    MpdGenericObjectManager.CreateTag(_aEmuleToED2KMetaTagsMap[j].nID, pTag.Int);
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
