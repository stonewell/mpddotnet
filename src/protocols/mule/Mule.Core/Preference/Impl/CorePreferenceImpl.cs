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
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Mule.Core.Impl;

namespace Mule.Core.Preference.Impl
{
    [Mule.Xml.Serialization.XmlRoot("MulePreference")]
    class CorePreferenceImpl : MuleBaseObjectImpl, CorePreference
    {
        #region Fields
        private List<string> tempDirectories_ =
            new List<string>();

        private static readonly Mule.Xml.Serialization.XmlSerializer xs_ =
            new Mule.Xml.Serialization.XmlSerializer(typeof(CorePreferenceImpl));

        private string incoming_directory_ = null;

        private object locker_ = new object();
        #endregion

        #region Properties
        public List<string> TempDirectories
        {
            get
            {
                return tempDirectories_;
            }

            set
            {
                tempDirectories_ = value;
            }
        }

        public string IncomingDirectory
        {
            get 
            {
                if (incoming_directory_ == null)
                    return GetDefaultDirectory(DefaultDirectoryEnum.EMULE_INCOMINGDIR, true);

                return new DirectoryInfo(incoming_directory_).FullName; 
            }

            set { incoming_directory_ = value; }
        }

        private string UserNick_;
        public string UserNick
        {
            get { return UserNick_; }
            set { UserNick_ = value; }
        }

        public int MaxUserNickLength
        {
            get { return 50; }
        }

        private bool IsReconnect_;
        public bool IsReconnect
        {
            get { return IsReconnect_; }
            set { IsReconnect_ = value; }
        }


        private string BindAddr_;
        public string BindAddr
        {
            get { return BindAddr_; }
            set { BindAddr_ = value; }
        }

        private UInt16 Port_;
        public UInt16 Port
        {
            get { return Port_; }
            set { Port_ = value; }
        }

        private UInt16 UDPPort_;
        public UInt16 UDPPort
        {
            get { return UDPPort_; }
            set { UDPPort_ = value; }
        }

        private UInt16 ServerUDPPort_;
        public UInt16 ServerUDPPort
        {
            get { return ServerUDPPort_; }
            set { ServerUDPPort_ = value; }
        }

        private byte[] UserHash_ = new byte[16];
        public byte[] UserHash
        {
            get { return UserHash_; }
            set 
            {
                if (value != null)
                {
                    Array.Clear(UserHash_, 0, UserHash_.Length);

                    Array.Copy(value,
                        UserHash_,
                        value.Length > 16 ? 16 : value.Length);
                }
            }
        }

        private UInt16 MinUpload_;
        public UInt16 MinUpload
        {
            get { return MinUpload_; }
            set { MinUpload_ = value; }
        }

        private UInt16 MaxUpload_;
        public UInt16 MaxUpload
        {
            get { return MaxUpload_; }
            set { MaxUpload_ = value; }
        }


        private bool IsICHEnabled_;
        public bool IsICHEnabled
        {
            get { return IsICHEnabled_; }
            set { IsICHEnabled_ = value; }
        }

        private bool DoesAutoUpdateServerList_;
        public bool DoesAutoUpdateServerList
        {
            get { return DoesAutoUpdateServerList_; }
            set { DoesAutoUpdateServerList_ = value; }
        }

        private bool DoesAutoConnect_;
        public bool DoesAutoConnect
        {
            get { return DoesAutoConnect_; }
            set { DoesAutoConnect_ = value; }
        }

        private bool DoesAddServersFromServer_;
        public bool DoesAddServersFromServer
        {
            get { return DoesAddServersFromServer_; }
            set { DoesAddServersFromServer_ = value; }
        }

        private bool DoesAddServersFromClients_;
        public bool DoesAddServersFromClients
        {
            get { return DoesAddServersFromClients_; }
            set { DoesAddServersFromClients_ = value; }
        }


        private bool IsFilterLanIPs_;
        public bool IsFilterLanIPs
        {
            get { return IsFilterLanIPs_; }
            set { IsFilterLanIPs_ = value; }
        }

