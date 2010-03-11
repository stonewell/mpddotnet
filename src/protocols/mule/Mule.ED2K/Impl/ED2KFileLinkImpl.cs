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
using Mule.AICH;
using System.Net;
using Mule.Definitions;
using Mpd.Utilities;
using Mpd.Generic.Types.IO;
using Mpd.Generic.Types;

namespace Mule.ED2K.Impl
{
    class ED2KFileLinkImpl : ED2KLinkImpl, ED2KFileLink
    {
        #region Fields
        private string name_ = null;
        private string strSize_ = null;
        private ulong size_ = 0;
        private byte[] hash_ = new byte[16];
        private bool aichHashValid_ = false;
        private AICHHash aichHash_ = null;

        private UnresolvedHostnameList hostnameSourcesList_ =
            new UnresolvedHostnameList();

        #endregion

        #region Constructors
        public ED2KFileLinkImpl(string pszName,
            string pszSize,
            string pszHash,
            string[] allParams,
            string pszSources)
        {
            try
            {
                //name_ = Encoding.UTF8.GetString(HttpUtility.UrlDecodeToBytes(pszName));
                name_ = Uri.UnescapeDataString(pszName);
                name_ = name_.Trim();

                if (string.IsNullOrEmpty(name_))
                    throw new MuleException("Invalid File Link:" + pszName);
            }
            catch (Exception ex)
            {
                throw new MuleException("Invalid File Link:" + pszName, ex);
            }

            SourcesList = null;
            HashSet = null;
            aichHashValid_ = false;

            strSize_ = pszSize;
            if (ulong.TryParse(pszSize, out size_))
            {
                if (size_ > MuleConstants.MAX_EMULE_FILE_SIZE)
                {
                    throw new MuleException("Too Large File:" + pszSize);
                }
            }
            else
            {
                throw new MuleException("Invalid File Size:" + pszSize);
            }

            if (pszHash.Length != 32)
                throw new MuleException("Invalid File Hash Format:" + pszHash);

            if (!MPDUtilities.DecodeHexString(pszHash, hash_))
            {
                throw new MuleException("Invalid File Hash Format:" + pszHash);
            }

            HandleParams(allParams);

            HandleSources(pszSources);
        }
        #endregion

        #region ED2KFileLink Members

        public string Name
        {
            get { return name_; }
        }

        public byte[] HashKey
        {
            get { return hash_; }
        }

        public Mule.AICH.AICHHash AICHHash
        {
            get { return aichHash_; }
        }

        public ulong Size
        {
            get { return size_; }
        }

        public bool HasValidSources
        {
            get { return SourcesList != null; }
        }

        public bool HasHostnameSources
        {
            get { return hostnameSourcesList_.Count != 0; }
        }

        public bool HasValidAICHHash
        {
            get { return aichHashValid_; }
        }

        #endregion

        #region Overrides
        public override string Link
        {
            get
            {
                return string.Format("ed2k://|file|{0}|{1}|{2}|/",
                    Uri.EscapeDataString(name_),
                    strSize_,
                    MPDUtilities.EncodeHexString(hash_));
            }
        }
        #endregion

        #region Methods
        private void HandleSources(string pszSources)
        {
            if (string.IsNullOrEmpty(pszSources))
                return;

            bool bAllowSources;
            int nYear, nMonth, nDay;

            ushort nCount = 0;
            uint dwID;
            ushort nPort;
            uint dwServerIP = 0;
            ushort nServerPort = 0;

            int nInvalid = 0;

            int pCh = pszSources.IndexOf("sources");

            if (pCh < 0)
                return;

            pCh = pCh + 7; // point to char after "sources"
            int pEnd = pszSources.Length;
            bAllowSources = true;

            // if there's an expiration date...
            if (pszSources[pCh] == '@' && (pEnd - pCh) > 7)
            {
                // after '@'
                pCh++;

                int.TryParse(pszSources.Substring(pCh, 2), out nYear);
                nYear += 2000;
                pCh += 2;

                int.TryParse(pszSources.Substring(pCh, 2), out nMonth);
                pCh += 2;

                int.TryParse(pszSources.Substring(pCh, 2), out nDay);
                pCh += 2;

                DateTime expirationDate = DateTime.Now;

                try
                {
                    expirationDate = new DateTime(nYear, nMonth, nDay);
                }
                catch
                {
                }

                bAllowSources = expirationDate != null;

                if (bAllowSources)
                    bAllowSources = (DateTime.Today < expirationDate);
            }

            // increment pCh to point to the first "ip:port" and check for sources
            if (bAllowSources && ++pCh < pEnd)
            {
                SourcesList = MpdGenericObjectManager.CreateSafeMemFile(256);
                // init to 0, we'll fix this at the end.
                SourcesList.WriteUInt16(nCount);
                // for each "ip:port" source string until the end
                // limit to prevent overflow (ushort due to CPartFile::AddClientSources)
                while (pCh < pEnd && nCount < ushort.MaxValue)
                {
                    string strIP = string.Empty;
                    int pIP = pCh;

                    // find the end of this ip:port string & start of next ip:port string.
                    if ((pCh = pszSources.IndexOf(',', pCh)) >= 0)
                    {
                        strIP = pszSources.Substring(pIP, pCh - pIP);

                        pCh++; // point to next "ip:port"
                    }
                    else
                    {
                        pCh = pEnd;
                    }

                    // if port is not present for this ip, go to the next ip.
                    int pPort = -1;

                    if ((pPort = strIP.IndexOf(':')) < 0)
                    {
                        nInvalid++;
                        continue;
                    }

                    string strPort = strIP.Substring(pPort + 1);	// terminate ip string
                    strIP = strIP.Substring(0, pPort);

                    if (!ushort.TryParse(strPort, out nPort))
                    {
                        nInvalid++;
                        continue;
                    }

                    // skip bad ips / ports
                    if (nPort > 0xFFFF || nPort == 0)	// port
                    {
                        nInvalid++;
                        continue;
                    }

                    IPAddress address = null;

                    if (!IPAddress.TryParse(strIP, out address))
                    {
                        // hostname?
                        if (strIP.Length > 512)
                        {
                            nInvalid++;
                            continue;
                        }

                        UnresolvedHostname hostname =
                            ED2KObjectManager.CreateUnresolvedHostname();

                        hostname.Port = nPort;
                        hostname.HostName = strIP;

                        hostnameSourcesList_.Add(hostname);
                        continue;
                    }

                    //TODO: This will filter out *.*.*.0 clients. Is there a nice way to fix?
                    dwID = BitConverter.ToUInt32(address.GetAddressBytes(),0);

                    if (MuleUtilities.IsLowID(dwID))	// ip
                    {
                        nInvalid++;
                        continue;
                    }

                    SourcesList.WriteUInt32(dwID);
                    SourcesList.WriteUInt16(nPort);
                    SourcesList.WriteUInt32(dwServerIP);
                    SourcesList.WriteUInt16(nServerPort);
                    nCount++;
                }

                SourcesList.SeekToBegin();
                SourcesList.WriteUInt16(nCount);
                SourcesList.SeekToBegin();

                if (nCount == 0)
                {
                    SourcesList = null;
                }
            }
        }

