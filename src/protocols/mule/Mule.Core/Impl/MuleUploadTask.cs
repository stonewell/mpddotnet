//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Mule.File;
//using System.IO;
//using Mpd.Utilities;

//namespace Mule.Core.Impl
//{
//    class MuleUploadTask
//    {
//        public MuleCollection MuleCollection { get; set; }
//        public Kademlia.KadWordList KadWordList { get; set; }

//        private UpDownClientList clientUploadList_ = new UpDownClientList();
//        private KnownFile knownFile_ = null;

//        public Packet CreateSrcInfoPacket(UpDownClient forClient,
//            byte byRequestedVersion, ushort nRequestedOptions)
//        {
//            if (clientUploadList_.Count == 0)
//                return null;

//            if (MPDUtilities.Md4Cmp(forClient.UploadFileID, FileHash) != 0)
//            {
//                // should never happen
//                //TODO:DEBUG_ONLY( DebugLogError(_T("*** %hs - client (%s) upload file \"%s\" does not match file \"%s\""), __FUNCTION__, forClient.DbgGetClientInfo(), DbgGetFileInfo(forClient.GetUploadFileID()), FileName) );
//                //ASSERT(0);
//                return null;
//            }

//            // check whether client has either no download status at all or a 
//            //download status which is valid for this file
//            if (!(forClient.UpPartCount == 0 && forClient.UpPartStatus == null) &&
//                !(forClient.UpPartCount == PartCount && forClient.UpPartStatus != null))
//            {
//                // should never happen
//                //TODO:DEBUG_ONLY( DebugLogError(_T("*** %hs - part count (%u) of client (%s) does not match part count (%u) of file \"%s\""), __FUNCTION__, forClient.GetUpPartCount(), forClient.DbgGetClientInfo(), GetPartCount(), FileName) );
//                //TODO:ASSERT(0);
//                return null;
//            }

//            SafeMemFile data = MuleEngine.CoreObjectManager.CreateSafeMemFile(1024);

//            byte byUsedVersion;
//            bool bIsSX2Packet;
//            if (forClient.SupportsSourceExchange2 && byRequestedVersion > 0)
//            {
//                // the client uses SourceExchange2 and requested the highest version he knows
//                // and we send the highest version we know, but of course not higher than his request
//                byUsedVersion = Math.Min(byRequestedVersion, Convert.ToByte(VersionsEnum.SOURCEEXCHANGE2_VERSION));
//                bIsSX2Packet = true;
//                data.WriteUInt8(byUsedVersion);

//                // we don't support any special SX2 options yet, reserved for later use
//                if (nRequestedOptions != 0)
//                {
//                    //TODO:DebugLogWarning(_T("Client requested unknown options for SourceExchange2: %u (%s)"), nRequestedOptions, forClient.DbgGetClientInfo());
//                }
//            }
//            else
//            {
//                byUsedVersion = forClient.SourceExchange1Version;
//                bIsSX2Packet = false;
//                if (forClient.SupportsSourceExchange2)
//                {
//                    //TODO:DebugLogWarning(_T("Client which announced to support SX2 sent SX1 packet instead (%s)"), forClient.DbgGetClientInfo());
//                }
//            }

//            ushort nCount = 0;
//            data.WriteHash16(forClient.UploadFileID);
//            data.WriteUInt16(nCount);
//            uint cDbgNoSrc = 0;
//            foreach (UpDownClient cur_src in clientUploadList_)
//            {
//                if (cur_src.HasLowID || cur_src == forClient ||
//                    !(cur_src.UploadState == UploadStateEnum.US_UPLOADING ||
//                    cur_src.UploadState == UploadStateEnum.US_ONUPLOADQUEUE))
//                    continue;
//                if (!cur_src.IsEd2kClient)
//                    continue;

