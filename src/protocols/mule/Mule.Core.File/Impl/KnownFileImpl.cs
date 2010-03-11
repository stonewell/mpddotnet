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
using Mule.Core.AICH;
using System.Collections;
using Kademlia;
using System.IO;
using Mule.Core.Preference;
using Mule.Core.ED2K;

namespace Mule.Core.File.Impl
{
    class KnownFileImpl : AbstractFileImpl, KnownFile
    {
        #region Fields
        protected ByteArrayArray hashlist_ =
            new ByteArrayArray();
        protected string strDirectory_ = string.Empty;
        protected string strFilePath_ = string.Empty;
        protected AICHHashSet pAICHHashSet_ = null;

        private UInt16 iPartCount_;
        private UInt16 iED2KPartCount_;
        private UInt16 iED2KPartHashCount_;
        private byte iUpPriority_;
        private bool bAutoUpPriority_;
        private bool bPublishedED2K_;
        private uint kadFileSearchID_;
        private uint lastPublishTimeKadSrc_;
        private uint lastPublishTimeKadNotes_;
        private uint lastBuddyIP_;
        private Kademlia.KadWordList wordlist_ = null;
        private uint uMetaDataVer_;
        private FileTypeEnum verifiedFileType_;

        protected uint tUtcLastModified_;

        protected StatisticFile statistic_;
        private uint nCompleteSourcesTime_;
        private UInt16 nCompleteSourcesCount_;
        private UInt16 nCompleteSourcesCountLo_;
        private UInt16 nCompleteSourcesCountHi_;
        private UpDownClientList clientUploadList_ = new UpDownClientList();
        private ushort[] availPartFrequency_ = new ushort[1];
        private MuleCollection collection_ = null;

        #endregion

        #region Constructors
        public KnownFileImpl()
        {
            collection_ = MuleEngine.CoreObjectManager.CreateMuleCollection();

            wordlist_ = MuleEngine.KadObjectManager.CreateWordList();

            statistic_ = MuleEngine.CoreObjectManager.CreateStatisticFile();
        }
        #endregion

        #region Abstract File
        public override void UpdateFileRatingCommentAvail(bool bForceUpdate)
        {
            bool bOldHasComment = HasComment;
            uint uOldUserRatings = UserRating;

            HasComment = false;
            uint uRatings = 0;
            uint uUserRatings = 0;

            foreach (KadEntry entry in KadNotes)
            {
                string desc = entry.GetStrTagValue(CoreConstants.TAG_DESCRIPTION);

                if (!HasComment && !string.IsNullOrEmpty(desc))
                    HasComment = true;
                uint rating = Convert.ToUInt32(entry.GetIntTagValue(CoreConstants.TAG_FILERATING));

                if (rating != 0)
                {
                    uRatings++;
                    uUserRatings += rating;
                }
            }

            if (uRatings > 0)
                UserRating = Convert.ToUInt32(Math.Round((float)uUserRatings / (float)uRatings));
            else
                UserRating = 0;

            if (bOldHasComment != HasComment ||
                uOldUserRatings != UserRating ||
                bForceUpdate)
            {
                //TODO: File Event which File comments / user rating changes
            }
        }
        #endregion

        #region KnownFile Members

        public virtual void SetFileName(string pszFileName, bool bReplaceInvalidFileSystemChars, bool bRemoveControlChars)
        {
            KnownFile pFile = null;

            // If this is called within the sharedfiles object during startup,
            // we cannot reference it yet..

            if (MuleEngine.SharedFiles != null)
                pFile = MuleEngine.SharedFiles.GetFileByID(FileHash);

            if (pFile != null && pFile == this)
                MuleEngine.SharedFiles.RemoveKeywords(this);

            base.SetFileName(pszFileName,
                bReplaceInvalidFileSystemChars,
                true,
                bRemoveControlChars);

            wordlist_.Clear();

            if (collection_ != null)
            {
                string sKeyWords = string.Format("{0} {1}",
                    collection_.GetCollectionAuthorKeyString(), FileName);
                MuleEngine.KadEngine.SearchManager.GetWords(sKeyWords, wordlist_);
            }
            else
                MuleEngine.KadEngine.SearchManager.GetWords(FileName, wordlist_);

            if (pFile != null && pFile == this)
                MuleEngine.SharedFiles.AddKeywords(this);
        }

        public string FileDirectory
        {
            get
            {
                return strDirectory_;
            }
            set
            {
                strDirectory_ = value;
            }
        }

        public string FilePath
        {
            get
            {
                return strFilePath_;
            }
            set
            {
                strFilePath_ = value;
            }
        }

