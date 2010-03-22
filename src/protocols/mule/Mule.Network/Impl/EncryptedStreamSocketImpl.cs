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
using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using CryptoPP;
using Mpd.Generic;
using Mpd.Generic.IO;
using Mpd.Utilities;

using Mule.File;
using System.Net;
using Mule.Preference;

namespace Mule.Network.Impl
{
    abstract class EncryptedStreamSocketImpl : AsyncSocketImpl, EncryptedStreamSocket
    {
        #region Static Fields
        private static readonly AutoSeededRandomPool cryptRandomGen_ =
            new AutoSeededRandomPool();

        private static readonly byte[] dh768_p_ =
        {
            0xF2,0xBF,0x52,0xC5,0x5F,0x58,0x7A,0xDD,0x53,0x71,0xA9,0x36,
            0xE8,0x86,0xEB,0x3C,0x62,0x17,0xA3,0x3E,0xC3,0x4C,0xB4,0x0D,
            0xC7,0x3A,0x41,0xA6,0x43,0xAF,0xFC,0xE7,0x21,0xFC,0x28,0x63,
            0x66,0x53,0x5B,0xDB,0xCE,0x25,0x9F,0x22,0x86,0xDA,0x4A,0x91,
            0xB2,0x07,0xCB,0xAA,0x52,0x55,0xD4,0xF6,0x1C,0xCE,0xAE,0xD4,
            0x5A,0xD5,0xE0,0x74,0x7D,0xF7,0x78,0x18,0x28,0x10,0x5F,0x34,
            0x0F,0x76,0x23,0x87,0xF8,0x8B,0x28,0x91,0x42,0xFB,0x42,0x68,
            0x8F,0x05,0x15,0x0F,0x54,0x8B,0x5F,0x43,0x6A,0xF7,0x0D,0xF3,
        };
        #endregion

        #region Constructor
        public EncryptedStreamSocketImpl() : 
            base(new IPEndPoint(0, 0).AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            streamCryptState_ =
                MuleApplication.Instance.Preference.IsClientCryptLayerSupported ? StreamCryptStateEnum.ECS_UNKNOWN : StreamCryptStateEnum.ECS_NONE;
            negotiatingState_ = NegotiatingStateEnum.ONS_NONE;
            rc4ReceiveKey_ = null;
            rc4SendKey_ = null;
            obfuscationBytesReceived_ = 0;
            fullReceive_ = true;
            DbgByEncryptionSupported = 0xFF;
            DbgByEncryptionRequested = 0xFF;
            DbgByEncryptionMethodSet = 0xFF;
            receiveBytesWanted_ = 0;
            fiReceiveBuffer_ = null;
            fiSendBuffer_ = null;
            encryptionMethod_ = EncryptionMethodsEnum.ENM_OBFUSCATION;
            randomKeyPart_ = 0;
            serverCrypt_ = false;
            cryptDHA_ = CryptoPPObjectManager.CreateInteger();
        }

        #endregion

        #region EncryptedStreamSocket Members
        public void SetConnectionEncryption(bool bEnabled, byte[] pTargetClientHash, bool bServerConnection)
        {
            if (streamCryptState_ != StreamCryptStateEnum.ECS_UNKNOWN &&
                streamCryptState_ != StreamCryptStateEnum.ECS_NONE)
            {
                if (!(streamCryptState_ == StreamCryptStateEnum.ECS_NONE) || bEnabled)
                {
                    Debug.Assert(false);
                }
                return;
            }

            Debug.Assert(rc4SendKey_ == null);
            Debug.Assert(rc4ReceiveKey_ == null);

            if (bEnabled && pTargetClientHash != null && !bServerConnection)
            {
                streamCryptState_ = StreamCryptStateEnum.ECS_PENDING;
                // create obfuscation keys, see on top for key format

                // use the crypt random generator
                randomKeyPart_ = cryptRandomGen_.GenerateWord32();

                byte[] achKeyData = new byte[21];
                MpdUtilities.Md4Cpy(achKeyData, pTargetClientHash);

                Array.Copy(BitConverter.GetBytes(randomKeyPart_), 0, achKeyData, 17, 4);

                achKeyData[16] = Convert.ToByte(MuleConstants.MAGICVALUE_REQUESTER);

                MD5 md5 = MD5.Create();

                rc4SendKey_ = MuleUtilities.RC4CreateKey(md5.ComputeHash(achKeyData), 16, null);

                achKeyData[16] = Convert.ToByte(MuleConstants.MAGICVALUE_SERVER);

                rc4ReceiveKey_ = MuleUtilities.RC4CreateKey(md5.ComputeHash(achKeyData), 16, null);
            }
            else if (bServerConnection && bEnabled)
            {
                serverCrypt_ = true;
                streamCryptState_ = StreamCryptStateEnum.ECS_PENDING_SERVER;
            }
            else
            {
                Debug.Assert(!bEnabled);
                streamCryptState_ = StreamCryptStateEnum.ECS_NONE;
            }
        }

        public int RealReceivedBytes
        {
            get { return obfuscationBytesReceived_; }
        }

