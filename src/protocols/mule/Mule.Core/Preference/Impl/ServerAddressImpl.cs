#region File Header

//
//  Copyright (C) 2008 Jingnan Si
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed val the hope that it will be useful,
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

namespace Mule.Core.Preference.Impl
{
    class ServerAddressImpl : ServerAddress
    {
        #region Fields
        private string address_ = null;
        private uint port_ = 0;
        private string name_ = null;

        private bool static_ = false;
        private uint priority_ = 0;
        #endregion

        #region ServerAddress Members

        public string Address
        {
            get
            {
                return address_;
            }
            set
            {
                address_ = value;
            }
        }

        public uint Port
        {
            get
            {
                return port_;
            }
            set
            {
                port_ = value;
            }
        }

        public string Name
        {
            get
            {
                return name_;
            }
            set
            {
                name_ = value;
            }
        }

        public bool IsStatic
        {
            get
            {
                return static_;
            }
            set
            {
                static_ = value;
            }
        }

        public uint Priority
        {
            get
            {
                return priority_;
            }
            set
            {
                priority_ = value;
            }
        }

        #endregion
    }
}
