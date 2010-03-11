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
using Mule.File;
using Mule.Core.Network.AsynchSocket.Impl;
using Mule.Definitions;
using Mpd.Generic.Types.IO;

namespace Mule.Core.Network.Impl
{
    abstract class EncryptedStreamSocketImpl : AsyncSocketExImpl, EncryptedStreamSocket
    {
        #region EncryptedStreamSocket Members

        public void SetConnectionEncryption(bool bEnabled, byte[] pTargetClientHash, bool bServerConnection)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public uint RealReceivedBytes
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool IsObfusicating
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool IsServerCryptEnabledConnection
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public byte DbgByEncryptionSupported
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public byte DbgByEncryptionRequested
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public byte DbgByEncryptionMethodSet
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        #endregion

        protected int Send(byte[] lpBuf, int nBufLen, int nFlags)
        {
            return -1;
        }

        protected int Receive(byte[] lpBuf, int nBufLen, int nFlags)
        {
            return -1;
        }

        protected abstract void OnError(int nErrorCode);

        protected virtual void OnSend(int nErrorCode)
        {
        }

        protected string DbgGetIPString()
        {
            return string.Empty;
        }

        protected void CryptPrepareSendData(byte[] pBuffer, uint nLen)
        {
        }

        protected bool IsEncryptionLayerReady()
        {
            return false;
        }

        protected byte GetSemiRandomNotProtocolMarker()
        {
            return 0x0;
        }

        protected uint obfuscationBytesReceived_;
        protected StreamCryptStateEnum streamCryptState_;
        protected EncryptionMethodsEnum encryptionMethod_;
        protected bool fullReceive_;
        protected bool serverCrypt_;

        private int Negotiate(byte[] pBuffer, uint nLen)
        {
            return -1;
        }

        private void StartNegotiation(bool bOutgoing)
        {
        }

        private int SendNegotiatingData(byte[] lpBuf, uint nBufLen, uint nStartCryptFromByte, bool bDelaySend)
        {
            return -1;
        }

        private RC4Key rc4SendKey_;
        private RC4Key rc4ReceiveKey_;
        private NegotiatingStateEnum negotiatingState_;
        private SafeMemFile fiReceiveBuffer_;
        private uint receiveBytesWanted_;
        private SafeMemFile fiSendBuffer_;
        private uint randomKeyPart_;
        private CryptoPP.Integer cryptDHA_;
    }
}
