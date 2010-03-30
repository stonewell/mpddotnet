using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.File
{
    public interface KnownFileList
    {
        void Process();
        ushort Requested { get; set; }
        ushort Accepted { get; set; }
        ulong Transferred { get; set; }
    }
}
