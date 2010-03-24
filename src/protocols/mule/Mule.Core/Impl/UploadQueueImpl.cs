using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core.Impl
{
    class UploadQueueImpl : UploadQueue
    {
        #region UploadQueue Members

        public void AddClientToQueue(UpDownClient client)
        {
            throw new NotImplementedException();
        }

        public void RemoveFromUploadQueue(UpDownClient client, string p)
        {
            throw new NotImplementedException();
        }

        public void Process()
        {
            throw new NotImplementedException();
        }

        public void AddClientToQueue(UpDownClient client, bool bIgnoreTimelimit)
        {
            throw new NotImplementedException();
        }

        public void RemoveFromUploadQueue(UpDownClient client)
        {
            throw new NotImplementedException();
        }

        public bool RemoveFromUploadQueue(UpDownClient client, string pszReason, bool updatewindow)
        {
            throw new NotImplementedException();
        }

        public bool RemoveFromUploadQueue(UpDownClient client, string pszReason, bool updatewindow, bool earlyabort)
        {
            throw new NotImplementedException();
        }

        public bool RemoveFromWaitingQueue(UpDownClient client)
        {
            throw new NotImplementedException();
        }

        public bool RemoveFromWaitingQueue(UpDownClient client, bool updatewindow)
        {
            throw new NotImplementedException();
        }

        public bool IsOnUploadQueue(UpDownClient client)
        {
            throw new NotImplementedException();
        }

        public bool IsDownloading(UpDownClient client)
        {
            throw new NotImplementedException();
        }

        public void UpdateDataRates()
        {
            throw new NotImplementedException();
        }

        public uint DataRate
        {
            get { throw new NotImplementedException(); }
        }

        public uint ToNetworkDataRate
        {
            get { throw new NotImplementedException(); }
        }

        public bool CheckForTimeOver(UpDownClient client)
        {
            throw new NotImplementedException();
        }

        public int WaitingUserCount
        {
            get;
            set;
        }

        public int UploadQueueLength
        {
            get;
            set;
        }

        public uint ActiveUploadsCount
        {
            get;
            set;
        }

        public uint GetWaitingUserForFileCount(IList<object> raFiles, bool bOnlyIfChanged)
        {
            throw new NotImplementedException();
        }

        public uint GetDatarateForFile(IList<object> raFiles)
        {
            throw new NotImplementedException();
        }

        public List<UpDownClient> WaitingList
        {
            get;
            set;
        }

        public List<UpDownClient> UploadingList
        {
            get;
            set;
        }

        public UpDownClient GetWaitingClientByIP_UDP(uint dwIP, ushort nUDPPort, bool bIgnorePortOnUniqueIP)
        {
            throw new NotImplementedException();
        }

        public UpDownClient GetWaitingClientByIP_UDP(uint dwIP, ushort nUDPPort, bool bIgnorePortOnUniqueIP, ref bool pbMultipleIPs)
        {
            throw new NotImplementedException();
        }

        public UpDownClient GetWaitingClientByIP(uint dwIP)
        {
            throw new NotImplementedException();
        }

        public UpDownClient GetNextClient(UpDownClient update)
        {
            throw new NotImplementedException();
        }

        public void DeleteAll()
        {
            throw new NotImplementedException();
        }

        public uint GetWaitingPosition(UpDownClient client)
        {
            throw new NotImplementedException();
        }

        public uint SuccessfullUploadCount
        {
            get;
            set;
        }

        public uint FailedUploadCount
        {
            get;
            set;
        }

        public uint AverageUploadTime
        {
            get;
            set;
        }

        public UpDownClient FindBestClientInQueue()
        {
            throw new NotImplementedException();
        }

        public void ResortUploadSlots()
        {
            throw new NotImplementedException();
        }

        public void ResortUploadSlots(bool force)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
