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
using Mule.Xml.Serialization;

namespace Mule.Core.Impl
{
    class MuleBaseObjectImpl : MuleBaseObject
    {
        #region Fields
        private MuleEngine muleEngine_ = null;
        #endregion

        #region MuleBaseObject Members
        [XmlIgnore]
        public virtual MuleEngine MuleEngine
        {
            get
            {
                return muleEngine_;
            }

            set
            {
                if (muleEngine_ != null)
                {
                    throw new MuleException("Unable to change MuleEngine When the object already attached to a MuleEngine.");
                }

                muleEngine_ = value;
            }
        }
        #endregion
    }
}
