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

namespace Mule.Core.AICH.SHA.Impl
{
    internal class SHAImpl : SHA
    {
        #region Fields
        private uint[] count_;
        private uint[] hash_;
        private uint[] buffer_;
        #endregion

        #region Constructors
        public SHAImpl()
        {
            count_ = new uint[2];
            hash_ = new uint[5];
            buffer_ = new uint[16];

            Reset();
        }
        #endregion

        #region AICHHashAlgo Members

        public void Reset()
        {
            count_[0] = count_[1] = 0;
            hash_[0] = 0x67452301;
            hash_[1] = 0xefcdab89;
            hash_[2] = 0x98badcfe;
            hash_[3] = 0x10325476;
            hash_[4] = 0xc3d2e1f0;
        }

        public void Add(byte[] pData)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Add(byte[] pData, uint offset, uint length)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Finish(AICHHash Hash)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void GetHash(AICHHash Hash)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