//                bool bNeeded = false;
//                byte[] rcvstatus = forClient.UpPartStatus;
//                if (rcvstatus != null)
//                {
//                    byte[] srcstatus = cur_src.UpPartStatus;
//                    if (srcstatus != null)
//                    {
//                        if (cur_src.UpPartCount == forClient.UpPartCount)
//                        {
//                            for (ushort x = 0; x < PartCount; x++)
//                            {
//                                if (srcstatus[x] != 0 && rcvstatus[x] == 0)
//                                {
//                                    // We know the recieving client needs a chunk from this client.
//                                    bNeeded = true;
//                                    break;
//                                }
//                            }
//                        }
//                        else
//                        {
//                            // should never happen
//                            //if (thePrefs.GetVerbose())
//                            //	DEBUG_ONLY( DebugLogError(_T("*** %hs - found source (%s) with wrong part count (%u) attached to file \"%s\" (partcount=%u)"), __FUNCTION__, cur_src.DbgGetClientInfo(), cur_src.GetUpPartCount(), FileName, GetPartCount()));
//                        }
//                    }
//                    else
//                    {
//                        cDbgNoSrc++;
//                        // This client doesn't support upload chunk status. So just send it and hope for the best.
//                        bNeeded = true;
//                    }
//                }
//                else
//                {
//                    //ASSERT( forClient.GetUpPartCount() == 0 );
//                    //TODO:TRACE(_T("%hs, requesting client has no chunk status - %s"), __FUNCTION__, forClient.DbgGetClientInfo());
//                    // remote client does not support upload chunk status, search sources which have at least one complete part
//                    // we could even sort the list of sources by available chunks to return as much sources as possible which
//                    // have the most available chunks. but this could be a noticeable performance problem.
//                    byte[] srcstatus = cur_src.UpPartStatus;
//                    if (srcstatus != null)
//                    {
//                        //ASSERT( cur_src.GetUpPartCount() == GetPartCount() );
//                        for (ushort x = 0; x < PartCount; x++)
//                        {
//                            if (srcstatus[x] != 0)
//                            {
//                                // this client has at least one chunk
//                                bNeeded = true;
//                                break;
//                            }
//                        }
//                    }
//                    else
//                    {
//                        // This client doesn't support upload chunk status. So just send it and hope for the best.
//                        bNeeded = true;
//                    }
//                }

//                if (bNeeded)
//                {
//                    nCount++;
//                    uint dwID;
//                    if (byUsedVersion >= 3)
//                        dwID = cur_src.UserIDHybrid;
//                    else
//                        dwID = cur_src.IP;
//                    data.WriteUInt32(dwID);
//                    data.WriteUInt16(cur_src.UserPort);
//                    data.WriteUInt32(cur_src.ServerIP);
//                    data.WriteUInt16(cur_src.ServerPort);
//                    if (byUsedVersion >= 2)
//                        data.WriteHash16(cur_src.UserHash);
//                    if (byUsedVersion >= 4)
//                    {
//                        // ConnectSettings - SourceExchange V4
//                        // 4 Reserved (!)
//                        // 1 DirectCallback Supported/Available 
//                        // 1 CryptLayer Required
//                        // 1 CryptLayer Requested
//                        // 1 CryptLayer Supported
//                        int uSupportsCryptLayer = cur_src.SupportsCryptLayer ? 1 : 0;
//                        int uRequestsCryptLayer = cur_src.RequestsCryptLayer ? 1 : 0;
//                        int uRequiresCryptLayer = cur_src.RequiresCryptLayer ? 1 : 0;
//                        //const byte uDirectUDPCallback	= cur_src.SupportsDirectUDPCallback() ? 1 : 0;
//                        int byCryptOptions = /*(uDirectUDPCallback << 3) |*/ (uRequiresCryptLayer << 2) | (uRequestsCryptLayer << 1) | (uSupportsCryptLayer << 0);
//                        data.WriteUInt8(Convert.ToByte(byCryptOptions));
//                    }
//                    if (nCount > 500)
//                        break;
//                }
//            }
//            //TODO:TRACE(_T("%hs: Out of %u clients, %u had no valid chunk status\n"), __FUNCTION__, m_ClientUploadList.GetCount(), cDbgNoSrc);
//            if (nCount == 0)
//                return null;
//            data.Seek(bIsSX2Packet ? 17 : 16, SeekOrigin.Begin);
//            data.WriteUInt16(nCount);

