using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.AICH;
using Mpd.Utilities;

namespace Mule.File.Impl
{
    class FileIdentifierImpl : FileIdentifierBaseImpl, FileIdentifier
    {
        #region Fields
        private ulong m_rFileSize;
        private ByteArrayArray m_aMD4HashSet = new ByteArrayArray();
        private List<AICHHash> m_aAICHPartHashSet = new List<AICHHash>();
        #endregion

        #region Constructors
        public FileIdentifierImpl(ulong rFileSize)
        {
            m_rFileSize = rFileSize;
        }

        public FileIdentifierImpl(FileIdentifier rFileIdentifier, ulong rFileSize)
            : base(rFileIdentifier)
        {
            m_rFileSize = rFileSize;

            for (int i = 0; i < rFileIdentifier.RawMD4HashSet.Count; i++)
            {
                byte[] pucHashSetPart = new byte[16];
                MpdUtilities.Md4Cpy(pucHashSetPart, rFileIdentifier.RawMD4HashSet[i]);
                m_aMD4HashSet.Add(pucHashSetPart);
            }

            for (int i = 0; i < rFileIdentifier.RawAICHHashSet.Count; i++)
                m_aAICHPartHashSet.Add(rFileIdentifier.RawAICHHashSet[i]);
        }
        #endregion

        #region FileIdentifier Members

        public void WriteHashSetsToPacket(Mpd.Generic.IO.FileDataIO pFile, bool bMD4, bool bAICH)
        {
            // 6 Options - RESERVED
            // 1 AICH HashSet
            // 1 MD4 HashSet
            byte byOptions = 0;
            if (bMD4)
            {
                if (TheoreticalMD4PartHashCount == 0)
                {
                    bMD4 = false;
                }
                else if (HasExpectedMD4HashCount)
                    byOptions |= 0x01;
                else
                {
                    bMD4 = false;
                }
            }
            if (bAICH)
            {
                if (TheoreticalAICHPartHashCount == 0)
                {
                    bAICH = false;
                }
                else if (HasExpectedAICHHashCount && HasAICHHash)
                    byOptions |= 0x02;
                else
                {
                    bAICH = false;
                }
            }
            pFile.WriteUInt8(byOptions);
            if (bMD4)
                WriteMD4HashsetToFile(pFile);
            if (bAICH)
                WriteAICHHashsetToFile(pFile);
        }

        public bool ReadHashSetsFromPacket(Mpd.Generic.IO.FileDataIO pFile, ref bool rbMD4, ref bool rbAICH)
        {
            byte byOptions = pFile.ReadUInt8();
            bool bMD4Present = (byOptions & 0x01) > 0;
            bool bAICHPresent = (byOptions & 0x02) > 0;
            // We don't abort on unkown option, because even if there is another unknown hashset, there is no data afterwards we
            // try to read on the only occasion this function is used. So we might be able to add optional flags in the future
            // without having to adjust the protocol any further (new additional data/hashs should not be appended without adjustement however)

            if (bMD4Present && !rbMD4)
            {
                // Even if we don't want it, we still have to read the file to skip it
                byte[] tmpHash = new byte[16];
                pFile.ReadHash16(tmpHash);
                uint parts = pFile.ReadUInt16();
                for (uint i = 0; i < parts; i++)
                    pFile.ReadHash16(tmpHash);
            }
            else if (!bMD4Present)
                rbMD4 = false;
            else if (bMD4Present && rbMD4)
            {
                if (!LoadMD4HashsetFromFile(pFile, true))
                {	// corrupt
                    rbMD4 = false;
                    rbAICH = false;
                    return false;
                }
            }

            if (bAICHPresent && !rbAICH)
            {
                // Even if we don't want it, we still have to read the file to skip it
                AICHHash tmpHash =
                    MuleApplication.Instance.AICHObjectManager.CreateAICHHash(pFile);
                ushort nCount = pFile.ReadUInt16();
                for (int i = 0; i < nCount; i++)
                    tmpHash.Read(pFile);
            }
            else if (!bAICHPresent || !HasAICHHash)
            {
                rbAICH = false;
            }
            else if (bAICHPresent && rbAICH)
            {
                if (!LoadAICHHashsetFromFile(pFile, true))
                {	// corrupt
                    if (rbMD4)
                    {
                        DeleteMD4Hashset();
                        rbMD4 = false;
                    }
                    rbAICH = false;
                    return false;
                }
            }
            return true;
        }

