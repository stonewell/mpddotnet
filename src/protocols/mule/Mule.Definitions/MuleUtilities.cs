using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Definitions
{
    public class MuleUtilities
    {
        public static bool IsLowID(uint id)
        {
            return (id < 16777216);
        }
    }
}
