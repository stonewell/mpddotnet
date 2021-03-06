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
using System.Collections.Generic;
using System.Text;


namespace Mule.Preference
{
    public enum ViewSharedFilesAccessEnum
    {
        vsfaEverybody = 0,
        vsfaFriends = 1,
        vsfaNobody = 2
    };

    public enum NotifierSoundTypeEnum
    {
        ntfstNoSound = 0,
        ntfstSoundFile = 1,
        ntfstSpeech = 2
    };

    public enum DefaultDirectoryEnum
    {
        EMULE_CONFIGDIR,
        EMULE_TEMPDIR,
        EMULE_INCOMINGDIR,
        EMULE_LOGDIR,
        EMULE_DATABASEDIR, // the parent directory of the incoming/temp folder
        EMULE_CONFIGBASEDIR, // the parent directory of the config folder 
        EMULE_EXPANSIONDIR // this is a base directory accessable for all users for things eMule installs
    };

    /*
     *
   ^(:b*){.+}(:b+){.+}(:b+)\{(:b*)get;(:b*)set;(:b*)\}.*$

   private \1 \2_;\npublic \1 \2 \n { \n get { return \2_; } \n set { \2_= value; } \n } \n

   ^(:b*){.+}(:b+){.+}(:b+)\{(:b*)get;(:b*)\}.*$
   private \1 \2_;\npublic \1 \2 \n { \n get { return \2_; } \n } \n 
     * 
     */
    public interface MulePreference
    {
        void Init();

        bool Save();
        bool Save(string filename);

        void Load();
        void Load(string filename);

        string GetTempDir();
        string GetTempDir(int id);
        int TempDirCount { get; }
        List<string> TempDirectories { get; }

        string IncomingDirectory { get; set; }

        string GetMuleDirectory(DefaultDirectoryEnum eDirectory);
        string GetMuleDirectory(DefaultDirectoryEnum eDirectory, bool bCreate);

        bool IsTempFile(string rstrDirectory, string rstrName);
        bool IsShareableDirectory(string rstrDirectory);
        bool IsInstallationDirectory(string rstrDir);

        string UserNick { get; set; }
        int MaxUserNickLength { get; }

        bool IsReconnect { get; set; }

        string BindAddr { get; set; }
        ushort Port { get; set; }
        ushort UDPPort { get; set; }
        ushort ServerUDPPort { get; set; }
        byte[] UserHash { get; set; }
        ushort MinUpload { get; set; }
        ushort MaxUpload { get; set; }

        bool IsAICHEnabled { get; set; }
        bool DoesAutoUpdateServerList { get; set; }
        bool DoesAutoConnect { get; set; }
        bool DoesAddServersFromServer { get; set; }
        bool DoesAddServersFromClients { get; set; }

        bool IsFilterLanIPs { get; set; }
        bool DoesAllowLocalHostIP { get; set; }
        bool IsOnlineSignatureEnabled { get; set; }

        uint MaxSourcePerFileDefault { get; }
        uint DeadServerRetries { get; set; }
        uint ServerKeepAliveTimeout { get; set; }
        bool DoesConditionalTCPAccept { get; set; }

        ViewSharedFilesAccessEnum CanSeeShares { get; set; }

        bool DoesSmartIdCheck { get; set; }
        uint SmartIdState { get; set; }
        bool DoesTransferFullChunks { get; set; }
        bool DoesNewAutoUpload { get; set; }
        bool DoesNewAutoDownload { get; set; }
        bool UseCreditSystem { get; set; }

        uint FileBufferSize { get; set; }
        uint QueueSize { get; set; }

        uint MaxConnectionsPerFile { get; set; }
        uint DefaultMaxConnectionsPerFile { get; }

        bool IsSafeServerConnectEnabled { get; set; }
        bool DoesInspectAllFileTypes { get; set; }
        bool DoesExtractMetaData { get; set; }
        bool DoesAdjustNTFSDaylightFileTime { get; set; }

        string Hostname { get; set; }
        bool IsCheckDiskspaceEnabled { get; set; }
        uint MinFreeDiskSpace { get; }
        bool UseSparsePartFiles { get; set; }

        bool DoesAutoConnectToStaticServersOnly { get; set; }

        int IPFilterLevel { get; set; }
        string MessageFilter { get; set; }
        string CommentFilter { get; set; }
        string FilenameCleanups { get; set; }