//            Packet result = MuleEngine.CoreObjectManager.CreatePacket(data, MuleConstants.OP_EMULEPROT);
//            result.OperationCode = bIsSX2Packet ? OperationCodeEnum.OP_ANSWERSOURCES2 : OperationCodeEnum.OP_ANSWERSOURCES;
//            // (1+)16+2+501*(4+2+4+2+16+1) = 14547 (14548) bytes max.
//            if (result.Size > 354)
//                result.PackPacket();
//            /*TODO:Log
//             * if (thePrefs.GetDebugSourceExchange())
//             *   AddDebugLogLine(false, _T("SXSend: Client source response SX2=%s, Version=%u; Count=%u, %s, File=\"%s\""), bIsSX2Packet ? _T("Yes") : _T("No"), byUsedVersion, nCount, forClient.DbgGetClientInfo(), FileName);
//             */
//            return result;
//        }

//        public void AddUploadingClient(UpDownClient client)
//        {
//            if (!clientUploadList_.Contains(client))
//            {
//                clientUploadList_.Add(client);
//                UpdateAutoUpPriority();
//            }
//        }

//        public void RemoveUploadingClient(UpDownClient client)
//        {
//            if (clientUploadList_.Contains(client))
//            {
//                clientUploadList_.Remove(client);
//                UpdateAutoUpPriority();
//            }
//        }

//        public void SetKnownFileName(string pszFileName, bool bReplaceInvalidFileSystemChars, bool bRemoveControlChars)
//        {
//            KnownFile pFile = null;

//            // If this is called within the sharedfiles object during startup,
//            // we cannot reference it yet..

//            if (MuleEngine.SharedFiles != null)
//                pFile = MuleEngine.SharedFiles.GetFileByID(FileHash);

//            if (pFile != null && pFile == knownFile_)
//                MuleEngine.SharedFiles.RemoveKeywords(knownFile_);

//            knownFile_.SetFileName(pszFileName,
//                bReplaceInvalidFileSystemChars,
//                true,
//                bRemoveControlChars);

//            KadWordList.Clear();

//            if (MuleCollection != null)
//            {
//                string sKeyWords = string.Format("{0} {1}",
//                    MuleCollection.GetCollectionAuthorKeyString(), FileName);
//                MuleEngine.KadEngine.SearchManager.GetWords(sKeyWords, KadWordList);
//            }
//            else
//                MuleEngine.KadEngine.SearchManager.GetWords(FileName, KadWordList);

//            if (pFile != null && pFile == this)
//                MuleEngine.SharedFiles.AddKeywords(this);
//        }

//        public MuleUploadTask()
//        {
//            MuleCollection = MuleEngine.CoreObjectManager.CreateMuleCollection();

//            KadWordList = MuleEngine.KadObjectManager.CreateWordList();
//        }

//        public void UpdateFileRatingCommentAvail()
//        {
//            UpdateFileRatingCommentAvail(false);
//        }

//        public void UpdateFileRatingCommentAvail(bool bForceUpdate)
//        {
//            HasComment = false;
//            uint uRatings = 0;
//            uint uUserRatings = 0;

//            foreach (KadEntry entry in KadNotes)
//            {
//                if (!HasComment &&
//                    !string.IsNullOrEmpty(entry.GetStrTagValue(MuleConstants.TAG_DESCRIPTION)))
//                    HasComment = true;
//                uint rating = (uint)entry.GetIntTagValue(MuleConstants.TAG_FILERATING);
//                if (rating != 0)
//                {
//                    uRatings++;
//                    uUserRatings += rating;
//                }
//            }

//            if (uRatings != 0)
//                UserRating = uUserRatings / uRatings;
//            else
//                UserRating = 0;
//        }
//        public virtual void UpdatePartsInfo()
//        {
//            // Cache part count
//            uint partcount = PartCount;
//            bool flag = (MPDUtilities.Time() - nCompleteSourcesTime_) > 0;

//            // Reset part counters
//            if (availPartFrequency_.Length < partcount)
//                Array.Resize<ushort>(ref availPartFrequency_, Convert.ToInt32(partcount));

//            for (uint i = 0; i < partcount; i++)
//                availPartFrequency_[i] = 0;

//            List<ushort> count = new List<ushort>();
//            if (flag)
//            {
//                count.Capacity = clientUploadList_.Count;
//            }
//            foreach (UpDownClient cur_src in clientUploadList_)
//            {
//                //This could be a partfile that just completed.. Many of these clients will not have this information.
//                if (cur_src.UpPartStatus != null && cur_src.UpPartCount == partcount)
//                {
//                    for (uint i = 0; i < partcount; i++)
//                    {
//                        if (cur_src.IsUpPartAvailable(i))
//                            availPartFrequency_[i] += 1;
//                    }
//                    if (flag)
//                        count.Add(cur_src.UpCompleteSourcesCount);
//                }
//            }

