using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core.Impl
{
    class LastCommonRouteFinderImpl : LastCommonRouteFinder
    {
        #region LastCommonRouteFinder Members

        public bool AddHostToCheck(uint ip)
        {
            throw new NotImplementedException();
        }

        public bool AddHostsToCheck(IList<Mule.ED2K.ED2KServer> list)
        {
            throw new NotImplementedException();
        }

        public bool AddHostsToCheck(UpDownClientList list)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
