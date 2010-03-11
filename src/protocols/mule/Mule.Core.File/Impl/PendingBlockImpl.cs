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

namespace Mule.Core.File.Impl
{
    public class PendingBlockImpl : PendingBlock
    {
        private RequestedBlock block_ = null;
        private uint totalUnzipped_ = 0;
        private uint value_ = 0;

        public PendingBlockImpl()
        {
            block_ = null;
            totalUnzipped_ = 0;
            FZStreamError = 0;
            FRecovered = 0;
            FQueued = 0;
        }

        public RequestedBlock Block
        {
            get { return block_; }
            set { block_ = value; }
        }

        public uint TotalUnzipped
        {
            get { return totalUnzipped_; }
            set { totalUnzipped_ = value; }
        }

        public uint FZStreamError
        {
            get
            {
                return value_ & 0x00000001;
            }

            set
            {
                value_ |= (value & 0x00000001);
            }
        }

        public uint FRecovered
        {
            get
            {
                return (value_ >> 1) & 0x00000001;
            }

            set
            {
                value_ |= ((value << 1) & 0x00000002);
            }
        }

        public uint FQueued
        {
            get
            {
                return (value_ >> 2) & 0x00000007;
            }

            set
            {
                value_ |= ((value << 2) & 0x0000001C);
            }
        }
    }
}