        private bool DoesAllowLocalHostIP_;
        public bool DoesAllowLocalHostIP
        {
            get { return DoesAllowLocalHostIP_; }
            set { DoesAllowLocalHostIP_ = value; }
        }

        private bool IsOnlineSignatureEnabled_;
        public bool IsOnlineSignatureEnabled
        {
            get { return IsOnlineSignatureEnabled_; }
            set { IsOnlineSignatureEnabled_ = value; }
        }


        private uint MaxSourcePerFileDefault_;
        public uint MaxSourcePerFileDefault
        {
            get { return MaxSourcePerFileDefault_; }
            set { MaxSourcePerFileDefault_ = value; }
        }

        private uint DeadServerRetries_;
        public uint DeadServerRetries
        {
            get { return DeadServerRetries_; }
            set { DeadServerRetries_ = value; }
        }

        private uint ServerKeepAliveTimeout_;
        public uint ServerKeepAliveTimeout
        {
            get { return ServerKeepAliveTimeout_; }
            set { ServerKeepAliveTimeout_ = value; }
        }

        private bool DoesConditionalTCPAccept_;
        public bool DoesConditionalTCPAccept
        {
            get { return DoesConditionalTCPAccept_; }
            set { DoesConditionalTCPAccept_ = value; }
        }


        private ViewSharedFilesAccessEnum CanSeeShares_;
        public ViewSharedFilesAccessEnum CanSeeShares
        {
            get { return CanSeeShares_; }
            set { CanSeeShares_ = value; }
        }


        private bool DoesSmartIdCheck_;
        public bool DoesSmartIdCheck
        {
            get { return DoesSmartIdCheck_; }
            set { DoesSmartIdCheck_ = value; }
        }

        private uint SmartIdState_;
        public uint SmartIdState
        {
            get { return SmartIdState_; }
            set { SmartIdState_ = value; }
        }

        private bool DoesTransferFullChunks_;
        public bool DoesTransferFullChunks
        {
            get { return DoesTransferFullChunks_; }
            set { DoesTransferFullChunks_ = value; }
        }

        private bool DoesNewAutoUpload_;
        public bool DoesNewAutoUpload
        {
            get { return DoesNewAutoUpload_; }
            set { DoesNewAutoUpload_ = value; }
        }

        private bool DoesNewAutoDownload_;
        public bool DoesNewAutoDownload
        {
            get { return DoesNewAutoDownload_; }
            set { DoesNewAutoDownload_ = value; }
        }

        private bool UseCreditSystem_;
        public bool UseCreditSystem
        {
            get { return UseCreditSystem_; }
            set { UseCreditSystem_ = value; }
        }


        private uint FileBufferSize_;
        public uint FileBufferSize
        {
            get { return FileBufferSize_; }
            set { FileBufferSize_ = value; }
        }

        private uint QueueSize_;
        public uint QueueSize
        {
            get { return QueueSize_; }
            set { QueueSize_ = value; }
        }


        private uint MaxConnectionsPerFile_;
        public uint MaxConnectionsPerFile
        {
            get { return MaxConnectionsPerFile_; }
            set { MaxConnectionsPerFile_ = value; }
        }

        public uint DefaultMaxConnectionsPerFile
        {
            get { return 20; }
        }


        private bool IsSafeServerConnectEnabled_;
        public bool IsSafeServerConnectEnabled
        {
            get { return IsSafeServerConnectEnabled_; }
            set { IsSafeServerConnectEnabled_ = value; }
        }

        private bool DoesInspectAllFileTypes_;
        public bool DoesInspectAllFileTypes
        {
            get { return DoesInspectAllFileTypes_; }
            set { DoesInspectAllFileTypes_ = value; }
        }

        private bool DoesExtractMetaData_;
        public bool DoesExtractMetaData
        {
            get { return DoesExtractMetaData_; }
            set { DoesExtractMetaData_ = value; }
        }

