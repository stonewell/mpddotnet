using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.AICH;
using Mpd.Utilities;

namespace Mule.File.Impl
{
    abstract class FileIdentifierBaseImpl : FileIdentifierBase
    {
        #region Fields
        protected byte[] md4Hash_ = new byte[16];
        protected AICHHash aichHash_ = MuleApplication.Instance.AICHObjectManager.CreateAICHHash();
        #endregion

        #region Constructors
        protected FileIdentifierBaseImpl()
        {
            MpdUtilities.Md4Clr(md4Hash_);
            HasAICHHash = false;
        }

        protected FileIdentifierBaseImpl(FileIdentifierBase rFileIdentifier)
        {
	        MpdUtilities.Md4Cpy(md4Hash_, rFileIdentifier.MD4Hash);
	        HasAICHHash = rFileIdentifier.HasAICHHash;
	        AICHHash = rFileIdentifier.AICHHash;
        }
        #endregion

        #region FileIdentifierBase Members

        public abstract ulong FileSize { get; }

        public void WriteIdentifier(Mpd.Generic.IO.FileDataIO pFile)
        {
            WriteIdentifier(pFile, false);
        }

        public void WriteIdentifier(Mpd.Generic.IO.FileDataIO pFile, bool bKadExcludeMD4)
        {
             uint uIncludesMD4 = bKadExcludeMD4 ? (uint)0 : (uint)1; // This is (currently) mandatory except for Kad
             uint uIncludesSize = (FileSize != 0) ? (uint)1 : (uint)0;
             uint uIncludesAICH = HasAICHHash ? (uint)1 : 0;
             uint uMandatoryOptions = 0; // RESERVED - Identifier invalid if we encounter unknown options
             uint uOptions = 0; // RESERVED

            byte byIdentifierDesc = (byte)
                        ((uOptions << 5) |
                        (uMandatoryOptions << 3) |
                        (uIncludesAICH << 2) |
                        (uIncludesSize << 1) |
                        (uIncludesMD4 << 0));
            pFile.WriteUInt8(byIdentifierDesc);
            if (!bKadExcludeMD4)
                pFile.WriteHash16(md4Hash_);
            if (FileSize != (ulong)0)
                pFile.WriteUInt64(FileSize);
            if (HasAICHHash)
                aichHash_.Write(pFile);
        }

        public bool CompareRelaxed(FileIdentifierBase rFileIdentifier)
        {
            return MpdUtilities.Md4Cmp(md4Hash_, rFileIdentifier.MD4Hash) == 0
                && (FileSize == 0 || rFileIdentifier.FileSize == 0 || 
                FileSize == rFileIdentifier.FileSize)
                && (!HasAICHHash || !rFileIdentifier.HasAICHHash || 
                AICHHash == rFileIdentifier.AICHHash);
        }

        public bool CompareStrict(FileIdentifierBase rFileIdentifier)
        {
            return MpdUtilities.Md4Cmp(md4Hash_, rFileIdentifier.MD4Hash) == 0
                && FileSize == rFileIdentifier.FileSize
                && !(HasAICHHash ^ rFileIdentifier.HasAICHHash)
                && aichHash_ == rFileIdentifier.AICHHash;
        }

        public byte[] MD4Hash
        {
            get
            {
                return md4Hash_;
            }
            set
            {
                MpdUtilities.Md4Cpy(md4Hash_, value);
            }
        }

        public void SetMD4Hash(Mpd.Generic.IO.FileDataIO pFile)
        {
            pFile.ReadHash16(md4Hash_);
        }

        public Mule.AICH.AICHHash AICHHash
        {
            get
            {
                return aichHash_;
            }

            set
            {
                aichHash_ = value;
                HasAICHHash = true;

            }
        }

        public bool HasAICHHash
        {
            get;set;
        }

        #endregion
    }
}