        uint MaxSourcesPerFile { get; set; }
        uint MaxConnections { get; set; }
        uint MaxHalfConnections { get; set; }

        bool IsSecureIdentEnabled { get; set; }
        bool UseNetworkKademlia { get; set; }
        bool UseNetworkED2K { get; set; }

        ProxySettings ProxySettings { get; set; }

        bool UseA4AFSaveCpu { get; set; }

        string HomepageBaseURL { get; set; }
        string VersionCheckBaseURL { get; set; }

        bool IsDefaultNick(string strCheck);

        // PeerCache
        bool IsPeerCacheDownloadEnabled { get; set; }
        uint PeerCacheLastSearch { get; set; }
        bool IsPeerCacheFound { get; set; }
        ushort PeerCachePort { get; set; }

        // Firewall settings
        bool IsOpenPortsOnStartupEnabled { get; set; }

        bool IsRememberingDownloadedFiles { get; set; }
        bool IsRememberingCancelledFiles { get; set; }

        // encryption
        bool IsClientCryptLayerSupported { get; set; }
        bool IsClientCryptLayerRequested { get; set; }
        bool IsClientCryptLayerRequired { get; set; }
        // not even incoming test connections will be answered
        bool IsClientCryptLayerRequiredStrict { get; set; }
        bool IsServerCryptLayerUDPEnabled { get; set; }
        bool IsServerCryptLayerTCPRequested { get; set; }
        uint KadUDPKey { get; set; }
        byte CryptTCPPaddingLength { get; set; }

        // UPnP
        bool DoesSkipWanIPSetup { get; set; }
        bool DoesSkipWanPPPSetup { get; set; }
        bool IsUPnPEnabled { get; set; }
        bool DoesCloseUPnPOnExit { get; set; }
        bool IsWinServUPnPImplDisabled { get; set; }
        bool IsMinilibUPnPImplDisabled { get; set; }
        int LastWorkingUPnPImpl { get; set; }

        // Spamfilter
        bool IsSearchSpamFilterEnabled { get; set; }

        bool IsStoringSearchesEnabled { get; set; }

        ushort RandomTCPPort { get; }
        ushort RandomUDPPort { get; }

        uint ConnectionPeakConnections { get; set; }

        List<SharedDirectory> SharedDirectories { get; set; }
        void AddSharedDirectory(SharedDirectory dir);
        void EnableSharedDirectory(string fullname, bool enable);
        void RemoveSharedDirectory(string fullname);

        List<ServerAddress> ServerAddresses { get; set; }
        void AddServer(ServerAddress server);
        void RemoveServer(string address, uint port);
        void RemoveServer(string name);

        List<FileComments> FileComments { get; set; }

        void SetFileComment(byte[] hash, string comment);
        void SetFileRating(byte[] hash, uint rating);
        string GetFileComment(byte[] hash);
        uint GetFileRating(byte[] hash);

        List<string> ServerAutoUpdateUrls { get; set; }

        bool IsFilterServerByIP { get; set; }

        bool CanFSHandleLargeFiles(int cat);

        bool IsAddNewFilesPaused
        {
            get;
            set;
        }

        int StartNextFile { get; set; }

        Category GetCategory(int index);
        int CategoryCount { get; }

        UInt32 GetMaxDonwload();
        ulong GetMaxDownloadInBytesPerSec();
        ulong GetMaxDownloadInBytesPerSec(bool dynamic);

        uint CommitFiles { get; set; }

        string GetCategoryPath(uint nCat);

        bool IsDynamicUploadEnabled { get; set; }
        int DynUpPingTolerance { get; }
        int DynUpGoingUpDivider { get; }
        int DynUpGoingDownDivider { get; }
        int DynUpNumberOfPings { get; }
        bool IsDynUpUseMillisecondPingTolerance { get; }
        int DynUpPingToleranceMilliseconds { get; set; }
    
        uint MaxDownload { get; set; }

        bool UseServerPriorities { get; set; }
        bool UseUserSortedServerList { get;set;}

        uint GetMaxGraphUploadRate(bool p);

        bool DoesShowOverhead { get; set; }
        bool DoesWatchClipboard4ED2KLinks { get; set; }
        uint TrafficOMeterInterval { get; set; }

        void EstimateMaxUploadCapability(uint p);
        uint StatsSaveInterval { get; set; }
        uint MaxGraphUploadRate { get; set; }

        uint MaxGraphUploadRateEstimated
        {
            get;
            set;
        }
    }
}