        private bool DoesAdjustNTFSDaylightFileTime_;
        public bool DoesAdjustNTFSDaylightFileTime
        {
            get { return DoesAdjustNTFSDaylightFileTime_; }
            set { DoesAdjustNTFSDaylightFileTime_ = value; }
        }


        private string Hostname_;
        public string Hostname
        {
            get { return Hostname_; }
            set { Hostname_ = value; }
        }

        private bool IsCheckDiskspaceEnabled_;
        public bool IsCheckDiskspaceEnabled
        {
            get { return IsCheckDiskspaceEnabled_; }
            set { IsCheckDiskspaceEnabled_ = value; }
        }

        public uint MinFreeDiskSpace
        {
            get { return 20 * 1024 * 1024; }
        }

        private bool UseSparsePartFiles_;
        public bool UseSparsePartFiles
        {
            get { return UseSparsePartFiles_; }
            set { UseSparsePartFiles_ = value; }
        }


        private bool DoesAutoConnectToStaticServersOnly_;
        public bool DoesAutoConnectToStaticServersOnly
        {
            get { return DoesAutoConnectToStaticServersOnly_; }
            set { DoesAutoConnectToStaticServersOnly_ = value; }
        }


        private int IPFilterLevel_;
        public int IPFilterLevel
        {
            get { return IPFilterLevel_; }
            set { IPFilterLevel_ = value; }
        }

        private string MessageFilter_;
        public string MessageFilter
        {
            get { return MessageFilter_; }
            set { MessageFilter_ = value; }
        }

        private string CommentFilter_;
        public string CommentFilter
        {
            get { return CommentFilter_; }
            set { CommentFilter_ = value; }
        }

        private string FilenameCleanups_;
        public string FilenameCleanups
        {
            get { return FilenameCleanups_; }
            set { FilenameCleanups_ = value; }
        }


        private uint MaxSourcesPerFile_;
        public uint MaxSourcesPerFile
        {
            get { return MaxSourcesPerFile_; }
            set { MaxSourcesPerFile_ = value; }
        }

        private uint MaxConnections_;
        public uint MaxConnections
        {
            get { return MaxConnections_; }
            set { MaxConnections_ = value; }
        }

        private uint MaxHalfConnections_;
        public uint MaxHalfConnections
        {
            get { return MaxHalfConnections_; }
            set { MaxHalfConnections_ = value; }
        }


        private bool IsSecureIdentEnabled_;
        public bool IsSecureIdentEnabled
        {
            get { return IsSecureIdentEnabled_; }
            set { IsSecureIdentEnabled_ = value; }
        }

        private bool UseNetworkKademlia_;
        public bool UseNetworkKademlia
        {
            get { return UseNetworkKademlia_; }
            set { UseNetworkKademlia_ = value; }
        }

        private bool UseNetworkED2K_;
        public bool UseNetworkED2K
        {
            get { return UseNetworkED2K_; }
            set { UseNetworkED2K_ = value; }
        }


        private ProxySettings ProxySettings_ = null;

        [Mule.Xml.Serialization.XmlElement(Type=typeof(ProxySettingsImpl))]
        public ProxySettings ProxySettings
        {
            get { return ProxySettings_; }
            set { ProxySettings_ = value; }
        }

        private bool UseA4AFSaveCpu_;
        public bool UseA4AFSaveCpu
        {
            get { return UseA4AFSaveCpu_; }
            set { UseA4AFSaveCpu_ = value; }
        }


        private string HomepageBaseURL_;
        public string HomepageBaseURL
        {
            get { return HomepageBaseURL_; }
            set { HomepageBaseURL_ = value; }
        }

        private string VersionCheckBaseURL_;
        public string VersionCheckBaseURL
        {
            get { return VersionCheckBaseURL_; }
            set { VersionCheckBaseURL_ = value; }
        }

        // PeerCache
        private bool IsPeerCacheDownloadEnabled_;
        public bool IsPeerCacheDownloadEnabled
        {
            get { return IsPeerCacheDownloadEnabled_; }
            set { IsPeerCacheDownloadEnabled_ = value; }
        }

