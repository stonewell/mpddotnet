﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Mule.ED2K.Impl;

using Mpd.Utilities;
using Mpd.Generic;

namespace Mule.ED2K
{
    public class ED2KObjectManager
    {
        public static ED2KServerLink CreateED2KServerLink(string ip, string port)
        {
            return MpdObjectManager.CreateObject(typeof(ED2KServerLinkImpl), ip, port) as ED2KServerLink;
        }

        public static ED2KNodesListLink CreateED2KNodesListLink(string address)
        {
            return MpdObjectManager.CreateObject(typeof(ED2KNodesListLinkImpl), address) as ED2KNodesListLink;
        }

        public static ED2KFileLink CreateED2KFileLink(string pszName,
            string pszSize,
            string pszHash,
            string[] allParams,
            string pszSources)
        {
            return MpdObjectManager.CreateObject(typeof(ED2KFileLinkImpl), pszName, pszSize, pszHash, allParams, pszSources) as ED2KFileLink;
        }

        public static UnresolvedHostname CreateUnresolvedHostname()
        {
            return MpdObjectManager.CreateObject(typeof(UnresolvedHostnameImpl)) as UnresolvedHostname;
        }

        public static ED2KServerListLink CreateED2KServerListLink(string address)
        {
            return MpdObjectManager.CreateObject(typeof(ED2KServerListLinkImpl), address) as ED2KServerListLink;
        }
        public static ED2KFileTypes CreateED2KFileTypes()
        {
            return MpdObjectManager.CreateObject(typeof(ED2KFileTypesImpl)) as ED2KFileTypes;
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
    }
}
