using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Preference
{
    public interface PreferenceObjectManager
    {
        MulePreference CreatePreference();

        MuleStatistics CreateStatistics();

        ProxySettings CreateProxySettings();

        FileComment CreateFileComment();

        FileComments CreateFileComments(string name);
    }
}
