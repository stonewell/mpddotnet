using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.File;
using System.IO;
using Mpd.Utilities;
using Mpd.Generic.Types.IO;
using Mpd.Generic.Types;
using Mule.Definitions;
using Kademlia;
using Mule.AICH;

namespace Mule.Core.Impl
{
    class MuleUploadTask : MuleFileTask
    {
        public MuleCollection MuleCollection { get; set; }
        public Kademlia.KadWordList KadWordList { get; set; }

        private UpDownClientList clientUploadList_ = new UpDownClientList();

        public KnownFile KnownFile
        {
            get { return AbstractFile as KnownFile; }
        }

        public Packet CreateSrcInfoPacket(UpDownClient forClient,
            byte byRequestedVersion, ushort nRequestedOptions)
        {
            if (clientUploadList_.Count == 0)
                return null;

            if (MPDUtilities.Md4Cmp(forClient.UploadFileID, KnownFile.FileHash) != 0)
            {
                // should never happen
                //TODO:DEBUG_ONLY( DebugLogError(_T("*** %hs - client (%s) upload file \"%s\" does not match file \"%s\""), __FUNCTION__, forClient.DbgGetClientInfo(), DbgGetFileInfo(forClient.GetUploadFileID()), FileName) );
                //ASSERT(0);
                return null;
            }

            // check whether client has either no download status at all or a 
            //download status which is valid for this file
            if (!(forClient.UpPartCount == 0 && forClient.UpPartStatus == null) &&
                !(forClient.UpPartCount == KnownFile.PartCount && forClient.UpPartStatus != null))
            {
                // should never happen
                //TODO:DEBUG_ONLY( DebugLogError(_T("*** %hs - part count (%u) of client (%s) does not match part count (%u) of file \"%s\""), __FUNCTION__, forClient.GetUpPartCount(), forClient.DbgGetClientInfo(), GetPartCount(), FileName) );
                //TODO:ASSERT(0);
                return null;
            }

            SafeMemFile data = MpdGenericObjectManager.CreateSafeMemFile(1024);

            byte byUsedVersion;
            bool bIsSX2Packet;
            if (forClient.SupportsSourceExchange2 && byRequestedVersion > 0)
            {
                // the client uses SourceExchange2 and requested the highest version he knows
                // and we send the highest version we know, but of course not higher than his request
                byUsedVersion = Math.Min(byRequestedVersion, Convert.ToByte(VersionsEnum.SOURCEEXCHANGE2_VERSION));
                bIsSX2Packet = true;
                data.WriteUInt8(byUsedVersion);

                // we don't support any special SX2 options yet, reserved for later use
                if (nRequestedOptions != 0)
                {
                    //TODO:DebugLogWarning(_T("Client requested unknown options for SourceExchange2: %u (%s)"), nRequestedOptions, forClient.DbgGetClientInfo());
                }
            }
            else
            {
                byUsedVersion = forClient.SourceExchange1Version;
                bIsSX2Packet = false;
                if (forClient.SupportsSourceExchange2)
                {
                    //TODO:DebugLogWarning(_T("Client which announced to support SX2 sent SX1 packet instead (%s)"), forClient.DbgGetClientInfo());
                }
            }

            ushort nCount = 0;
            data.WriteHash16(forClient.UploadFileID);
            data.WriteUInt16(nCount);
            uint cDbgNoSrc = 0;
            foreach (UpDownClient cur_src in clientUploadList_)
            {
                if (cur_src.HasLowID || cur_src == forClient ||
                    !(cur_src.UploadState == UploadStateEnum.US_UPLOADING ||
                    cur_src.UploadState == UploadStateEnum.US_ONUPLOADQUEUE))
                    continue;
                if (!cur_src.IsEd2kClient)
                    continue;

                bool bNeeded = false;
                byte[] rcvstatus = forClient.UpPartStatus;
                if (rcvstatus != null)
                {
                    byte[] srcstatus = cur_src.UpPartStatus;
                    if (srcstatus != null)
                    {
                        if (cur_src.UpPartCount == forClient.UpPartCount)
                        {
                            for (ushort x = 0; x < KnownFile.PartCount; x++)
                            {
                                if (srcstatus[x] != 0 && rcvstatus[x] == 0)
                                {
                                    // We know the recieving client needs a chunk from this client.
                                    bNeeded = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // should never happen
                            //if (thePrefs.GetVerbose())
                            //	DEBUG_ONLY( DebugLogError(_T("*** %hs - found source (%s) with wrong part count (%u) attached to file \"%s\" (partcount=%u)"), __FUNCTION__, cur_src.DbgGetClientInfo(), cur_src.GetUpPartCount(), FileName, GetPartCount()));
                        }
                    }
                    else
                    {
                        cDbgNoSrc++;
                        // This client doesn't support upload chunk status. So just send it and hope for the best.
                        bNeeded = true;
                    }
                }
                else
                {
                    //ASSERT( forClient.GetUpPartCount() == 0 );
                    //TODO:TRACE(_T("%hs, requesting client has no chunk status - %s"), __FUNCTION__, forClient.DbgGetClientInfo());
                    // remote client does not support upload chunk status, search sources which have at least one complete part
                    // we could even sort the list of sources by available chunks to return as much sources as possible which
                    // have the most available chunks. but this could be a noticeable performance problem.
                    byte[] srcstatus = cur_src.UpPartStatus;
                    if (srcstatus != null)
                    {
                        //ASSERT( cur_src.GetUpPartCount() == GetPartCount() );
                        for (ushort x = 0; x < KnownFile.PartCount; x++)
                        {
                            if (srcstatus[x] != 0)
                            {
                                // this client has at least one chunk
                                bNeeded = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // This client doesn't support upload chunk status. So just send it and hope for the best.
                        bNeeded = true;
                    }
                }

                if (bNeeded)
                {
                    nCount++;
                    uint dwID;
                    if (byUsedVersion >= 3)
                        dwID = cur_src.UserIDHybrid;
                    else
                        dwID = cur_src.IP;
                    data.WriteUInt32(dwID);
                    data.WriteUInt16(cur_src.UserPort);
                    data.WriteUInt32(cur_src.ServerIP);
                    data.WriteUInt16(cur_src.ServerPort);
                    if (byUsedVersion >= 2)
                        data.WriteHash16(cur_src.UserHash);
                    if (byUsedVersion >= 4)
                    {
                        // ConnectSettings - SourceExchange V4
                        // 4 Reserved (!)
                        // 1 DirectCallback Supported/Available 
                        // 1 CryptLayer Required
                        // 1 CryptLayer Requested
                        // 1 CryptLayer Supported
                        int uSupportsCryptLayer = cur_src.SupportsCryptLayer ? 1 : 0;
                        int uRequestsCryptLayer = cur_src.RequestsCryptLayer ? 1 : 0;
                        int uRequiresCryptLayer = cur_src.RequiresCryptLayer ? 1 : 0;
                        //const byte uDirectUDPCallback	= cur_src.SupportsDirectUDPCallback() ? 1 : 0;
                        int byCryptOptions = /*(uDirectUDPCallback << 3) |*/ (uRequiresCryptLayer << 2) | (uRequestsCryptLayer << 1) | (uSupportsCryptLayer << 0);
                        data.WriteUInt8(Convert.ToByte(byCryptOptions));
                    }
                    if (nCount > 500)
                        break;
                }
            }
            //TODO:TRACE(_T("%hs: Out of %u clients, %u had no valid chunk status\n"), __FUNCTION__, m_ClientUploadList.GetCount(), cDbgNoSrc);
            if (nCount == 0)
                return null;
            data.Seek(bIsSX2Packet ? 17 : 16, SeekOrigin.Begin);
            data.WriteUInt16(nCount);

            Packet result = MuleEngine.CoreObjectManager.CreatePacket(data, MuleConstants.OP_EMULEPROT);
            result.OperationCode = bIsSX2Packet ? OperationCodeEnum.OP_ANSWERSOURCES2 : OperationCodeEnum.OP_ANSWERSOURCES;
            // (1+)16+2+501*(4+2+4+2+16+1) = 14547 (14548) bytes max.
            if (result.Size > 354)
                result.PackPacket();
            /*TODO:Log
             * if (thePrefs.GetDebugSourceExchange())
             *   AddDebugLogLine(false, _T("SXSend: Client source response SX2=%s, Version=%u; Count=%u, %s, File=\"%s\""), bIsSX2Packet ? _T("Yes") : _T("No"), byUsedVersion, nCount, forClient.DbgGetClientInfo(), FileName);
             */
            return result;
        }

        public void AddUploadingClient(UpDownClient client)
        {
            if (!clientUploadList_.Contains(client))
            {
                clientUploadList_.Add(client);
                UpdateAutoUpPriority();
            }
        }

        public void RemoveUploadingClient(UpDownClient client)
        {
            if (clientUploadList_.Contains(client))
            {
                clientUploadList_.Remove(client);
                UpdateAutoUpPriority();
            }
        }

        public void SetKnownFileName(string pszFileName, bool bReplaceInvalidFileSystemChars, bool bRemoveControlChars)
        {
            KnownFile pFile = null;

            // If this is called within the sharedfiles object during startup,
            // we cannot reference it yet..

            if (MuleEngine.SharedFiles != null)
                pFile = MuleEngine.SharedFiles.GetFileByID(KnownFile.FileHash);

            if (pFile != null && pFile == KnownFile)
                MuleEngine.SharedFiles.RemoveKeywords(KnownFile);

            KnownFile.SetFileName(pszFileName,
                bReplaceInvalidFileSystemChars,
                true,
                bRemoveControlChars);

            KadWordList.Clear();

            if (MuleCollection != null)
            {
                string sKeyWords = string.Format("{0} {1}",
                    MuleCollection.GetCollectionAuthorKeyString(), KnownFile.FileName);
                MuleEngine.KadEngine.SearchManager.GetWords(sKeyWords, KadWordList);
            }
            else
                MuleEngine.KadEngine.SearchManager.GetWords(KnownFile.FileName, KadWordList);

            if (pFile != null && pFile == this)
                MuleEngine.SharedFiles.AddKeywords(KnownFile);
        }

        public MuleUploadTask()
        {
            MuleCollection = MuleEngine.CoreObjectManager.CreateMuleCollection();

            KadWordList = MuleEngine.KadObjectManager.CreateWordList();
        }

        public virtual void UpdatePartsInfo()
        {
            // Cache part count
            uint partcount = KnownFile.PartCount;
            bool flag = (MPDUtilities.Time() - KnownFile.CompleteSourcesTime) > 0;

            // Reset part counters
            ushort[] availPartFrequency = new ushort[KnownFile.AvailPartFrequency.Length];

            if (availPartFrequency.Length < partcount)
                Array.Resize<ushort>(ref availPartFrequency, Convert.ToInt32(partcount));

            for (uint i = 0; i < partcount; i++)
                availPartFrequency[i] = 0;

            List<ushort> count = new List<ushort>();

            if (flag)
            {
                count.Capacity = clientUploadList_.Count;
            }
            foreach (UpDownClient cur_src in clientUploadList_)
            {
                //This could be a partfile that just completed.. Many of these clients will not have this information.
                if (cur_src.UpPartStatus != null && cur_src.UpPartCount == partcount)
                {
                    for (uint i = 0; i < partcount; i++)
                    {
                        if (cur_src.IsUpPartAvailable(i))
                            availPartFrequency[i] += 1;
                    }

                    if (flag)
                        count.Add(cur_src.UpCompleteSourcesCount);
                }
            }

            KnownFile.AvailPartFrequency = availPartFrequency;

            if (flag)
            {
                KnownFile.CompleteSourcesCount =
                    KnownFile.CompleteSourcesCountLo = KnownFile.CompleteSourcesCountHi = 0;

                if (partcount > 0)
                    KnownFile.CompleteSourcesCount = availPartFrequency[0];
                for (uint i = 1; i < partcount; i++)
                {
                    if (KnownFile.CompleteSourcesCount > availPartFrequency[i])
                        KnownFile.CompleteSourcesCount = availPartFrequency[i];
                }

                // plus 1 since we have the file complete too
                count.Add(Convert.ToUInt16(KnownFile.CompleteSourcesCount + 1));

                int n = count.Count;
                if (n > 0)
                {
                    // SLUGFILLER: heapsortCompletesrc
                    int r;
                    for (r = n / 2; r-- > 0; )
                        MPDUtilities.HeapSort(ref count, r, n - 1);
                    for (r = n; --r > 0; )
                    {
                        ushort t = count[r];
                        count[r] = count[0];
                        count[0] = t;
                        MPDUtilities.HeapSort(ref count, 0, r - 1);
                    }
                    // SLUGFILLER: heapsortCompletesrc

                    // calculate range
                    int i = n >> 1;			// (n / 2)
                    int j = (n * 3) >> 2;	// (n * 3) / 4
                    int k = (n * 7) >> 3;	// (n * 7) / 8

                    //For complete files, trust the people your uploading to more...

                    //For low guess and normal guess count
                    //	If we see more sources then the guessed low and normal, use what we see.
                    //	If we see less sources then the guessed low, adjust network accounts for 100%, we account for 0% with what we see and make sure we are still above the normal.
                    //For high guess
                    //  Adjust 100% network and 0% what we see.
                    if (n < 20)
                    {
                        if (count[i] < KnownFile.CompleteSourcesCount)
                            KnownFile.CompleteSourcesCountLo = KnownFile.CompleteSourcesCount;
                        else
                            KnownFile.CompleteSourcesCountLo = count[i];
                        KnownFile.CompleteSourcesCount = KnownFile.CompleteSourcesCountLo;
                        KnownFile.CompleteSourcesCountHi = count[j];
                        if (KnownFile.CompleteSourcesCountHi < KnownFile.CompleteSourcesCount)
                            KnownFile.CompleteSourcesCountHi = KnownFile.CompleteSourcesCount;
                    }
                    else
                    {
                        //Many sources..
                        //For low guess
                        //	Use what we see.
                        //For normal guess
                        //	Adjust network accounts for 100%, we account for 0% with what we see and make sure we are still above the low.
                        //For high guess
                        //  Adjust network accounts for 100%, we account for 0% with what we see and make sure we are still above the normal.
                        KnownFile.CompleteSourcesCountLo = KnownFile.CompleteSourcesCount;
                        KnownFile.CompleteSourcesCount = count[j];
                        if (KnownFile.CompleteSourcesCount < KnownFile.CompleteSourcesCountLo)
                            KnownFile.CompleteSourcesCount = KnownFile.CompleteSourcesCountLo;
                        KnownFile.CompleteSourcesCountHi = count[k];
                        if (KnownFile.CompleteSourcesCountHi < KnownFile.CompleteSourcesCount)
                            KnownFile.CompleteSourcesCountHi = KnownFile.CompleteSourcesCount;
                    }
                }
                KnownFile.CompleteSourcesTime = MPDUtilities.Time() + 60;
            }
        }

        public int QueuedCount
        {
            get { return clientUploadList_.Count; }
        }

        public void UpdateAutoUpPriority()
        {
            if (!KnownFile.IsAutoUpPriority)
                return;

            if (QueuedCount > 20)
            {
                if (KnownFile.UpPriority != Convert.ToByte(PriorityEnum.PR_LOW))
                {
                    KnownFile.UpPriority = Convert.ToByte(PriorityEnum.PR_LOW);
                }
                return;
            }
            if (QueuedCount > 1)
            {
                if (KnownFile.UpPriority != Convert.ToByte(PriorityEnum.PR_NORMAL))
                {
                    KnownFile.UpPriority = Convert.ToByte(PriorityEnum.PR_NORMAL);
                }
                return;
            }

            if (KnownFile.UpPriority != Convert.ToByte(PriorityEnum.PR_HIGH))
            {
                KnownFile.UpPriority = Convert.ToByte(PriorityEnum.PR_HIGH);
            }
        }

        public bool PublishSrc()
        {
            uint lastBuddyIP = 0;
            if (MuleEngine.IsFirewalled &&
                (MuleEngine.KadEngine.UDPFirewallTester.IsFirewalledUDP(true) ||
                !MuleEngine.KadEngine.UDPFirewallTester.IsVerified))
            {
                UpDownClient buddy = MuleEngine.ClientList.Buddy;
                if (buddy != null)
                {
                    lastBuddyIP = MuleEngine.ClientList.Buddy.IP;

                    if (lastBuddyIP != KnownFile.LastPublishBuddy)
                    {
                        KnownFile.LastPublishTimeKadSrc =
                            MPDUtilities.Time() + MuleConstants.KADEMLIAREPUBLISHTIMES;
                        KnownFile.LastPublishBuddy = lastBuddyIP;
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (KnownFile.LastPublishTimeKadSrc > MPDUtilities.Time())
                return false;

            KnownFile.LastPublishTimeKadSrc =
                MPDUtilities.Time() + MuleConstants.KADEMLIAREPUBLISHTIMES;
            KnownFile.LastPublishBuddy = lastBuddyIP;
            return true;
        }

        public bool PublishNotes()
        {
            if (KnownFile.LastPublishTimeKadNotes > MPDUtilities.Time())
            {
                return false;
            }

            if (!string.IsNullOrEmpty(KnownFile.FileComment))
            {
                KnownFile.LastPublishTimeKadNotes = MPDUtilities.Time() + MuleConstants.KADEMLIAREPUBLISHTIMEN;
                return true;
            }

            if (KnownFile.FileRating != 0)
            {
                KnownFile.LastPublishTimeKadNotes = MPDUtilities.Time() + MuleConstants.KADEMLIAREPUBLISHTIMEN;
                return true;
            }

            return false;
        }

        public bool CreateAICHHashSetOnly()
        {
            KnownFile.AICHHashSet.FreeHashSet();

            try
            {
                using (Stream file = new FileStream(KnownFile.FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    // create aichhashset
                    ulong togo = KnownFile.FileSize;
                    uint hashcount;
                    for (hashcount = 0; togo >= MuleConstants.PARTSIZE; )
                    {
                        AICHHashTree pBlockAICHHashTree =
                            KnownFile.AICHHashSet.HashTree.FindHash((ulong)hashcount * MuleConstants.PARTSIZE, MuleConstants.PARTSIZE);

                        if (!KnownFile.CreateHash(file, MuleConstants.PARTSIZE, null, pBlockAICHHashTree))
                        {
                            //TODO:LogError(_T("Failed to hash file \"%s\" - %s"), GetFilePath(), _tcserror(errno));
                            return false;
                        }

                        togo -= MuleConstants.PARTSIZE;
                        hashcount++;
                    }

                    if (togo != 0)
                    {
                        AICHHashTree pBlockAICHHashTree =
                            KnownFile.AICHHashSet.HashTree.FindHash((ulong)hashcount * MuleConstants.PARTSIZE, togo);

                        if (!KnownFile.CreateHash(file, togo, null, pBlockAICHHashTree))
                        {
                            //TODO:LogError(_T("Failed to hash file \"%s\" - %s"), GetFilePath(), _tcserror(errno));
                            return false;
                        }
                    }

                    KnownFile.AICHHashSet.ReCalculateHash(false);
                    if (KnownFile.AICHHashSet.VerifyHashTree(true))
                    {
                        KnownFile.AICHHashSet.Status = AICHStatusEnum.AICH_HASHSETCOMPLETE;
                        if (!SaveHashSet())
                        {
                            //TODO:LogError(LOG_STATUSBAR, GetResString(IDS_SAVEACFAILED));
                        }
                    }
                    else
                    {
                        // now something went pretty wrong
                        //TODO:DebugLogError(LOG_STATUSBAR, _T("Failed to calculate AICH Hashset from file %s"), FileName);
                    }

                }

                return true;
            }
            catch (Exception)
            {
                //TODO:Log
                return false;
            }
        }

        public bool CreateFromFile(string directory, string filename)
        {
            KnownFile.FileDirectory = directory;
            KnownFile.FileName = filename;

            // open file
            string strFilePath = System.IO.Path.Combine(directory, filename);
            KnownFile.FilePath = strFilePath;

            try
            {
                using (System.IO.FileStream fs = new System.IO.FileStream(strFilePath, FileMode.Open, FileAccess.Read))
                {
                    if ((ulong)fs.Length > MuleConstants.MAX_EMULE_FILE_SIZE)
                    {
                        return false;
                    }

                    KnownFile.FileSize = Convert.ToUInt64(fs.Length);

                    KnownFile.AvailPartFrequency = new ushort[KnownFile.PartCount];

                    for (uint i = 0; i < KnownFile.PartCount; i++)
                        KnownFile.AvailPartFrequency[i] = 0;

                    // create hashset
                    ulong togo = KnownFile.FileSize;
                    uint hashcount;
                    AICHHashTree pBlockAICHHashTree = null;
                    for (hashcount = 0; togo >= MuleConstants.PARTSIZE; )
                    {
                        pBlockAICHHashTree =
                            KnownFile.AICHHashSet.HashTree.FindHash((ulong)hashcount * MuleConstants.PARTSIZE, MuleConstants.PARTSIZE);

                        byte[] newhash = new byte[16];

                        try
                        {
                            KnownFile.CreateHash(fs, MuleConstants.PARTSIZE, newhash, pBlockAICHHashTree);
                        }
                        catch
                        {
                            //TODO:LogError(_T("Failed to hash file \"%s\" - %s"), strFilePath, _tcserror(errno));
                            return false;
                        }

                        KnownFile.Hashset.Add(newhash);
                        togo -= MuleConstants.PARTSIZE;
                        hashcount++;
                    }

                    if (togo == 0)
                    {
                        // sha hashtree doesnt takes hash of 0-sized data
                        pBlockAICHHashTree = null;
                    }
                    else
                    {
                        pBlockAICHHashTree = KnownFile.AICHHashSet.HashTree.FindHash((ulong)hashcount * MuleConstants.PARTSIZE, togo);
                    }

                    byte[] lasthash = new byte[16];
                    MPDUtilities.Md4Clr(lasthash);
                    try
                    {
                        KnownFile.CreateHash(fs, togo, lasthash, pBlockAICHHashTree);
                    }
                    catch
                    {
                        //TODO:LogError(_T("Failed to hash file \"%s\" - %s"), strFilePath, _tcserror(errno));
                        return false;
                    }

                    KnownFile.AICHHashSet.ReCalculateHash(false);
                    if (KnownFile.AICHHashSet.VerifyHashTree(true))
                    {
                        KnownFile.AICHHashSet.Status = AICHStatusEnum.AICH_HASHSETCOMPLETE;
                        if (!SaveHashSet())
                        {
                            //TODO:LogError(LOG_STATUSBAR, GetResString(IDS_SAVEACFAILED));
                        }
                    }
                    else
                    {
                        // now something went pretty wrong
                        //TODO:DebugLogError(LOG_STATUSBAR, _T("Failed to calculate AICH Hashset from file %s"), FileName);
                    }

                    if (hashcount == 0)
                    {
                        MPDUtilities.Md4Cpy(KnownFile.FileHash, lasthash);
                    }
                    else
                    {
                        KnownFile.Hashset.Add(lasthash);
                        byte[] buffer = new byte[KnownFile.Hashset.Count * 16];
                        for (int i = 0; i < KnownFile.Hashset.Count; i++)
                            MPDUtilities.Md4Cpy(buffer, i * 16, KnownFile.Hashset[i], 0, KnownFile.Hashset[i].Length);
                        KnownFile.CreateHash(buffer, Convert.ToUInt64(buffer.Length), KnownFile.FileHash);
                    }

                    KnownFile.UtcLastModified = MPDUtilities.DateTime2UInt32Time(System.IO.File.GetLastWriteTimeUtc(KnownFile.FilePath));
                }
            }
            catch (FileNotFoundException/* ex*/)
            {
                //TODO:Log
                return false;
            }

            // Add filetags
            KnownFile.UpdateMetaDataTags();

            UpdatePartsInfo();

            return true;
        }

        protected bool SaveHashSet()
        {
            if (KnownFile.AICHHashSet.Status != AICHStatusEnum.AICH_HASHSETCOMPLETE)
            {
                return false;
            }

            if (!KnownFile.AICHHashSet.HashTree.HashValid ||
                KnownFile.AICHHashSet.HashTree.DataSize != KnownFile.FileSize)
            {
                return false;
            }

            //if (!KnownFile.AICHHashSet.AICHHashSetStatics..WaitOne(5000, true))
            //    return false;

            string fullpath =
                MuleEngine.CoreObjectManager.Preference.GetMuleDirectory(DefaultDirectoryEnum.EMULE_CONFIGDIR);
            fullpath += MuleConstants.KNOWN2_MET_FILENAME;

            SafeFile file =
                MpdGenericObjectManager.OpenSafeFile(fullpath,
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

            if (file == null)
            {
                //TODO: log here
                return false;
            }
            try
            {
                //setvbuf(file.Stream, NULL, _IOFBF, 16384);
                byte header = file.ReadUInt8();
                if (header != MuleConstants.KNOWN2_MET_VERSION)
                {
                    throw new ApplicationException("end of file:" + fullpath);
                }
                // first we check if the hashset we want to write is already stored
                AICHHash CurrentHash = AICHObjectManager.CreateAICHHash();
                uint nExistingSize = (uint)file.Length;
                uint nHashCount;
                while (file.Position < nExistingSize)
                {
                    CurrentHash.Read(file);
                    if (KnownFile.AICHHashSet.HashTree.Hash.Equals(CurrentHash))
                    {
                        // this hashset if already available, no need to save it again
                        return true;
                    }
                    nHashCount = file.ReadUInt32();
                    if (file.Position + nHashCount * MuleConstants.HASHSIZE > nExistingSize)
                    {
                        throw new ApplicationException("end of file:" + fullpath);
                    }
                    // skip the rest of this hashset
                    file.Seek(nHashCount * MuleConstants.HASHSIZE, SeekOrigin.Current);
                }
                // write hashset
                KnownFile.AICHHashSet.HashTree.Hash.Write(file);
                //use to remove the warning;
                ulong tmp_part_size =
                    (MuleConstants.PARTSIZE);
                nHashCount =
                    (uint)((MuleConstants.PARTSIZE / MuleConstants.EMBLOCKSIZE +
                        ((tmp_part_size % MuleConstants.EMBLOCKSIZE != 0) ? (ulong)1 : (ulong)0)) *
                        (KnownFile.AICHHashSet.HashTree.DataSize / MuleConstants.PARTSIZE));

                if (KnownFile.AICHHashSet.HashTree.DataSize % MuleConstants.PARTSIZE != 0)
                    nHashCount += (uint)(((ulong)KnownFile.AICHHashSet.HashTree.DataSize % MuleConstants.PARTSIZE) / MuleConstants.EMBLOCKSIZE +
                        (((KnownFile.AICHHashSet.HashTree.DataSize % MuleConstants.PARTSIZE) % MuleConstants.EMBLOCKSIZE != 0) ? (ulong)1 : (ulong)0));
                file.WriteUInt32(nHashCount);
                if (!KnownFile.AICHHashSet.HashTree.WriteLowestLevelHashs(file, 0, true, true))
                {
                    // thats bad... really
                    file.SetLength(nExistingSize);
                    //TODO:Log
                    //theApp.QueueDebugLogLine(true, _T("Failed to save HashSet: WriteLowestLevelHashs() failed!"));
                    return false;
                }
                if (file.Length != nExistingSize + (nHashCount + 1) * MuleConstants.HASHSIZE + 4)
                {
                    // thats even worse
                    file.SetLength(nExistingSize);
                    //TODO:Log
                    //theApp.QueueDebugLogLine(true, _T("Failed to save HashSet: Calculated and real size of hashset differ!"));
                    return false;
                }
                //TODO:Log
                //theApp.QueueDebugLogLine(false, _T("Successfully saved eMuleAC Hashset, %u Hashs + 1 Masterhash written"), nHashCount);
                file.Flush();
                file.Close();
            }
            catch
            {
                //TODO:Log
                return false;
            }

            KnownFile.AICHHashSet.FreeHashSet();
            return true;
        }
    }
}
