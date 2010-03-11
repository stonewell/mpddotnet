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
using Mule.Core.AICH;
using Mule.Core.AICH.Impl;
using Mule.Core.File;
using Mule.Core.Preference;
using Mule.Core.AICH.SHA;
using Mule.Core.Preference.Impl;
using System.IO;
using Mule.Core.ED2K;
using Mule.Core.ED2K.Impl;
using Mule.Core.File.Impl;
using System.Reflection;

namespace Mule.Core
{
    sealed public class CoreObjectManager
    {
        #region Fields
        private CorePreference preference_ = null;
        private Random radom0_ = new Random(0);
        private MuleEngine muleEngine_ = null;
        #endregion

        #region Constructor
        internal CoreObjectManager(MuleEngine muleEngine)
        {
            muleEngine_ = muleEngine;

            try
            {
                preference_ = new CorePreferenceImpl();
                preference_.Load();
            }
            catch
            {
                //TODO:Log
                preference_ = new CorePreferenceImpl();
                preference_.Init();
            }
        }

        static CoreObjectManager()
        {
        }
        #endregion

        #region Methods
        public AICHHash CreateAICHHash()
        {
            return CreateObject(typeof(AICHHashImpl)) as AICHHash;
        }
        public AICHHash CreateAICHHash(FileDataIO file)
        {
            return CreateObject(typeof(AICHHashImpl), file) as AICHHash;
        }
        public AICHHash CreateAICHHash(byte[] data)
        {
            return CreateObject(typeof(AICHHashImpl), data) as AICHHash;
        }
        public AICHHash CreateAICHHash(AICHHash hash)
        {
            return CreateObject(typeof(AICHHashImpl), hash) as AICHHash;
        }

        public AICHHashTree CreateAICHHashTree(ulong nLeft, bool bLeftBranch, ulong nBaseSize)
        {
            return CreateObject(typeof(AICHHashTreeImpl), nLeft, bLeftBranch, nBaseSize) as AICHHashTree;
        }

        public AICHHashSet CreateAICHHashSet(KnownFile pOwner)
        {
            return CreateObject(typeof(AICHHashSetImpl), pOwner) as AICHHashSet;
        }

        public SafeFile OpenSafeFile(string fullpath,
            System.IO.FileMode fileMode,
            System.IO.FileAccess fileAccess,
            System.IO.FileShare fileShare)
        {
            return CreateObject(typeof(SafeFileImpl), fullpath, fileMode, fileAccess, fileShare) as SafeFile;
        }

        public SafeMemFile CreateSafeMemFile(int size)
        {
            return CreateObject(typeof(SafeMemFileImpl), size) as SafeMemFile;
        }

        public CorePreference Preference
        {
            get
            {
                return preference_;
            }
        }

        public SHA CreateSHA()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public Random Random0
        {
            get { return radom0_; }
        }

        public CoreStats CreateCoreStatistics()
        {
            return CreateObject(typeof(CoreStatsImpl)) as CoreStats;
        }

        public ProxySettings CreateProxySettings()
        {
            return CreateObject(typeof(ProxySettingsImpl)) as ProxySettings;
        }

        public ED2KServerLink CreateED2KServerLink(string ip, string port)
        {
            return CreateObject(typeof(ED2KServerLinkImpl), ip, port) as ED2KServerLink;
        }

        public ED2KNodesListLink CreateED2KNodesListLink(string address)
        {
            return CreateObject(typeof(ED2KNodesListLinkImpl), address) as ED2KNodesListLink;
        }

        public ED2KFileLink CreateED2KFileLink(string pszName,
            string pszSize,
            string pszHash,
            string[] allParams,
            string pszSources)
        {
            return CreateObject(typeof(ED2KFileLinkImpl), pszName, pszSize, pszHash, allParams, pszSources) as ED2KFileLink;
        }

        public UnresolvedHostname CreateUnresolvedHostname()
        {
            return CreateObject(typeof(UnresolvedHostnameImpl)) as UnresolvedHostname;
        }

        public ED2KServerListLink CreateED2KServerListLink(string address)
        {
            return CreateObject(typeof(ED2KServerListLinkImpl), address) as ED2KServerListLink;
        }
        #endregion

        public Tag CreateTag(params object[] parameters)
        {
            return CreateObject(typeof(TagImpl), parameters) as Tag;
        }

        public FileComments CreateFileComments(string p)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public ED2KFileTypes CreateED2KFileTypes()
        {
            return CreateObject(typeof(ED2KFileTypesImpl)) as ED2KFileTypes;
        }

        internal CoreUtilities CreateCoreUtilities()
        {
            return CreateObject(typeof(CoreUtilities)) as CoreUtilities;
        }

        public SharedFileList CreateSharedFileList()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        private object CreateObject(Type t, params object[] parameters)
        {
            object obj = t.Assembly.CreateInstance(t.FullName,
                true,
                BindingFlags.CreateInstance,
                null,
                parameters,
                null,
                null);

            if (obj is MuleBaseObject)
            {
                (obj as MuleBaseObject).MuleEngine = muleEngine_;
            }

            return obj;
        }

        internal MuleCollection CreateMuleCollection()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        internal FileComment CreateFileComment()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        internal Packet CreatePacket(SafeMemFile data, byte p)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        internal StatisticFile CreateStatisticFile()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        internal ED2KLink CreateLinkFromUrl(string strURI)
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
                if (string.Compare(strTok,"file") == 0)
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
                                    if (string.Compare(strTok,"/") == 0)
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
                else if (string.Compare(strTok,"serverlist") == 0)
                {
                    string strURL = GetNextToken(ref iPos, tokens);
                    if (!string.IsNullOrEmpty(strURL) && 
                        string.Compare(GetNextToken(ref iPos, tokens),"/") == 0)
                        return CreateED2KServerListLink(strURL);
                }
                else if (string.Compare(strTok,"server") == 0)
                {
                    string strServer = GetNextToken(ref iPos, tokens);
                    if (!string.IsNullOrEmpty(strServer))
                    {
                        string strPort = GetNextToken(ref iPos, tokens);
                        if (!string.IsNullOrEmpty(strPort) && 
                            string.Compare(GetNextToken(ref iPos, tokens),"/") == 0)
                            return CreateED2KServerLink(strServer, strPort);
                    }
                }
                else if (string.Compare(strTok,"nodeslist") == 0)
                {
                    string strURL = GetNextToken(ref iPos, tokens);
                    if (!string.IsNullOrEmpty(strURL) && 
                        string.Compare(GetNextToken(ref iPos, tokens),"/") == 0)
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

        public PartFile CreatePartFile(params object[] parameters)
        {
            return CreateObject(typeof(PartFileImpl), parameters) as PartFile;
        }

        internal SafeBufferedFile CreateSafeBufferedFile(params object[] parameters)
        {
            return CreateObject(typeof(SafeBufferedFileImpl), parameters) as SafeBufferedFile;
        }

        internal Gap CreateGap()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        internal AddFileThread CreateAddFileThread()
        {
            return CreateObject(typeof(AddFileThread), null) as AddFileThread;
        }

        internal KnownFile CreateKnownFile()
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
}
