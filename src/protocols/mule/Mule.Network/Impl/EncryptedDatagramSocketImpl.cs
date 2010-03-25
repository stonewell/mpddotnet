using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Mpd.Utilities;
using System.Diagnostics;
using System.Security.Cryptography;
using CryptoPP;

namespace Mule.Network.Impl
{
    class EncryptedDatagramSocketImpl : AsyncSocketImpl, EncryptedDatagramSocket
    {
        #region Constants
        private const int CRYPT_HEADER_WITHOUTPADDING = 8;
        private const byte MAGICVALUE_UDP = 91;
        private const uint MAGICVALUE_UDP_SYNC_CLIENT = 0x395F2EC1;
        private const uint MAGICVALUE_UDP_SYNC_SERVER = 0x13EF24D5;
        private const byte MAGICVALUE_UDP_SERVERCLIENT = 0xA5;
        private const byte MAGICVALUE_UDP_CLIENTSERVER = 0x6B;

        private static readonly AutoSeededRandomPool cryptRandomGen_ =
            new AutoSeededRandomPool();
        #endregion

        #region Constructors
        public EncryptedDatagramSocketImpl() :
            base(new IPEndPoint(IPAddress.Any, 0).AddressFamily, SocketType.Dgram, ProtocolType.Udp)
        {
        }
        #endregion

        #region EncryptedDatagramSocket Members

