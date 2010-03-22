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

namespace Mule.AICH.Impl
{
    class AICHUntrustedHashImpl : AICHUntrustedHash
    {
        #region Fields
        private AICHHash hash_ = new AICHHashImpl();
        private List<uint> ipsSigning_ = new List<uint>();
        #endregion

        #region AICHUntrustedHash Members

        public bool AddSigningIP(uint dwIP)
        {
            // we use only the 20 most significant bytes for unique IPs
            dwIP &= 0x00F0FFFF;
            for (int i = 0; i < ipsSigning_.Count; i++)
            {
                if (ipsSigning_[i] == dwIP)
                    return false;
            }
            ipsSigning_.Add(dwIP);
            return true;
        }

        public AICHHash Hash
        {
            get { return hash_; }
        }

        public List<uint> IpsSigning
        {
            get { return IpsSigning; }
        }

        #endregion
    }
}
