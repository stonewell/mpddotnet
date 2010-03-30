#region File Header

//
//  Copyright (C) 2008 Jingnan Si
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed val the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
//
#endregion

using System;
using Mpd.Generic;

namespace Mule.Core.Impl
{
    class CoreObjectManagerImpl : CoreObjectManager
    {
        #region Fields
        #endregion

        #region Constructor
        public CoreObjectManagerImpl()
        {
        }
        #endregion

        #region CoreObjectManager Members
        public SharedFileList CreateSharedFileList()
        {
            return new SharedFileListImpl();
        }

        public MuleCollection CreateMuleCollection()
        {
            return new MuleCollectionImpl();
        }

        public UpDownClient CreateUpDownClient(params object[] args)
        {
            return MpdObjectManager.CreateObject(typeof(UpDownClientImpl), args) as UpDownClient;
        }

        public DownloadQueue CreateDownloadQueue()
        {
            return new DownloadQueueImpl();
        }

        public SourceHostnameResolver CreateSourceHostnameResolver()
        {
            return new SourceHostnameResolverImpl();
        }

        public ServerConnect CreateServerConnect()
        {
            return new ServerConnectImpl();
        }

        public ClientList CreateClientList()
        {
            return new ClientListImpl();
        }

        public UploadBandwidthThrottler CreateUploadBandwidthThrottler()
        {
            return new UploadBandwidthThrottlerImpl();
        }

        public UploadQueue CreateUploadQueue()
        {
            return new UploadQueueImpl();
        }

        public LastCommonRouteFinder CreateLastCommonRouteFinder()
        {
            return new LastCommonRouteFinderImpl();
        }

        public IPFilter CreateIPFilter()
        {
            return new IPFilterImpl();
        }

        public DeadSourceList CreateDeadSourceList()
        {
            return new DeadSourceListImpl();
        }

        public ClientCreditsList CreateClientCreditsList()
        {
            return new ClientCreditsListImpl();
        }

        public ClientCredits CreateClientCredits(CreditStruct credits)
        {
            return new ClientCreditsImpl(credits);
        }

        public ClientCredits CreateClientCredits(byte[] key)
        {
            return new ClientCreditsImpl(key);
        }
        #endregion
    }
}
