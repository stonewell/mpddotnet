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
        #region PreferenceObjectManager Members

        public FileComments CreateFileComments(string name)
        {
            FileCommentsImpl fc = new FileCommentsImpl();
            fc.Name = name;
            return fc;
        }

        public FileComment CreateFileComment()
        {
            return new FileCommentImpl();
        }

        public MuleStatistics CreateStatistics()
        {
            return MpdObjectManager.CreateObject(typeof(MuleStatisticsImpl)) as MuleStatistics;
        }

        public ProxySettings CreateProxySettings()
        {
            return MpdObjectManager.CreateObject(typeof(ProxySettingsImpl)) as ProxySettings;
        }

        public MulePreference CreatePreference()
        {
            return new MulePreferenceImpl();
        }

        #endregion
    }
}