        private uint PeerCacheLastSearch_;
        public uint PeerCacheLastSearch
        {
            get { return PeerCacheLastSearch_; }
            set { PeerCacheLastSearch_ = value; }
        }

        private bool IsPeerCacheFound_;
        public bool IsPeerCacheFound
        {
            get { return IsPeerCacheFound_; }
            set { IsPeerCacheFound_ = value; }
        }

        private UInt16 PeerCachePort_;
        public UInt16 PeerCachePort
        {
            get { return PeerCachePort_; }
            set { PeerCachePort_ = value; }
        }


        // Firewall settings
        private bool IsOpenPortsOnStartupEnabled_;
        public bool IsOpenPortsOnStartupEnabled
        {
            get { return IsOpenPortsOnStartupEnabled_; }
            set { IsOpenPortsOnStartupEnabled_ = value; }
        }


        private bool IsRememberingDownloadedFiles_;
        public bool IsRememberingDownloadedFiles
        {
            get { return IsRememberingDownloadedFiles_; }
            set { IsRememberingDownloadedFiles_ = value; }
        }

        private bool IsRememberingCancelledFiles_;
        public bool IsRememberingCancelledFiles
        {
            get { return IsRememberingCancelledFiles_; }
            set { IsRememberingCancelledFiles_ = value; }
        }


        // encryption
        private bool IsClientCryptLayerSupported_;
        public bool IsClientCryptLayerSupported
        {
            get { return IsClientCryptLayerSupported_; }
            set { IsClientCryptLayerSupported_ = value; }
        }

        private bool IsClientCryptLayerRequested_;
        public bool IsClientCryptLayerRequested
        {
            get { return IsClientCryptLayerRequested_; }
            set { IsClientCryptLayerRequested_ = value; }
        }

        private bool IsClientCryptLayerRequired_;
        public bool IsClientCryptLayerRequired
        {
            get { return IsClientCryptLayerRequired_; }
            set { IsClientCryptLayerRequired_ = value; }
        }

        // not even incoming test connections will be answered
        private bool IsClientCryptLayerRequiredStrict_;
        public bool IsClientCryptLayerRequiredStrict
        {
            get { return IsClientCryptLayerRequiredStrict_; }
            set { IsClientCryptLayerRequiredStrict_ = value; }
        }

        private bool IsServerCryptLayerUDPEnabled_;
        public bool IsServerCryptLayerUDPEnabled
        {
            get { return IsServerCryptLayerUDPEnabled_; }
            set { IsServerCryptLayerUDPEnabled_ = value; }
        }

        private bool IsServerCryptLayerTCPRequested_;
        public bool IsServerCryptLayerTCPRequested
        {
            get { return IsServerCryptLayerTCPRequested_; }
            set { IsServerCryptLayerTCPRequested_ = value; }
        }

        private uint KadUDPKey_;
        public uint KadUDPKey
        {
            get { return KadUDPKey_; }
            set { KadUDPKey_ = value; }
        }

        private byte CryptTCPPaddingLength_;
        public byte CryptTCPPaddingLength
        {
            get { return CryptTCPPaddingLength_; }
            set { CryptTCPPaddingLength_ = value; }
        }


        // UPnP
        private bool DoesSkipWanIPSetup_;
        public bool DoesSkipWanIPSetup
        {
            get { return DoesSkipWanIPSetup_; }
            set { DoesSkipWanIPSetup_ = value; }
        }

        private bool DoesSkipWanPPPSetup_;
        public bool DoesSkipWanPPPSetup
        {
            get { return DoesSkipWanPPPSetup_; }
            set { DoesSkipWanPPPSetup_ = value; }
        }

        private bool IsUPnPEnabled_;
        public bool IsUPnPEnabled
        {
            get { return IsUPnPEnabled_; }
            set { IsUPnPEnabled_ = value; }
        }

