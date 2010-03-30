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
        private const byte CRYPT_CIP_REMOTECLIENT = 10;
        private const byte CRYPT_CIP_LOCALCLIENT = 20;
        private const byte CRYPT_CIP_NONECLIENT = 30;

        private Dictionary<MapCKey, ClientCredits> m_mapClients = new Dictionary<MapCKey, ClientCredits>();
        private uint m_nLastSaved;
        private RSAPKCS1SignatureFormatter m_pSignkey;
        private byte[] m_abyMyPublicKey = new byte[80];
        private byte m_nMyPublicKeyLen;
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

        public byte CreateSignature(ClientCredits pTarget,
            byte[] pachOutput,
            byte nMaxSize,
            uint ChallengeIP, byte byChaIPKind,
            RSAPKCS1SignatureFormatter sigkey)
        {
            // sigkey param is used for debug only
            if (sigkey == null)
                sigkey = m_pSignkey;

            // create a signature of the public key from pTarget
            byte nResult;
            if (!IsCryptoAvailable)
                return 0;
            try
            {
                byte[] abyBuffer = new byte[CreditStruct.MAXPUBKEYSIZE + 9];
                uint keylen = pTarget.SecIDKeyLen;
                Array.Copy(pTarget.SecureIdent, abyBuffer, keylen);
                // 4 additional bytes random data send from this client
                uint challenge = pTarget.CryptRndChallengeFrom;
                Array.Copy(BitConverter.GetBytes(challenge), 0,
                    abyBuffer, keylen, 4);
                ushort ChIpLen = 0;
                if (byChaIPKind != 0)
                {
                    ChIpLen = 5;
                    Array.Copy(BitConverter.GetBytes(ChallengeIP), 0,
                        abyBuffer, keylen + 4, 4);
                    abyBuffer[keylen + 4 + 4] = byChaIPKind;
                }

                byte[] tmpBuf = new byte[keylen + 4 + ChIpLen];
                Array.Copy(abyBuffer, tmpBuf, keylen + 4 + ChIpLen);
                byte[] outBuf = sigkey.CreateSignature(tmpBuf);

                nResult = (byte)outBuf.Length;

                if (outBuf.Length > nMaxSize)
                    nResult = nMaxSize;
                Array.Copy(outBuf, pachOutput, nResult);
            }
            catch (Exception ex)
            {
                MpdUtilities.DebugLogError(ex);
                nResult = 0;
            }
            return nResult;
        }

        public bool VerifyIdent(ClientCredits pTarget,
            byte[] pachSignature, byte nInputSize,
            uint dwForIP, byte byChaIPKind)
        {
            if (!IsCryptoAvailable)
            {
                pTarget.IdentState = IdentStateEnum.IS_NOTAVAILABLE;
                return false;
            }
            bool bResult;
            try
            {
                RSAPKCS1SignatureDeformatter pubkey =
                    MpdObjectManager.CreateRSAPKCS1V15SHA1Verifier(pTarget.SecureIdent, pTarget.SecIDKeyLen);
                // 4 additional bytes random data send from this client +5 bytes v2
                byte[] abyBuffer = new byte[CreditStruct.MAXPUBKEYSIZE + 9];
                Array.Copy(m_abyMyPublicKey, abyBuffer, m_nMyPublicKeyLen);
                uint challenge = pTarget.CryptRndChallengeFor;

                Array.Copy(BitConverter.GetBytes(challenge), 0,
                    abyBuffer, m_nMyPublicKeyLen, 4);

                // v2 security improvments (not supported by 29b, not used as default by 29c)
                byte nChIpSize = 0;
                if (byChaIPKind != 0)
                {
                    nChIpSize = 5;
                    uint ChallengeIP = 0;
                    switch (byChaIPKind)
                    {
                        case CRYPT_CIP_LOCALCLIENT:
                            ChallengeIP = dwForIP;
                            break;
                        case CRYPT_CIP_REMOTECLIENT:
                            if (MuleApplication.Instance.ServerConnect.ClientID == 0 ||
                                MuleApplication.Instance.ServerConnect.IsLowID)
                            {
                                ChallengeIP = MuleApplication.Instance.ServerConnect.LocalIP;
                            }
                            else
                                ChallengeIP = MuleApplication.Instance.ServerConnect.ClientID;
                            break;
                        case CRYPT_CIP_NONECLIENT: // maybe not supported in future versions
                            ChallengeIP = 0;
                            break;
                    }
                    Array.Copy(BitConverter.GetBytes(ChallengeIP), 0,
                        abyBuffer, m_nMyPublicKeyLen + 4, 4);
                    abyBuffer[m_nMyPublicKeyLen + 4 + 4] = byChaIPKind;
                }
                //v2 end

                byte[] hash = new byte[m_nMyPublicKeyLen + 4 + nChIpSize];
                Array.Copy(abyBuffer, hash, m_nMyPublicKeyLen + 4 + nChIpSize);

                byte[] sign = new byte[nInputSize];
                Array.Copy(pachSignature, sign, nInputSize);

                bResult = pubkey.VerifySignature(hash, sign);
            }
            catch (Exception ex)
            {
                MpdUtilities.DebugLogError(ex);
                bResult = false;
            }
            if (!bResult)
            {
                if (pTarget.IdentState == IdentStateEnum.IS_IDNEEDED)
                    pTarget.IdentState = IdentStateEnum.IS_IDFAILED;
            }
            else
            {
                pTarget.Verified(dwForIP);
            }
            return bResult;
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
            get { return m_nMyPublicKeyLen; }
        }

        public byte[] PublicKey
        {
            get { return m_abyMyPublicKey; }
        }

        public bool IsCryptoAvailable
        {
            get
            {
                return (m_nMyPublicKeyLen > 0 &&
                    m_pSignkey != null &&
                    MuleApplication.Instance.Preference.IsSecureIdentEnabled);
            }
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
            m_nMyPublicKeyLen = 0;
            Array.Clear(m_abyMyPublicKey, 0, 80); // not really needed; better for debugging tho
            m_pSignkey = null;
            if (!MuleApplication.Instance.Preference.IsSecureIdentEnabled)
                return;
            // check if keyfile is there
            bool bCreateNewKey = false;

            string filename =
                System.IO.Path.Combine(MuleApplication.Instance.Preference.GetMuleDirectory(Mule.Preference.DefaultDirectoryEnum.EMULE_CONFIGDIR),
                "crytkey.dat");
            if (System.IO.File.Exists(filename))
            {
                FileInfo info = new FileInfo(filename);

                if (info.Length == 0)
                    bCreateNewKey = true;
            }
            else
                bCreateNewKey = true;

            if (bCreateNewKey)
                CreateKeyPair();

            // load key
            try
            {
                string keyText = System.IO.File.ReadAllText(filename);
                byte[] key = Convert.FromBase64String(keyText);

                // load private key
                m_pSignkey = MpdObjectManager.CreateRSAPKCS1V15SHA1Signer(key);

                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider((int)MuleConstants.RSAKEYSIZE);
                rsa.ImportCspBlob(key);

                byte[] tmp = rsa.ExportCspBlob(false);
                Array.Copy(tmp, m_abyMyPublicKey, tmp.Length);
                m_nMyPublicKeyLen = (byte)tmp.Length;
            }
            catch (Exception ex)
            {
                m_pSignkey = null;
                MpdUtilities.DebugLogError(ex);
            }
        }

        protected bool CreateKeyPair()
        {
            try
            {
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider((int)MuleConstants.RSAKEYSIZE);

                byte[] key = rsa.ExportCspBlob(true);

                string keyText = Convert.ToBase64String(key);

                string filename =
                    System.IO.Path.Combine(MuleApplication.Instance.Preference.GetMuleDirectory(Mule.Preference.DefaultDirectoryEnum.EMULE_CONFIGDIR),
                    "crytkey.dat");

                System.IO.File.WriteAllText(filename, keyText);

                return true;
            }
            catch (Exception ex)
            {
                MpdUtilities.DebugLogError(ex);
                return false;
            }
        }
        #endregion
    }
}