        public bool IsObfusicating
        {
            get
            {
                return streamCryptState_ == StreamCryptStateEnum.ECS_ENCRYPTING &&
                encryptionMethod_ == EncryptionMethodsEnum.ENM_OBFUSCATION;
            }
        }

        public bool IsServerCryptEnabledConnection
        {
            get { return serverCrypt_; }
        }

        public byte DbgByEncryptionSupported
        { get; set; }

        public byte DbgByEncryptionRequested
        { get; set; }

        public byte DbgByEncryptionMethodSet
        { get; set; }

        #endregion

        #region Protects
        public override int Send(byte[] lpBuf, int offset, int nBufLen, SocketFlags nFlags)
        {
            if (!IsEncryptionLayerReady)
            {
                Debug.Assert(false); // must be a bug
                return 0;
            }
            else if (serverCrypt_ &&
                streamCryptState_ == StreamCryptStateEnum.ECS_ENCRYPTING &&
                fiSendBuffer_ != null)
            {
                Debug.Assert(negotiatingState_ == NegotiatingStateEnum.ONS_BASIC_SERVER_DELAYEDSENDING);
                // handshakedata was delayed to put it into one frame with the first paypload to the server
                // do so now with the payload attached
                int nRes = SendNegotiatingData(lpBuf, Convert.ToUInt32(nBufLen), Convert.ToUInt32(offset + nBufLen));
                Debug.Assert(nRes != SOCKET_ERROR);
                return nBufLen;	// report a full send, even if we didn't for some reason - the data is know in our buffer and will be handled later
            }
            else if (negotiatingState_ == NegotiatingStateEnum.ONS_BASIC_SERVER_DELAYEDSENDING)
                Debug.Assert(false);

            if (streamCryptState_ == StreamCryptStateEnum.ECS_UNKNOWN)
            {
                //this happens when the encryption option was not set on a outgoing connection
                //or if we try to send before receiving on a incoming connection - both shouldn't happen
                streamCryptState_ = StreamCryptStateEnum.ECS_NONE;
                MpdUtilities.DebugLogError(("CEncryptedStreamSocket: Overwriting State ECS_UNKNOWN with ECS_NONE because of premature Send() (%s)"), DbgGetIPString());
            }

            return base.Send(lpBuf, offset, nBufLen, nFlags);
        }

