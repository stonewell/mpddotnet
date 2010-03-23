using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core.Impl
{
    class SourceHostnameResolverImpl : SourceHostnameResolver
    {
        #region SourceHostnameResolver Members

        public void AddToResolve(byte[] fileid, string hostname, ushort port)
        {
            throw new NotImplementedException();
        }

        public void AddToResolve(byte[] fileid, string hostname, ushort port, string url)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
