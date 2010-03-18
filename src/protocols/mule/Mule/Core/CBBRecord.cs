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

namespace Mule.Core
{
    public class CBBRecordArray : List<CBBRecord>
    {
    }

    public enum BBRStatusEnum
    {
        BBR_NONE = 0,
        BBR_VERIFIED,
        BBR_CORRUPTED
    };

    public interface CBBRecord
    {
        bool Merge(ulong nStartPos, ulong nEndPos, uint dwIP/*, EBBRStatus BBRStatus = BBR_NONE*/);
        bool Merge(ulong nStartPos, ulong nEndPos, uint dwIP, BBRStatusEnum BBRStatus);
        bool CanMerge(ulong nStartPos, ulong nEndPos, uint dwIP/*, EBBRStatus BBRStatus = BBR_NONE*/);
        bool CanMerge(ulong nStartPos, ulong nEndPos, uint dwIP, BBRStatusEnum BBRStatus);

        ulong StartPos { get;set;}
        ulong EndPos { get;set;}
        uint IP { get;set;}
        BBRStatusEnum BBRStatus { get;set;}
    }
}