        public override int Receive(byte[] lpBuf, int offset, int nBufLen, SocketFlags nFlags)
        {
            obfuscationBytesReceived_ = base.Receive(lpBuf, offset, nBufLen, nFlags);

            fullReceive_ = obfuscationBytesReceived_ == (uint)nBufLen;

            if (obfuscationBytesReceived_ == SOCKET_ERROR || obfuscationBytesReceived_ <= 0)
            {
                return obfuscationBytesReceived_;
            }
            switch (streamCryptState_)
            {
                case StreamCryptStateEnum.ECS_NONE: // disabled, just pass it through
                    return obfuscationBytesReceived_;
                case StreamCryptStateEnum.ECS_PENDING:
                case StreamCryptStateEnum.ECS_PENDING_SERVER:
                    Debug.Assert(false);
                    MpdUtilities.DebugLogError(("CEncryptedStreamSocket Received data before sending on outgoing connection"));
                    streamCryptState_ = StreamCryptStateEnum.ECS_NONE;
                    return obfuscationBytesReceived_;
                case StreamCryptStateEnum.ECS_UNKNOWN:
                    {
                        int nRead = 1;
                        bool bNormalHeader = false;
                        switch (lpBuf[offset])
                        {
                            case MuleConstants.OP_EDONKEYPROT:
                            case MuleConstants.OP_PACKEDPROT:
                            case MuleConstants.OP_EMULEPROT:
                                bNormalHeader = true;
                                break;
                        }
                        if (!bNormalHeader)
                        {
                            StartNegotiation(false);
                            int nNegRes = Negotiate(lpBuf, offset + nRead, obfuscationBytesReceived_ - nRead);
                            if (nNegRes == (-1))
                                return 0;
                            nRead += nNegRes;
                            if (nRead != obfuscationBytesReceived_)
                            {
                                // this means we have more data then the current negotiation step required (or there is a bug) and this should never happen
                                // (note: even if it just finished the handshake here, there still can be no data left, since the other client didnt received our response yet)
                                MpdUtilities.DebugLogError(("CEncryptedStreamSocket: Client %s sent more data then expected while negotiating, disconnecting (1)"), DbgGetIPString());
                                OnError(Convert.ToInt32(EMSocketErrorCodeEnum.ERR_ENCRYPTION));
                            }
                            return 0;
                        }
                        else
                        {
                            // doesn't seems to be encrypted
                            streamCryptState_ = StreamCryptStateEnum.ECS_NONE;

                            // if we require an encrypted connection, cut the connection here. This shouldn't happen that often
                            // at least with other up-to-date eMule clients because they check for incompability before connecting if possible
                            if (MuleApplication.Instance.Preference.IsClientCryptLayerRequired)
                            {
                                // TODO: Remove me when i have been solved
                                // Even if the Require option is enabled, we currently have to accept unencrypted connection which are made
                                // for lowid/firewall checks from servers and other from us selected client. Otherwise, this option would
                                // always result in a lowid/firewalled status. This is of course not nice, but we can't avoid this walkarround
                                // untill servers and kad completely support encryption too, which will at least for kad take a bit
                                // only exception is the .ini option ClientCryptLayerRequiredStrict which will even ignore test connections
                                // Update: New server now support encrypted callbacks

                                IPEndPoint remote = RemoteEndPoint as IPEndPoint;
                                uint address = BitConverter.ToUInt32(remote.Address.GetAddressBytes(), 0);

                                if (MuleApplication.Instance.Preference.IsClientCryptLayerRequiredStrict ||
                                    (!MuleApplication.Instance.ServerConnect.AwaitingTestFromIP(address)
                                    && !MuleApplication.Instance.ClientList.IsKadFirewallCheckIP(address)))
                                {
                                    MpdUtilities.AddDebugLogLine(EDebugLogPriority.DLP_DEFAULT, false, ("Rejected incoming connection because Obfuscation was required but not used %s"), DbgGetIPString());
                                    OnError(Convert.ToInt32(EMSocketErrorCodeEnum.ERR_ENCRYPTION_NOTALLOWED));
                                    return 0;
                                }
                                else
                                    MpdUtilities.AddDebugLogLine(EDebugLogPriority.DLP_DEFAULT, false, ("Incoming unencrypted firewallcheck connection permitted despite RequireEncryption setting  - %s"), DbgGetIPString());
                            }

                            return obfuscationBytesReceived_; // buffer was unchanged, we can just pass it through
                        }
                    }
                case StreamCryptStateEnum.ECS_ENCRYPTING:
                    // basic obfuscation enabled and set, so decrypt and pass along
                    MuleUtilities.RC4Crypt(lpBuf, offset, lpBuf, offset, Convert.ToUInt32(obfuscationBytesReceived_), rc4ReceiveKey_);
                    return obfuscationBytesReceived_;
                case StreamCryptStateEnum.ECS_NEGOTIATING:
                    {
                        int nRead = Negotiate(lpBuf, offset, obfuscationBytesReceived_);
                        if (nRead == (-1))
                            return 0;
                        else if (nRead != obfuscationBytesReceived_ &&
                            streamCryptState_ != StreamCryptStateEnum.ECS_ENCRYPTING)
                        {
                            // this means we have more data then the current negotiation step required (or there is a bug) and this should never happen
                            MpdUtilities.DebugLogError(("CEncryptedStreamSocket: Client %s sent more data then expected while negotiating, disconnecting (2)"), DbgGetIPString());
                            OnError(Convert.ToInt32(EMSocketErrorCodeEnum.ERR_ENCRYPTION));
                            return 0;
                        }
                        else if (nRead != (uint)obfuscationBytesReceived_ && streamCryptState_ == StreamCryptStateEnum.ECS_ENCRYPTING)
                        {
                            // we finished the handshake and if we this was an outgoing connection it is allowed (but strange and unlikely) that the client sent payload
                            MpdUtilities.DebugLogWarning(("CEncryptedStreamSocket: Client %s has finished the handshake but also sent payload on a outgoing connection"), DbgGetIPString());
                            Array.Copy(lpBuf, offset + nRead, lpBuf, offset + 0, obfuscationBytesReceived_ - nRead);
                            return obfuscationBytesReceived_ - nRead;
                        }
                        else
                            return 0;
                    }
                default:
                    Debug.Assert(false);
                    return obfuscationBytesReceived_;
            }
        }

        protected override void OnSend(int nErrorCode)
        {
            // if the socket just connected and this is outgoing, we might want to start the handshake here
            if (streamCryptState_ == StreamCryptStateEnum.ECS_PENDING ||
                streamCryptState_ == StreamCryptStateEnum.ECS_PENDING_SERVER)
            {
                StartNegotiation(true);
                return;
            }
            // check if we have negotiating data pending
            if (fiSendBuffer_ != null)
            {
                Debug.Assert(streamCryptState_ >= StreamCryptStateEnum.ECS_NEGOTIATING);
                SendNegotiatingData(null, 0);
            }
        }

        protected string DbgGetIPString()
        {
            return (RemoteEndPoint as IPEndPoint).Address.ToString();
        }

        protected void CryptPrepareSendData(byte[] pBuffer, uint nLen)
        {
            if (!IsEncryptionLayerReady)
            {
                Debug.Assert(false); // must be a bug
                return;
            }
            if (streamCryptState_ == StreamCryptStateEnum.ECS_UNKNOWN)
            {
                //this happens when the encryption option was not set on a outgoing connection
                //or if we try to send before receiving on a incoming connection - both shouldn't happen
                streamCryptState_ = StreamCryptStateEnum.ECS_NONE;
                MpdUtilities.DebugLogError(("CEncryptedStreamSocket: Overwriting State ECS_UNKNOWN with ECS_NONE because of premature Send() (%s)"), DbgGetIPString());
            }
            if (streamCryptState_ == StreamCryptStateEnum.ECS_ENCRYPTING)
                MuleUtilities.RC4Crypt(pBuffer, pBuffer, nLen, rc4SendKey_);
        }