        public bool CreateFromFile(string directory, string filename)
        {
            FileDirectory = directory;
            FileName = filename;

            // open file
            string strFilePath = System.IO.Path.Combine(directory, filename);
            FilePath = strFilePath;

            try
            {
                using (System.IO.FileStream fs = new System.IO.FileStream(strFilePath, FileMode.Open, FileAccess.Read))
                {
                    if ((ulong)fs.Length > CoreConstants.MAX_EMULE_FILE_SIZE)
                    {
                        return false;
                    }

                    FileSize = Convert.ToUInt64(fs.Length);

                    availPartFrequency_ = new ushort[PartCount];

                    for (uint i = 0; i < PartCount; i++)
                        availPartFrequency_[i] = 0;

                    // create hashset
                    ulong togo = FileSize;
                    uint hashcount;
                    AICHHashTree pBlockAICHHashTree = null;
                    for (hashcount = 0; togo >= CoreConstants.PARTSIZE; )
                    {
                        pBlockAICHHashTree =
                            AICHHashSet.HashTree.FindHash((ulong)hashcount * CoreConstants.PARTSIZE, CoreConstants.PARTSIZE);

                        byte[] newhash = new byte[16];

                        try
                        {
                            CreateHash(fs, CoreConstants.PARTSIZE, newhash, pBlockAICHHashTree);
                        }
                        catch
                        {
                            //TODO:LogError(_T("Failed to hash file \"%s\" - %s"), strFilePath, _tcserror(errno));
                            return false;
                        }

                        hashlist_.Add(newhash);
                        togo -= CoreConstants.PARTSIZE;
                        hashcount++;
                    }

                    if (togo == 0)
                    {
                        // sha hashtree doesnt takes hash of 0-sized data
                        pBlockAICHHashTree = null;
                    }
                    else
                    {
                        pBlockAICHHashTree = AICHHashSet.HashTree.FindHash((ulong)hashcount * CoreConstants.PARTSIZE, togo);
                    }

                    byte[] lasthash = new byte[16];
                    MuleEngine.CoreUtilities.Md4Clr(lasthash);
                    try
                    {
                        CreateHash(fs, togo, lasthash, pBlockAICHHashTree);
                    }
                    catch
                    {
                        //TODO:LogError(_T("Failed to hash file \"%s\" - %s"), strFilePath, _tcserror(errno));
                        return false;
                    }

                    AICHHashSet.ReCalculateHash(false);
                    if (AICHHashSet.VerifyHashTree(true))
                    {
                        AICHHashSet.Status = AICHStatusEnum.AICH_HASHSETCOMPLETE;
                        if (!AICHHashSet.SaveHashSet())
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
                        MuleEngine.CoreUtilities.Md4Cpy(FileHash, lasthash);
                    }
                    else
                    {
                        hashlist_.Add(lasthash);
                        byte[] buffer = new byte[hashlist_.Count * 16];
                        for (int i = 0; i < hashlist_.Count; i++)
                            MuleEngine.CoreUtilities.Md4Cpy(buffer, i * 16, hashlist_[i], 0, hashlist_[i].Length);
                        CreateHash(buffer, Convert.ToUInt64(buffer.Length), FileHash);
                    }

                    tUtcLastModified_ = MuleEngine.CoreUtilities.DateTime2UInt32Time(File.GetLastWriteTimeUtc(FilePath));
                }
            }
            catch (FileNotFoundException/* ex*/)
            {
                //TODO:Log
                return false;
            }

            // Add filetags
            UpdateMetaDataTags();

            UpdatePartsInfo();

            return true;
        }

        public bool LoadFromFile(FileDataIO file)
        {
            bool ret1 = LoadDateFromFile(file);
            bool ret2 = LoadHashsetFromFile(file, false);
            bool ret3 = LoadTagsFromFile(file);
            UpdatePartsInfo();
            return ret1 && ret2 && ret3 && ED2KPartHashCount == HashCount;
        }

