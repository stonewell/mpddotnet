using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Utilities;

namespace Mule.Core.Impl
{
    class ClientCreditsImpl : ClientCredits
    {
        #region Fields
        private CreditStruct credits_;
        private byte[] publicKey_ = new byte[80];
        private byte publicKeyLen_;
        private uint identIP_;
        private uint secureWaitTime_;
        private uint unSecureWaitTime_;
        private uint waitTimeIP_;
        #endregion

        #region Constructors
        public ClientCreditsImpl(CreditStruct in_credits)
        {
            credits_ = in_credits;
            InitalizeIdent();
            unSecureWaitTime_ = 0;
            secureWaitTime_ = 0;
            waitTimeIP_ = 0;
        }

        public ClientCreditsImpl(byte[] key)
        {
            credits_ = new CreditStruct();
            MpdUtilities.Md4Cpy(credits_.abyKey, key);
            InitalizeIdent();
            unSecureWaitTime_ = MpdUtilities.GetTickCount();
            secureWaitTime_ = MpdUtilities.GetTickCount();
            waitTimeIP_ = 0;
        }
        #endregion

        #region ClientCredits Members
        public IdentStateEnum IdentState { get; set; }

        public byte[] Key
        {
            get { return credits_.abyKey; }
        }

        public byte[] SecureIdent
        {
            get { return publicKey_; }
        }

        public byte SecIDKeyLen
        {
            get { return publicKeyLen_; }
        }

        public CreditStruct DataStruct
        {
            get { return credits_; }
        }

        public void ClearWaitStartTime()
        {
            unSecureWaitTime_ = 0;
            secureWaitTime_ = 0;
        }

        public void AddDownloaded(uint bytes, uint dwForIP)
        {
            if ((GetCurrentIdentState(dwForIP) == IdentStateEnum.IS_IDFAILED ||
                GetCurrentIdentState(dwForIP) == IdentStateEnum.IS_IDBADGUY ||
                GetCurrentIdentState(dwForIP) == IdentStateEnum.IS_IDNEEDED) &&
                MuleApplication.Instance.ClientCredits.IsCryptoAvailable)
            {
                return;
            }

            //encode
            ulong current = (((ulong)credits_.nDownloadedHi << 32) | credits_.nDownloadedLo) + bytes;

            //recode
            credits_.nDownloadedLo = (uint)current;
            credits_.nDownloadedHi = (uint)(current >> 32);
        }

        public void AddUploaded(uint bytes, uint dwForIP)
        {
            if ((GetCurrentIdentState(dwForIP) == IdentStateEnum.IS_IDFAILED ||
                GetCurrentIdentState(dwForIP) == IdentStateEnum.IS_IDBADGUY ||
                GetCurrentIdentState(dwForIP) == IdentStateEnum.IS_IDNEEDED) &&
                MuleApplication.Instance.ClientCredits.IsCryptoAvailable)
            {
                return;
            }

            //encode
            ulong current = (((ulong)credits_.nUploadedHi << 32) | credits_.nUploadedLo) + bytes;

            //recode
            credits_.nUploadedLo = (uint)current;
            credits_.nUploadedHi = (uint)(current >> 32);
        }

        public ulong GetUploadedTotal()
        {
            return ((ulong)credits_.nUploadedHi << 32) | credits_.nUploadedLo;
        }

        public ulong GetDownloadedTotal()
        {
            return ((ulong)credits_.nDownloadedHi << 32) | credits_.nDownloadedLo;
        }

        public float GetScoreRatio(uint dwForIP)
        {
            // check the client ident status
            if ((GetCurrentIdentState(dwForIP) == IdentStateEnum.IS_IDFAILED ||
                GetCurrentIdentState(dwForIP) == IdentStateEnum.IS_IDBADGUY ||
                GetCurrentIdentState(dwForIP) == IdentStateEnum.IS_IDNEEDED) &&
                MuleApplication.Instance.ClientCredits.IsCryptoAvailable)
            {
                // bad guy - no credits for you
                return 1.0F;
            }

            if (GetDownloadedTotal() < 1048576)
                return 1.0F;
            float result = 0.0F;
            if (GetUploadedTotal() == 0)
                result = 10.0F;
            else
                result = (float)(((double)GetDownloadedTotal() * 2.0) / (double)GetUploadedTotal());

            // exponential calcualtion of the max multiplicator based on uploaded data (9.2MB = 3.34, 100MB = 10.0)
            float result2 = 0.0F;
            result2 = (float)(GetDownloadedTotal() / 1048576.0);
            result2 += 2.0F;
            result2 = (float)Math.Sqrt(result2);

            // linear calcualtion of the max multiplicator based on uploaded data for the first chunk (1MB = 1.01, 9.2MB = 3.34)
            float result3 = 10.0F;
            if (GetDownloadedTotal() < 9646899)
            {
                result3 = (((float)(GetDownloadedTotal() - 1048576) / 8598323.0F) * 2.34F) + 1.0F;
            }

            // take the smallest result
            result = Math.Min(result, Math.Min(result2, result3));

            if (result < 1.0F)
                return 1.0F;
            else if (result > 10.0F)
                return 10.0F;
            return result;
        }

