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
using System.Collections;
using System.IO;
using Mule.ED2K;
using Mpd.Generic.IO;
using Mpd.Generic;
using Mule.Definitions;
using Mpd.Utilities;

namespace Mule.File.Impl
{
    class KnownFileImpl : AbstractFileImpl, KnownFile
    {
        public event EventHandler UpdatePartsInfo;

        #region Fields
        protected ByteArrayArray hashlist_ =
            new ByteArrayArray();
        protected string strDirectory_ = string.Empty;
        protected string strFilePath_ = string.Empty;
        protected AICHHashSet pAICHHashSet_ = null;

        private ushort iPartCount_;
        private ushort iED2KPartCount_;
        private ushort iED2KPartHashCount_;
        private byte iUpPriority_;
        private bool bAutoUpPriority_;
        private bool bPublishedED2K_;
        private uint lastPublishTimeKadSrc_;
        private uint lastPublishTimeKadNotes_;
        private uint uMetaDataVer_;
        private FileTypeEnum verifiedFileType_;

        protected uint tUtcLastModified_;

        protected StatisticFile statistic_;
        private ushort[] availPartFrequency_ = new ushort[1];

        #endregion

        #region Constructors
        public KnownFileImpl()
        {
            statistic_ = FileObjectManager.CreateStatisticFile();
        }
        #endregion

        #region KnownFile Members
        public StatisticFile Statistic { get { return statistic_; } }
        public uint CompleteSourcesTime { get; set; }
        public ushort CompleteSourcesCount { get; set; }
        public ushort CompleteSourcesCountLo { get; set; }
        public ushort CompleteSourcesCountHi { get; set; }