        public bool WriteToFile(FileDataIO file)
        {
            // date
            file.WriteUInt32(tUtcLastModified_);

            // hashset
            file.WriteHash16(FileHash);
            int parts = hashlist_.Count;
            file.WriteUInt16(Convert.ToUInt16(parts));
            for (int i = 0; i < parts; i++)
                file.WriteHash16(hashlist_[i]);

            uint uTagCount = 0;
            ulong uTagCountFilePos = Convert.ToUInt64(file.Position);
            file.WriteUInt32(uTagCount);

            if (MuleEngine.CoreUtilities.WriteOptED2KUTF8Tag(file, FileName, CoreConstants.FT_FILENAME))
                uTagCount++;

            Tag nametag = MuleEngine.CoreObjectManager.CreateTag(CoreConstants.FT_FILENAME, FileName);
            nametag.WriteTagToFile(file);
            uTagCount++;

            Tag sizetag = MuleEngine.CoreObjectManager.CreateTag(CoreConstants.FT_FILESIZE, FileSize, IsLargeFile);
            sizetag.WriteTagToFile(file);
            uTagCount++;

            // statistic
            if (statistic_.AllTimeTransferred > 0)
            {
                Tag attag1 = MuleEngine.CoreObjectManager.CreateTag(CoreConstants.FT_ATTRANSFERRED,
                    Convert.ToUInt32(statistic_.AllTimeTransferred & 0xFFFFFFFF));
                attag1.WriteTagToFile(file);
                uTagCount++;

                Tag attag4 = MuleEngine.CoreObjectManager.CreateTag(CoreConstants.FT_ATTRANSFERREDHI,
                    Convert.ToUInt32(statistic_.AllTimeTransferred >> 32));
                attag4.WriteTagToFile(file);
                uTagCount++;
            }

            if (statistic_.AllTimeRequests > 0)
            {
                Tag attag2 = MuleEngine.CoreObjectManager.CreateTag(CoreConstants.FT_ATREQUESTED,
                    statistic_.AllTimeRequests);
                attag2.WriteTagToFile(file);
                uTagCount++;
            }

            if (statistic_.AllTimeAccepts > 0)
            {
                Tag attag3 = MuleEngine.CoreObjectManager.CreateTag(CoreConstants.FT_ATACCEPTED,
                    statistic_.AllTimeAccepts);
                attag3.WriteTagToFile(file);
                uTagCount++;
            }

            // priority N permission
            Tag priotag = MuleEngine.CoreObjectManager.CreateTag(CoreConstants.FT_ULPRIORITY,
                IsAutoUpPriority ? Convert.ToByte(PriorityEnum.PR_AUTO) : iUpPriority_);
            priotag.WriteTagToFile(file);
            uTagCount++;

            //AICH Filehash
            if (AICHHashSet.HasValidMasterHash &&
                (AICHHashSet.Status == AICHStatusEnum.AICH_HASHSETCOMPLETE ||
                    AICHHashSet.Status == AICHStatusEnum.AICH_VERIFIED))
            {
                Tag aichtag = MuleEngine.CoreObjectManager.CreateTag(CoreConstants.FT_AICH_HASH,
                    AICHHashSet.GetMasterHash().HashString);
                aichtag.WriteTagToFile(file);
                uTagCount++;
            }


            if (lastPublishTimeKadSrc_ > 0)
            {
                Tag kadLastPubSrc = MuleEngine.CoreObjectManager.CreateTag(CoreConstants.FT_KADLASTPUBLISHSRC, lastPublishTimeKadSrc_);
                kadLastPubSrc.WriteTagToFile(file);
                uTagCount++;
            }

            if (lastPublishTimeKadNotes_ > 0)
            {
                Tag kadLastPubNotes = MuleEngine.CoreObjectManager.CreateTag(CoreConstants.FT_KADLASTPUBLISHNOTES, lastPublishTimeKadNotes_);
                kadLastPubNotes.WriteTagToFile(file);
                uTagCount++;
            }

            if (uMetaDataVer_ > 0)
            {
                // Misc. Flags
                // ------------------------------------------------------------------------------
                // Bits  3-0: Meta data version
                //				0 = Unknown
                //				1 = we have created that meta data by examining the file contents.
                // Bits 31-4: Reserved
                uint uFlags = uMetaDataVer_ & 0x0F;
                Tag tagFlags = MuleEngine.CoreObjectManager.CreateTag(CoreConstants.FT_FLAGS, uFlags);
                tagFlags.WriteTagToFile(file);
                uTagCount++;
            }

            //other tags
            for (int j = 0; j < taglist_.Count; j++)
            {
                if (taglist_[j].IsStr || taglist_[j].IsInt)
                {
                    taglist_[j].WriteTagToFile(file);
                    uTagCount++;
                }
            }


            file.Seek(Convert.ToInt64(uTagCountFilePos), SeekOrigin.Begin);
            file.WriteUInt32(uTagCount);
            file.Seek(0, SeekOrigin.End);

            return true;
        }

