using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.File.Impl
{
    class StatisticFileImpl : StatisticFile
    {
        #region Fields
        #endregion

        #region Constructors
        public StatisticFileImpl()
        {
            Requests = 0;
            Transferred = 0;
            Accepts = 0;
            AllTimeRequests = 0;
            AllTimeTransferred = 0;
            AllTimeAccepts = 0;
        }
        #endregion

        #region StatisticFile Members

        public void MergeFileStats(StatisticFile toMerge)
        {
            Requests += toMerge.Requests;
            Accepts += toMerge.Accepts;
            Transferred += toMerge.Transferred;
            AllTimeRequests = AllTimeRequests + toMerge.AllTimeRequests;
            AllTimeTransferred = AllTimeTransferred + toMerge.AllTimeTransferred;
            AllTimeAccepts = AllTimeAccepts + toMerge.AllTimeAccepts;
        }

        public void AddRequest()
        {
            Requests++;
            AllTimeRequests++;
            MuleApplication.Instance.KnownFiles.Requested++;
            MuleApplication.Instance.SharedFiles.UpdateFile(FileParent);
        }

        public void AddAccepted()
        {
            Accepts++;
            AllTimeAccepts++;
            MuleApplication.Instance.KnownFiles.Accepted++;
            MuleApplication.Instance.SharedFiles.UpdateFile(FileParent);
        }

        public void AddTransferred(ulong bytes)
        {
            Transferred+=bytes;
            AllTimeTransferred+=bytes;
            MuleApplication.Instance.KnownFiles.Transferred+=bytes;
            MuleApplication.Instance.SharedFiles.UpdateFile(FileParent);
        }

        public uint Requests
        {
            get;
            set;
        }

        public uint Accepts
        {
            get;
            set;
        }

        public ulong Transferred
        {
            get;
            set;
        }

        public uint AllTimeRequests
        {
            get;
            set;
        }

        public uint AllTimeAccepts
        {
            get;
            set;
        }

        public ulong AllTimeTransferred
        {
            get;
            set;
        }

        public KnownFile FileParent
        {
            get;
            set;
        }

        #endregion
    }
}
