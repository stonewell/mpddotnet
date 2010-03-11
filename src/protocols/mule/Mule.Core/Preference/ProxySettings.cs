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

namespace Mule.Core.Preference
{
    public interface ProxySettings
    {
        UInt16 Type { get; set;}
        UInt16 Port { get; set;}
        string Name { get; set;}
        string User { get; set;}
        string Password { get; set;}
        bool IsPasswordEnabled { get; set;}
        bool UseProxy { get;set; }

        void LoadFrom(System.IO.MemoryStream ms);

        void SaveTo(System.IO.MemoryStream ms);
    }
}