//            if (flag)
//            {
//                nCompleteSourcesCount_ = nCompleteSourcesCountLo_ = nCompleteSourcesCountHi_ = 0;

//                if (partcount > 0)
//                    nCompleteSourcesCount_ = availPartFrequency_[0];
//                for (uint i = 1; i < partcount; i++)
//                {
//                    if (nCompleteSourcesCount_ > availPartFrequency_[i])
//                        nCompleteSourcesCount_ = availPartFrequency_[i];
//                }

//                // plus 1 since we have the file complete too
//                count.Add(Convert.ToUInt16(nCompleteSourcesCount_ + 1));

//                int n = count.Count;
//                if (n > 0)
//                {
//                    // SLUGFILLER: heapsortCompletesrc
//                    int r;
//                    for (r = n / 2; r-- > 0; )
//                        HeapSort(ref count, r, n - 1);
//                    for (r = n; --r > 0; )
//                    {
//                        ushort t = count[r];
//                        count[r] = count[0];
//                        count[0] = t;
//                        HeapSort(ref count, 0, r - 1);
//                    }
//                    // SLUGFILLER: heapsortCompletesrc

//                    // calculate range
//                    int i = n >> 1;			// (n / 2)
//                    int j = (n * 3) >> 2;	// (n * 3) / 4
//                    int k = (n * 7) >> 3;	// (n * 7) / 8

//                    //For complete files, trust the people your uploading to more...

//                    //For low guess and normal guess count
//                    //	If we see more sources then the guessed low and normal, use what we see.
//                    //	If we see less sources then the guessed low, adjust network accounts for 100%, we account for 0% with what we see and make sure we are still above the normal.
//                    //For high guess
//                    //  Adjust 100% network and 0% what we see.
//                    if (n < 20)
//                    {
//                        if (count[i] < nCompleteSourcesCount_)
//                            nCompleteSourcesCountLo_ = nCompleteSourcesCount_;
//                        else
//                            nCompleteSourcesCountLo_ = count[i];
//                        nCompleteSourcesCount_ = nCompleteSourcesCountLo_;
//                        nCompleteSourcesCountHi_ = count[j];
//                        if (nCompleteSourcesCountHi_ < nCompleteSourcesCount_)
//                            nCompleteSourcesCountHi_ = nCompleteSourcesCount_;
//                    }
//                    else
//                    {
//                        //Many sources..
//                        //For low guess
//                        //	Use what we see.
//                        //For normal guess
//                        //	Adjust network accounts for 100%, we account for 0% with what we see and make sure we are still above the low.
//                        //For high guess
//                        //  Adjust network accounts for 100%, we account for 0% with what we see and make sure we are still above the normal.
//                        nCompleteSourcesCountLo_ = nCompleteSourcesCount_;
//                        nCompleteSourcesCount_ = count[j];
//                        if (nCompleteSourcesCount_ < nCompleteSourcesCountLo_)
//                            nCompleteSourcesCount_ = nCompleteSourcesCountLo_;
//                        nCompleteSourcesCountHi_ = count[k];
//                        if (nCompleteSourcesCountHi_ < nCompleteSourcesCount_)
//                            nCompleteSourcesCountHi_ = nCompleteSourcesCount_;
//                    }
//                }
//                nCompleteSourcesTime_ = MPDUtilities.Time() + 60;
//            }
//        }
//        public void UpdateAutoUpPriority()
//        {
//            if (!IsAutoUpPriority)
//                return;

//            if (QueuedCount > 20)
//            {
//                if (UpPriority != Convert.ToByte(PriorityEnum.PR_LOW))
//                {
//                    UpPriority = Convert.ToByte(PriorityEnum.PR_LOW);
//                }
//                return;
//            }
//            if (QueuedCount > 1)
//            {
//                if (UpPriority != Convert.ToByte(PriorityEnum.PR_NORMAL))
//                {
//                    UpPriority = Convert.ToByte(PriorityEnum.PR_NORMAL);
//                }
//                return;
//            }

