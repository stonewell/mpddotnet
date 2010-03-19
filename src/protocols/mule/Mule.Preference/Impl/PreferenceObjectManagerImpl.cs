using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Utilities;
using Mpd.Generic;
using Mule.Preference.Impl;

namespace Mule.Preference.Impl
{
    class PreferenceObjectManagerImpl : PreferenceObjectManager
    {
        public static FileComments CreateFileComments(string p)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public static FileComment CreateFileComment()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public static MuleStatistics CreateCoreStatistics()
        {
            return MpdObjectManager.CreateObject(typeof(MuleStatisticsImpl)) as MuleStatistics;
        }

        public static ProxySettings CreateProxySettings()
        {
            return MpdObjectManager.CreateObject(typeof(ProxySettingsImpl)) as ProxySettings;
        }

        #region PreferenceObjectManager Members

        public MulePreference CreatePreference()
        {
            throw new Exception();
        }

        #endregion
    }
}
