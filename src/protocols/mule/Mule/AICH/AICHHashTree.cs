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

namespace Mule.AICH
{
    public interface AICHHashTree
    {
        AICHHash Hash
        {
            get;
        }

        bool HashValid
        {
            get;
            set;
        }

        ulong DataSize
        {
            get;
        }

        ulong BaseSize
        {
            get;
        }

        bool IsLeftBranch
        {
            get;
        }

        void SetBlockHash(ulong nSize, ulong nStartPos, AICHHashAlgorithm pHashAlg);

        bool ReCalculateHash(AICHHashAlgorithm hashalg, bool bDontReplace);

        bool VerifyHashTree(AICHHashAlgorithm hashalg, bool bDeleteBadTrees);

        AICHHashTree FindHash(ulong nStartPos, ulong nSize);
        AICHHashTree FindHash(ulong nStartPos, ulong nSize, ref byte nLevel);
        bool CreatePartRecoveryData(ulong nStartPos, ulong nSize, FileDataIO fileDataOut, uint wHashIdent, bool b32BitIdent);

        bool SetHash(FileDataIO fileInput, uint wHashIdent);
        bool SetHash(FileDataIO fileInput, uint wHashIdent, sbyte nLevel);
        bool SetHash(FileDataIO fileInput, uint wHashIdent, sbyte nLevel, bool bAllowOverwrite);
        bool WriteLowestLevelHashs(FileDataIO fileDataOut, uint wHashIdent, bool bNoIdent, bool b32BitIdent);
        bool LoadLowestLevelHashs(FileDataIO fileInput);
        void WriteHash(FileDataIO fileDataOut, uint wHashIdent, bool b32BitIdent);
    }
}
