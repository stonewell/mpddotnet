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
using System.Runtime.InteropServices;

namespace Mule.Core
{
    public class CreditStruct_29a
    {
        public CreditStruct_29a()
        {
            abyKey = new byte[16];
        }

        public byte[] abyKey;
        public uint nUploadedLo;	// uploaded TO him
        public uint nDownloadedLo;	// downloaded from him
        public uint nLastSeen;
        public uint nUploadedHi;	// upload high 32
        public uint nDownloadedHi;	// download high 32
        public ushort nReserved3;
    };

    public class CreditStruct
    {
        public const int MAXPUBKEYSIZE = 80;

        public CreditStruct()
        {
            abyKey = new byte[16];
            Array.Clear(abyKey, 0, abyKey.Length);
            abySecureIdent = new byte[MAXPUBKEYSIZE];
            Array.Clear(abySecureIdent, 0, abySecureIdent.Length);
            nUploadedLo = nDownloadedLo = nUploadedHi = nDownloadedHi = 0;
            nKeySize = 0; nLastSeen = 0;
        }

        public byte[] abyKey;
        public uint nUploadedLo;	// uploaded TO him
        public uint nDownloadedLo;	// downloaded from him
        public uint nLastSeen;
        public uint nUploadedHi;	// upload high 32
        public uint nDownloadedHi;	// download high 32
        public ushort nReserved3;
        public byte nKeySize;
        public byte[] abySecureIdent;
    };

    public enum IdentStateEnum
    {
        IS_NOTAVAILABLE,
        IS_IDNEEDED,
        IS_IDENTIFIED,
        IS_IDFAILED,
        IS_IDBADGUY,
    };

    public interface ClientCredits
    {
        byte[] Key { get; }
        byte[] SecureIdent { get; }
        byte SecIDKeyLen { get;}
        CreditStruct DataStruct { get; }
        void ClearWaitStartTime();
        void AddDownloaded(uint bytes, uint dwForIP);
        void AddUploaded(uint bytes, uint dwForIP);
        ulong GetUploadedTotal();
        ulong GetDownloadedTotal();
        float GetScoreRatio(uint dwForIP);
        void SetLastSeen();
        bool SetSecureIdent(byte[] pachIdent, byte nIdentLen);
        IdentStateEnum GetCurrentIdentState(uint dwForIP);
        uint GetSecureWaitStartTime(uint dwForIP);
        void SetSecWaitStartTime(uint dwForIP);

        uint CryptRndChallengeFor { get;set;}
        uint CryptRndChallengeFrom { get; set; }
    }
}
