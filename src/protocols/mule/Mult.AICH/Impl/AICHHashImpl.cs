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
using Mpd.Generic.IO;
using Mpd.Utilities;
using Mule.Definitions;

namespace Mule.AICH.Impl
{
    class AICHHashImpl : AICHHash
    {
        #region Fields
        private byte[] byBuffer_ = null;
        #endregion

        #region Constructors
        public AICHHashImpl()
        {
            byBuffer_ = new byte[MuleConstants.HASHSIZE];

            Array.Clear(byBuffer_, 0, byBuffer_.Length);
        }

        public AICHHashImpl(byte[] data)
            : this()
        {
            Read(data);
        }

        public AICHHashImpl(AICHHash k)
            : this()
        {
            if (k != null)
                Array.Copy(k.RawHash, byBuffer_, MuleConstants.HASHSIZE);
        }
        #endregion

        #region AICHHash Members

        public void Read(byte[] data)
        {
            int length = MuleConstants.HASHSIZE;

            if (length > data.Length)
                length = data.Length;

            Array.Copy(data, byBuffer_, length);
        }

        public string HashString
        {
            get { return MpdUtilities.EncodeBase32(byBuffer_); }
        }

        public byte[] RawHash
        {
            get { return byBuffer_; }
        }

        #endregion

        public override bool Equals(object obj)
        {
            if (obj is AICHHash)
            {
                return Encoding.Default.GetString(RawHash).Equals(Encoding.Default.GetString((obj as AICHHash).RawHash));
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return HashString.GetHashCode();
        }
        public void Read(FileDataIO file)
        {
            file.Read(RawHash);
        }

        public void Write(FileDataIO file)
        {
            file.Write(RawHash);
        }
    }
}