        public bool CalculateMD4HashByHashSet(bool bVerifyOnly)
        {
            return CalculateMD4HashByHashSet(bVerifyOnly, true);
        }

        public bool CalculateMD4HashByHashSet(bool bVerifyOnly, bool bDeleteOnVerifyFail)
        {
            if (m_aMD4HashSet.Count <= 1)
            {
                return false;
            }
            byte[] buffer = new byte[m_aMD4HashSet.Count * 16];
            for (int i = 0; i < m_aMD4HashSet.Count; i++)
                MpdUtilities.Md4Cpy(buffer, i * 16, m_aMD4HashSet[i], 0, m_aMD4HashSet[i].Length);
            byte[] aucResult = new byte[16];
            KnownFile knownFile = MuleApplication.Instance.FileObjectManager.CreateKnownFile();

            knownFile.CreateHash(buffer, (ulong)m_aMD4HashSet.Count * 16, aucResult);
            if (bVerifyOnly)
            {
                if (MpdUtilities.Md4Cmp(aucResult, MD4Hash) != 0)
                {
                    if (bDeleteOnVerifyFail)
                        DeleteMD4Hashset();
                    return false;
                }
                else
                    return true;
            }
            else
            {
                MpdUtilities.Md4Cpy(MD4Hash, aucResult);
                return true;
            }
        }

        public bool LoadMD4HashsetFromFile(Mpd.Generic.IO.FileDataIO file, bool bVerifyExistingHash)
        {
            byte[] checkid = new byte[16];
            file.ReadHash16(checkid);
            DeleteMD4Hashset();

            uint parts = file.ReadUInt16();
            //TRACE("Nr. hashs: %u\n", (uint)parts);
            if (bVerifyExistingHash &&
                (MpdUtilities.Md4Cmp(MD4Hash, checkid) != 0 ||
                parts != TheoreticalMD4PartHashCount))
                return false;
            for (uint i = 0; i < parts; i++)
            {
                byte[] cur_hash = new byte[16];
                file.ReadHash16(cur_hash);
                m_aMD4HashSet.Add(cur_hash);
            }

            if (!bVerifyExistingHash)
                MpdUtilities.Md4Cpy(MD4Hash, checkid);

            // Calculate hash out of hashset and compare to existing filehash
            if (m_aMD4HashSet.Count > 0)
                return CalculateMD4HashByHashSet(true, true);
            else
                return true;
        }

        public void WriteMD4HashsetToFile(Mpd.Generic.IO.FileDataIO pFile)
        {
            pFile.WriteHash16(MD4Hash);
            int uParts = m_aMD4HashSet.Count;
            pFile.WriteUInt16((ushort)uParts);
            for (int i = 0; i < uParts; i++)
                pFile.WriteHash16(m_aMD4HashSet[i]);
        }

        public bool SetMD4HashSet(ByteArrayArray aHashset)
        {
            // delete hashset
            m_aMD4HashSet.Clear();
            m_aMD4HashSet.AddRange(aHashset);

            // verify new hash
            if (m_aMD4HashSet.Count == 0)
                return true;
            else
                return CalculateMD4HashByHashSet(true, true);
        }

        public byte[] GetMD4PartHash(uint part)
        {
            if (part < m_aMD4HashSet.Count)
                return m_aMD4HashSet[(int)part];

            return null;
        }

        public void DeleteMD4Hashset()
        {
            m_aMD4HashSet.Clear();
        }

        public ushort TheoreticalMD4PartHashCount
        {
            get
            {
                if (m_rFileSize == (ulong)0)
                {
                    return 0;
                }
                ushort uResult = (ushort)((ulong)m_rFileSize / MuleConstants.PARTSIZE);
                if (uResult > 0)
                    uResult++;
                return uResult;
            }
        }

        public ushort AvailableMD4PartHashCount
        {
            get { return (ushort)m_aMD4HashSet.Count; }
        }

        public bool HasExpectedMD4HashCount
        {
            get { return TheoreticalMD4PartHashCount == AvailableMD4PartHashCount; }
        }

        public ByteArrayArray RawMD4HashSet
        {
            get { return m_aMD4HashSet; }
        }

        public bool LoadAICHHashsetFromFile(Mpd.Generic.IO.FileDataIO pFile)
        {
            return LoadAICHHashsetFromFile(pFile, true);
        }