        public bool CreateAICHHashSetOnly()
        {
            pAICHHashSet_.FreeHashSet();

            try
            {
                using (Stream file = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    // create aichhashset
                    ulong togo = FileSize;
                    uint hashcount;
                    for (hashcount = 0; togo >= CoreConstants.PARTSIZE; )
                    {
                        AICHHashTree pBlockAICHHashTree =
                            pAICHHashSet_.HashTree.FindHash((ulong)hashcount * CoreConstants.PARTSIZE, CoreConstants.PARTSIZE);

                        if (!CreateHash(file, CoreConstants.PARTSIZE, null, pBlockAICHHashTree))
                        {
                            //TODO:LogError(_T("Failed to hash file \"%s\" - %s"), GetFilePath(), _tcserror(errno));
                            return false;
                        }

                        togo -= CoreConstants.PARTSIZE;
                        hashcount++;
                    }

                    if (togo != 0)
                    {
                        AICHHashTree pBlockAICHHashTree =
                            pAICHHashSet_.HashTree.FindHash((ulong)hashcount * CoreConstants.PARTSIZE, togo);

                        if (!CreateHash(file, togo, null, pBlockAICHHashTree))
                        {
                            //TODO:LogError(_T("Failed to hash file \"%s\" - %s"), GetFilePath(), _tcserror(errno));
                            return false;
                        }
                    }

                    pAICHHashSet_.ReCalculateHash(false);
                    if (pAICHHashSet_.VerifyHashTree(true))
                    {
                        pAICHHashSet_.Status = AICHStatusEnum.AICH_HASHSETCOMPLETE;
                        if (!pAICHHashSet_.SaveHashSet())
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

        public FileTypeEnum VerifiedFileType
        {
            get
            {
                return verifiedFileType_;
            }
            set
            {
                verifiedFileType_ = value;
            }
        }

        public DateTime UtcCFileDate
        {
            get { return new DateTime(tUtcLastModified_); }
        }

        public uint UtcFileDate
        {
            get { return tUtcLastModified_; }
        }

        public override ulong FileSize
        {
            get
            {
                return base.FileSize;
            }
            set
            {
                base.FileSize = value;
                pAICHHashSet_.SetFileSize(value);
                // Examples of parthashs, hashsets and filehashs for different filesizes
                // according the ed2k protocol
                //----------------------------------------------------------------------
                //
                //File size: 3 bytes
                //File hash: 2D55E87D0E21F49B9AD25F98531F3724
                //Nr. hashs: 0
                //
                //
                //File size: 1*PARTSIZE
                //File hash: A72CA8DF7F07154E217C236C89C17619
                //Nr. hashs: 2
                //Hash[  0]: 4891ED2E5C9C49F442145A3A5F608299
                //Hash[  1]: 31D6CFE0D16AE931B73C59D7E0C089C0	*special part hash*
                //
                //
                //File size: 1*PARTSIZE + 1 byte
                //File hash: 2F620AE9D462CBB6A59FE8401D2B3D23
                //Nr. hashs: 2
                //Hash[  0]: 121795F0BEDE02DDC7C5426D0995F53F
                //Hash[  1]: C329E527945B8FE75B3C5E8826755747
                //
                //
                //File size: 2*PARTSIZE
                //File hash: A54C5E562D5E03CA7D77961EB9A745A4
                //Nr. hashs: 3
                //Hash[  0]: B3F5CE2A06BF403BFB9BFFF68BDDC4D9
                //Hash[  1]: 509AA30C9EA8FC136B1159DF2F35B8A9
                //Hash[  2]: 31D6CFE0D16AE931B73C59D7E0C089C0	*special part hash*
                //
                //
                //File size: 3*PARTSIZE
                //File hash: 5E249B96F9A46A18FC2489B005BF2667
                //Nr. hashs: 4
                //Hash[  0]: 5319896A2ECAD43BF17E2E3575278E72
                //Hash[  1]: D86EF157D5E49C5ED502EDC15BB5F82B
                //Hash[  2]: 10F2D5B1FCB95C0840519C58D708480F
                //Hash[  3]: 31D6CFE0D16AE931B73C59D7E0C089C0	*special part hash*
                //
                //
                //File size: 3*PARTSIZE + 1 byte
                //File hash: 797ED552F34380CAFF8C958207E40355
                //Nr. hashs: 4
                //Hash[  0]: FC7FD02CCD6987DCF1421F4C0AF94FB8
                //Hash[  1]: 2FE466AF8A7C06DA3365317B75A5ACFE
                //Hash[  2]: 873D3BF52629F7C1527C6E8E473C1C30
                //Hash[  3]: BCE50BEE7877BB07BB6FDA56BFE142FB
                //

                // File size       Data parts      ED2K parts      ED2K part hashs
                // ---------------------------------------------------------------
                // 1..PARTSIZE-1   1               1               0(!)
                // PARTSIZE        1               2(!)            2(!)
                // PARTSIZE+1      2               2               2
                // PARTSIZE*2      2               3(!)            3(!)
                // PARTSIZE*2+1    3               3               3            
                if (value == (ulong)0)
                {
                    iPartCount_ = 0;
                    iED2KPartCount_ = 0;
                    iED2KPartHashCount_ = 0;
                    return;
                }

                // nr. of data parts
                iPartCount_ = (UInt16)(((ulong)value + (CoreConstants.PARTSIZE - 1)) / CoreConstants.PARTSIZE);

                // nr. of parts to be used with OP_FILESTATUS
                iED2KPartCount_ = (UInt16)((ulong)value / CoreConstants.PARTSIZE + 1);

                // nr. of parts to be used with OP_HASHSETANSWER
                iED2KPartHashCount_ = (UInt16)((ulong)value / CoreConstants.PARTSIZE);
                if (iED2KPartHashCount_ != 0)
                    iED2KPartHashCount_ += 1;
            }
        }

        public uint HashCount
        {
            get { return Convert.ToUInt32(hashlist_.Count); }
        }

        public byte[] GetPartHash(uint part)
        {
            if (part >= hashlist_.Count)
                return null;

            return hashlist_[Convert.ToInt32(part)];
        }

        public ByteArrayArray Hashset
        {
            get
            {
                return hashlist_;
            }

            set
            {
                SetHashSet(value);
            }
        }

        public bool SetHashSet(ByteArrayArray newHashSet)
        {
            hashlist_.Clear();

            // set new hash
            for (int i = 0; i < newHashSet.Count; i++)
            {
                byte[] pucHash = new byte[16];
                MuleEngine.CoreUtilities.Md4Cpy(pucHash, newHashSet[i]);
                hashlist_.Add(pucHash);
            }

            // verify new hash
            if (hashlist_.Count == 0)
                return true;

            byte[] aucHashsetHash = new byte[16];
            byte[] buffer = new byte[hashlist_.Count * 16];
            for (int i = 0; i < hashlist_.Count; i++)
                MuleEngine.CoreUtilities.Md4Cpy(buffer, (i * 16), hashlist_[i], 0, 16);
            CreateHash(buffer, Convert.ToUInt64(buffer.Length), aucHashsetHash);

            bool bResult = (MuleEngine.CoreUtilities.Md4Cmp(aucHashsetHash, FileHash) == 0);
            if (!bResult)
            {
                // delete hashset
                hashlist_.Clear();
            }

            return bResult;
        }

        public ushort ED2KPartHashCount
        {
            get { return iED2KPartHashCount_; }
        }

        public ushort PartCount
        {
            get { return iPartCount_; }
        }

        public ushort ED2KPartCount
        {
            get { return iED2KPartCount_; }
        }

        public byte UpPriority
        {
            get
            {
                return iUpPriority_;
            }
            set
            {
                SetUpPriority(iUpPriority_, true);
            }
        }

        public void SetUpPriority(byte iUpPriority, bool save)
        {
            iUpPriority_ = iUpPriority;

            if (IsPartFile && save)
                (this as PartFile).SavePartFile();
        }

        public bool IsAutoUpPriority
        {
            get
            {
                return bAutoUpPriority_; ;
            }
            set
            {
                bAutoUpPriority_ = value;
            }
        }

        public void UpdateAutoUpPriority()
        {
            if (!IsAutoUpPriority)
                return;

            if (QueuedCount > 20)
            {
                if (UpPriority != Convert.ToByte(PriorityEnum.PR_LOW))
                {
                    UpPriority = Convert.ToByte(PriorityEnum.PR_LOW);
                }
                return;
            }
            if (QueuedCount > 1)
            {
                if (UpPriority != Convert.ToByte(PriorityEnum.PR_NORMAL))
                {
                    UpPriority = Convert.ToByte(PriorityEnum.PR_NORMAL);
                }
                return;
            }

            if (UpPriority != Convert.ToByte(PriorityEnum.PR_HIGH))
            {
                UpPriority = Convert.ToByte(PriorityEnum.PR_HIGH);
            }
        }

        public uint QueuedCount
        {
            get { return Convert.ToUInt32(clientUploadList_.Count); }
        }

        public bool LoadHashsetFromFile(FileDataIO file, bool checkhash)
        {
            byte[] checkid = new byte[16];
            file.ReadHash16(checkid);

            uint parts = file.ReadUInt16();

            for (uint i = 0; i < parts; i++)
            {
                byte[] cur_hash = new byte[16];
                file.ReadHash16(cur_hash);
                hashlist_.Add(cur_hash);
            }

            if (!checkhash)
            {
                MuleEngine.CoreUtilities.Md4Cpy(FileHash, checkid);
                if (parts <= 1)	// nothing to check
                    return true;
            }
            else if (MuleEngine.CoreUtilities.Md4Cmp(FileHash, checkid) != 0)
            {
                hashlist_.Clear();
                return false;	// wrong file?
            }
            else
            {
                if (parts != ED2KPartHashCount)
                {
                    hashlist_.Clear();
                    return false;
                }
            }
            // SLUGFILLER: SafeHash

            if (hashlist_.Count != 0)
            {
                byte[] buffer = new byte[hashlist_.Count * 16];
                for (int i = 0; i < hashlist_.Count; i++)
                    MuleEngine.CoreUtilities.Md4Cpy(buffer, (i * 16), hashlist_[i], 0, 16);
                CreateHash(buffer, Convert.ToUInt64(buffer.Length), checkid);
            }
            if (MuleEngine.CoreUtilities.Md4Cmp(FileHash, checkid) == 0)
                return true;
            else
            {
                hashlist_.Clear();
                return false;
            }
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

        public virtual void UpdatePartsInfo()
        {
            // Cache part count
            uint partcount = PartCount;
            bool flag = (MuleEngine.CoreUtilities.Time() - nCompleteSourcesTime_) > 0;

            // Reset part counters
            if (availPartFrequency_.Length < partcount)
                Array.Resize<ushort>(ref availPartFrequency_, Convert.ToInt32(partcount));

            for (uint i = 0; i < partcount; i++)
                availPartFrequency_[i] = 0;

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
                            availPartFrequency_[i] += 1;
                    }
                    if (flag)
                        count.Add(cur_src.UpCompleteSourcesCount);
                }
            }

            if (flag)
            {
                nCompleteSourcesCount_ = nCompleteSourcesCountLo_ = nCompleteSourcesCountHi_ = 0;

                if (partcount > 0)
                    nCompleteSourcesCount_ = availPartFrequency_[0];
                for (uint i = 1; i < partcount; i++)
                {
                    if (nCompleteSourcesCount_ > availPartFrequency_[i])
                        nCompleteSourcesCount_ = availPartFrequency_[i];
                }

                // plus 1 since we have the file complete too
                count.Add(Convert.ToUInt16(nCompleteSourcesCount_ + 1));

                int n = count.Count;
                if (n > 0)
                {
                    // SLUGFILLER: heapsortCompletesrc
                    int r;
                    for (r = n / 2; r-- > 0; )
                        HeapSort(ref count, r, n - 1);
                    for (r = n; --r > 0; )
                    {
                        ushort t = count[r];
                        count[r] = count[0];
                        count[0] = t;
                        HeapSort(ref count, 0, r - 1);
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
                        if (count[i] < nCompleteSourcesCount_)
                            nCompleteSourcesCountLo_ = nCompleteSourcesCount_;
                        else
                            nCompleteSourcesCountLo_ = count[i];
                        nCompleteSourcesCount_ = nCompleteSourcesCountLo_;
                        nCompleteSourcesCountHi_ = count[j];
                        if (nCompleteSourcesCountHi_ < nCompleteSourcesCount_)
                            nCompleteSourcesCountHi_ = nCompleteSourcesCount_;
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
                        nCompleteSourcesCountLo_ = nCompleteSourcesCount_;
                        nCompleteSourcesCount_ = count[j];
                        if (nCompleteSourcesCount_ < nCompleteSourcesCountLo_)
                            nCompleteSourcesCount_ = nCompleteSourcesCountLo_;
                        nCompleteSourcesCountHi_ = count[k];
                        if (nCompleteSourcesCountHi_ < nCompleteSourcesCount_)
                            nCompleteSourcesCountHi_ = nCompleteSourcesCount_;
                    }
                }
                nCompleteSourcesTime_ = MuleEngine.CoreUtilities.Time() + 60;
            }
        }

        public override string FileComment
        {
            get
            {
                return base.FileComment;
            }

            set
            {
                base.FileComment = value;

                MuleEngine.CoreObjectManager.Preference.SetFileComment(FileHash, value);
            }
        }

        public override uint FileRating
        {
            get
            {
                return base.FileRating;
            }

            set
            {
                base.FileRating = value;
                MuleEngine.CoreObjectManager.Preference.SetFileRating(FileHash, value);
            }
        }

        public bool PublishedED2K
        {
            get
            {
                return bPublishedED2K_;
            }
            set
            {
                bPublishedED2K_ = value;
            }
        }

        public uint KadFileSearchID
        {
            get
            {
                return kadFileSearchID_;
            }
            set
            {
                kadFileSearchID_ = value;
            }
        }

        public Kademlia.KadWordList KadKeywords
        {
            get { return wordlist_; }
        }

        public uint LastPublishTimeKadSrc
        {
            get
            {
                return lastPublishTimeKadSrc_;
            }
            set
            {
                lastPublishTimeKadSrc_ = value;
            }
        }

        public uint LastPublishBuddy
        {
            get
            {
                return lastBuddyIP_;
            }
            set
            {
                lastBuddyIP_ = value;
            }
        }

        public uint LastPublishTimeKadNotes
        {
            get
            {
                return lastPublishTimeKadNotes_;
            }
            set
            {
                lastPublishTimeKadNotes_ = value;
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

                    if (lastBuddyIP != lastBuddyIP_)
                    {
                        lastPublishTimeKadSrc_ = MuleEngine.CoreUtilities.Time() + CoreConstants.KADEMLIAREPUBLISHTIMES;
                        lastBuddyIP_ = lastBuddyIP;
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (lastPublishTimeKadSrc_ > MuleEngine.CoreUtilities.Time())
                return false;

            lastPublishTimeKadSrc_ = MuleEngine.CoreUtilities.Time() + CoreConstants.KADEMLIAREPUBLISHTIMES;
            lastBuddyIP_ = lastBuddyIP;
            return true;
        }

        public bool PublishNotes()
        {
            if (lastPublishTimeKadNotes_ > MuleEngine.CoreUtilities.Time())
            {
                return false;
            }

            if (!string.IsNullOrEmpty(FileComment))
            {
                lastPublishTimeKadNotes_ = MuleEngine.CoreUtilities.Time() + CoreConstants.KADEMLIAREPUBLISHTIMEN;
                return true;
            }

            if (FileRating != 0)
            {
                lastPublishTimeKadNotes_ = MuleEngine.CoreUtilities.Time() + CoreConstants.KADEMLIAREPUBLISHTIMEN;
                return true;
            }

            return false;
        }

        public Packet CreateSrcInfoPacket(UpDownClient forClient,
            byte byRequestedVersion, ushort nRequestedOptions)
        {
            if (clientUploadList_.Count == 0)
                return null;

            if (MuleEngine.CoreUtilities.Md4Cmp(forClient.UploadFileID, FileHash) != 0)
            {
                // should never happen
                //TODO:DEBUG_ONLY( DebugLogError(_T("*** %hs - client (%s) upload file \"%s\" does not match file \"%s\""), __FUNCTION__, forClient.DbgGetClientInfo(), DbgGetFileInfo(forClient.GetUploadFileID()), FileName) );
                //ASSERT(0);
                return null;
            }

            // check whether client has either no download status at all or a 
            //download status which is valid for this file
            if (!(forClient.UpPartCount == 0 && forClient.UpPartStatus == null) &&
                !(forClient.UpPartCount == PartCount && forClient.UpPartStatus != null))
            {
                // should never happen
                //TODO:DEBUG_ONLY( DebugLogError(_T("*** %hs - part count (%u) of client (%s) does not match part count (%u) of file \"%s\""), __FUNCTION__, forClient.GetUpPartCount(), forClient.DbgGetClientInfo(), GetPartCount(), FileName) );
                //TODO:ASSERT(0);
                return null;
            }

            SafeMemFile data = MuleEngine.CoreObjectManager.CreateSafeMemFile(1024);

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
                            for (ushort x = 0; x < PartCount; x++)
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
                        for (ushort x = 0; x < PartCount; x++)
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

            Packet result = MuleEngine.CoreObjectManager.CreatePacket(data, CoreConstants.OP_EMULEPROT);
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

        public uint MetaDataVer
        {
            get { return uMetaDataVer_; }
        }

        public void UpdateMetaDataTags()
        {
            RemoveMetaDataTags();

            if (MuleEngine.CoreObjectManager.Preference.DoesExtractMetaData)
                return;

            uMetaDataVer_ = CoreConstants.META_DATA_VER;
        }

        public void RemoveMetaDataTags()
        {
            byte[] _aEmuleMetaTags = new byte[]
	        {
		        CoreConstants.FT_MEDIA_ARTIST,
		        CoreConstants.FT_MEDIA_ALBUM,
		        CoreConstants.FT_MEDIA_TITLE,
		        CoreConstants.FT_MEDIA_LENGTH,
		        CoreConstants.FT_MEDIA_BITRATE,
		        CoreConstants.FT_MEDIA_CODEC
	        };

            for (int j = 0; j < _aEmuleMetaTags.Length; j++)
            {
                int i = 0;

                while (i < taglist_.Count)
                {
                    if (taglist_[i].NameID == _aEmuleMetaTags[j])
                    {
                        taglist_.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }

            uMetaDataVer_ = 0;
        }

        public bool IsMovie
        {
            get
            {
                ED2KFileTypes ed2kFileTypes = MuleEngine.CoreObjectManager.CreateED2KFileTypes();
                return ed2kFileTypes.GetED2KFileTypeID(FileName) == ED2KFileTypeEnum.ED2KFT_VIDEO;
            }
        }

        public bool GrabImage(byte nFramesToGrab, double dStartTime, bool bReduceColor, ushort nMaxWidth, object pSender)
        {
            return GrabImage(System.IO.Path.Combine(FileDirectory, FileName),
                nFramesToGrab,
                dStartTime,
                bReduceColor,
                nMaxWidth, pSender);
        }

        public bool GrabImage(string strFileName, byte nFramesToGrab, double dStartTime, bool bReduceColor, ushort nMaxWidth, object pSender)
        {
            if (!IsMovie)
                return false;

            //TODO: Grab Image
            throw new Exception("The method or operation is not implemented.");
        }

        public void GrabbingFinished(CxImage.CxImageList imgResults, byte nFramesGrabbed, object pSender)
        {
            //TODO:
            throw new Exception("The method or operation is not implemented.");
        }

        public Mule.Core.AICH.AICHHashSet AICHHashSet
        {
            get
            {
                return pAICHHashSet_;
            }
            set
            {
                pAICHHashSet_ = value;
            }
        }

        public string InfoSummary
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public string UpPriorityDisplayString
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool LoadTagsFromFile(FileDataIO file)
        {
            uint tagcount = file.ReadUInt32();
            for (uint j = 0; j < tagcount; j++)
            {
                Tag newtag = MuleEngine.CoreObjectManager.CreateTag(file, false);
                switch (newtag.NameID)
                {
                    case CoreConstants.FT_FILENAME:
                        {
                            if (newtag.IsStr)
                            {
                                if (string.IsNullOrEmpty(FileName))
                                    FileName = newtag.Str;
                            }

                            break;
                        }
                    case CoreConstants.FT_FILESIZE:
                        {
                            if (newtag.IsInt64(true))
                            {
                                FileSize = newtag.Int64;
                                Array.Resize<ushort>(ref availPartFrequency_, Convert.ToInt32(PartCount));
                                for (uint i = 0; i < PartCount; i++)
                                    availPartFrequency_[i] = 0;
                            }

                            break;
                        }
                    case CoreConstants.FT_ATTRANSFERRED:
                        {
                            if (newtag.IsInt)
                                statistic_.AllTimeTransferred = newtag.Int;

                            break;
                        }
                    case CoreConstants.FT_ATTRANSFERREDHI:
                        {
                            if (newtag.IsInt)
                                statistic_.AllTimeTransferred = (Convert.ToUInt64(newtag.Int) << 32) | statistic_.AllTimeTransferred;

                            break;
                        }
                    case CoreConstants.FT_ATREQUESTED:
                        {
                            if (newtag.IsInt)
                                statistic_.AllTimeRequests = newtag.Int;

                            break;
                        }
                    case CoreConstants.FT_ATACCEPTED:
                        {
                            if (newtag.IsInt)
                                statistic_.AllTimeAccepts = newtag.Int;

                            break;
                        }
                    case CoreConstants.FT_ULPRIORITY:
                        {
                            if (newtag.IsInt)
                            {
                                iUpPriority_ = Convert.ToByte(newtag.Int);
                                if (iUpPriority_ == Convert.ToByte(PriorityEnum.PR_AUTO))
                                {
                                    iUpPriority_ = Convert.ToByte(PriorityEnum.PR_HIGH);
                                    bAutoUpPriority_ = true;
                                }
                                else
                                {
                                    if (!Enum.IsDefined(typeof(PriorityEnum), iUpPriority_))
                                        iUpPriority_ = Convert.ToByte(PriorityEnum.PR_NORMAL);
                                    bAutoUpPriority_ = false;
                                }
                            }

                            break;
                        }
                    case CoreConstants.FT_KADLASTPUBLISHSRC:
                        {
                            if (newtag.IsInt)
                            {
                                lastPublishTimeKadSrc_ = newtag.Int;
                                lastBuddyIP_ = 0;
                            }

                            if (lastPublishTimeKadSrc_ > MuleEngine.CoreUtilities.Time() + CoreConstants.KADEMLIAREPUBLISHTIMES)
                            {
                                //There may be a posibility of an older client that saved a random number here.. This will check for that..
                                lastPublishTimeKadSrc_ = 0;
                                lastBuddyIP_ = 0;
                            }

                            break;
                        }
                    case CoreConstants.FT_KADLASTPUBLISHNOTES:
                        {
                            if (newtag.IsInt)
                            {
                                lastPublishTimeKadSrc_ = newtag.Int;
                            }

                            break;
                        }
                    case CoreConstants.FT_FLAGS:
                        // Misc. Flags
                        // ------------------------------------------------------------------------------
                        // Bits  3-0: Meta data version
                        //				0 = Unknown
                        //				1 = we have created that meta data by examining the file contents.
                        // Bits 31-4: Reserved
                        if (newtag.IsInt)
                            uMetaDataVer_ = newtag.Int & 0x0F;

                        break;
                    // old tags: as long as they are not needed, take the chance to purge them
                    case CoreConstants.FT_PERMISSIONS:
                        break;
                    case CoreConstants.FT_KADLASTPUBLISHKEY:
                        break;
                    case CoreConstants.FT_AICH_HASH:
                        {
                            if (!newtag.IsStr)
                            {
                                //ASSERT( false ); uncomment later
                                break;
                            }

                            AICHHash hash = MuleEngine.CoreObjectManager.CreateAICHHash();
                            if (MuleEngine.CoreUtilities.DecodeBase32(newtag.Str.ToCharArray(), hash) ==
                                CoreConstants.HASHSIZE)
                                pAICHHashSet_.SetMasterHash(hash, AICHStatusEnum.AICH_HASHSETCOMPLETE);
                            else
                            {
                                //TODO:LOGASSERT( false );
                            }

                            break;
                        }
                    default:
                        MuleEngine.CoreUtilities.ConvertED2KTag(ref newtag);
                        if (newtag != null)
                        {
                            taglist_.Add(newtag);
                        }
                        break;
                }
            }

            // [bc]: ed2k and Kad are already full of totally wrong and/or not properly attached meta data. Take
            // the chance to clean any available meta data tags and provide only tags which were determined by us.
            // It's a brute force method, but that wrong meta data is driving me crazy because wrong meta data is even worse than
            // missing meta data.
            if (uMetaDataVer_ == 0)
                RemoveMetaDataTags();

            return true;
        }

        public bool LoadDateFromFile(FileDataIO file)
        {
            tUtcLastModified_ = file.ReadUInt32();

            return true;
        }

        public bool CreateHash(System.IO.Stream pFile,
            ulong uSize, byte[] pucHash)
        {
            return CreateHash(pFile, uSize, pucHash, null);
        }

        public bool CreateHash(System.IO.Stream pFile,
            ulong uSize, byte[] pucHash,
            Mule.Core.AICH.AICHHashTree pShaHashOut)
        {
            ulong Required = uSize;
            byte[] X = new byte[64 * 128];
            ulong posCurrentEMBlock = 0;
            ulong nIACHPos = 0;
            AICHHashAlgo pHashAlg = pAICHHashSet_.NewHashAlgo;
            MuleMD4 md4 = new MuleMD4();

            while (Required >= 64)
            {
                uint len;
                if ((Required / 64) > Convert.ToUInt64(X.Length / 64))
                    len = Convert.ToUInt32(X.Length / 64);
                else
                    len = Convert.ToUInt32(Required / 64);
                pFile.Read(X, 0, Convert.ToInt32(len * 64));

                // SHA hash needs 180KB blocks
                if (pShaHashOut != null)
                {
                    if (nIACHPos + len * 64 >= CoreConstants.EMBLOCKSIZE)
                    {
                        uint nToComplete = Convert.ToUInt32(CoreConstants.EMBLOCKSIZE - nIACHPos);
                        pHashAlg.Add(X, 0, nToComplete);
                        pShaHashOut.SetBlockHash(CoreConstants.EMBLOCKSIZE, posCurrentEMBlock, pHashAlg);
                        posCurrentEMBlock += CoreConstants.EMBLOCKSIZE;
                        pHashAlg.Reset();
                        pHashAlg.Add(X, nToComplete, (len * 64) - nToComplete);
                        nIACHPos = (len * 64) - nToComplete;
                    }
                    else
                    {
                        pHashAlg.Add(X, 0, len * 64);
                        nIACHPos += len * 64;
                    }
                }

                if (pucHash != null)
                {
                    md4.Add(X, len * 64);
                }
                Required -= len * 64;
            }

            Required = uSize % 64;
            if (Required != 0)
            {
                pFile.Read(X, 0, Convert.ToInt32(Required));

                if (pShaHashOut != null)
                {
                    if (nIACHPos + Required >= CoreConstants.EMBLOCKSIZE)
                    {
                        uint nToComplete = Convert.ToUInt32(CoreConstants.EMBLOCKSIZE - nIACHPos);
                        pHashAlg.Add(X, 0, nToComplete);
                        pShaHashOut.SetBlockHash(CoreConstants.EMBLOCKSIZE, posCurrentEMBlock, pHashAlg);
                        posCurrentEMBlock += CoreConstants.EMBLOCKSIZE;
                        pHashAlg.Reset();
                        pHashAlg.Add(X, nToComplete, Convert.ToUInt32(Required - nToComplete));
                        nIACHPos = Required - nToComplete;
                    }
                    else
                    {
                        pHashAlg.Add(X, 0, Convert.ToUInt32(Required));
                        nIACHPos += Required;
                    }
                }
            }
            if (pShaHashOut != null)
            {
                if (nIACHPos > 0)
                {
                    pShaHashOut.SetBlockHash(nIACHPos, posCurrentEMBlock, pHashAlg);
                    posCurrentEMBlock += nIACHPos;
                }

                pShaHashOut.ReCalculateHash(pHashAlg, false);
            }

            if (pucHash != null)
            {
                md4.Add(X, (uint)Required);
                md4.Finish();
                MuleEngine.CoreUtilities.Md4Cpy(pucHash, md4.GetHash());
            }

            return true;
        }

        public bool CreateHash(byte[] pucData, ulong usize, byte[] pucHash)
        {
            return CreateHash(pucData, usize, pucHash, null);
        }

        public bool CreateHash(byte[] pucData, ulong usize, byte[] pucHash, Mule.Core.AICH.AICHHashTree pShaHashOut)
        {
            MemoryStream ms = new MemoryStream(pucData, 0, Convert.ToInt32(usize), false);

            return CreateHash(ms, usize, pucHash, pShaHashOut);
        }

        #endregion

        #region Private Functions
        private static void HeapSort(ref List<ushort> count, int first, int last)
        {
            int r;
            for (r = first; (r & int.MinValue) == 0 && (r << 1) < last; )
            {
                int r2 = (r << 1) + 1;
                if (r2 != last)
                    if (count[r2] < count[r2 + 1])
                        r2++;
                if (count[r] < count[r2])
                {
                    ushort t = count[r2];
                    count[r2] = count[r];
                    count[r] = t;
                    r = r2;
                }
                else
                    break;
            }
        }
        #endregion
    }
}
