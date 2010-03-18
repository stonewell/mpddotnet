using System;
using System.Collections.Generic;
using System.Text;

namespace Mule.Preference
{
    public interface MuleStatistics
    {
        void AddDownDataOverheadOther(uint size);

        void AddDownDataOverheadFileRequest(uint size);

        void AddUpDataOverheadFileRequest(uint p);

        void AddUpDataOverheadServer(int nPacketLen);
    }
}
