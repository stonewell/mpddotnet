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
using Mpd.Generic;

namespace Mule.Core.Impl
{
    class CoreObjectManagerImpl : CoreObjectManager
    {
        #region Fields
        #endregion

        #region Constructor
        public CoreObjectManagerImpl()
        {
            //try
            //{
            //    preference_ = new MulePreferenceImpl();
            //    preference_.Load();
            //}
            //catch
            //{
            //    //TODO:Log
            //    preference_ = new MulePreferenceImpl();
            //    preference_.Init();
            //}
        }

        static CoreObjectManagerImpl()
        {
        }
        #endregion

        public CoreUtilities CreateCoreUtilities()
        {
            return MpdObjectManager.CreateObject(typeof(CoreUtilities)) as CoreUtilities;
        }

        public SharedFileList CreateSharedFileList()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public MuleCollection CreateMuleCollection()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public UpDownClient CreateUpDownClient(params object[] args)
        {
            throw new NotImplementedException();
        }
    }
}