        public bool LoadAICHHashsetFromFile(Mpd.Generic.IO.FileDataIO pFile, bool bVerify)
        {
            m_aAICHPartHashSet.Clear();
            AICHHash masterHash =
                MuleApplication.Instance.AICHObjectManager.CreateAICHHash(pFile);
            if (HasAICHHash && masterHash != AICHHash)
            {
                return false;
            }
            ushort nCount = pFile.ReadUInt16();
            for (int i = 0; i < nCount; i++)
                m_aAICHPartHashSet.Add(MuleApplication.Instance.AICHObjectManager.CreateAICHHash(pFile));
            if (bVerify)
                return VerifyAICHHashSet();
            else
                return true;
        }

        public void WriteAICHHashsetToFile(Mpd.Generic.IO.FileDataIO pFile)
        {
            AICHHash.Write(pFile);
            int uParts = m_aAICHPartHashSet.Count;
            pFile.WriteUInt16((ushort)uParts);
            for (int i = 0; i < uParts; i++)
                m_aAICHPartHashSet[i].Write(pFile);
        }

        public bool SetAICHHashSet(Mule.AICH.AICHRecoveryHashSet sourceHashSet)
        {
            if (sourceHashSet.Status != AICHStatusEnum.AICH_HASHSETCOMPLETE ||
                sourceHashSet.MasterHash != AICHHash)
            {
                return false;
            }
            return sourceHashSet.GetPartHashs(m_aAICHPartHashSet) && HasExpectedAICHHashCount;
        }

        public bool SetAICHHashSet(FileIdentifier rSourceHashSet)
        {
            if (!rSourceHashSet.HasAICHHash || !rSourceHashSet.HasExpectedAICHHashCount)
            {
                return false;
            }
            m_aAICHPartHashSet.Clear();
            m_aAICHPartHashSet.AddRange(rSourceHashSet.RawAICHHashSet);
            return HasExpectedAICHHashCount;
        }

        public bool VerifyAICHHashSet()
        {
            if (m_rFileSize == 0 || !HasAICHHash)
            {
                return false;
            }
            if (!HasExpectedAICHHashCount)
                return false;
            AICHRecoveryHashSet tmpAICHHashSet =
                MuleApplication.Instance.AICHObjectManager.CreateAICHRecoveryHashSet(null, m_rFileSize);
            tmpAICHHashSet.SetMasterHash(AICHHash, AICHStatusEnum.AICH_HASHSETCOMPLETE);

            uint uPartCount = (uint)((m_rFileSize + MuleConstants.PARTSIZE - 1) / MuleConstants.PARTSIZE);
            if (uPartCount <= 1)
                return true; // No AICH Part Hashs
            for (uint nPart = 0; nPart < uPartCount; nPart++)
            {
                ulong nPartStartPos = (ulong)nPart * MuleConstants.PARTSIZE;
                uint nPartSize = (uint)Math.Min(MuleConstants.PARTSIZE, (ulong)FileSize - nPartStartPos);
                AICHHashTree pPartHashTree = tmpAICHHashSet.HashTree.FindHash(nPartStartPos, nPartSize);
                if (pPartHashTree != null)
                {
                    pPartHashTree.Hash = this.RawAICHHashSet[(int)nPart];
                    pPartHashTree.HashValid = true;
                }
                else
                {
                    return false;
                }
            }
            if (!tmpAICHHashSet.VerifyHashTree(false))
            {
                m_aAICHPartHashSet.Clear();
                return false;
            }
            else
                return true;
        }

        public ushort TheoreticalAICHPartHashCount
        {
            get
            {
                return (m_rFileSize <= MuleConstants.PARTSIZE) ? (ushort)0 :
                    ((ushort)((m_rFileSize + MuleConstants.PARTSIZE - 1) / MuleConstants.PARTSIZE));
            }
        }

        public ushort AvailableAICHPartHashCount
        {
            get { return (ushort)m_aAICHPartHashSet.Count; }
        }

        public bool HasExpectedAICHHashCount
        {
            get { return TheoreticalAICHPartHashCount == AvailableAICHPartHashCount; }
        }

        public IList<Mule.AICH.AICHHash> RawAICHHashSet
        {
            get { return m_aAICHPartHashSet; }
        }

        #endregion

        #region FileIdentifierBase Members
        public override ulong FileSize
        {
            get { return m_rFileSize; }
        }
        #endregion
    }
}