        public virtual int DecryptReceivedClient(byte[] pbyBufIn, int nBufLen,
            out byte[] ppbyBufOut, uint dwIP, out uint nReceiverVerifyKey, out uint nSenderVerifyKey)
        {
            int nResult = nBufLen;
            ppbyBufOut = pbyBufIn;
            nReceiverVerifyKey = 0;
            nSenderVerifyKey = 0;

            if (nResult <= CRYPT_HEADER_WITHOUTPADDING /*|| !MuleApplication.Instance.Preference.IsClientCryptLayerSupported()*/)
                return nResult;

            switch (pbyBufIn[0])
            {
                case MuleConstants.OP_EMULEPROT:
                case MuleConstants.OP_KADEMLIAPACKEDPROT:
                case MuleConstants.OP_KADEMLIAHEADER:
                case MuleConstants.OP_UDPRESERVEDPROT1:
                case MuleConstants.OP_UDPRESERVEDPROT2:
                case MuleConstants.OP_PACKEDPROT:
                    return nResult; // no encrypted packet (see description on top)
            }

            // might be an encrypted packet, try to decrypt
            RC4Key keyReceiveKey = null;
            uint dwValue = 0;
            // check the marker bit which type this packet could be and which key to test first, this is only an indicator since old clients have it set random
            // see the header for marker bits explanation
            byte byCurrentTry = (byte)(((pbyBufIn[0] & 0x03) == 3) ? 1 : (pbyBufIn[0] & 0x03));
            byte byTries;
            if (MuleApplication.Instance.KadEngine.Preference == null)
            {
                // if kad never run, no point in checking anything except for ed2k encryption
                byTries = 1;
                byCurrentTry = 1;
            }
            else
                byTries = 3;
            bool bKadRecvKeyUsed = false;
            bool bKad = false;
            do
            {
                byTries--;
                MD5 md5 = MD5.Create();
                byte[] rawHash = null;
                if (byCurrentTry == 0)
                {
                    // kad packet with NodeID as key
                    bKad = true;
                    bKadRecvKeyUsed = false;
                    if (MuleApplication.Instance.KadEngine.Preference != null)
                    {
                        byte[] achKeyData = new byte[18];
                        Array.Copy(MuleApplication.Instance.KadEngine.Preference.KadID.Bytes, 0, achKeyData, 0, 16);
                        Array.Copy(pbyBufIn, 1, achKeyData, 16, 2); // random key part sent from remote client
                        rawHash = md5.ComputeHash(achKeyData);
                    }
                }
                else if (byCurrentTry == 1)
                {
                    // ed2k packet
                    bKad = false;
                    bKadRecvKeyUsed = false;
                    byte[] achKeyData = new byte[23];
                    MpdUtilities.Md4Cpy(achKeyData, MuleApplication.Instance.Preference.UserHash);
                    achKeyData[20] = MAGICVALUE_UDP;
                    Array.Copy(BitConverter.GetBytes(dwIP), 0, achKeyData, 16, 4);
                    Array.Copy(pbyBufIn, 1, achKeyData, 21, 2); // random key part sent from remote client
                    rawHash = md5.ComputeHash(achKeyData);
                }
                else if (byCurrentTry == 2)
                {
                    // kad packet with ReceiverKey as key
                    bKad = true;
                    bKadRecvKeyUsed = true;
                    if (MuleApplication.Instance.KadEngine.Preference != null)
                    {
                        byte[] achKeyData = new byte[6];
                        Array.Copy(BitConverter.GetBytes(MuleApplication.Instance.KadEngine.Preference.GetUDPVerifyKey(dwIP)),
                            achKeyData, 4);
                        Array.Copy(pbyBufIn, 1, achKeyData, 4, 2); // random key part sent from remote client
                        rawHash = md5.ComputeHash(achKeyData);
                    }
                }
                else
                    Debug.Assert(false);

                MuleUtilities.RC4CreateKey(rawHash, 16, ref keyReceiveKey, true);
                byte[] outBuf = new byte[4];
                MuleUtilities.RC4Crypt(pbyBufIn, 3, outBuf, 0, 4, keyReceiveKey);
                dwIP = BitConverter.ToUInt32(outBuf, 0);
                byCurrentTry = (byte)((byCurrentTry + 1) % 3);
            } while (dwValue != MAGICVALUE_UDP_SYNC_CLIENT && byTries > 0); // try to decrypt as ed2k as well as kad packet if needed (max 3 rounds)

            if (dwValue == MAGICVALUE_UDP_SYNC_CLIENT)
            {
                // yup this is an encrypted packet
                // debugoutput notices
                // the following cases are "allowed" but shouldn't happen given that there is only our implementation yet
                if (bKad && (pbyBufIn[0] & 0x01) != 0)
                    MpdUtilities.DebugLog(
                        string.Format("Received obfuscated UDP packet from clientIP: {0} with wrong key marker bits (kad packet, ed2k bit)", MpdUtilities.IP2String(dwIP)));
                else if (bKad && !bKadRecvKeyUsed && (pbyBufIn[0] & 0x02) != 0)
                    MpdUtilities.DebugLog(
                        string.Format("Received obfuscated UDP packet from clientIP: {0} with wrong key marker bits (kad packet, nodeid key, recvkey bit)", MpdUtilities.IP2String(dwIP)));
                else if (bKad && bKadRecvKeyUsed && (pbyBufIn[0] & 0x02) == 0)
                    MpdUtilities.DebugLog(
                        string.Format("Received obfuscated UDP packet from clientIP: {0} with wrong key marker bits (kad packet, recvkey key, nodeid bit)", MpdUtilities.IP2String(dwIP)));

                byte byPadLen;
                byte[] outBuf = new byte[1];
                MuleUtilities.RC4Crypt(pbyBufIn, 7, outBuf, 0, 1, keyReceiveKey);
                byPadLen = outBuf[0];

                nResult -= CRYPT_HEADER_WITHOUTPADDING;
                if (nResult <= byPadLen)
                {
                    MpdUtilities.DebugLogError(
                        string.Format("Invalid obfuscated UDP packet from clientIP: {0}, Paddingsize ({1}) larger than received bytes",
                            MpdUtilities.IP2String(dwIP), byPadLen));
                    return nBufLen; // pass through, let the Receivefunction do the errorhandling on this junk
                }
                if (byPadLen > 0)
                    MuleUtilities.RC4Crypt(null, null, byPadLen, keyReceiveKey);
                nResult -= byPadLen;

                if (bKad)
                {
                    if (nResult <= 8)
                    {
                        MpdUtilities.DebugLogError(
                            string.Format("Obfuscated Kad packet with mismatching size (verify keys missing) received from clientIP: {0}",
                                MpdUtilities.IP2String(dwIP)));
                        return nBufLen; // pass through, let the Receivefunction do the errorhandling on this junk;
                    }
                    // read the verify keys
                    outBuf = new byte[4];
                    MuleUtilities.RC4Crypt(pbyBufIn, (int)(CRYPT_HEADER_WITHOUTPADDING + byPadLen),
                        outBuf, 0, 4, keyReceiveKey);
                    nReceiverVerifyKey = BitConverter.ToUInt32(outBuf, 0);
                    MuleUtilities.RC4Crypt(pbyBufIn, (int)(CRYPT_HEADER_WITHOUTPADDING + byPadLen + 4),
                        outBuf, 0, 4, keyReceiveKey);
                    nSenderVerifyKey = BitConverter.ToUInt32(outBuf, 0);
                    nResult -= 8;
                }

                ppbyBufOut = new byte[nResult];
                Array.Copy(pbyBufIn, (nBufLen - nResult), ppbyBufOut, 0, nResult);

                MuleUtilities.RC4Crypt(ppbyBufOut, ppbyBufOut, (uint)nResult, keyReceiveKey);
                MuleApplication.Instance.Statistics.AddDownDataOverheadCrypt((uint)(nBufLen - nResult));
                //DEBUG_ONLY( MpdUtilities.DebugLog(("Received obfuscated UDP packet from clientIP: %s, Key: %s, RKey: %u, SKey: %u"), MpdUtilities.IP2String(dwIP), bKad ? (bKadRecvKeyUsed ? ("ReceiverKey") : ("NodeID")) : ("UserHash")
                //	, nReceiverVerifyKey != 0 ? *nReceiverVerifyKey : 0, nSenderVerifyKey != 0 ? *nSenderVerifyKey : 0) );
                return nResult; // done
            }
            else
            {
                MpdUtilities.DebugLogWarning(
                    string.Format("Obfuscated packet expected but magicvalue mismatch on UDP packet from clientIP: {0}, Possible RecvKey: {1}",
                    MpdUtilities.IP2String(dwIP), MuleApplication.Instance.KadEngine.Preference.GetUDPVerifyKey(dwIP)));
                return nBufLen; // pass through, let the Receivefunction do the errorhandling on this junk
            }
        }

