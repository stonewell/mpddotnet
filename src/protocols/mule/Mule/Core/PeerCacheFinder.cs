using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core
{
    public interface PeerCacheFinder
    {
        void FoundMyPublicIPAddress(int value);
    }
}
