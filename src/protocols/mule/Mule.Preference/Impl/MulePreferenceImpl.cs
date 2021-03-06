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

using Mpd.Utilities;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Mule.Preference.Impl
{
    [System.Xml.Serialization.XmlRoot("MulePreference")]
    class MulePreferenceImpl : MulePreference
    {
        public const string HOME_URL = "http://code.google.com/p/mpddotnet";
        public const int MAXFILECOMMENTLEN = 50;

        #region Fields
        private List<string> tempDirectories_ =
            new List<string>();
        private List<Category> categories_ =
            new List<Category>();

        private string incoming_directory_ = null;

        private object locker_ = new object();
        #endregion

        #region Constructor
        static MulePreferenceImpl()
        {
        }

        public MulePreferenceImpl()
        {
            Init();
        }
        #endregion

        #region Properties
        public uint ConnectionPeakConnections { get; set; }

        public List<string> TempDirectories
        {
            get
            {
                return tempDirectories_;
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

        public string UserNick
        {
            get;
            set;
        }

        public int MaxUserNickLength
        {
            get { return 50; }
        }

        public bool IsReconnect
        {
            get;
            set;
        }


        public string BindAddr
        {
            get;
            set;
        }


        public ushort Port
        {
            get;
            set;
        }


        public ushort UDPPort
        {
            get;
            set;
        }


        public ushort ServerUDPPort
        {
            get;
            set;
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


        public ushort MinUpload
        {
            get;
            set;
        }


        public ushort MaxUpload
        {
            get;
            set;
        }


        public bool IsAICHEnabled
        {
            get;
            set;
        }


        public bool DoesAutoUpdateServerList
        {
            get;
            set;
        }


        public bool DoesAutoConnect
        {
            get;
            set;
        }


        public bool DoesAddServersFromServer
        {
            get;
            set;
        }


        public bool DoesAddServersFromClients
        {
            get;
            set;
        }



        public bool IsFilterLanIPs
        {
            get;
            set;
        }


        public bool DoesAllowLocalHostIP
        {
            get;
            set;
        }


        public bool IsOnlineSignatureEnabled
        {
            get;
            set;
        }



        public uint MaxSourcePerFileDefault
        {
            get;
            set;
        }


        public uint DeadServerRetries
        {
            get;
            set;
        }


        public uint ServerKeepAliveTimeout
        {
            get;
            set;
        }


        public bool DoesConditionalTCPAccept
        {
            get;
            set;
        }


        [DefaultValueAttribute(ViewSharedFilesAccessEnum.vsfaEverybody)]
        public ViewSharedFilesAccessEnum CanSeeShares
        {
            get;
            set;
        }



        public bool DoesSmartIdCheck
        {
            get;
            set;
        }


        public uint SmartIdState
        {
            get;
            set;
        }


        public bool DoesTransferFullChunks
        {
            get;
            set;
        }


        public bool DoesNewAutoUpload
        {
            get;
            set;
        }


        public bool DoesNewAutoDownload
        {
            get;
            set;
        }


        public bool UseCreditSystem
        {
            get;
            set;
        }



        public uint FileBufferSize
        {
            get;
            set;
        }


        public uint QueueSize
        {
            get;
            set;
        }



        public uint MaxConnectionsPerFile
        {
            get;
            set;
        }

        public uint DefaultMaxConnectionsPerFile
        {
            get { return 20; }
        }



        public bool IsSafeServerConnectEnabled
        {
            get;
            set;
        }


        public bool DoesInspectAllFileTypes
        {
            get;
            set;
        }


        public bool DoesExtractMetaData
        {
            get;
            set;
        }


        public bool DoesAdjustNTFSDaylightFileTime
        {
            get;
            set;
        }



        public string Hostname
        {
            get;
            set;
        }


        public bool IsCheckDiskspaceEnabled
        {
            get;
            set;
        }

        public uint MinFreeDiskSpace
        {
            get { return 20 * 1024 * 1024; }
        }


        public bool UseSparsePartFiles
        {
            get;
            set;
        }



        public bool DoesAutoConnectToStaticServersOnly
        {
            get;
            set;
        }



        public int IPFilterLevel
        {
            get;
            set;
        }


        public string MessageFilter
        {
            get;
            set;
        }


        public string CommentFilter
        {
            get;
            set;
        }


        public string FilenameCleanups
        {
            get;
            set;
        }



        public uint MaxSourcesPerFile
        {
            get;
            set;
        }


        public uint MaxConnections
        {
            get;
            set;
        }


        public uint MaxHalfConnections
        {
            get;
            set;
        }



        public bool IsSecureIdentEnabled
        {
            get;
            set;
        }


        public bool UseNetworkKademlia
        {
            get;
            set;
        }


        public bool UseNetworkED2K
        {
            get;
            set;
        }

        [System.Xml.Serialization.XmlElement(Type = typeof(ProxySettingsImpl))]
        public ProxySettings ProxySettings
        {
            get;
            set;
        }


        public bool UseA4AFSaveCpu
        {
            get;
            set;
        }



        public string HomepageBaseURL
        {
            get;
            set;
        }


        public string VersionCheckBaseURL
        {
            get;
            set;
        }

        // PeerCache

        public bool IsPeerCacheDownloadEnabled
        {
            get;
            set;
        }


        public uint PeerCacheLastSearch
        {
            get;
            set;
        }


        public bool IsPeerCacheFound
        {
            get;
            set;
        }


        public ushort PeerCachePort
        {
            get;
            set;
        }


        // Firewall settings

        public bool IsOpenPortsOnStartupEnabled
        {
            get;
            set;
        }



        public bool IsRememberingDownloadedFiles
        {
            get;
            set;
        }


        public bool IsRememberingCancelledFiles
        {
            get;
            set;
        }


        // encryption

        public bool IsClientCryptLayerSupported
        {
            get;
            set;
        }


        public bool IsClientCryptLayerRequested
        {
            get;
            set;
        }


        public bool IsClientCryptLayerRequired
        {
            get;
            set;
        }

        // not even incoming test connections will be answered

        public bool IsClientCryptLayerRequiredStrict
        {
            get;
            set;
        }


        public bool IsServerCryptLayerUDPEnabled
        {
            get;
            set;
        }


        public bool IsServerCryptLayerTCPRequested
        {
            get;
            set;
        }


        public uint KadUDPKey
        {
            get;
            set;
        }


        public byte CryptTCPPaddingLength
        {
            get;
            set;
        }


        // UPnP

        public bool DoesSkipWanIPSetup
        {
            get;
            set;
        }


        public bool DoesSkipWanPPPSetup
        {
            get;
            set;
        }


        public bool IsUPnPEnabled
        {
            get;
            set;
        }


        public bool DoesCloseUPnPOnExit
        {
            get;
            set;
        }


        public bool IsWinServUPnPImplDisabled
        {
            get;
            set;
        }


        public bool IsMinilibUPnPImplDisabled
        {
            get;
            set;
        }


        public int LastWorkingUPnPImpl
        {
            get;
            set;
        }


        // Spamfilter

        public bool IsSearchSpamFilterEnabled
        {
            get;
            set;
        }



        public bool IsStoringSearchesEnabled
        {
            get;
            set;
        }

        [System.Xml.Serialization.XmlIgnore]
        public ushort RandomTCPPort
        {
            get;
            set;
        }

        [System.Xml.Serialization.XmlIgnore]
        public ushort RandomUDPPort
        {
            get;
            set;
        }

        [System.Xml.Serialization.XmlArray]
        [System.Xml.Serialization.XmlArrayItem(Type = typeof(SharedDirectoryImpl))]
        public List<SharedDirectory> SharedDirectories
        {
            get;
            set;
        }


        [System.Xml.Serialization.XmlArray]
        [System.Xml.Serialization.XmlArrayItem(Type = typeof(ServerAddressImpl))]
        public List<ServerAddress> ServerAddresses
        {
            get;
            set;
        }

        [System.Xml.Serialization.XmlArray]
        [System.Xml.Serialization.XmlArrayItem(Type = typeof(FileCommentsImpl))]
        public List<FileComments> FileComments
        {
            get;
            set;
        }
        #endregion

        #region CorePreference Members

        public void Init()
        {
            CreateUserHash();
            SharedDirectories = new List<SharedDirectory>();
            ServerAddresses = new List<ServerAddress>();
            FileComments = new List<FileComments>();
            ProxySettings = MuleApplication.Instance.PreferenceObjectManager.CreateProxySettings();
        }

        private void CreateUserHash()
        {
            for (int i = 0; i < 8; i++)
            {
                ushort random = MpdUtilities.GetRandomUInt16();

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
                if (System.IO.File.Exists(filename))
                {
                    System.IO.File.Copy(filename, tmpfile, true);
                }

                BackupUtil.DoBackupData("Mono.Mule.Preference.Backup",
                    filename);

                using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    MpdUtilities.XmlSerialize(fs, this);
                }

                return true;
            }
            catch
            {
                if (System.IO.File.Exists(filename))
                {
                    System.IO.File.Delete(filename);
                }

                System.IO.File.Move(tmpfile, filename);

                //TODO:Log
                return false;
            }
            finally
            {
                try
                {
                    System.IO.File.Delete(tmpfile);
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

        [System.Xml.Serialization.XmlIgnore]
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
            if (MpdUtilities.IsSameDirectory(rstrDir, GetMuleDirectory(DefaultDirectoryEnum.EMULE_CONFIGDIR)))
                return true;

            return false;
        }

        public bool IsDefaultNick(string nick)
        {
            return HOME_URL.Equals(nick, StringComparison.OrdinalIgnoreCase);
        }

        public void AddSharedDirectory(SharedDirectory dir)
        {
            RemoveSharedDirectory(dir.FullName);
            SharedDirectories.Add(dir);
        }

        public void EnableSharedDirectory(string fullname, bool enable)
        {
            foreach (SharedDirectory dir in SharedDirectories)
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
            foreach (SharedDirectory dir in SharedDirectories)
            {
                if (dir.FullName.Equals(fullname))
                {
                    SharedDirectories.Remove(dir);
                    break;
                }
            }
        }

        public void AddServer(ServerAddress server)
        {
            RemoveServer(server.Address, server.Port);
            RemoveServer(server.Name);

            ServerAddresses.Add(server);
        }

        public void RemoveServer(string address, uint port)
        {
            foreach (ServerAddress server in ServerAddresses)
            {
                if (server.Address.Equals(address) &&
                    server.Port == port)
                {
                    ServerAddresses.Remove(server);
                    break;
                }
            }
        }

        public void RemoveServer(string name)
        {
            foreach (ServerAddress server in ServerAddresses)
            {
                if (server.Name.Equals(name))
                {
                    ServerAddresses.Remove(server);
                    break;
                }
            }
        }

        #endregion

        #region Methods
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
                MpdUtilities.XmlDeserialize(fs, this);
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
                    if (string.Compare(comments.Name, MpdUtilities.EncodeHexString(hash)) == 0)
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

                            FileComment comment = MuleApplication.Instance.PreferenceObjectManager.CreateFileComment();

                            comment.Comment = fileComment;

                            comments.Comments.Add(comment);
                        }

                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    FileComments comments = MuleApplication.Instance.PreferenceObjectManager.CreateFileComments(MpdUtilities.EncodeHexString(hash));

                    if (comments.Comments == null)
                        comments.Comments = new List<FileComment>();

                    FileComment comment = MuleApplication.Instance.PreferenceObjectManager.CreateFileComment();

                    comment.Comment = fileComment;

                    comments.Comments.Add(comment);

                    FileComments.Add(comments);
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
                    if (string.Compare(comments.Name, MpdUtilities.EncodeHexString(hash)) == 0)
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

                            FileComment comment = MuleApplication.Instance.PreferenceObjectManager.CreateFileComment();

                            comment.Rate = rating;

                            comments.Comments.Add(comment);
                        }

                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    FileComments comments = MuleApplication.Instance.PreferenceObjectManager.CreateFileComments(MpdUtilities.EncodeHexString(hash));

                    if (comments.Comments == null)
                        comments.Comments = new List<FileComment>();

                    FileComment comment = MuleApplication.Instance.PreferenceObjectManager.CreateFileComment();

                    comment.Rate = rating;

                    comments.Comments.Add(comment);

                    FileComments.Add(comments);
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
                        if (string.Compare(comments.Name, MpdUtilities.EncodeHexString(hash)) == 0)
                        {
                            if (comments.Comments != null &&
                                comments.Comments.Count > 0)
                            {
                                if (comments.Comments[0].Comment != null)
                                {
                                    return comments.Comments[0].Comment.Substring(0, MAXFILECOMMENTLEN);
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
                        if (string.Compare(comments.Name, MpdUtilities.EncodeHexString(hash)) == 0)
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

        [XmlIgnore]
        public List<string> ServerAutoUpdateUrls
        {
            get;
            set;
        }

        public bool IsFilterServerByIP
        {
            get;
            set;
        }

        public bool CanFSHandleLargeFiles(int cat)
        {
            bool bResult = false;
            for (int i = 0; i != TempDirectories.Count; i++)
            {
                if (!MpdUtilities.IsFileOnFATVolume(TempDirectories[i]))
                {
                    bResult = true;
                    break;
                }
            }
            return bResult && 
                !MpdUtilities.IsFileOnFATVolume((cat > 0) ? GetCategoryPath((uint)cat) : 
                GetMuleDirectory(DefaultDirectoryEnum.EMULE_INCOMINGDIR));
        }

        public bool IsAddNewFilesPaused
        {
            get;
            set;
        }

        public int StartNextFile
        {
            get;
            set;
        }

        public Category GetCategory(int index)
        {
            if (index >= 0 && index < categories_.Count)
                return categories_[index];

            return new Category();
        }

        public int CategoryCount
        {
            get { return categories_.Count; }
        }

        public uint GetMaxDonwload()
        {
            return (uint)(GetMaxDownloadInBytesPerSec() / 1024);
        }

        public ulong GetMaxDownloadInBytesPerSec()
        {
            return GetMaxDownloadInBytesPerSec(false);
        }

        public ulong GetMaxDownloadInBytesPerSec(bool dynamic)
        {
            //dont be a Lam3r :)
            uint maxup;
            if (dynamic && IsDynamicUploadEnabled && 
                MuleApplication.Instance.UploadQueue.WaitingUserCount != 0 &&
                MuleApplication.Instance.UploadQueue.DataRate != 0)
            {
                maxup = MuleApplication.Instance.UploadQueue.DataRate;
            }
            else
            {
                maxup = (uint)(MaxUpload * 1024);
            }

            if (maxup < 4 * 1024)
                return (((maxup < 10 * 1024) && ((ulong)maxup * 3 < MaxDownload * 1024)) ?
                    (ulong)maxup * 3 : MaxDownload * 1024);

            return (((maxup < 10 * 1024) && ((ulong)maxup * 4 < MaxDownload * 1024)) ?
                (ulong)maxup * 4 : MaxDownload * 1024);
        }

        public uint CommitFiles
        {
            get;
            set;
        }

        public string GetCategoryPath(uint nCat)
        {
            return categories_[(int)nCat].IncomingPath;
        }

        public bool IsDynamicUploadEnabled { get; set; }
        public int DynUpPingTolerance { get; set; }
        public int DynUpGoingUpDivider { get; set; }
        public int DynUpGoingDownDivider { get; set; }
        public int DynUpNumberOfPings { get; set; }
        public bool IsDynUpUseMillisecondPingTolerance { get; set; }
        public int DynUpPingToleranceMilliseconds { get; set; }

        public uint MaxDownload { get; set; }
        public bool UseServerPriorities { get; set; }
        public bool UseUserSortedServerList { get; set; }

        private uint maxGraphUploadRate_ = MuleConstants.UNLIMITED;

        public uint MaxGraphUploadRate
        {
            get { return maxGraphUploadRate_; }
            set
            {
                if (value > 0)
                    maxGraphUploadRate_ = value;
                else
                    maxGraphUploadRate_ = MuleConstants.UNLIMITED;
            }
        }

        public uint MaxGraphUploadRateEstimated
        {
            get;
            set;
        }

        public uint GetMaxGraphUploadRate(bool bEstimateIfUnlimited)
        {
            if (maxGraphUploadRate_ != MuleConstants.UNLIMITED || !bEstimateIfUnlimited)
            {
                return maxGraphUploadRate_;
            }
            else
            {
                if (MaxGraphUploadRateEstimated != 0)
                {
                    return MaxGraphUploadRateEstimated + 4;
                }
                else
                    return 16;
            }
        }

        public bool DoesShowOverhead
        {
            get;set;
        }

        public bool DoesWatchClipboard4ED2KLinks
        {
            get;
            set;
        }

        public uint TrafficOMeterInterval
        {
            get;
            set;
        }

        public void EstimateMaxUploadCapability(uint nCurrentUpload)
        {
            if (MaxGraphUploadRateEstimated + 1 < nCurrentUpload)
            {
                MaxGraphUploadRateEstimated = nCurrentUpload;
            }
        }

        public uint StatsSaveInterval
        {
            get;
            set;
        }

        #endregion
    }
}
