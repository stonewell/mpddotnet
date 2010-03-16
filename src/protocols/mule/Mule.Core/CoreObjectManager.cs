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
using Mule.AICH;
using Mule.AICH.Impl;
using Mule.File;
using Mule.AICH.SHA;
using System.IO;
using Mule.ED2K;
using Mule.ED2K.Impl;
using Mule.File.Impl;
using System.Reflection;
using Mpd.Generic.IO;
using Mule.Network;
using Mule.Preference.Impl;
using Mule.Preference;
using Mpd.Generic;

namespace Mule.Core
{
    sealed public class CoreObjectManager
    {
        #region Fields
        private MulePreference preference_ = null;
        private Random radom0_ = new Random(0);
        private MuleEngine muleEngine_ = null;
        #endregion

        #region Constructor
        public CoreObjectManager(MuleEngine muleEngine)
        {
            muleEngine_ = muleEngine;

            try
            {
                preference_ = new MulePreferenceImpl();
                preference_.Load();
            }
            catch
            {
                //TODO:Log
                preference_ = new MulePreferenceImpl();
                preference_.Init();
            }
        }

        static CoreObjectManager()
        {
        }
        #endregion

        #region Methods
        public MulePreference Preference
        {
            get
            {
                return preference_;
            }
        }

        public Random Random0
        {
            get { return radom0_; }
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