        private bool DoesCloseUPnPOnExit_;
        public bool DoesCloseUPnPOnExit
        {
            get { return DoesCloseUPnPOnExit_; }
            set { DoesCloseUPnPOnExit_ = value; }
        }

        private bool IsWinServUPnPImplDisabled_;
        public bool IsWinServUPnPImplDisabled
        {
            get { return IsWinServUPnPImplDisabled_; }
            set { IsWinServUPnPImplDisabled_ = value; }
        }

        private bool IsMinilibUPnPImplDisabled_;
        public bool IsMinilibUPnPImplDisabled
        {
            get { return IsMinilibUPnPImplDisabled_; }
            set { IsMinilibUPnPImplDisabled_ = value; }
        }

        private int LastWorkingUPnPImpl_;
        public int LastWorkingUPnPImpl
        {
            get { return LastWorkingUPnPImpl_; }
            set { LastWorkingUPnPImpl_ = value; }
        }


        // Spamfilter
        private bool IsSearchSpamFilterEnabled_;
        public bool IsSearchSpamFilterEnabled
        {
            get { return IsSearchSpamFilterEnabled_; }
            set { IsSearchSpamFilterEnabled_ = value; }
        }


        private bool IsStoringSearchesEnabled_;
        public bool IsStoringSearchesEnabled
        {
            get { return IsStoringSearchesEnabled_; }
            set { IsStoringSearchesEnabled_ = value; }
        }

        [Mule.Xml.Serialization.XmlIgnore]
        public UInt16 RandomTCPPort
        {
            get { throw new Exception("Not Implement"); }
        }

        [Mule.Xml.Serialization.XmlIgnore]
        public UInt16 RandomUDPPort
        {
            get { throw new Exception("Not Implement"); }
        }

        private List<SharedDirectory> SharedDirectories_ =
            new List<SharedDirectory>();
        [Mule.Xml.Serialization.XmlArray]
        [Mule.Xml.Serialization.XmlArrayItem(Type = typeof(SharedDirectoryImpl))]
        public List<SharedDirectory> SharedDirectories
        {
            get { return SharedDirectories_; }
            set { SharedDirectories_ = value; }
        }


        private List<ServerAddress> ServerAddresses_ =
            new List<ServerAddress>();
        [Mule.Xml.Serialization.XmlArray]
        [Mule.Xml.Serialization.XmlArrayItem(Type = typeof(ServerAddressImpl))]
        public List<ServerAddress> ServerAddresses
        {
            get { return ServerAddresses_; }
            set { ServerAddresses_ = value; }
        }


        private CoreStats Statistics_ = null;            
        [Mule.Xml.Serialization.XmlElement(Type = typeof(CoreStatsImpl))]
        public CoreStats Statistics
        {
            get { return Statistics_; }
            set { Statistics_ = value; }
        }


        private List<FileComments> FileComments_ = 
            new List<FileComments>();
        [Mule.Xml.Serialization.XmlArray]
        [Mule.Xml.Serialization.XmlArrayItem(Type = typeof(FileCommentsImpl))]
        public List<FileComments> FileComments
        {
            get { return FileComments_; }
            set { FileComments_ = value; }
        }
        #endregion

        #region CorePreference Members

        public void Init()
        {
            CreateUserHash();
        }

        private void CreateUserHash()
        {
            for (int i = 0; i < 8; i++)
            {
                UInt16 random = MuleEngine.CoreUtilities.GetRandomUInt16();

                Array.Copy(BitConverter.GetBytes(random), 0,
                    UserHash_, i * 2, 2);
            }

            // mark as emule client. that will be need in later version
            UserHash_[5] = 14;
            UserHash_[14] = 111;
        }

        public bool Save()
        {
            return Save(GetDefaultPreferenceFileName());
        }

        public bool Save(string filename)
        {
            string tmpfile = Path.GetTempFileName();

            try
            {
                if (File.Exists(filename))
                {
                    File.Copy(filename, tmpfile, true);
                }

                BackupUtil.DoBackupData("Mono.Mule.Preference.Backup",
                    filename);

                using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    xs_.Serialize(fs, this);
                }

                return true;
            }
            catch
            {
                if (File.Exists(filename))
                {
                    File.Copy(tmpfile, filename);
                }

                //TODO:Log
                return false;
            }
            finally
            {
                try
                {
                    File.Delete(tmpfile);
                }
                catch
                {
                }
            }
        }

