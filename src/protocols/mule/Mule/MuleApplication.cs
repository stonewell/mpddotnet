using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.Preference;
using Mule.Core;
using Mule.Network;
using Kademlia;
using Mule.ED2K;
using Mule.File;
using Mule.AICH;

namespace Mule
{
    public class MuleApplication
    {
        public static MuleApplication Instance { get; private set; }

        static MuleApplication()
        {
        }

        public static void InitApplication()
        {
        }

        public MulePreference Preference { get; private set; }

        public int PublicIP { get; private set; }

        public SharedFileList SharedFiles { get; private set; }

        public KadEngine KadEngine { get; private set; }

        public bool IsConnected { get; private set; }
        public ServerConnect ServerConnect { get; private set; }
        public ClientList ClientList { get; private set; }
        public UploadBandwidthThrottler UploadBandwidthThrottler { get; private set; }

        public NetworkObjectManager NetworkObjectManager { get; private set; }
        public CoreObjectManager CoreObjectManager { get; private set; }
        public KadObjectManager KadObjectManager { get; private set; }
        public ED2KObjectManager ED2KObjectManager { get; private set; }
        public FileObjectManager FileObjectManager { get; private set; }
        public AICHObjectManager AICHObjectManager { get; private set; }

        public bool IsFirewalled { get; private set; }
        public UploadQueue UploadQueue { get; private set; }
        public DownloadQueue DownloadQueue { get; private set; }
        public LastCommonRouteFinder LastCommonRouteFinder { get; private set; }
        public IPFilter IPFilter { get; private set; }

        public bool CanDoCallback(UpDownClient client)
        {
            return true;
        }
    }
}
