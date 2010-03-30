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
using Mpd.Utilities;

namespace Mpd.Generic
{
    public class MapCKey
    {
        public MapCKey()
            : this((byte[])null)
        {
        }

        public MapCKey(byte[] key)
        {
            Key = key;
        }

        public MapCKey(MapCKey cKey)
        {
            Key = cKey.Key;
        }

        public byte[] Key { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is MapCKey)
            {
                return MpdUtilities.EncodeHexString(Key).Equals(MpdUtilities.EncodeHexString((obj as MapCKey).Key));
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            int hash = 1;
            
            if (Key != null)
            {
                for (int i = 0; i != 16; i++)
                {
                    if (i >= Key.Length)
                        break;
                    hash += (Key[i] + 1) * ((i * i) + 1);
                }
            }

            return hash;
        }
    }

    public class MapSKey
    {
        private byte[] key_ = new byte[16];

        public MapSKey()
            : this((byte[])null)
        {
        }

        public MapSKey(byte[] key)
        {
            if (key != null)
                MpdUtilities.Md4Cpy(key_, key);
            else
                MpdUtilities.Md4Clr(key_);
        }

        public MapSKey(MapSKey key)
        {
            MpdUtilities.Md4Cpy(key_, key.key_);
        }

        public byte[] Key { get { return key_; } }

        public override bool Equals(object obj)
        {
            if (obj is MapCKey)
            {
                return MpdUtilities.Md4Cmp(key_, (obj as MapCKey).Key) == 0;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            int hash = 1;

            for (int i = 0; i != 16; i++)
            {
                hash += (key_[i] + 1) * ((i * i) + 1);
            }

            return hash;
        }
    }
}
