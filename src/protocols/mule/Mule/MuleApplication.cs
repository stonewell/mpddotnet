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
using Mpd.Generic;

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
            KadEngine = new KadEngine();

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

            Statistics = PreferenceObjectManager.CreateStatistics();
            Statistics.Load();

            SharedFiles = CoreObjectManager.CreateSharedFileList();

            ServerConnect = CoreObjectManager.CreateServerConnect();
            ClientList = CoreObjectManager.CreateClientList();
            UploadBandwidthThrottler = CoreObjectManager.CreateUploadBandwidthThrottler();

            UploadQueue = CoreObjectManager.CreateUploadQueue();
            LastCommonRouteFinder = CoreObjectManager.CreateLastCommonRouteFinder();
            ServerList = ED2KObjectManager.CreateED2KServerList();
            DownloadQueue = CoreObjectManager.CreateDownloadQueue();
            IPFilter = CoreObjectManager.CreateIPFilter();

            ListenSocket = NetworkObjectManager.CreateListenSocket();
            ClientUDP = NetworkObjectManager.CreateClientUDPSocket();
        }

        private void InitObjectManagers()
        {
            NetworkObjectManager =
                CreateObject(System.Configuration.ConfigurationSettings.AppSettings["NetworkObjectManager"],
                    "Mule.Network.Impl.NetworkObjectManagerImpl, Mule.Network") as NetworkObjectManager;
            CoreObjectManager =
                    CreateObject(System.Configuration.ConfigurationSettings.AppSettings["CoreObjectManager"],
                        "Mule.Core.Impl.CoreObjectManagerImpl, Mule.Core") as CoreObjectManager;

            KadObjectManager = KadEngine.ObjectManager;

            ED2KObjectManager =
                    CreateObject(System.Configuration.ConfigurationSettings.AppSettings["ED2KObjectManager"],
                        "Mule.ED2K.Impl.ED2KObjectManagerImpl, Mule.ED2K") as ED2KObjectManager;
            FileObjectManager =
                    CreateObject(System.Configuration.ConfigurationSettings.AppSettings["FileObjectManager"],
                        "Mule.File.Impl.FileObjectManagerImpl, Mule.File") as FileObjectManager;
            AICHObjectManager =
                    CreateObject(System.Configuration.ConfigurationSettings.AppSettings["AICHObjectManager"],
                        "Mule.AICH.Impl.AICHObjectManagerImpl, Mule.AICH") as AICHObjectManager;
            PreferenceObjectManager =
                    CreateObject(System.Configuration.ConfigurationSettings.AppSettings["PreferenceObjectManager"],
                        "Mule.Preference.Impl.PreferenceObjectManagerImpl, Mule.Preference") as PreferenceObjectManager;
        }

        private object CreateObject(string configFullTypeName,
            string defaultFullName)
        {
            if (string.IsNullOrEmpty(configFullTypeName))
                return MpdObjectManager.CreateObject(Type.GetType(defaultFullName));
            else
                return MpdObjectManager.CreateObject(Type.GetType(configFullTypeName));
        }

        public MulePreference Preference { get; private set; }
        public MuleStatistics Statistics { get; private set; }
        public ListenSocket ListenSocket { get; private set; }

        public int PublicIP { get; set; }

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
            if (IsRunning)
                return;

            IsRunning = false;

            try
            {
                ServerList.Init();

                DownloadQueue.Init();

                ListenSocket.StartListening();

                ClientUDP.Create();

                if (Preference.DoesAutoConnect)
                    StartConnection();

                IsRunning = true;
            }
            catch (Exception ex)
            {
                MpdUtilities.DebugLogError("MuleApplication StartUp Fail",
                    ex);
            }
        }

        public void CloseConnection()
        {
            if (ServerConnect.IsConnected)
            {
                ServerConnect.Disconnect();
            }

            if (ServerConnect.IsConnecting)
            {
                ServerConnect.StopConnectionTry();
            }

            KadEngine.Stop();
        }

        public void StartConnection()
        {
            if ((!ServerConnect.IsConnecting && !ServerConnect.IsConnected)
                || !KadEngine.IsRunning)
            {
                // ed2k
                if (Preference.UseNetworkED2K &&
                    !ServerConnect.IsConnecting && !ServerConnect.IsConnected)
                {
                    ServerConnect.ConnectToAnyServer();
                }

                // kad
                if ((Preference.UseNetworkKademlia) && !KadEngine.IsRunning)
                {
                    KadEngine.Start();
                }
            }

        }

        public void Stop()
        {
            try
            {
                CloseConnection();
            }
            catch (Exception ex)
            {
                MpdUtilities.DebugLogError("MuleApplication Stop Fail",
                    ex);
            }

            try
            {
                ServerConnect.Stop();
            }
            catch (Exception ex)
            {
                MpdUtilities.DebugLogError("MuleApplication Stop Fail",
                    ex);
            }

            try
            {
                ClientUDP.Close();
            }
            catch (Exception ex)
            {
                MpdUtilities.DebugLogError("MuleApplication Stop Fail",
                    ex);
            }

            try
            {
                ListenSocket.StopListening();
            }
            catch (Exception ex)
            {
                MpdUtilities.DebugLogError("MuleApplication Stop Fail",
                    ex);
            }

            try
            {
                LastCommonRouteFinder.StopFinder();
            }
            catch (Exception ex)
            {
                MpdUtilities.DebugLogError("MuleApplication Stop Fail",
                    ex);
            }

            try
            {
                Preference.Save();
            }
            catch (Exception ex)
            {
                MpdUtilities.DebugLogError("MuleApplication Stop Fail",
                    ex);
            }
        }

        public bool IsRunning { get; set; }

        public Version Version
        {
            get
            {
                return GetType().Assembly.GetName().Version;
            }
        }

        public ClientUDPSocket ClientUDP { get; private set; }
    }
}