        public ushort[] AvailPartFrequency
        {
            get { return availPartFrequency_; }
            set { availPartFrequency_ = value; }
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

        public bool LoadFromFile(FileDataIO file)
        {
            bool ret1 = LoadDateFromFile(file);
            bool ret2 = LoadHashsetFromFile(file, false);
            bool ret3 = LoadTagsFromFile(file);
            UpdatePartsInfo(this, new EventArgs());
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

            if (ED2KUtilities.WriteOptED2KUTF8Tag(file, FileName, MuleConstants.FT_FILENAME))
                uTagCount++;

            Tag nametag = MpdObjectManager.CreateTag(MuleConstants.FT_FILENAME, FileName);
            nametag.WriteTagToFile(file);
            uTagCount++;

            Tag sizetag = MpdObjectManager.CreateTag(MuleConstants.FT_FILESIZE, FileSize, IsLargeFile);
            sizetag.WriteTagToFile(file);
            uTagCount++;

            // statistic
            if (statistic_.AllTimeTransferred > 0)
            {
                Tag attag1 = MpdObjectManager.CreateTag(MuleConstants.FT_ATTRANSFERRED,
                    Convert.ToUInt32(statistic_.AllTimeTransferred & 0xFFFFFFFF));
                attag1.WriteTagToFile(file);
                uTagCount++;

                Tag attag4 = MpdObjectManager.CreateTag(MuleConstants.FT_ATTRANSFERREDHI,
                    Convert.ToUInt32(statistic_.AllTimeTransferred >> 32));
                attag4.WriteTagToFile(file);
                uTagCount++;
            }

            if (statistic_.AllTimeRequests > 0)
            {
                Tag attag2 = MpdObjectManager.CreateTag(MuleConstants.FT_ATREQUESTED,
                    statistic_.AllTimeRequests);
                attag2.WriteTagToFile(file);
                uTagCount++;
            }

            if (statistic_.AllTimeAccepts > 0)
            {
                Tag attag3 = MpdObjectManager.CreateTag(MuleConstants.FT_ATACCEPTED,
                    statistic_.AllTimeAccepts);
                attag3.WriteTagToFile(file);
                uTagCount++;
            }

            // priority N permission
            Tag priotag = MpdObjectManager.CreateTag(MuleConstants.FT_ULPRIORITY,
                IsAutoUpPriority ? Convert.ToByte(PriorityEnum.PR_AUTO) : iUpPriority_);
            priotag.WriteTagToFile(file);
            uTagCount++;

            //AICH Filehash
            if (AICHHashSet.HasValidMasterHash &&
                (AICHHashSet.Status == AICHStatusEnum.AICH_HASHSETCOMPLETE ||
                    AICHHashSet.Status == AICHStatusEnum.AICH_VERIFIED))
            {
                Tag aichtag = MpdObjectManager.CreateTag(MuleConstants.FT_AICH_HASH,
                    AICHHashSet.GetMasterHash().HashString);
                aichtag.WriteTagToFile(file);
                uTagCount++;
            }


            if (lastPublishTimeKadSrc_ > 0)
            {
                Tag kadLastPubSrc = MpdObjectManager.CreateTag(MuleConstants.FT_KADLASTPUBLISHSRC, lastPublishTimeKadSrc_);
                kadLastPubSrc.WriteTagToFile(file);
                uTagCount++;
            }

            if (lastPublishTimeKadNotes_ > 0)
            {
                Tag kadLastPubNotes = MpdObjectManager.CreateTag(MuleConstants.FT_KADLASTPUBLISHNOTES, lastPublishTimeKadNotes_);
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
                Tag tagFlags = MpdObjectManager.CreateTag(MuleConstants.FT_FLAGS, uFlags);
                tagFlags.WriteTagToFile(file);
                uTagCount++;
            }

            //other tags
            for (int j = 0; j < tagList_.Count; j++)
            {
                if (tagList_[j].IsStr || tagList_[j].IsInt)
                {
                    tagList_[j].WriteTagToFile(file);
                    uTagCount++;
                }
            }


            file.Seek(Convert.ToInt64(uTagCountFilePos), SeekOrigin.Begin);
            file.WriteUInt32(uTagCount);
            file.Seek(0, SeekOrigin.End);

            return true;
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

        public DateTime UtcFileDate
        {
            get { return new DateTime(tUtcLastModified_); }
        }

        public uint UtcLastModified
        {
            get { return tUtcLastModified_; }
            set { tUtcLastModified_ = value; }
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
                iPartCount_ = (ushort)(((ulong)value + (MuleConstants.PARTSIZE) - 1) / MuleConstants.PARTSIZE);

                // nr. of parts to be used with OP_FILESTATUS
                iED2KPartCount_ = (ushort)((ulong)value / MuleConstants.PARTSIZE + 1);

                // nr. of parts to be used with OP_HASHSETANSWER
                iED2KPartHashCount_ = (ushort)((ulong)value / MuleConstants.PARTSIZE);
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

        private bool SetHashSet(ByteArrayArray newHashSet)
        {
            hashlist_.Clear();

            // set new hash
            for (int i = 0; i < newHashSet.Count; i++)
            {
                byte[] pucHash = new byte[16];
                MpdUtilities.Md4Cpy(pucHash, newHashSet[i]);
                hashlist_.Add(pucHash);
            }

            // verify new hash
            if (hashlist_.Count == 0)
                return true;

            byte[] aucHashsetHash = new byte[16];
            byte[] buffer = new byte[hashlist_.Count * 16];
            for (int i = 0; i < hashlist_.Count; i++)
                MpdUtilities.Md4Cpy(buffer, (i * 16), hashlist_[i], 0, 16);
            CreateHash(buffer, Convert.ToUInt64(buffer.Length), aucHashsetHash);

            bool bResult = (MpdUtilities.Md4Cmp(aucHashsetHash, FileHash) == 0);
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
                MpdUtilities.Md4Cpy(FileHash, checkid);
                if (parts <= 1)	// nothing to check
                    return true;
            }
            else if (MpdUtilities.Md4Cmp(FileHash, checkid) != 0)
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
                    MpdUtilities.Md4Cpy(buffer, (i * 16), hashlist_[i], 0, 16);
                CreateHash(buffer, Convert.ToUInt64(buffer.Length), checkid);
            }
            if (MpdUtilities.Md4Cmp(FileHash, checkid) == 0)
                return true;
            else
            {
                hashlist_.Clear();
                return false;
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

        public uint LastPublishBuddy { get; set; }

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

        public uint MetaDataVer
        {
            get { return uMetaDataVer_; }
        }

        public void UpdateMetaDataTags()
        {
            RemoveMetaDataTags();

            uMetaDataVer_ = MuleConstants.META_DATA_VER;
        }

        public void RemoveMetaDataTags()
        {
            byte[] _aEmuleMetaTags = new byte[]
	        {
		        MuleConstants.FT_MEDIA_ARTIST,
		        MuleConstants.FT_MEDIA_ALBUM,
		        MuleConstants.FT_MEDIA_TITLE,
		        MuleConstants.FT_MEDIA_LENGTH,
		        MuleConstants.FT_MEDIA_BITRATE,
		        MuleConstants.FT_MEDIA_CODEC
	        };

            for (int j = 0; j < _aEmuleMetaTags.Length; j++)
            {
                int i = 0;

                while (i < tagList_.Count)
                {
                    if (tagList_[i].NameID == _aEmuleMetaTags[j])
                    {
                        tagList_.RemoveAt(i);
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
                ED2KFileTypes ed2kFileTypes = ED2KObjectManager.CreateED2KFileTypes();
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

        public Mule.AICH.AICHHashSet AICHHashSet
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
                Tag newtag = MpdObjectManager.CreateTag(file, false);
                switch (newtag.NameID)
                {
                    case MuleConstants.FT_FILENAME:
                        {
                            if (newtag.IsStr)
                            {
                                if (string.IsNullOrEmpty(FileName))
                                    FileName = newtag.Str;
                            }

                            break;
                        }
                    case MuleConstants.FT_FILESIZE:
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
                    case MuleConstants.FT_ATTRANSFERRED:
                        {
                            if (newtag.IsInt)
                                statistic_.AllTimeTransferred = newtag.Int;

                            break;
                        }
                    case MuleConstants.FT_ATTRANSFERREDHI:
                        {
                            if (newtag.IsInt)
                                statistic_.AllTimeTransferred = (Convert.ToUInt64(newtag.Int) << 32) | statistic_.AllTimeTransferred;

                            break;
                        }
                    case MuleConstants.FT_ATREQUESTED:
                        {
                            if (newtag.IsInt)
                                statistic_.AllTimeRequests = newtag.Int;

                            break;
                        }
                    case MuleConstants.FT_ATACCEPTED:
                        {
                            if (newtag.IsInt)
                                statistic_.AllTimeAccepts = newtag.Int;

                            break;
                        }
                    case MuleConstants.FT_ULPRIORITY:
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
                    case MuleConstants.FT_KADLASTPUBLISHSRC:
                        {
                            if (newtag.IsInt)
                            {
                                lastPublishTimeKadSrc_ = newtag.Int;
                                LastPublishBuddy = 0;
                            }

                            if (lastPublishTimeKadSrc_ > MpdUtilities.Time() + MuleConstants.KADEMLIAREPUBLISHTIMES)
                            {
                                //There may be a posibility of an older client that saved a random number here.. This will check for that..
                                lastPublishTimeKadSrc_ = 0;
                                LastPublishBuddy = 0;
                            }

                            break;
                        }
                    case MuleConstants.FT_KADLASTPUBLISHNOTES:
                        {
                            if (newtag.IsInt)
                            {
                                lastPublishTimeKadSrc_ = newtag.Int;
                            }

                            break;
                        }
                    case MuleConstants.FT_FLAGS:
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
                    case MuleConstants.FT_PERMISSIONS:
                        break;
                    case MuleConstants.FT_KADLASTPUBLISHKEY:
                        break;
                    case MuleConstants.FT_AICH_HASH:
                        {
                            if (!newtag.IsStr)
                            {
                                //ASSERT( false ); uncomment later
                                break;
                            }

                            AICHHash hash = AICHObjectManager.CreateAICHHash();
                            if (MpdUtilities.DecodeBase32(newtag.Str.ToCharArray(), hash.RawHash) ==
                                MuleConstants.HASHSIZE)
                                pAICHHashSet_.SetMasterHash(hash, AICHStatusEnum.AICH_HASHSETCOMPLETE);
                            else
                            {
                                //TODO:LOGASSERT( false );
                            }

                            break;
                        }
                    default:
                        ED2KUtilities.ConvertED2KTag(ref newtag);
                        if (newtag != null)
                        {
                            tagList_.Add(newtag);
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
            Mule.AICH.AICHHashTree pShaHashOut)
        {
            ulong required = uSize;
            byte[] X = new byte[64 * 128];
            ulong posCurrentEMBlock = 0;
            ulong nIACHPos = 0;
            AICHHashAlgorithm pHashAlg = AICHObjectManager.CreateAICHHashAlgorithm();
            MD4 md4 = new MD4();

            while (required >= 64)
            {
                uint len;
                if ((required / 64) > Convert.ToUInt64(X.Length / 64))
                    len = Convert.ToUInt32(X.Length / 64);
                else
                    len = Convert.ToUInt32(required / 64);
                pFile.Read(X, 0, Convert.ToInt32(len * 64));

                // SHA hash needs 180KB blocks
                if (pShaHashOut != null)
                {
                    if (nIACHPos + len * 64 >= MuleConstants.EMBLOCKSIZE)
                    {
                        uint nToComplete = Convert.ToUInt32(MuleConstants.EMBLOCKSIZE - nIACHPos);
                        pHashAlg.Add(X, 0, nToComplete);
                        pShaHashOut.SetBlockHash(MuleConstants.EMBLOCKSIZE, posCurrentEMBlock, pHashAlg);
                        posCurrentEMBlock += MuleConstants.EMBLOCKSIZE;
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
                required -= len * 64;
            }

            required = uSize % 64;
            if (required != 0)
            {
                pFile.Read(X, 0, Convert.ToInt32(required));

                if (pShaHashOut != null)
                {
                    if (nIACHPos + required >= MuleConstants.EMBLOCKSIZE)
                    {
                        uint nToComplete = Convert.ToUInt32(MuleConstants.EMBLOCKSIZE - nIACHPos);
                        pHashAlg.Add(X, 0, nToComplete);
                        pShaHashOut.SetBlockHash(MuleConstants.EMBLOCKSIZE, posCurrentEMBlock, pHashAlg);
                        posCurrentEMBlock += MuleConstants.EMBLOCKSIZE;
                        pHashAlg.Reset();
                        pHashAlg.Add(X, nToComplete, Convert.ToUInt32(required - nToComplete));
                        nIACHPos = required - nToComplete;
                    }
                    else
                    {
                        pHashAlg.Add(X, 0, Convert.ToUInt32(required));
                        nIACHPos += required;
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
                md4.Add(X, (uint)required);
                md4.Finish();
                MpdUtilities.Md4Cpy(pucHash, md4.GetHash());
            }

            return true;
        }

        public bool CreateHash(byte[] pucData, ulong usize, byte[] pucHash)
        {
            return CreateHash(pucData, usize, pucHash, null);
        }

        public bool CreateHash(byte[] pucData, ulong usize, byte[] pucHash, Mule.AICH.AICHHashTree pShaHashOut)
        {
            MemoryStream ms = new MemoryStream(pucData, 0, Convert.ToInt32(usize), false);

            return CreateHash(ms, usize, pucHash, pShaHashOut);
        }
        #endregion

    }
}
