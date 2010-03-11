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
using System.Threading;
using Mule.Core.Network;
using Mule.Core.File;

namespace Mule.Core.AICH
{
    class AICHHashSetStatics
    {
        private static readonly List<AICHRequestedData> requestedData_ =
            new List<AICHRequestedData>();

        private static readonly Mutex known2File_ =
            new Mutex();

        public static List<AICHRequestedData> RequestedData
        {
            get
            {
                return requestedData_;
            }
        }

        public static Mutex Known2File
        {
            get
            {
                return known2File_;
            }
        }

        public static void ClientAICHRequestFailed(UpDownClient pClient)
        {
        }

        public static void RemoveClientAICHRequest(UpDownClient pClient)
        {
        }

        public static bool IsClientRequestPending(PartFile pForFile, UInt16 nPart)
        {
            return false;
        }

        public static AICHRequestedData GetAICHReqDetails(UpDownClient pClient)
        {
            return null;
        }
    }

    public interface AICHHashSet
    {
        AICHHashTree HashTree { get; }

        bool CreatePartRecoveryData(ulong nPartStartPos, FileDataIO fileDataOut, bool bDbgDontLoad);

        bool ReadRecoveryData(ulong nPartStartPos, SafeMemFile fileDataIn);


        bool ReCalculateHash(bool bDontReplace);

        bool VerifyHashTree(bool bDeleteBadTrees);
        void UntrustedHashReceived(AICHHash Hash, uint dwFromIP);
        bool IsPartDataAvailable(ulong nPartStartPos);
        AICHStatusEnum Status { get; set;}

        KnownFile Owner { get; set;}

        void FreeHashSet();

        void SetFileSize(ulong nSize);

        AICHHash GetMasterHash();
        void SetMasterHash(AICHHash hash, AICHStatusEnum newStatus);

        bool HasValidMasterHash { get; }

        bool SaveHashSet();

        // only call directly when debugging
        bool LoadHashSet();

        AICHHashAlgo NewHashAlgo
        {
            get;
        }

        void DbgTest();
    }
}