        private void HandleParams(string[] allParams)
        {
            bool bError = false;
            for (int i = 0; !bError && i < allParams.Length; i++)
            {
                string strParam = allParams[i];

                string[] tokens = strParam.Split('=');

                if (tokens == null || tokens.Length < 2)
                {
                    //TODO:Log
                    continue;
                }

                string strTok = tokens[0];
                if (strTok.Equals("s"))
                {
                    string strURL = strParam.Substring(strTok.Length + 1);

                    if (!string.IsNullOrEmpty(strURL))
                    {
                        if (Uri.IsWellFormedUriString(strURL, UriKind.RelativeOrAbsolute))
                        {
                            Uri uri = new Uri(strURL, UriKind.RelativeOrAbsolute);

                            UnresolvedHostname hostname =
                                ED2KObjectManager.CreateUnresolvedHostname();

                            hostname.Url = strURL;
                            hostname.HostName = uri.Host;
                            hostname.Port = Convert.ToUInt16(uri.Port);
                            hostnameSourcesList_.Add(hostname);
                        }
                        else
                        {
                            //ASSERT(0);
                            //TODO:Log
                        }
                    }
                    else
                    {
                        //ASSERT(0);
                        //TODO:Log
                    }
                }
                else if (strTok.Equals("p"))
                {
                    string strPartHashs = tokens[1];

                    if (HashSet != null)
                    {
                        //ASSERT(0);
                        //TODO:Log
                        bError = true;
                        break;
                    }

                    HashSet = MpdGenericObjectManager.CreateSafeMemFile(256);
                    HashSet.WriteHash16(hash_);
                    HashSet.WriteUInt16(0);

                    int iPartHashs = 0;
                    string[] strHashs = strPartHashs.Split(':');

                    while (strHashs != null && iPartHashs < strHashs.Length)
                    {
                        if (string.IsNullOrEmpty(strHashs[iPartHashs]))
                        {
                            //TODO:Log
                            continue;
                        }

                        byte[] aucPartHash = new byte[16];
                        if (!MPDUtilities.DecodeHexString(strHashs[iPartHashs], aucPartHash))
                        {
                            bError = true;
                            break;
                        }
                        HashSet.WriteHash16(aucPartHash);
                        iPartHashs++;
                    }

                    if (bError)
                        break;

                    HashSet.Seek(16, System.IO.SeekOrigin.Begin);
                    HashSet.WriteUInt16(Convert.ToUInt16(iPartHashs));
                    HashSet.Seek(0, System.IO.SeekOrigin.Begin);
                }
                else if (strTok.Equals("h"))
                {
                    string strHash = strParam.Substring(strTok.Length + 1);
                    if (!string.IsNullOrEmpty(strHash))
                    {
                        if (MPDUtilities.DecodeBase32(strHash.ToCharArray(), aichHash_.RawHash) == MuleConstants.HASHSIZE)
                        {
                            aichHashValid_ = true;
                        }
                        else
                        {
                            //ASSERT(0);
                            //TODO:Log
                        }
                    }
                    else
                    {
                        //ASSERT(0);
                        //TODO:Log
                    }
                }
                else
                {
                    //ASSERT(0);
                    //TODO:Log
                }
            }

            if (bError)
            {
                HashSet = null;
            }
        }

        public SafeMemFile SourcesList
        {
            get;set;
        }

        public SafeMemFile HashSet
        {
            get;set;
        }

        public UnresolvedHostnameList HostnameSourcesList
        {
            get
            {
                return hostnameSourcesList_;
            }
        }

        #endregion
    }
}