        public virtual int EncryptSendClient(ref byte[] ppbyBuf, int nBufLen, byte[] pachClientHashOrKadID, bool bKad, uint nReceiverVerifyKey, uint nSenderVerifyKey)
        {
            Debug.Assert(MuleApplication.Instance.PublicIP != 0 || bKad);
            Debug.Assert(MuleApplication.Instance.Preference.IsClientCryptLayerSupported);
            Debug.Assert(pachClientHashOrKadID != null || nReceiverVerifyKey != 0);
            Debug.Assert((nReceiverVerifyKey == 0 && nSenderVerifyKey == 0) || bKad);

            byte byPadLen = 0;			// padding disabled for UDP currently
            int nCryptHeaderLen = byPadLen + CRYPT_HEADER_WITHOUTPADDING + (bKad ? 8 : 0);

            int nCryptedLen = nBufLen + nCryptHeaderLen;
            byte[] pachCryptedBuffer = new byte[nCryptedLen];
            bool bKadRecKeyUsed = false;

            ushort nRandomKeyPart = (ushort)cryptRandomGen_.GenerateWord32(0x0000, 0xFFFF);
            MD5 md5 = MD5.Create();
            byte[] rawHash = null;
            if (bKad)
            {
                if ((pachClientHashOrKadID == null ||
                    MpdUtilities.IsNullMd4(pachClientHashOrKadID)) &&
                    nReceiverVerifyKey != 0)
                {
                    bKadRecKeyUsed = true;
                    byte[] achKeyData = new byte[6];
                    Array.Copy(BitConverter.GetBytes(nReceiverVerifyKey), achKeyData, 4);
                    Array.Copy(BitConverter.GetBytes(nRandomKeyPart), 0,
                        achKeyData, 4, 2);
                    rawHash = md5.ComputeHash(achKeyData);
                    //DEBUG_ONLY( DebugLog(_T("Creating obfuscated Kad packet encrypted by ReceiverKey (%u)"), nReceiverVerifyKey) );  
                }
                else if (pachClientHashOrKadID != null && !MpdUtilities.IsNullMd4(pachClientHashOrKadID))
                {
                    byte[] achKeyData = new byte[18];
                    MpdUtilities.Md4Cpy(achKeyData, pachClientHashOrKadID);
                    Array.Copy(BitConverter.GetBytes(nRandomKeyPart), 0,
                        achKeyData, 16, 2);
                    rawHash = md5.ComputeHash(achKeyData);
                    //DEBUG_ONLY( DebugLog(_T("Creating obfuscated Kad packet encrypted by Hash/NodeID %s"), md4str(pachClientHashOrKadID)) );  
                }
                else
                {
                    ppbyBuf = null;
                    Debug.Assert(false);
                    return nBufLen;
                }
            }
            else
            {
                byte[] achKeyData = new byte[23];
                MpdUtilities.Md4Cpy(achKeyData, pachClientHashOrKadID);
                uint dwIP = (uint)MuleApplication.Instance.PublicIP;
                Array.Copy(BitConverter.GetBytes(dwIP), 0, achKeyData, 16, 4);
                Array.Copy(BitConverter.GetBytes(nRandomKeyPart), 0, achKeyData, 21, 2);
                achKeyData[20] = MAGICVALUE_UDP;
                rawHash = md5.ComputeHash(achKeyData);
            }
            RC4Key keySendKey = null;
            MuleUtilities.RC4CreateKey(rawHash, 16, ref keySendKey, true);

            // create the semi random byte encryption header
            byte bySemiRandomNotProtocolMarker = 0;
            int i;
            for (i = 0; i < 128; i++)
            {
                bySemiRandomNotProtocolMarker = cryptRandomGen_.GenerateByte();
                bySemiRandomNotProtocolMarker =
                    (byte)(bKad ? (bySemiRandomNotProtocolMarker & 0xFE) :
                    (bySemiRandomNotProtocolMarker | 0x01)); // set the ed2k/kad marker bit
                if (bKad)
                    bySemiRandomNotProtocolMarker =
                        (byte)(bKadRecKeyUsed ? ((bySemiRandomNotProtocolMarker & 0xFE) | 0x02) :
                        (bySemiRandomNotProtocolMarker & 0xFC)); // set the ed2k/kad and nodeid/reckey markerbit
                else
                    bySemiRandomNotProtocolMarker = (byte)(bySemiRandomNotProtocolMarker | 0x01); // set the ed2k/kad marker bit

                bool bOk = false;
                switch (bySemiRandomNotProtocolMarker)
                { // not allowed values
                    case MuleConstants.OP_EMULEPROT:
                    case MuleConstants.OP_KADEMLIAPACKEDPROT:
                    case MuleConstants.OP_KADEMLIAHEADER:
                    case MuleConstants.OP_UDPRESERVEDPROT1:
                    case MuleConstants.OP_UDPRESERVEDPROT2:
                    case MuleConstants.OP_PACKEDPROT:
                        break;
                    default:
                        bOk = true;
                        break;
                }
                if (bOk)
                    break;
            }
            if (i >= 128)
            {
                // either we have _really_ bad luck or the randomgenerator is a bit messed up
                Debug.Assert(false);
                bySemiRandomNotProtocolMarker = 0x01;
            }

            uint dwMagicValue = MAGICVALUE_UDP_SYNC_CLIENT;
            pachCryptedBuffer[0] = bySemiRandomNotProtocolMarker;
            Array.Copy(BitConverter.GetBytes(nRandomKeyPart), 0, pachCryptedBuffer, 1, 2);
            MuleUtilities.RC4Crypt(BitConverter.GetBytes(dwMagicValue), 0, pachCryptedBuffer, 3, 4, keySendKey);
            MuleUtilities.RC4Crypt(BitConverter.GetBytes(byPadLen), 0, pachCryptedBuffer, 7, 1, keySendKey);

            Random rand = new Random();
            for (int j = 0; j < byPadLen; j++)
            {
                byte byRand = (byte)rand.Next(255);	// they actually dont really need to be random, but it doesn't hurts either
                MuleUtilities.RC4Crypt(BitConverter.GetBytes(byRand), 0,
                    pachCryptedBuffer, CRYPT_HEADER_WITHOUTPADDING + j, 1, keySendKey);
            }

            if (bKad)
            {
                MuleUtilities.RC4Crypt(BitConverter.GetBytes(nReceiverVerifyKey), 0,
                    pachCryptedBuffer, CRYPT_HEADER_WITHOUTPADDING + byPadLen, 4, keySendKey);
                MuleUtilities.RC4Crypt(BitConverter.GetBytes(nSenderVerifyKey), 0,
                    pachCryptedBuffer, CRYPT_HEADER_WITHOUTPADDING + byPadLen + 4, 4, keySendKey);
            }

            MuleUtilities.RC4Crypt(ppbyBuf, 0,
                pachCryptedBuffer, nCryptHeaderLen, (uint)nBufLen, keySendKey);
            ppbyBuf = pachCryptedBuffer;

            MuleApplication.Instance.Statistics.AddUpDataOverheadCrypt((uint)(nCryptedLen - nBufLen));
            return nCryptedLen;
        }