//            if (UpPriority != Convert.ToByte(PriorityEnum.PR_HIGH))
//            {
//                UpPriority = Convert.ToByte(PriorityEnum.PR_HIGH);
//            }
//        }
//        public bool PublishSrc()
//        {
//            uint lastBuddyIP = 0;
//            if (MuleEngine.IsFirewalled &&
//                (MuleEngine.KadEngine.UDPFirewallTester.IsFirewalledUDP(true) ||
//                !MuleEngine.KadEngine.UDPFirewallTester.IsVerified))
//            {
//                UpDownClient buddy = MuleEngine.ClientList.Buddy;
//                if (buddy != null)
//                {
//                    lastBuddyIP = MuleEngine.ClientList.Buddy.IP;

//                    if (lastBuddyIP != lastBuddyIP_)
//                    {
//                        lastPublishTimeKadSrc_ = MPDUtilities.Time() + MuleConstants.KADEMLIAREPUBLISHTIMES;
//                        lastBuddyIP_ = lastBuddyIP;
//                        return true;
//                    }
//                }
//                else
//                {
//                    return false;
//                }
//            }

//            if (lastPublishTimeKadSrc_ > MPDUtilities.Time())
//                return false;

//            lastPublishTimeKadSrc_ = MPDUtilities.Time() + MuleConstants.KADEMLIAREPUBLISHTIMES;
//            lastBuddyIP_ = lastBuddyIP;
//            return true;
//        }

//        public bool PublishNotes()
//        {
//            if (lastPublishTimeKadNotes_ > MPDUtilities.Time())
//            {
//                return false;
//            }

//            if (!string.IsNullOrEmpty(FileComment))
//            {
//                lastPublishTimeKadNotes_ = MPDUtilities.Time() + MuleConstants.KADEMLIAREPUBLISHTIMEN;
//                return true;
//            }

//            if (FileRating != 0)
//            {
//                lastPublishTimeKadNotes_ = MPDUtilities.Time() + MuleConstants.KADEMLIAREPUBLISHTIMEN;
//                return true;
//            }

//            return false;
//        }

//        public bool CreateAICHHashSetOnly()
//        {
//            knownFile_.AICHHashSet.FreeHashSet();

//            try
//            {
//                using (Stream file = new FileStream(knownFile_.FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
//                {
//                    // create aichhashset
//                    ulong togo = FileSize;
//                    uint hashcount;
//                    for (hashcount = 0; togo >= MuleConstants.PARTSIZE; )
//                    {
//                        AICHHashTree pBlockAICHHashTree =
//                            pAICHHashSet_.HashTree.FindHash((ulong)hashcount * MuleConstants.PARTSIZE, MuleConstants.PARTSIZE);

//                        if (!CreateHash(file, MuleConstants.PARTSIZE, null, pBlockAICHHashTree))
//                        {
//                            //TODO:LogError(_T("Failed to hash file \"%s\" - %s"), GetFilePath(), _tcserror(errno));
//                            return false;
//                        }

//                        togo -= MuleConstants.PARTSIZE;
//                        hashcount++;
//                    }

//                    if (togo != 0)
//                    {
//                        AICHHashTree pBlockAICHHashTree =
//                            pAICHHashSet_.HashTree.FindHash((ulong)hashcount * MuleConstants.PARTSIZE, togo);

//                        if (!CreateHash(file, togo, null, pBlockAICHHashTree))
//                        {
//                            //TODO:LogError(_T("Failed to hash file \"%s\" - %s"), GetFilePath(), _tcserror(errno));
//                            return false;
//                        }
//                    }

//                    pAICHHashSet_.ReCalculateHash(false);
//                    if (pAICHHashSet_.VerifyHashTree(true))
//                    {
//                        pAICHHashSet_.Status = AICHStatusEnum.AICH_HASHSETCOMPLETE;
//                        if (!pAICHHashSet_.SaveHashSet())
//                        {
//                            //TODO:LogError(LOG_STATUSBAR, GetResString(IDS_SAVEACFAILED));
//                        }
//                    }
//                    else
//                    {
//                        // now something went pretty wrong
//                        //TODO:DebugLogError(LOG_STATUSBAR, _T("Failed to calculate AICH Hashset from file %s"), FileName);
//                    }

//                }