        protected bool IsEncryptionLayerReady
        {
            get
            {
                return ((streamCryptState_ == StreamCryptStateEnum.ECS_NONE ||
                    streamCryptState_ == StreamCryptStateEnum.ECS_ENCRYPTING ||
                    streamCryptState_ == StreamCryptStateEnum.ECS_UNKNOWN)
                    && (fiSendBuffer_ == null ||
                    (serverCrypt_ &&
                    negotiatingState_ == NegotiatingStateEnum.ONS_BASIC_SERVER_DELAYEDSENDING)));
            }
        }

        protected byte GetSemiRandomNotProtocolMarker()
        {
            byte bySemiRandomNotProtocolMarker = 0;
            int i;
            for (i = 0; i < 128; i++)
            {
                bySemiRandomNotProtocolMarker = cryptRandomGen_.GenerateByte();
                bool bOk = false;
                switch (bySemiRandomNotProtocolMarker)
                { // not allowed values
                    case MuleConstants.OP_EDONKEYPROT:
                    case MuleConstants.OP_PACKEDPROT:
                    case MuleConstants.OP_EMULEPROT:
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
                // either we have _real_ bad luck or the randomgenerator is a bit messed up
                Debug.Assert(false);
                bySemiRandomNotProtocolMarker = 0x01;
            }
            return bySemiRandomNotProtocolMarker;
        }

        protected int obfuscationBytesReceived_;
        protected StreamCryptStateEnum streamCryptState_;
        protected EncryptionMethodsEnum encryptionMethod_;
        protected bool fullReceive_;
        protected bool serverCrypt_;
        #endregion

        #region Privates
        private int Negotiate(byte[] pBuffer, int nLen)
        {
            return Negotiate(pBuffer, 0, nLen);
        }

        private int Negotiate(byte[] pBuffer, int offset, int nLen)
        {
            int nRead = 0;
            Debug.Assert(receiveBytesWanted_ > 0);
            try
            {
                while (negotiatingState_ != NegotiatingStateEnum.ONS_COMPLETE && receiveBytesWanted_ > 0)
                {
                    if (receiveBytesWanted_ > 512)
                    {
                        Debug.Assert(false);
                        return 0;
                    }

                    if (fiReceiveBuffer_ == null)
                    {
                        byte[] pReceiveBuffer = new byte[512]; // use a fixed size buffer
                        fiReceiveBuffer_ = MpdObjectManager.CreateSafeMemFile(pReceiveBuffer);
                    }
                    int nToRead = Math.Min(Convert.ToInt32(nLen) - nRead, Convert.ToInt32(receiveBytesWanted_));
                    fiReceiveBuffer_.Write(pBuffer, nRead, nToRead);
                    nRead += nToRead;
                    receiveBytesWanted_ -= Convert.ToUInt32(nToRead);
                    if (receiveBytesWanted_ > 0)
                        return nRead;
                    uint nCurrentBytesLen = (uint)fiReceiveBuffer_.Position;

                    if (negotiatingState_ != NegotiatingStateEnum.ONS_BASIC_CLIENTA_RANDOMPART &&
                        negotiatingState_ != NegotiatingStateEnum.ONS_BASIC_SERVER_DHANSWER)
                    { // don't have the keys yet
                        byte[] pCryptBuffer = fiReceiveBuffer_.Buffer;
                        MuleUtilities.RC4Crypt(pCryptBuffer, pCryptBuffer, nCurrentBytesLen, rc4ReceiveKey_);
                    }
                    fiReceiveBuffer_.SeekToBegin();

                    switch (negotiatingState_)
                    {
                        case NegotiatingStateEnum.ONS_NONE: // would be a bug
                            Debug.Assert(false);
                            return 0;
                        case NegotiatingStateEnum.ONS_BASIC_CLIENTA_RANDOMPART:
                            {
                                Debug.Assert(rc4ReceiveKey_ == null);

                                byte[] achKeyData = new byte[21];
                                MpdUtilities.Md4Cpy(achKeyData, MuleApplication.Instance.Preference.UserHash);
                                achKeyData[16] = Convert.ToByte(MuleConstants.MAGICVALUE_REQUESTER);
                                fiReceiveBuffer_.Read(achKeyData, 17, 4); // random key part sent from remote client

                                MD5 md5 = MD5.Create();
                                rc4ReceiveKey_ = MuleUtilities.RC4CreateKey(md5.ComputeHash(achKeyData), 16, null);
                                achKeyData[16] = Convert.ToByte(MuleConstants.MAGICVALUE_SERVER);
                                rc4SendKey_ = MuleUtilities.RC4CreateKey(md5.ComputeHash(achKeyData), 16, null);

                                negotiatingState_ = NegotiatingStateEnum.ONS_BASIC_CLIENTA_MAGICVALUE;
                                receiveBytesWanted_ = 4;
                                break;
                            }
                        case NegotiatingStateEnum.ONS_BASIC_CLIENTA_MAGICVALUE:
                            {
                                uint dwValue = fiReceiveBuffer_.ReadUInt32();
                                if (dwValue == MuleConstants.MAGICVALUE_SYNC)
                                {
                                    // yup, the one or the other way it worked, this is an encrypted stream
                                    //DEBUG_ONLY( MpdUtilities.DebugLog(("Received proper magic value, clientIP: %s"), DbgGetIPString()) );
                                    // set the receiver key
                                    negotiatingState_ = NegotiatingStateEnum.ONS_BASIC_CLIENTA_METHODTAGSPADLEN;
                                    receiveBytesWanted_ = 3;
                                }
                                else
                                {
                                    MpdUtilities.DebugLogError(("CEncryptedStreamSocket: Received wrong magic value from clientIP %s on a supposly encrytped stream / Wrong Header"), DbgGetIPString());
                                    OnError(Convert.ToInt32(EMSocketErrorCodeEnum.ERR_ENCRYPTION));
                                    return (-1);
                                }
                                break;
                            }
                        case NegotiatingStateEnum.ONS_BASIC_CLIENTA_METHODTAGSPADLEN:
                            DbgByEncryptionSupported = fiReceiveBuffer_.ReadUInt8();
                            DbgByEncryptionRequested = fiReceiveBuffer_.ReadUInt8();
                            if (DbgByEncryptionRequested != Convert.ToByte(EncryptionMethodsEnum.ENM_OBFUSCATION))
                                MpdUtilities.AddDebugLogLine(EDebugLogPriority.DLP_LOW, false, ("CEncryptedStreamSocket: Client %s preffered unsupported encryption method (%i)"), DbgGetIPString(), DbgByEncryptionRequested);
                            receiveBytesWanted_ = fiReceiveBuffer_.ReadUInt8();
                            negotiatingState_ = NegotiatingStateEnum.ONS_BASIC_CLIENTA_PADDING;
                            if (receiveBytesWanted_ > 0)
                                break;
                            else
                                goto case NegotiatingStateEnum.ONS_BASIC_CLIENTA_PADDING;
                        case NegotiatingStateEnum.ONS_BASIC_CLIENTA_PADDING:
                            {
                                // ignore the random bytes, send the response, set status complete
                                SafeMemFile fileResponse = MpdObjectManager.CreateSafeMemFile(26);
                                fileResponse.WriteUInt32(MuleConstants.MAGICVALUE_SYNC);
                                byte bySelectedEncryptionMethod = Convert.ToByte(EncryptionMethodsEnum.ENM_OBFUSCATION); // we do not support any further encryption in this version, so no need to look which the other client preferred
                                fileResponse.WriteUInt8(bySelectedEncryptionMethod);

                                IPEndPoint remoteEp = RemoteEndPoint as IPEndPoint;

                                byte byPaddingLen = 
                                    MuleApplication.Instance.ServerConnect.AwaitingTestFromIP(BitConverter.ToUInt32(remoteEp.Address.GetAddressBytes(), 0)) 
                                        ? Convert.ToByte(16) : 
                                        Convert.ToByte(MuleApplication.Instance.Preference.CryptTCPPaddingLength + 1);
                                byte byPadding = Convert.ToByte(cryptRandomGen_.GenerateByte() % byPaddingLen);

                                fileResponse.WriteUInt8(byPadding);
                                for (int i = 0; i < byPadding; i++)
                                    fileResponse.WriteUInt8(MpdUtilities.GetRandomUInt8());
                                SendNegotiatingData(fileResponse.Buffer, (uint)fileResponse.Length);
                                negotiatingState_ = NegotiatingStateEnum.ONS_COMPLETE;
                                streamCryptState_ = StreamCryptStateEnum.ECS_ENCRYPTING;
                                //DEBUG_ONLY( MpdUtilities.DebugLog(("CEncryptedStreamSocket: Finished Obufscation handshake with client %s (incoming)"), DbgGetIPString()) );
                                break;
                            }
                        case NegotiatingStateEnum.ONS_BASIC_CLIENTB_MAGICVALUE:
                            {
                                if (fiReceiveBuffer_.ReadUInt32() != MuleConstants.MAGICVALUE_SYNC)
                                {
                                    MpdUtilities.DebugLogError(("CEncryptedStreamSocket: EncryptedstreamSyncError: Client sent wrong Magic Value as answer, cannot complete handshake (%s)"), DbgGetIPString());
                                    OnError(Convert.ToInt32(EMSocketErrorCodeEnum.ERR_ENCRYPTION));
                                    return (-1);
                                }
                                negotiatingState_ = NegotiatingStateEnum.ONS_BASIC_CLIENTB_METHODTAGSPADLEN;
                                receiveBytesWanted_ = 2;
                                break;
                            }
                        case NegotiatingStateEnum.ONS_BASIC_CLIENTB_METHODTAGSPADLEN:
                            {
                                DbgByEncryptionMethodSet = fiReceiveBuffer_.ReadUInt8();
                                if (DbgByEncryptionMethodSet != Convert.ToByte(EncryptionMethodsEnum.ENM_OBFUSCATION))
                                {
                                    MpdUtilities.DebugLogError(("CEncryptedStreamSocket: Client %s set unsupported encryption method (%i), handshake failed"),
                                        DbgGetIPString(), DbgByEncryptionMethodSet);
                                    OnError(Convert.ToInt32(EMSocketErrorCodeEnum.ERR_ENCRYPTION));
                                    return (-1);
                                }
                                receiveBytesWanted_ = fiReceiveBuffer_.ReadUInt8();
                                negotiatingState_ = NegotiatingStateEnum.ONS_BASIC_CLIENTB_PADDING;
                                if (receiveBytesWanted_ > 0)
                                    break;
                                else
                                    goto case NegotiatingStateEnum.ONS_BASIC_CLIENTB_PADDING;
                            }
                        case NegotiatingStateEnum.ONS_BASIC_CLIENTB_PADDING:
                            // ignore the random bytes, the handshake is complete
                            negotiatingState_ = NegotiatingStateEnum.ONS_COMPLETE;
                            streamCryptState_ = StreamCryptStateEnum.ECS_ENCRYPTING;
                            //DEBUG_ONLY( MpdUtilities.DebugLog(("CEncryptedStreamSocket: Finished Obufscation handshake with client %s (outgoing)"), DbgGetIPString()) );
                            break;
                        case NegotiatingStateEnum.ONS_BASIC_SERVER_DHANSWER:
                            {
                                Debug.Assert(!cryptDHA_.IsZero);
                                byte[] aBuffer = new byte[MuleConstants.PRIMESIZE_BYTES + 1];
                                fiReceiveBuffer_.Read(aBuffer, 0, Convert.ToInt32(MuleConstants.PRIMESIZE_BYTES));
                                CryptoPP.Integer cryptDHAnswer =
                                    CryptoPPObjectManager.CreateInteger(aBuffer, MuleConstants.PRIMESIZE_BYTES);
                                CryptoPP.Integer cryptDHPrime =
                                    CryptoPPObjectManager.CreateInteger(dh768_p_, MuleConstants.PRIMESIZE_BYTES);  // our fixed prime
                                CryptoPP.Integer cryptResult =
                                    CryptoPPUtilities.AExpBModC(cryptDHAnswer, cryptDHA_, cryptDHPrime);

                                cryptDHA_.Int32Value = 0;
                                Array.Clear(aBuffer, 0, aBuffer.Length);
                                Debug.Assert(cryptResult.MinEncodedSize() <= MuleConstants.PRIMESIZE_BYTES);

                                // create the keys
                                cryptResult.Encode(aBuffer, MuleConstants.PRIMESIZE_BYTES);
                                aBuffer[MuleConstants.PRIMESIZE_BYTES] = Convert.ToByte(MuleConstants.MAGICVALUE_REQUESTER);
                                MD5 md5 = MD5.Create();

                                rc4SendKey_ = MuleUtilities.RC4CreateKey(md5.ComputeHash(aBuffer), 16, null);
                                aBuffer[MuleConstants.PRIMESIZE_BYTES] = Convert.ToByte(MuleConstants.MAGICVALUE_SERVER);
                                rc4ReceiveKey_ = MuleUtilities.RC4CreateKey(md5.ComputeHash(aBuffer), 16, null);

                                negotiatingState_ = NegotiatingStateEnum.ONS_BASIC_SERVER_MAGICVALUE;
                                receiveBytesWanted_ = 4;
                                break;
                            }
                        case NegotiatingStateEnum.ONS_BASIC_SERVER_MAGICVALUE:
                            {
                                uint dwValue = fiReceiveBuffer_.ReadUInt32();
                                if (dwValue == MuleConstants.MAGICVALUE_SYNC)
                                {
                                    // yup, the one or the other way it worked, this is an encrypted stream
                                    MpdUtilities.DebugLog(("Received proper magic value after DH-Agreement from Serverconnection IP: %s"), DbgGetIPString());
                                    // set the receiver key
                                    negotiatingState_ = NegotiatingStateEnum.ONS_BASIC_SERVER_METHODTAGSPADLEN;
                                    receiveBytesWanted_ = 3;
                                }
                                else
                                {
                                    MpdUtilities.DebugLogError(("CEncryptedStreamSocket: Received wrong magic value after DH-Agreement from Serverconnection"), DbgGetIPString());
                                    OnError(Convert.ToInt32(EMSocketErrorCodeEnum.ERR_ENCRYPTION));
                                    return (-1);
                                }
                                break;
                            }
                        case NegotiatingStateEnum.ONS_BASIC_SERVER_METHODTAGSPADLEN:
                            DbgByEncryptionSupported = fiReceiveBuffer_.ReadUInt8();
                            DbgByEncryptionRequested = fiReceiveBuffer_.ReadUInt8();
                            if (DbgByEncryptionRequested != Convert.ToByte(EncryptionMethodsEnum.ENM_OBFUSCATION))
                                MpdUtilities.AddDebugLogLine(EDebugLogPriority.DLP_LOW, false, ("CEncryptedStreamSocket: Server %s preffered unsupported encryption method (%i)"), DbgGetIPString(), DbgByEncryptionRequested);
                            receiveBytesWanted_ = fiReceiveBuffer_.ReadUInt8();
                            negotiatingState_ = NegotiatingStateEnum.ONS_BASIC_SERVER_PADDING;
                            if (receiveBytesWanted_ > 16)
                                MpdUtilities.AddDebugLogLine(EDebugLogPriority.DLP_LOW, false, ("CEncryptedStreamSocket: Server %s sent more than 16 (%i) padding bytes"), DbgGetIPString(), receiveBytesWanted_);
                            if (receiveBytesWanted_ > 0)
                                break;
                            else
                                goto case NegotiatingStateEnum.ONS_BASIC_SERVER_PADDING;
                        case NegotiatingStateEnum.ONS_BASIC_SERVER_PADDING:
                            {
                                // ignore the random bytes (they are decrypted already), send the response, set status complete
                                SafeMemFile fileResponse = MpdObjectManager.CreateSafeMemFile(26);
                                fileResponse.WriteUInt32(MuleConstants.MAGICVALUE_SYNC);
                                byte bySelectedEncryptionMethod =
                                    Convert.ToByte(EncryptionMethodsEnum.ENM_OBFUSCATION); // we do not support any further encryption in this version, so no need to look which the other client preferred
                                fileResponse.WriteUInt8(bySelectedEncryptionMethod);
                                byte byPadding = (byte)(cryptRandomGen_.GenerateByte() % 16);
                                fileResponse.WriteUInt8(byPadding);
                                for (int i = 0; i < byPadding; i++)
                                    fileResponse.WriteUInt8(MpdUtilities.GetRandomUInt8());

                                negotiatingState_ = NegotiatingStateEnum.ONS_BASIC_SERVER_DELAYEDSENDING;
                                SendNegotiatingData(fileResponse.Buffer, (uint)fileResponse.Length, 0, true); // don't actually send it right now, store it in our sendbuffer
                                streamCryptState_ = StreamCryptStateEnum.ECS_ENCRYPTING;
                                MpdUtilities.DebugLog(("CEncryptedStreamSocket: Finished DH Obufscation handshake with Server %s"), DbgGetIPString());
                                break;
                            }
                        default:
                            Debug.Assert(false);
                            break;
                    }
                    fiReceiveBuffer_.SeekToBegin();
                }

                fiReceiveBuffer_ = null;
                return nRead;
            }
            catch (Exception)
            {
                Debug.Assert(false);
                OnError(Convert.ToInt32(EMSocketErrorCodeEnum.ERR_ENCRYPTION));
                fiReceiveBuffer_ = null;
                return (-1);
            }
        }

        private void StartNegotiation(bool bOutgoing)
        {
            if (!bOutgoing)
            {
                negotiatingState_ = NegotiatingStateEnum.ONS_BASIC_CLIENTA_RANDOMPART;
                streamCryptState_ = StreamCryptStateEnum.ECS_NEGOTIATING;
                receiveBytesWanted_ = 4;
            }
            else if (streamCryptState_ == StreamCryptStateEnum.ECS_PENDING)
            {
                SafeMemFile fileRequest = MpdObjectManager.CreateSafeMemFile(29);
                byte bySemiRandomNotProtocolMarker = GetSemiRandomNotProtocolMarker();
                fileRequest.WriteUInt8(bySemiRandomNotProtocolMarker);
                fileRequest.WriteUInt32(randomKeyPart_);
                fileRequest.WriteUInt32(MuleConstants.MAGICVALUE_SYNC);
                byte bySupportedEncryptionMethod = Convert.ToByte(EncryptionMethodsEnum.ENM_OBFUSCATION); // we do not support any further encryption in this version
                fileRequest.WriteUInt8(bySupportedEncryptionMethod);
                fileRequest.WriteUInt8(bySupportedEncryptionMethod); // so we also prefer this one
                byte byPadding = Convert.ToByte(cryptRandomGen_.GenerateByte() %
                    (MuleApplication.Instance.Preference.CryptTCPPaddingLength + 1));
                fileRequest.WriteUInt8(byPadding);
                for (int i = 0; i < byPadding; i++)
                    fileRequest.WriteUInt8(cryptRandomGen_.GenerateByte());

                negotiatingState_ = NegotiatingStateEnum.ONS_BASIC_CLIENTB_MAGICVALUE;
                streamCryptState_ = StreamCryptStateEnum.ECS_NEGOTIATING;
                receiveBytesWanted_ = 4;

                SendNegotiatingData(fileRequest.Buffer, (uint)fileRequest.Length, 5);
            }
            else if (streamCryptState_ == StreamCryptStateEnum.ECS_PENDING_SERVER)
            {
                SafeMemFile fileRequest = MpdObjectManager.CreateSafeMemFile(113);
                byte bySemiRandomNotProtocolMarker = GetSemiRandomNotProtocolMarker();
                fileRequest.WriteUInt8(bySemiRandomNotProtocolMarker);

                cryptDHA_.Randomize(cryptRandomGen_, MuleConstants.DHAGREEMENT_A_BITS); // our random a
                Debug.Assert(cryptDHA_.MinEncodedSize() <= MuleConstants.DHAGREEMENT_A_BITS / 8);
                CryptoPP.Integer cryptDHPrime = CryptoPPObjectManager.CreateInteger(dh768_p_, MuleConstants.PRIMESIZE_BYTES);  // our fixed prime
                // calculate g^a % p
                CryptoPP.Integer cryptDHGexpAmodP =
                    CryptoPPUtilities.AExpBModC(CryptoPPObjectManager.CreateInteger(2), cryptDHA_, cryptDHPrime);

                Debug.Assert(cryptDHA_.MinEncodedSize() <= MuleConstants.PRIMESIZE_BYTES);
                // put the result into a buffer
                byte[] aBuffer = new byte[MuleConstants.PRIMESIZE_BYTES];
                cryptDHGexpAmodP.Encode(aBuffer, MuleConstants.PRIMESIZE_BYTES);

                fileRequest.Write(aBuffer);
                byte byPadding = (byte)(cryptRandomGen_.GenerateByte() % 16); // add random padding
                fileRequest.WriteUInt8(byPadding);
                for (int i = 0; i < byPadding; i++)
                    fileRequest.WriteUInt8(cryptRandomGen_.GenerateByte());

                negotiatingState_ = NegotiatingStateEnum.ONS_BASIC_SERVER_DHANSWER;
                streamCryptState_ = StreamCryptStateEnum.ECS_NEGOTIATING;
                receiveBytesWanted_ = 96;

                SendNegotiatingData(fileRequest.Buffer, (uint)fileRequest.Length, (uint)fileRequest.Length);
            }
            else
            {
                Debug.Assert(false);
                streamCryptState_ = StreamCryptStateEnum.ECS_NONE;
                return;
            }
        }

        private int SendNegotiatingData(byte[] lpBuf, uint nBufLen)
        {
            return SendNegotiatingData(lpBuf, nBufLen, 0, false);
        }

        private int SendNegotiatingData(byte[] lpBuf, uint nBufLen, uint nStartCryptFromByte)
        {
            return SendNegotiatingData(lpBuf, nBufLen, nStartCryptFromByte, false);
        }

        private int SendNegotiatingData(byte[] lpBuf, uint nBufLen, uint nStartCryptFromByte, bool bDelaySend)
        {
            Debug.Assert(streamCryptState_ == StreamCryptStateEnum.ECS_NEGOTIATING ||
                streamCryptState_ == StreamCryptStateEnum.ECS_ENCRYPTING);
            Debug.Assert(nStartCryptFromByte <= nBufLen);
            Debug.Assert(negotiatingState_ == NegotiatingStateEnum.ONS_BASIC_SERVER_DELAYEDSENDING ||
                !bDelaySend);

            byte[] pBuffer = null;
            bool bProcess = false;
            if (lpBuf != null)
            {
                pBuffer = new byte[nBufLen];
                if (nStartCryptFromByte > 0)
                    Array.Copy(lpBuf, pBuffer, nStartCryptFromByte);
                if (nBufLen - nStartCryptFromByte > 0)
                    MuleUtilities.RC4Crypt(lpBuf, Convert.ToInt32(nStartCryptFromByte),
                        pBuffer, Convert.ToInt32(nStartCryptFromByte),
                        nBufLen - nStartCryptFromByte,
                        rc4SendKey_);
                if (fiSendBuffer_ != null)
                {
                    // we already have data pending. Attach it and try to send
                    if (negotiatingState_ == NegotiatingStateEnum.ONS_BASIC_SERVER_DELAYEDSENDING)
                        negotiatingState_ = NegotiatingStateEnum.ONS_COMPLETE;
                    else
                        Debug.Assert(false);
                    fiSendBuffer_.Seek(0, System.IO.SeekOrigin.End);
                    fiSendBuffer_.Write(pBuffer, 0, Convert.ToInt32(nBufLen));
                    pBuffer = null;
                    nStartCryptFromByte = 0;
                    bProcess = true; // we want to try to send it right now
                }
            }
            if (lpBuf == null || bProcess)
            {
                // this call is for processing pending data
                if (fiSendBuffer_ == null || nStartCryptFromByte != 0)
                {
                    Debug.Assert(false);
                    return 0;							// or not
                }
                nBufLen = Convert.ToUInt32(fiSendBuffer_.Length);
                pBuffer = fiSendBuffer_.Buffer;
                fiSendBuffer_ = null;
            }
            Debug.Assert(fiSendBuffer_ == null);
            int result = 0;
            if (!bDelaySend)
                result = base.Send(pBuffer, Convert.ToInt32(nBufLen));
            if (result == SOCKET_ERROR || bDelaySend)
            {
                fiSendBuffer_ = MpdObjectManager.CreateSafeMemFile(128);
                fiSendBuffer_.Write(pBuffer, 0, Convert.ToInt32(nBufLen));
                return result;
            }
            else
            {
                if (result < nBufLen)
                {
                    fiSendBuffer_ = MpdObjectManager.CreateSafeMemFile(128);
                    fiSendBuffer_.Write(pBuffer, Convert.ToInt32(result), Convert.ToInt32(nBufLen - result));
                }
                return result;
            }
        }

        private RC4Key rc4SendKey_;
        private RC4Key rc4ReceiveKey_;
        private NegotiatingStateEnum negotiatingState_;
        private SafeMemFile fiReceiveBuffer_;
        private uint receiveBytesWanted_;
        private SafeMemFile fiSendBuffer_;
        private uint randomKeyPart_;
        private CryptoPP.Integer cryptDHA_;
        #endregion

    }
}
