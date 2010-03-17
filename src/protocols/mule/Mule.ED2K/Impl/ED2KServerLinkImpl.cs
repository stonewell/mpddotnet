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
    class ED2KServerLinkImpl : ED2KLinkImpl, ED2KServerLink
    {
        #region Fields
        private string ip_ = null;
        private string strPort_ = null;
        private ushort port_ = 0;
        private string defaultName_ = null;
        #endregion

        #region Constructors
        public ED2KServerLinkImpl(string ip, string port)
        {
            ip_ = ip;
            strPort_ = port;

            if (!ushort.TryParse(port, out port_))
            {
                throw new MuleException("Invalid Port:" + strPort_);
            }

            defaultName_ = "Server " + ip_ + ":" + port_;
        }
        #endregion

        #region ED2KServerLink Members

        public string Address
        {
            get { return ip_; }
        }

        public ushort Port
        {
            get { return port_; }
        }

        public string DefaultName
        {
            get { return defaultName_; }
        }

        #endregion

        #region Overrides
        public override string Link
        {
            get
            {
                return string.Format("ed2k://|server|{0}|{1}|/", Address, Port);
            }
        }
        #endregion
    }
}
