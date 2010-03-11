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
    abstract class ED2KLinkImpl : ED2KLink
    {
        #region ED2KLink Members

        public virtual LinkTypeEnum Kind
        {
            get 
            {
                if (this is ED2KServerListLink)
                {
                    return LinkTypeEnum.kServerList;
                }
                else if (this is ED2KServerLink)
                {
                    return LinkTypeEnum.kServer;
                }
                else if (this is ED2KFileLink)
                {
                    return LinkTypeEnum.kFile;
                }
                else if (this is ED2KNodesListLink)
                {
                    return LinkTypeEnum.kNodesList;
                }
                else
                {
                    return LinkTypeEnum.kInvalid;
                }
            }
        }

        public abstract string Link { get; }

        public virtual ED2KServerListLink ServerListLink
        {
            get 
            { 
                if (this is ED2KServerListLink)
                    return this as ED2KServerListLink;

                return null;
            }
        }

        public virtual ED2KServerLink ServerLink
        {
            get
            {
                if (this is ED2KServerLink)
                    return this as ED2KServerLink;

                return null;
            }
        }

        public virtual ED2KFileLink FileLink
        {
            get
            {
                if (this is ED2KFileLink)
                    return this as ED2KFileLink;

                return null;
            }
        }

        public virtual ED2KNodesListLink NodesListLink
        {
            get
            {
                if (this is ED2KNodesListLink)
                    return this as ED2KNodesListLink;

                return null;
            }
        }

        #endregion
    }
}
