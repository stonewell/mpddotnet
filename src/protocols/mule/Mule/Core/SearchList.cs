using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core
{
    public interface SearchList
    {
        uint ProcessUDPSearchAnswer(Mpd.Generic.IO.SafeMemFile data, bool p, uint nIP, int p_4);
    }
}
