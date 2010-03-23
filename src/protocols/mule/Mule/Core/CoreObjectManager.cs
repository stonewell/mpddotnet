using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core
{
    public interface CoreObjectManager
    {
        MuleCollection CreateMuleCollection();
        UpDownClient CreateUpDownClient(params object[] args);

        DownloadQueue CreateDownloadQueue();

        SourceHostnameResolver CreateSourceHostnameResolver();

        SharedFileList CreateSharedFileList();

        ServerConnect CreateServerConnect();

        ClientList CreateClientList();

        UploadBandwidthThrottler CreateUploadBandwidthThrottler();

        UploadQueue CreateUploadQueue();

        LastCommonRouteFinder CreateLastCommonRouteFinder();

        IPFilter CreateIPFilter();
    }
}
