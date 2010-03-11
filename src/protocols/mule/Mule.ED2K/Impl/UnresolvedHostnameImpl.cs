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

namespace Mule.ED2K.Impl
{
    class UnresolvedHostnameImpl : UnresolvedHostname
    {
        #region Fields
        private string hostname_ = null;
        private ushort port_ = 0;
        private string url_ = null;
        #endregion

        #region Constructors
        public UnresolvedHostnameImpl()
        {
        }
        #endregion

        #region UnresolvedHostname Members

        public string HostName
        {
            get
            {
                return hostname_;
            }
            set
            {
                hostname_ = value;
            }
        }

        public string Url
        {
            get
            {
                return url_;
            }
            set
            {
                url_ = value;
            }
        }

        public ushort Port
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

        #endregion
    }
}
