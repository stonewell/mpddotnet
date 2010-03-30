using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Generic;
using Mpd.Utilities;
using Mpd.Generic.IO;
using System.IO;
using System.Security.Cryptography;

namespace Mule.Core.Impl
{
    class ClientCreditsListImpl : ClientCreditsList
    {
        #region Fields
        private Dictionary<MapCKey, ClientCredits> m_mapClients = new Dictionary<MapCKey, ClientCredits>();
        private uint m_nLastSaved;
        private RSAPKCS1SignatureFormatter m_pSignkey;
        private byte[] m_abyMyPublicKey = new byte[80];
        private uint m_nMyPublicKeyLen;
        #endregion

        #region Constructors
        public ClientCreditsListImpl()
        {
            m_nLastSaved = MpdUtilities.GetTickCount();
            LoadList();

            InitalizeCrypting();
        }
        #endregion

        #region ClientCreditList Members

        public void Process()
        {
            if (MpdUtilities.GetTickCount() - m_nLastSaved > MuleConstants.ONE_MIN_MS * 13)
                SaveList();
        }

        public byte CreateSignature(ClientCredits pTarget, byte[] pachOutput, byte nMaxSize, uint ChallengeIP, byte byChaIPKind)
        {
            return CreateSignature(pTarget, pachOutput, nMaxSize, ChallengeIP, byChaIPKind, null);
        }

        public byte CreateSignature(ClientCredits pTarget, byte[] pachOutput, byte nMaxSize, uint ChallengeIP, byte byChaIPKind, RSAPKCS1SignatureFormatter sigkey)
        {
    //// sigkey param is used for debug only
    //if (sigkey == null)
    //    sigkey = m_pSignkey;

    //// create a signature of the public key from pTarget
    //byte nResult;
    //if ( !IsCryptoAvailable )
    //    return 0;
    //try{
    //    SecByteBlock sbbSignature(sigkey.SignatureLength);
    //    AutoSeededRandomPool rng;
    //    byte[] abyBuffer = new byte[CreditStruct.MAXPUBKEYSIZE+9];
    //    uint keylen = pTarget.SecIDKeyLen;
    //    memcpy(abyBuffer,pTarget.SecureIdent,keylen);
    //    // 4 additional bytes random data send from this client
    //    uint challenge = pTarget.CryptRndChallengeFrom;
    //    PokeUInt32(abyBuffer+keylen, challenge);
    //    ushort ChIpLen = 0;
    //    if ( byChaIPKind != 0){
    //        ChIpLen = 5;
    //        PokeUInt32(abyBuffer+keylen+4, ChallengeIP);
    //        PokeUInt8(abyBuffer+keylen+4+4, byChaIPKind);
    //    }
    //    sigkey.SignMessage(rng, abyBuffer ,keylen+4+ChIpLen , sbbSignature.begin());
    //    ArraySink asink(pachOutput, nMaxSize);
    //    asink.Put(sbbSignature.begin(), sbbSignature.size());
    //    nResult = (byte)asink.TotalPutLength();			
    //}
    //catch(Exception ex)
    //{
    //    MpdUtilities.DebugLogError(ex);
    //    nResult = 0;
    //}
    //return nResult;
            return 0;
        }

        public bool VerifyIdent(ClientCredits pTarget, byte[] pachSignature, byte nInputSize, uint dwForIP, byte byChaIPKind)
        {
            throw new NotImplementedException();
        }

        public ClientCredits GetCredit(byte[] key)
        {
            MapCKey cKey = new MapCKey(key);

            if (!m_mapClients.ContainsKey(cKey))
            {
                m_mapClients[cKey] =
                    MuleApplication.Instance.CoreObjectManager.CreateClientCredits(key);
            }

            m_mapClients[cKey].SetLastSeen();

            return m_mapClients[cKey];
        }

        public byte PubKeyLength
        {
            get { throw new NotImplementedException(); }
        }

        public byte[] PublicKey
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsCryptoAvailable
        {
            get { throw new NotImplementedException(); }
        }

        public void CleanUp()
        {
            SaveList();
        }

        #endregion

