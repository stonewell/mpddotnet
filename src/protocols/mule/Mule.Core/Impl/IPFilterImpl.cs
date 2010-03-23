using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core.Impl
{
    class IPFilterImpl : IPFilter
    {
        #region IPFilter Members

        public bool IsFiltered(uint ip)
        {
            throw new NotImplementedException();
        }

        public bool IsFiltered(uint ip, uint level)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
