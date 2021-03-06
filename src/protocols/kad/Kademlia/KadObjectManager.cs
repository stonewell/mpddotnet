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
using Mpd.Generic;

namespace Kademlia
{
    public sealed class KadObjectManager
    {
        #region Fields
        private KadEngine engine_ = null;
        #endregion

        #region Constructors
        public KadObjectManager(KadEngine engine)
        {
            engine_ = engine;
        }
        #endregion

        public KadWordList CreateWordList()
        {
            return new KadWordList();
        }

        public UInt128 CreateUInt128(object p)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public KadUDPKey CreateKadUDPKey(uint key, uint ip)
        {
            throw new NotImplementedException();
        }
    }
}
