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
using Mpd.Generic.Types.IO;

namespace Mule.AICH
{
    public enum AICHStatusEnum
    {
        AICH_ERROR = 0,
        AICH_EMPTY,
        AICH_UNTRUSTED,
        AICH_TRUSTED,
        AICH_VERIFIED,
        AICH_HASHSETCOMPLETE
    };

    public interface AICHHash
    {
        void Read(FileDataIO file);
        void Write(FileDataIO file);
        void Read(byte[] data);
        string HashString { get;}
        byte[] RawHash { get;}
    }
}