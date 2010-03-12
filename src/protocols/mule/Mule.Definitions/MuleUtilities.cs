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

        public static uint SwapAlways(uint val)
        {
            uint v1 = val & 0xFFFF;
            uint v2 = (val >> 16) & 0xFFFF;

            return v1 << 16 | v2;
        }
    }
}