        public string GetTempDir()
        {
            return GetTempDir(0);
        }

        public string GetTempDir(int id)
        {
            if (id < TempDirCount && id >= 0)
            {
                return new DirectoryInfo(tempDirectories_[id]).FullName;
            }

            if (tempDirectories_.Count > 0)
                return new DirectoryInfo(tempDirectories_[0]).FullName;

            return GetDefaultDirectory(DefaultDirectoryEnum.EMULE_TEMPDIR, true);
        }

        [Mule.Xml.Serialization.XmlIgnore]
        public int TempDirCount
        {
            get { return tempDirectories_.Count; }
        }

        public string GetMuleDirectory(DefaultDirectoryEnum eDirectory, bool bCreate)
        {
            switch (eDirectory)
            {
                case DefaultDirectoryEnum.EMULE_INCOMINGDIR:
                    return IncomingDirectory;
                case DefaultDirectoryEnum.EMULE_TEMPDIR:
                    return GetTempDir(0);
                default:
                    return GetDefaultDirectory(eDirectory, bCreate);
            }
        }

        private string GetDefaultDirectory(DefaultDirectoryEnum eDirectory, bool bCreate)
        {
            string appbase = 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".MonoMule");

            string dirpath = null;

            switch (eDirectory)
            {
                case DefaultDirectoryEnum.EMULE_CONFIGBASEDIR:
                    dirpath = appbase;
                    break;
                case DefaultDirectoryEnum.EMULE_CONFIGDIR:
                    dirpath = Path.Combine(appbase, "Config");
                    break;
                case DefaultDirectoryEnum.EMULE_DATABASEDIR:
                    dirpath = appbase;
                    break;
                case DefaultDirectoryEnum.EMULE_EXPANSIONDIR:
                    dirpath = Path.Combine(appbase, "Expansion");
                    break;
                case DefaultDirectoryEnum.EMULE_INCOMINGDIR:
                    dirpath = Path.Combine(appbase, "Incoming");
                    break;
                case DefaultDirectoryEnum.EMULE_LOGDIR:
                    dirpath = Path.Combine(appbase, "Log");
                    break;
                case DefaultDirectoryEnum.EMULE_TEMPDIR:
                    dirpath = Path.Combine(appbase, "Temp");
                    break;
            }

            if (bCreate && !Directory.Exists(dirpath))
            {
                Directory.CreateDirectory(dirpath);
            }

