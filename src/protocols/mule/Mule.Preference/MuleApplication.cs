using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Preference
{
    public interface MuleApplication
    {
        bool AwaitingTestFromIP(uint ip);
        bool IsKadFirewallCheckIP(uint ip);
        MulePreference Preference { get; }
    }
}