        public void SetLastSeen()
        {
            credits_.nLastSeen = MpdUtilities.Time();
        }

        public bool SetSecureIdent(byte[] pachIdent, byte nIdentLen)
        {
            if (CreditStruct.MAXPUBKEYSIZE < nIdentLen || credits_.nKeySize != 0)
                return false;
            Array.Copy(pachIdent, publicKey_, nIdentLen);
            publicKeyLen_ = nIdentLen;
            IdentState = IdentStateEnum.IS_IDNEEDED;
            return true;
        }

        public IdentStateEnum GetCurrentIdentState(uint dwForIP)
        {
            if (IdentState != IdentStateEnum.IS_IDENTIFIED)
                return IdentState;
            else
            {
                if (dwForIP == identIP_)
                    return IdentStateEnum.IS_IDENTIFIED;
                else
                    return IdentStateEnum.IS_IDBADGUY;
                // mod note: clients which just reconnected after an IP change and have to ident yet will also have this state for 1-2 seconds
                //		 so don't try to spam such clients with "bad guy" messages (besides: spam messages are always bad)
            }
        }

        public uint GetSecureWaitStartTime(uint dwForIP)
        {
            if (unSecureWaitTime_ == 0 || secureWaitTime_ == 0)
                SetSecWaitStartTime(dwForIP);

            if (credits_.nKeySize != 0)
            {	// this client is a SecureHash Client
                if (GetCurrentIdentState(dwForIP) == IdentStateEnum.IS_IDENTIFIED)
                { // good boy
                    return secureWaitTime_;
                }
                else
                {	// not so good boy
                    if (dwForIP == waitTimeIP_)
                    {
                        return unSecureWaitTime_;
                    }
                    else
                    {	// bad boy
                        unSecureWaitTime_ = MpdUtilities.GetTickCount();
                        waitTimeIP_ = dwForIP;
                        return unSecureWaitTime_;
                    }
                }
            }
            else
            {	// not a SecureHash Client - handle it like before for now (no security checks)
                return unSecureWaitTime_;
            }
        }

        public void SetSecWaitStartTime(uint dwForIP)
        {
            unSecureWaitTime_ = MpdUtilities.GetTickCount() - 1;
            secureWaitTime_ = MpdUtilities.GetTickCount() - 1;
            waitTimeIP_ = dwForIP;
        }

        public uint CryptRndChallengeFor
        {
            get;
            set;
        }

        public uint CryptRndChallengeFrom
        {
            get;
            set;
        }

        #endregion

        #region Protected
        public void Verified(uint dwForIP)
        {
            identIP_ = dwForIP;
            // client was verified, copy the keyto store him if not done already
            if (credits_.nKeySize == 0)
            {
                credits_.nKeySize = publicKeyLen_;
                Array.Copy(publicKey_, credits_.abySecureIdent, publicKeyLen_);
                if (GetDownloadedTotal() > 0)
                {
                    // for security reason, we have to delete all prior credits here
                    credits_.nDownloadedHi = 0;
                    credits_.nDownloadedLo = 1;
                    credits_.nUploadedHi = 0;
                    credits_.nUploadedLo = 1; // in order to safe this client, set 1 byte
                }
            }
            IdentState = IdentStateEnum.IS_IDENTIFIED;
        }
        #endregion

        #region Privates
        private void InitalizeIdent()
        {
            if (credits_.nKeySize == 0)
            {
                Array.Clear(publicKey_, 0, publicKey_.Length);
                publicKeyLen_ = 0;
                IdentState = IdentStateEnum.IS_NOTAVAILABLE;
            }
            else
            {
                publicKeyLen_ = credits_.nKeySize;
                Array.Copy(credits_.abySecureIdent, publicKey_, publicKeyLen_);
                IdentState = IdentStateEnum.IS_IDNEEDED;
            }
            CryptRndChallengeFor = 0;
            CryptRndChallengeFrom = 0;
            identIP_ = 0;
        }
        #endregion
    }
}