        #region Protected
        private const string CLIENTS_MET_FILENAME = "clients.met";
        protected void LoadList()
        {
            string strFileName =
                System.IO.Path.Combine(MuleApplication.Instance.Preference.GetMuleDirectory(Mule.Preference.DefaultDirectoryEnum.EMULE_CONFIGDIR),
                    CLIENTS_MET_FILENAME);

            if (!System.IO.File.Exists(strFileName))
                return;
            try
            {
                SafeBufferedFile file =
                    MpdObjectManager.CreateSafeBufferedFile(strFileName,
                        System.IO.FileMode.Open,
                        System.IO.FileAccess.Read,
                        System.IO.FileShare.None);

                byte version = file.ReadUInt8();

                if (version != (byte)VersionsEnum.CREDITFILE_VERSION &&
                    version != (byte)VersionsEnum.CREDITFILE_VERSION_29)
                {
                    file.Close();
                    return;
                }

                // everything is ok, lets see if the backup exist...
                string strBakFileName =
                    System.IO.Path.Combine(MuleApplication.Instance.Preference.GetMuleDirectory(Mule.Preference.DefaultDirectoryEnum.EMULE_CONFIGDIR),
                        string.Format("{0}{1}", CLIENTS_MET_FILENAME, ".bak"));

                uint dwBakFileSize = 0;
                bool bCreateBackup = true;

                if (System.IO.File.Exists(strBakFileName))
                {
                    using (FileStream fs = new FileStream(strBakFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        dwBakFileSize = (uint)fs.Length;
                        if (dwBakFileSize > (uint)file.Length)
                        {
                            // the size of the backup was larger then the org. file, something is wrong here, don't overwrite old backup..
                            bCreateBackup = false;
                        }
                    }
                }
                //else: the backup doesn't exist, create it

                if (bCreateBackup)
                {
                    file.Close(); // close the file before copying

                    System.IO.File.Copy(strFileName, strBakFileName, true);

                    file = MpdObjectManager.CreateSafeBufferedFile(strFileName,
                    System.IO.FileMode.Open,
                    System.IO.FileAccess.Read,
                    System.IO.FileShare.None);

                    file.Seek(1, SeekOrigin.Begin); //set filepointer behind file version byte
                }

                uint count = file.ReadUInt32();

                uint dwExpired = MpdUtilities.Time() - 12960000; // today - 150 day
                uint cDeleted = 0;
                for (uint i = 0; i < count; i++)
                {
                    CreditStruct newcstruct = new CreditStruct();
                    if (version == (byte)VersionsEnum.CREDITFILE_VERSION_29)
                        ReadCreditStruct29a(file, newcstruct);
                    else
                        ReadCreditStruct(file, newcstruct);

                    if (newcstruct.nLastSeen < dwExpired)
                    {
                        cDeleted++;
                        continue;
                    }

                    ClientCredits newcredits =
                        MuleApplication.Instance.CoreObjectManager.CreateClientCredits(newcstruct);
                    m_mapClients[new MapCKey(newcredits.Key)] = newcredits;
                }
                file.Close();
            }
            catch (Exception error)
            {
                MpdUtilities.DebugLogError(error);
            }
        }

        private void ReadCreditStruct29a(SafeBufferedFile file, CreditStruct newcstruct)
        {
            file.Read(newcstruct.abyKey);
            newcstruct.nUploadedLo = file.ReadUInt32();	// uploaded TO him
            newcstruct.nDownloadedLo = file.ReadUInt32();	// downloaded from him
            newcstruct.nLastSeen = file.ReadUInt32();
            newcstruct.nUploadedHi = file.ReadUInt32();	// upload high 32
            newcstruct.nDownloadedHi = file.ReadUInt32();	// download high 32
            newcstruct.nReserved3 = file.ReadUInt16();
        }

        private void ReadCreditStruct(SafeBufferedFile file, CreditStruct newcstruct)
        {
            ReadCreditStruct29a(file, newcstruct);
            newcstruct.nKeySize = file.ReadUInt8();
            file.Read(newcstruct.abySecureIdent, 0, (int)CreditStruct.MAXPUBKEYSIZE);
        }

        protected void SaveList()
        {
            m_nLastSaved = MpdUtilities.GetTickCount();

            string name =
                System.IO.Path.Combine(MuleApplication.Instance.Preference.GetMuleDirectory(Mule.Preference.DefaultDirectoryEnum.EMULE_CONFIGDIR),
                    CLIENTS_MET_FILENAME);
            try
            {
                using (FileStream file = new FileStream(name, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    int count = m_mapClients.Count;
                    SafeMemFile memfile =
                        MpdObjectManager.CreateSafeMemFile(count * (16 + 5 * 4 + 1 * 2 + 1 + CreditStruct.MAXPUBKEYSIZE));

                    memfile.WriteUInt8((byte)VersionsEnum.CREDITFILE_VERSION);
                    Dictionary<MapCKey, ClientCredits>.Enumerator pos = m_mapClients.GetEnumerator();
                    count = 0;
                    while (pos.MoveNext())
                    {
                        ClientCredits cur_credit = pos.Current.Value;
                        if (cur_credit.GetUploadedTotal() != 0 || cur_credit.GetDownloadedTotal() != 0)
                        {
                            WriteCreditStruct(memfile, cur_credit.DataStruct);
                            count++;
                        }
                    }

                    memfile.WriteUInt32((uint)count);
                    file.Write(memfile.Buffer, 0, (int)memfile.Length);
                    file.Flush();
                    file.Close();
                    memfile.Close();
                }
            }
            catch (Exception error)
            {
                MpdUtilities.DebugLogError(error);
            }
        }

        private void WriteCreditStruct(SafeMemFile memfile, CreditStruct creditStruct)
        {
            memfile.WriteUInt32(creditStruct.nUploadedLo);	// uploaded TO him
            memfile.WriteUInt32(creditStruct.nDownloadedLo);	// downloaded from him
            memfile.WriteUInt32(creditStruct.nLastSeen);
            memfile.WriteUInt32(creditStruct.nUploadedHi);	// upload high 32
            memfile.WriteUInt32(creditStruct.nDownloadedHi);	// download high 32
            memfile.WriteUInt16(creditStruct.nReserved3);
            memfile.WriteUInt8(creditStruct.nKeySize);
            memfile.Write(creditStruct.abySecureIdent);
        }

        protected void InitalizeCrypting()
        {
            throw new NotImplementedException();
        }

        protected bool CreateKeyPair()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
