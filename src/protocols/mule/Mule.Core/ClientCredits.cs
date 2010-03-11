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
        public UInt16 nReserved3;
    };

    public class CreditStruct
    {
        public const int MAXPUBKEYSIZE = 80;

        public CreditStruct()
        {
            abyKey = new byte[16];
            abySecureIdent = new byte[MAXPUBKEYSIZE];
        }

        public byte[] abyKey;
        public uint nUploadedLo;	// uploaded TO him
        public uint nDownloadedLo;	// downloaded from him
        public uint nLastSeen;
        public uint nUploadedHi;	// upload high 32
        public uint nDownloadedHi;	// download high 32
        public UInt16 nReserved3;
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
        char[] Key { get; }
        char[] SecureIdent { get; }
        byte SecIDKeyLen { get;}
        CreditStruct DataStruct { get; }
        void ClearWaitStartTime();
        void AddDownloaded(uint bytes, uint dwForIP);
        void AddUploaded(uint bytes, uint dwForIP);
        ulong GetUploadedTotal();
        ulong GetDownloadedTotal();
        float GetScoreRatio(uint dwForIP);
        void SetLastSeen();
        // Public key cannot change, use only if there is not public key yet
        bool SetSecureIdent(char[] pachIdent, byte nIdentLen);
        // can be != IdentState
        IdentStateEnum GetCurrentIdentState(uint dwForIP);
        uint GetSecureWaitStartTime(uint dwForIP);
        void SetSecWaitStartTime(uint dwForIP);

        void Verified(uint dwForIP);
        void InitalizeIdent();
    }
}
