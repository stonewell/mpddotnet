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
using Mule.AICH;
using Mpd.Generic.Types.IO;

namespace Mule.ED2K
{
    public enum LinkTypeEnum
    {
        kServerList, kServer, kFile, kNodesList, kInvalid
    };

    public interface ED2KFileLink : ED2KLink
    {
        string Name { get;}
        byte[] HashKey { get;}
        AICHHash AICHHash { get;}
        ulong Size { get;}
        bool HasValidSources { get;}
        bool HasHostnameSources { get;}
        bool HasValidAICHHash { get;}

        SafeMemFile SourcesList { get; }
        SafeMemFile HashSet { get; }
        UnresolvedHostnameList HostnameSourcesList { get;}
    };
}
