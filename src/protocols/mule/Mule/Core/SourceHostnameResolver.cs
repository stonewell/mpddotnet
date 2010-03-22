using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core
{
    public interface SourceHostnameResolver
    {
        void AddToResolve(byte[] fileid, string hostname, ushort port);
        void AddToResolve(byte[] fileid, string hostname, ushort port, string url);
    }
}