            return new DirectoryInfo(dirpath).FullName;
        }

        public string GetMuleDirectory(DefaultDirectoryEnum eDirectory)
        {
            return GetMuleDirectory(eDirectory, true);
        }

        private const string TEMP_FILE_PATTERNS = 
            @"\d+\.part$|\d+\.part\.met$|\d+\.part\.met\.bak$|\d+\.part\.met\.bakup$";
        private static readonly Regex Temp_File_Regex = 
            new Regex(TEMP_FILE_PATTERNS);

        public bool IsTempFile(string rstrDirectory, string rstrName)
        {
            bool bFound = IsTempDirectory(rstrDirectory);

            if (!bFound) return false;

            return Temp_File_Regex.IsMatch(rstrName.ToLower());
        }

        private bool IsTempDirectory(string rstrDirectory)
        {
            DirectoryInfo rInfo = new DirectoryInfo(rstrDirectory);

            bool ignore_case =
                Environment.OSVersion.Platform != PlatformID.Unix;

            bool bFound = false;

            if (tempDirectories_.Count > 0)
            {
                foreach (string tmpdir in tempDirectories_)
                {
                    DirectoryInfo tInfo = new DirectoryInfo(tmpdir);

                    if (tInfo.FullName.Equals(rInfo.FullName, ignore_case ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    {
                        bFound = true;
                        break;
                    }
                }
            }
            else
            {
                DirectoryInfo tInfo = new DirectoryInfo(GetTempDir());

                if (tInfo.FullName.Equals(rInfo.FullName, ignore_case ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    bFound = true;
                }
            }

            return bFound;
        }

        public bool IsShareableDirectory(string rstrDirectory)
        {
            return !IsInstallationDirectory(rstrDirectory) &&
                !IsTempDirectory(rstrDirectory);
        }

        public bool IsInstallationDirectory(string rstrDir)
        {
            if (MuleEngine.CoreUtilities.IsSameDirectory(rstrDir, GetMuleDirectory(DefaultDirectoryEnum.EMULE_CONFIGDIR)))
                return true;

            return false;
        }

        public bool IsDefaultNick(string nick)
        {
            return CoreConstants.HOME_URL.Equals(nick, StringComparison.OrdinalIgnoreCase);
        }

        public void AddSharedDirectory(SharedDirectory dir)
        {
            RemoveSharedDirectory(dir.FullName);
            SharedDirectories_.Add(dir);
        }

        public void EnableSharedDirectory(string fullname, bool enable)
        {
            foreach (SharedDirectory dir in SharedDirectories_)
            {
                if (dir.FullName.Equals(fullname))
                {
                    dir.IsEnabled = enable;
                    break;
                }
            }
        }

        public void RemoveSharedDirectory(string fullname)
        {
            foreach (SharedDirectory dir in SharedDirectories_)
            {
                if (dir.FullName.Equals(fullname))
                {
                    SharedDirectories_.Remove(dir);
                    break;
                }
            }
        }

        public void AddServer(ServerAddress server)
        {
            RemoveServer(server.Address, server.Port);
            RemoveServer(server.Name);

            ServerAddresses_.Add(server);
        }

        public void RemoveServer(string address, uint port)
        {
            foreach (ServerAddress server in ServerAddresses_)
            {
                if (server.Address.Equals(address) &&
                    server.Port == port)
                {
                    ServerAddresses_.Remove(server);
                    break;
                }
            }
        }

        public void RemoveServer(string name)
        {
            foreach (ServerAddress server in ServerAddresses_)
            {
                if (server.Name.Equals(name))
                {
                    ServerAddresses_.Remove(server);
                    break;
                }
            }
        }

        #endregion

        #region Fields
        public string GetDefaultPreferenceFileName()
        {
            string path = GetMuleDirectory(DefaultDirectoryEnum.EMULE_CONFIGDIR, true);

            return Path.Combine(path, "Preference.xml");
        }

        public void Load()
        {
            Load(GetDefaultPreferenceFileName());
        }

        public void Load(string filename)
        {
            using (FileStream fs = 
                new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                CorePreference tmp =
                    xs_.Deserialize(fs) as CorePreference;

                Type t = typeof(CorePreference);

                PropertyInfo[] infos = t.GetProperties();

                if (infos != null)
                {
                    foreach (PropertyInfo info in infos)
                    {
                        MethodInfo getInfo = info.GetGetMethod();
                        MethodInfo setInfo = info.GetSetMethod();

                        if (setInfo != null && getInfo != null)
                        {
                            setInfo.Invoke(this,
                                new object[] { getInfo.Invoke(tmp, null) });
                        }
                    }
                }
            }

            if (!IsValidUserHash())
                CreateUserHash();

            UserHash_[5] = 14;
            UserHash_[14] = 111;
        }

        private bool IsValidUserHash()
        {
            if (UserHash_ == null)
                return false;

            foreach (byte b in UserHash_)
            {
                if (b != 0x00)
                    return true;
            }

            return false;
        }

        #endregion

        #region Overrides
        public override MuleEngine MuleEngine
        {
            get
            {
                return base.MuleEngine;
            }
            set
            {
                base.MuleEngine = value;

                ProxySettings_ = MuleEngine.CoreObjectManager.CreateProxySettings();
                Statistics_ = MuleEngine.CoreObjectManager.CreateCoreStatistics();
            }
        }

        public void SetFileComment(byte[] hash, string fileComment)
        {
            lock (locker_)
            {
                if (FileComments == null)
                {
                    FileComments = new List<FileComments>();
                }

                bool found = false;

                foreach (FileComments comments in FileComments)
                {
                    if (string.Compare(comments.Name, MuleEngine.CoreUtilities.EncodeHexString(hash)) == 0)
                    {
                        if (comments.Comments != null &&
                            comments.Comments.Count > 0)
                        {
                            comments.Comments[0].Comment = fileComment;
                        }
                        else
                        {
                            if (comments.Comments == null)
                                comments.Comments = new List<FileComment>();

                            FileComment comment = MuleEngine.CoreObjectManager.CreateFileComment();

                            comment.Comment = fileComment;

                            comments.Comments.Add(comment);
                        }

                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    FileComments comments = MuleEngine.CoreObjectManager.CreateFileComments(MuleEngine.CoreUtilities.EncodeHexString(hash));

                    if (comments.Comments == null)
                        comments.Comments = new List<FileComment>();

                    FileComment comment = MuleEngine.CoreObjectManager.CreateFileComment();

                    comment.Comment = fileComment;

                    comments.Comments.Add(comment);

                    FileComments_.Add(comments);
                }
            }
        }

        public void SetFileRating(byte[] hash, uint rating)
        {
            lock (locker_)
            {
                if (FileComments == null)
                {
                    FileComments = new List<FileComments>();
                }

                bool found = false;

                foreach (FileComments comments in FileComments)
                {
                    if (string.Compare(comments.Name, MuleEngine.CoreUtilities.EncodeHexString(hash)) == 0)
                    {
                        if (comments.Comments != null &&
                            comments.Comments.Count > 0)
                        {
                            comments.Comments[0].Rate = rating;
                        }
                        else
                        {
                            if (comments.Comments == null)
                                comments.Comments = new List<FileComment>();

                            FileComment comment = MuleEngine.CoreObjectManager.CreateFileComment();

                            comment.Rate = rating;

                            comments.Comments.Add(comment);
                        }

                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    FileComments comments = MuleEngine.CoreObjectManager.CreateFileComments(MuleEngine.CoreUtilities.EncodeHexString(hash));

                    if (comments.Comments == null)
                        comments.Comments = new List<FileComment>();

                    FileComment comment = MuleEngine.CoreObjectManager.CreateFileComment();

                    comment.Rate = rating;

                    comments.Comments.Add(comment);

                    FileComments_.Add(comments);
                }
            }
        }

        public string GetFileComment(byte[] hash)
        {
            lock (locker_)
            {
                if (FileComments != null)
                {
                    foreach (FileComments comments in FileComments)
                    {
                        if (string.Compare(comments.Name, MuleEngine.CoreUtilities.EncodeHexString(hash)) == 0)
                        {
                            if (comments.Comments != null &&
                                comments.Comments.Count > 0)
                            {
                                if (comments.Comments[0].Comment != null)
                                {
                                    return comments.Comments[0].Comment.Substring(0, CoreConstants.MAXFILECOMMENTLEN);
                                }
                                else
                                {
                                    return string.Empty;
                                }
                            }
                        }
                    }
                }

                return string.Empty;
            }
        }

        public uint GetFileRating(byte[] hash)
        {
            lock (locker_)
            {
                if (FileComments != null)
                {
                    foreach (FileComments comments in FileComments)
                    {
                        if (string.Compare(comments.Name, MuleEngine.CoreUtilities.EncodeHexString(hash)) == 0)
                        {
                            if (comments.Comments != null &&
                                comments.Comments.Count > 0)
                            {
                                if (comments.Comments[0].Comment != null)
                                {
                                    return comments.Comments[0].Rate;
                                }
                                else
                                {
                                    return 0;
                                }
                            }
                        }
                    }
                }

                return 0;
            }
        }

        #endregion
    }
}
