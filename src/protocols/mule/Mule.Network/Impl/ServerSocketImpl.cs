using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.Core;

namespace Mule.Network.Impl
{
    class ServerSocketImpl : EMSocketImpl, ServerSocket
    {
        #region Constructors
        public ServerSocketImpl(ServerConnect serverConnect, bool singleConnect)
        {
        }
        #endregion

        #region ServerSocket Members

        public void ConnectTo(Mule.ED2K.ED2KServer toconnect, bool bNoCrypt)
        {
            throw new NotImplementedException();
        }

        public bool IsDeleting
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public Mule.ED2K.ED2KServer CurrentServer
        {
            get { throw new NotImplementedException(); }
        }

        public uint LastTransmission
        {
            get { throw new NotImplementedException(); }
        }

        #endregion
    }
}
