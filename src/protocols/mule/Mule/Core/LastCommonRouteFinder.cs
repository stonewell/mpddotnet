using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.ED2K;

namespace Mule.Core
{
    public interface LastCommonRouteFinder
    {
        bool AddHostToCheck(uint ip);
	    bool AddHostsToCheck(IList<ED2KServer> list);
	    bool AddHostsToCheck(UpDownClientList list);
    }
}