        public virtual int DecryptReceivedServer(byte[] pbyBufIn, int nBufLen, out byte[] ppbyBufOut, uint dwBaseKey, uint dbgIP)
        {
            int nResult = nBufLen;
            ppbyBufOut = pbyBufIn;

            if (nResult <= CRYPT_HEADER_WITHOUTPADDING ||
                !MuleApplication.Instance.Preference.IsServerCryptLayerUDPEnabled ||
                dwBaseKey == 0)
                return nResult;

            if (pbyBufIn[0] == MuleConstants.OP_EDONKEYPROT)
                return nResult; // no encrypted packet (see description on top)

            // might be an encrypted packet, try to decrypt
            byte[] achKeyData = new byte[7];
            Array.Copy(BitConverter.GetBytes(dwBaseKey), achKeyData, 4);
            achKeyData[4] = MAGICVALUE_UDP_SERVERCLIENT;
            Array.Copy(pbyBufIn, 1, achKeyData, 5, 2); // random key part sent from remote server
            MD5 md5 = MD5.Create();
            byte[] rawHash = md5.ComputeHash(achKeyData);
            RC4Key keyReceiveKey = null;
            MuleUtilities.RC4CreateKey(rawHash, 16, ref keyReceiveKey, true);

            byte[] dwValueBuf = new byte[4];
            MuleUtilities.RC4Crypt(pbyBufIn, 3, dwValueBuf, 0, 4, keyReceiveKey);
            uint dwValue = BitConverter.ToUInt32(dwValueBuf, 0);

            if (dwValue == MAGICVALUE_UDP_SYNC_SERVER)
            {
                // yup this is an encrypted packet
                byte[] byPadLenBuf = new byte[1];
                MuleUtilities.RC4Crypt(pbyBufIn, 7, byPadLenBuf, 0, 1, keyReceiveKey);
                byte byPadLen = byPadLenBuf[0];
                byPadLen &= 15;
                nResult -= CRYPT_HEADER_WITHOUTPADDING;
                if (nResult <= byPadLen)
                {
                    return nBufLen; // pass through, let the Receivefunction do the errorhandling on this junk
                }
                if (byPadLen > 0)
                    MuleUtilities.RC4Crypt(null, null, byPadLen, keyReceiveKey);
                nResult -= byPadLen;
                ppbyBufOut = new byte[nResult];
                Array.Copy(pbyBufIn, (nBufLen - nResult), ppbyBufOut, 0, nResult);
                MuleUtilities.RC4Crypt(ppbyBufOut, ppbyBufOut, (uint)nResult, keyReceiveKey);

                MuleApplication.Instance.Statistics.AddDownDataOverheadCrypt((uint)(nBufLen - nResult));
                return nResult; // done
            }
            else
            {
                return nBufLen; // pass through, let the Receivefunction do the errorhandling on this junk
            }
        }

