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
using System.IO;


namespace Mule.AICH.Impl
{
    class AICHHashSetImpl : AICHHashSet
    {
        #region Fields
        private AICHHashTree hashTree_ = AICHObjectManager.CreateAICHHashTree(0, true, MuleConstants.PARTSIZE);

        private AICHStatusEnum status_ = AICHStatusEnum.AICH_EMPTY;
        private List<AICHUntrustedHash> untrustedHashs_ =
            new List<AICHUntrustedHash>();
        #endregion

        #region Constructors
        public AICHHashSetImpl()
        {
        }
        #endregion

        #region AICHHashSet Members
        public bool ReCalculateHash(bool bDontReplace)
        {
            return false;
        }

        public bool VerifyHashTree(bool bDeleteBadTrees)
        {
            return false;
        }

        public void UntrustedHashReceived(AICHHash Hash, uint dwFromIP)
        {
            return;
        }

        public bool IsPartDataAvailable(ulong nPartStartPos)
        {
            return false;
        }

        public AICHStatusEnum Status
        {
            get { return status_; }
            set { status_ = value; }
        }

        public void FreeHashSet()
        {
        }

        public void SetFileSize(ulong nSize)
        {
        }

        public AICHHash GetMasterHash()
        {
            return hashTree_.Hash;
        }

        public void SetMasterHash(AICHHash hash, AICHStatusEnum newStatus)
        {
        }

        public bool HasValidMasterHash
        {
            get { return hashTree_.HashValid; }
        }

        public void DbgTest()
        {
        }

        public AICHHashTree HashTree
        {
            get { return hashTree_; }
        }

        #endregion
    }
}
