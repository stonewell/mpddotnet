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

namespace Mule.AICH
{
    class AICHHashSetStatics
    {
        private List<AICHRequestedData> requestedData_ =
            new List<AICHRequestedData>();

        private Mutex known2File_ =
            new Mutex();

        public List<AICHRequestedData> RequestedData
        {
            get
            {
                return requestedData_;
            }
        }

        public Mutex Known2File
        {
            get
            {
                return known2File_;
            }
        }

    }

    public interface AICHHashSet
    {
        AICHHashTree HashTree { get; }

        bool ReCalculateHash(bool bDontReplace);

        bool VerifyHashTree(bool bDeleteBadTrees);
        void UntrustedHashReceived(AICHHash Hash, uint dwFromIP);
        bool IsPartDataAvailable(ulong nPartStartPos);
        AICHStatusEnum Status { get; set;}

        void FreeHashSet();

        void SetFileSize(ulong nSize);

        AICHHash GetMasterHash();
        void SetMasterHash(AICHHash hash, AICHStatusEnum newStatus);

        bool HasValidMasterHash { get; }

        void DbgTest();
    }
}
