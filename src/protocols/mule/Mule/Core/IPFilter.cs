using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core
{
    public interface IPFilter
    {
        bool IsFiltered(uint ip) /*const*/;
        bool IsFiltered(uint ip, uint level) /*const*/;
    }
}