//                return true;
//            }
//            catch (Exception)
//            {
//                //TODO:Log
//                return false;
//            }
//        }

//        public bool CreateFromFile(string directory, string filename)
//        {
//            FileDirectory = directory;
//            FileName = filename;

//            // open file
//            string strFilePath = System.IO.Path.Combine(directory, filename);
//            FilePath = strFilePath;

//            try
//            {
//                using (System.IO.FileStream fs = new System.IO.FileStream(strFilePath, FileMode.Open, FileAccess.Read))
//                {
//                    if ((ulong)fs.Length > MuleConstants.MAX_EMULE_FILE_SIZE)
//                    {
//                        return false;
//                    }

//                    FileSize = Convert.ToUInt64(fs.Length);

//                    availPartFrequency_ = new ushort[PartCount];

//                    for (uint i = 0; i < PartCount; i++)
//                        availPartFrequency_[i] = 0;

//                    // create hashset
//                    ulong togo = FileSize;
//                    uint hashcount;
//                    AICHHashTree pBlockAICHHashTree = null;
//                    for (hashcount = 0; togo >= MuleConstants.PARTSIZE; )
//                    {
//                        pBlockAICHHashTree =
//                            AICHHashSet.HashTree.FindHash((ulong)hashcount * MuleConstants.PARTSIZE, MuleConstants.PARTSIZE);

//                        byte[] newhash = new byte[16];

//                        try
//                        {
//                            CreateHash(fs, MuleConstants.PARTSIZE, newhash, pBlockAICHHashTree);
//                        }
//                        catch
//                        {
//                            //TODO:LogError(_T("Failed to hash file \"%s\" - %s"), strFilePath, _tcserror(errno));
//                            return false;
//                        }

//                        hashlist_.Add(newhash);
//                        togo -= MuleConstants.PARTSIZE;
//                        hashcount++;
//                    }

//                    if (togo == 0)
//                    {
//                        // sha hashtree doesnt takes hash of 0-sized data
//                        pBlockAICHHashTree = null;
//                    }
//                    else
//                    {
//                        pBlockAICHHashTree = AICHHashSet.HashTree.FindHash((ulong)hashcount * MuleConstants.PARTSIZE, togo);
//                    }

//                    byte[] lasthash = new byte[16];
//                    MPDUtilities.Md4Clr(lasthash);
//                    try
//                    {
//                        CreateHash(fs, togo, lasthash, pBlockAICHHashTree);
//                    }
//                    catch
//                    {
//                        //TODO:LogError(_T("Failed to hash file \"%s\" - %s"), strFilePath, _tcserror(errno));
//                        return false;
//                    }

//                    AICHHashSet.ReCalculateHash(false);
//                    if (AICHHashSet.VerifyHashTree(true))
//                    {
//                        AICHHashSet.Status = AICHStatusEnum.AICH_HASHSETCOMPLETE;
//                        if (!AICHHashSet.SaveHashSet())
//                        {
//                            //TODO:LogError(LOG_STATUSBAR, GetResString(IDS_SAVEACFAILED));
//                        }
//                    }
//                    else
//                    {
//                        // now something went pretty wrong
//                        //TODO:DebugLogError(LOG_STATUSBAR, _T("Failed to calculate AICH Hashset from file %s"), FileName);
//                    }

//                    if (hashcount == 0)
//                    {
//                        MPDUtilities.Md4Cpy(FileHash, lasthash);
//                    }
//                    else
//                    {
//                        hashlist_.Add(lasthash);
//                        byte[] buffer = new byte[hashlist_.Count * 16];
//                        for (int i = 0; i < hashlist_.Count; i++)
//                            MPDUtilities.Md4Cpy(buffer, i * 16, hashlist_[i], 0, hashlist_[i].Length);
//                        CreateHash(buffer, Convert.ToUInt64(buffer.Length), FileHash);
//                    }

//                    tUtcLastModified_ = MPDUtilities.DateTime2UInt32Time(System.IO.File.GetLastWriteTimeUtc(FilePath));
//                }
//            }
//            catch (FileNotFoundException/* ex*/)
//            {
//                //TODO:Log
//                return false;
//            }

//            // Add filetags
//            UpdateMetaDataTags();

//            UpdatePartsInfo();

//            return true;
//        }
//    }
//}
