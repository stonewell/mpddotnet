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
    class RequestedBlockImpl : RequestedBlock
    {
        public ulong startOffset_ = 0;
        public ulong endOffset_ = 0;
        public byte[] fileID_ = null;
        public ulong transferred_ = 0;

        public RequestedBlockImpl()
        {
            fileID_ = new byte[16];
        }

        public ulong StartOffset
        {
            get { return startOffset_; }
            set { startOffset_ = value; }
        }

        public ulong EndOffset
        {
            get { return endOffset_; }
            set { endOffset_ = value; }
        }

        public byte[] FileID
        {
            get { return fileID_; }
            set { fileID_ = value; }
        }

        public ulong Transferred
        {
            get { return transferred_; }
            set { transferred_ = value; }
        }

    }
}