        public virtual int EncryptSendServer(ref byte[] ppbyBuf, int nBufLen, uint dwBaseKey)
        {
            Debug.Assert(MuleApplication.Instance.Preference.IsServerCryptLayerUDPEnabled);
            Debug.Assert(dwBaseKey != 0);

            byte byPadLen = 0;			// padding disabled for UDP currently
            int nCryptedLen = nBufLen + byPadLen + CRYPT_HEADER_WITHOUTPADDING;
            byte[] pachCryptedBuffer = new byte[nCryptedLen];

            ushort nRandomKeyPart = (ushort)cryptRandomGen_.GenerateWord32(0x0000, 0xFFFF);

            byte[] achKeyData = new byte[7];
            Array.Copy(BitConverter.GetBytes(dwBaseKey), achKeyData, 4);
            achKeyData[4] = MAGICVALUE_UDP_CLIENTSERVER;
            Array.Copy(BitConverter.GetBytes(nRandomKeyPart), 0, achKeyData, 5, 2);
            MD5 md5 = MD5.Create();
            byte[] rawHash = md5.ComputeHash(achKeyData);
            RC4Key keySendKey = null;
            MuleUtilities.RC4CreateKey(rawHash, 16, ref keySendKey, true);

            // create the semi random byte encryption header
            byte bySemiRandomNotProtocolMarker = 0;
            int i;
            for (i = 0; i < 128; i++)
            {
                bySemiRandomNotProtocolMarker = cryptRandomGen_.GenerateByte();
                if (bySemiRandomNotProtocolMarker != MuleConstants.OP_EDONKEYPROT) // not allowed values
                    break;
            }
            if (i >= 128)
            {
                // either we have _real_ bad luck or the randomgenerator is a bit messed up
                Debug.Assert(false);
                bySemiRandomNotProtocolMarker = 0x01;
            }

            uint dwMagicValue = MAGICVALUE_UDP_SYNC_SERVER;
            pachCryptedBuffer[0] = bySemiRandomNotProtocolMarker;
            Array.Copy(BitConverter.GetBytes(nRandomKeyPart), 0, pachCryptedBuffer, 1, 2);
            MuleUtilities.RC4Crypt(BitConverter.GetBytes(dwMagicValue), 0,
                pachCryptedBuffer, 3, 4, keySendKey);
            MuleUtilities.RC4Crypt(new byte[] { byPadLen }, 0, pachCryptedBuffer, 7, 1, keySendKey);

            Random rand = new Random();
            for (int j = 0; j < byPadLen; j++)
            {
                byte byRand = (byte)rand.Next(255);	// they actually dont really need to be random, but it doesn't hurts either
                MuleUtilities.RC4Crypt(new byte[] { byRand }, 0,
                    pachCryptedBuffer, CRYPT_HEADER_WITHOUTPADDING + j, 1, keySendKey);
            }
            MuleUtilities.RC4Crypt(ppbyBuf, 0,
                pachCryptedBuffer, CRYPT_HEADER_WITHOUTPADDING + byPadLen, (uint)nBufLen, keySendKey);

            ppbyBuf = pachCryptedBuffer;

            MuleApplication.Instance.Statistics.AddUpDataOverheadCrypt((uint)(nCryptedLen - nBufLen));
            return nCryptedLen;
        }

        #endregion
    }
}
