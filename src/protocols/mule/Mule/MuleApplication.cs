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
using Mpd.Utilities;

namespace Mule
{
    public class MuleApplication
    {
        public static MuleApplication Instance { get; private set; }

        static MuleApplication()
        {
            Instance = new MuleApplication();
        }

        public void InitApplication()
        {
            InitObjectManagers();

            try
            {
                Preference = PreferenceObjectManager.CreatePreference();
                Preference.Load();
            }
            catch
            {
                //TODO:Log
                Preference = PreferenceObjectManager.CreatePreference();
                Preference.Init();
            }

            ServerList = ED2KObjectManager.CreateED2KServerList();
            DownloadQueue = CoreObjectManager.CreateDownloadQueue();
            Statistics = PreferenceObjectManager.CreateStatistics();
        }

        private void InitObjectManagers()
        {
        }

        public MulePreference Preference { get; private set; }
        public MuleStatistics Statistics { get; private set; }

        public int PublicIP { get; private set; }

        public SharedFileList SharedFiles { get; private set; }

        public KadEngine KadEngine { get; private set; }

        public bool IsConnected
        {
            get
            {
                return (ServerConnect.IsConnected || KadEngine.IsConnected);
            }
        }

        public ServerConnect ServerConnect { get; private set; }
        public ClientList ClientList { get; private set; }
        public UploadBandwidthThrottler UploadBandwidthThrottler { get; private set; }

        public NetworkObjectManager NetworkObjectManager { get; private set; }
        public CoreObjectManager CoreObjectManager { get; private set; }
        public KadObjectManager KadObjectManager { get; private set; }
        public ED2KObjectManager ED2KObjectManager { get; private set; }
        public FileObjectManager FileObjectManager { get; private set; }
        public AICHObjectManager AICHObjectManager { get; private set; }
        public PreferenceObjectManager PreferenceObjectManager { get; private set; }

        public bool IsFirewalled
        {
            get
            {
                if (ServerConnect.IsConnected && !ServerConnect.IsLowID)
                    return false; // we have an eD2K HighID . not firewalled

                if (KadEngine.IsConnected && !KadEngine.IsFirewalled)
                    return false; // we have an Kad HighID . not firewalled

                return true; // firewalled
            }
        }

        public UploadQueue UploadQueue { get; private set; }
        public DownloadQueue DownloadQueue { get; private set; }
        public LastCommonRouteFinder LastCommonRouteFinder { get; private set; }
        public IPFilter IPFilter { get; private set; }

        public ED2KServerList ServerList { get; private set; }

        public bool CanDoCallback(UpDownClient client)
        {
            if (KadEngine.IsConnected)
            {
                if (ServerConnect.IsConnected)
                {
                    if (ServerConnect.IsLowID)
                    {
                        if (KadEngine.IsFirewalled)
                        {
                            //Both Connected - Both Firewalled
                            return false;
                        }
                        else
                        {
                            if (client.ServerIP == ServerConnect.CurrentServer.IP &&
                                client.ServerPort == ServerConnect.CurrentServer.Port)
                            {
                                //Both Connected - Server lowID, Kad Open - Client on same server
                                //We prevent a callback to the server as this breaks the protocol and will get you banned.
                                return false;
                            }
                            else
                            {
                                //Both Connected - Server lowID, Kad Open - Client on remote server
                                return true;
                            }
                        }
                    }
                    else
                    {
                        //Both Connected - Server HighID, Kad don't care
                        return true;
                    }
                }
                else
                {
                    if (KadEngine.IsFirewalled)
                    {
                        //Only Kad Connected - Kad Firewalled
                        return false;
                    }
                    else
                    {
                        //Only Kad Conected - Kad Open
                        return true;
                    }
                }
            }
            else
            {
                if (ServerConnect.IsConnected)
                {
                    if (ServerConnect.IsLowID)
                    {
                        //Only Server Connected - Server LowID
                        return false;
                    }
                    else
                    {
                        //Only Server Connected - Server HighID
                        return true;
                    }
                }
                else
                {
                    //We are not connected at all!
                    return false;
                }
            }
        }

        public void StartUp()
        {
            try
            {
                ServerList.Init();

                DownloadQueue.Init();
            }
            catch (Exception ex)
            {
                MpdUtilities.DebugLogError("MuleApplication StartUp Fail",
                    ex);
            }
        }

        public bool IsRunning { get; set; }
    }
}
