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
using Mule.Core.File;

namespace Mule.Core.AICH
{
    public class AICHRequestedData
    {
        #region Fields
        private UInt16 part_;
        private PartFile partFile_;
        private UpDownClient client_;
        #endregion

        #region Constructors
        public AICHRequestedData()
        {
            part_ = 0;
            partFile_ = null;
            client_ = null;
        }
        #endregion

        #region Properties
        public UInt16 Part
        {
            get
            {
                return part_;
            }
            set
            {
                part_ = value;
            }
        }

        public PartFile PartFile
        {
            get { return partFile_; }
            set { partFile_ = value; }
        }

        public UpDownClient UpDownClient
        {
            get { return client_; }
            set { client_ = value; }
        }
        #endregion
    }
}
