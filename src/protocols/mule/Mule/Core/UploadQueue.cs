using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.File;

namespace Mule.Core
{
    public interface UploadQueue
    {

        void Process();
        void AddClientToQueue(UpDownClient client);
        void AddClientToQueue(UpDownClient client, bool bIgnoreTimelimit);
        bool RemoveFromUploadQueue(UpDownClient client);
        bool RemoveFromUploadQueue(UpDownClient client, string reason);
        bool RemoveFromUploadQueue(UpDownClient client, string pszReason,
            bool updatewindow);
        bool RemoveFromUploadQueue(UpDownClient client, string pszReason, 
            bool updatewindow, bool earlyabort);
        bool RemoveFromWaitingQueue(UpDownClient client);
        bool RemoveFromWaitingQueue(UpDownClient client, bool updatewindow);
        bool IsOnUploadQueue(UpDownClient client);
        bool IsDownloading(UpDownClient client);

        void UpdateDataRates();
        uint DataRate { get; }
        uint ToNetworkDataRate { get; }

        bool CheckForTimeOver(UpDownClient client);
        int WaitingUserCount { get; }
        int UploadQueueLength { get; }
        uint ActiveUploadsCount { get; }
        uint GetWaitingUserForFileCount(List<KnownFile> raFiles, bool bOnlyIfChanged);
        uint GetDatarateForFile(List<KnownFile> raFiles);

        List<UpDownClient> WaitingList { get; }
        List<UpDownClient> UploadingList { get; }

        UpDownClient GetWaitingClientByIP_UDP(uint dwIP,
        ushort nUDPPort, bool bIgnorePortOnUniqueIP);
        UpDownClient GetWaitingClientByIP_UDP(uint dwIP,
            ushort nUDPPort, bool bIgnorePortOnUniqueIP, ref bool pbMultipleIPs);
        UpDownClient GetWaitingClientByIP(uint dwIP);
        UpDownClient GetNextClient(UpDownClient update);


        void DeleteAll();
        uint GetWaitingPosition(UpDownClient client);

        uint SuccessfullUploadCount { get; }
        uint FailedUploadCount { get; }
        uint AverageUploadTime { get; }

        UpDownClient FindBestClientInQueue();
        void ResortUploadSlots();
        void ResortUploadSlots(bool force);
        void UpdateMaxClientScore();
    }
}
