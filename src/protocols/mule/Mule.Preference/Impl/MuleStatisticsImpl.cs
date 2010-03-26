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

namespace Mule.Preference.Impl
{
    class MuleStatisticsImpl : MuleStatistics
    {
        #region MuleStatistics Members

        public void AddDownDataOverheadOther(uint size)
        {
            throw new NotImplementedException();
        }

        public void AddDownDataOverheadFileRequest(uint size)
        {
            throw new NotImplementedException();
        }

        public void AddUpDataOverheadFileRequest(uint p)
        {
            throw new NotImplementedException();
        }

        public void AddUpDataOverheadServer(int nPacketLen)
        {
            throw new NotImplementedException();
        }

        public void Init()
        {
        }

        public void RecordRate()
        {
            throw new NotImplementedException();
        }

        public float GetAvgDownloadRate(int averageType)
        {
            throw new NotImplementedException();
        }

        public float GetAvgUploadRate(int averageType)
        {
            throw new NotImplementedException();
        }

        public uint TransferTime
        {
            get;set;
        }

        public uint UploadTime
        {
            get;set;
        }

        public uint DownloadTime
        {
            get;set;
        }

        public uint ServerDuration
        {
            get;set;
        }

        public void Add2TotalServerDuration()
        {
            throw new NotImplementedException();
        }

        public void UpdateConnectionStats(float uploadrate, float downloadrate)
        {
            throw new NotImplementedException();
        }

        public void CompDownDatarateOverhead()
        {
            throw new NotImplementedException();
        }

        public void ResetDownDatarateOverhead()
        {
            throw new NotImplementedException();
        }

        public void AddDownDataOverheadSourceExchange(uint data)
        {
            throw new NotImplementedException();
        }

        public void AddDownDataOverheadServer(uint data)
        {
            throw new NotImplementedException();
        }

        public void AddDownDataOverheadKad(uint data)
        {
            throw new NotImplementedException();
        }

        public void AddDownDataOverheadCrypt(uint data)
        {
            throw new NotImplementedException();
        }

        public uint DownDatarateOverhead
        {
            get;
            set;
        }

        public ulong DownDataOverheadSourceExchange
        {
            get;
            set;
        }

        public ulong DownDataOverheadFileRequest
        {
            get;
            set;
        }

        public ulong DownDataOverheadServer
        {
            get;
            set;
        }

        public ulong DownDataOverheadKad
        {
            get;set;
        }

        public ulong DownDataOverheadOther
        {
            get;
            set;
        }

        public ulong DownDataOverheadSourceExchangePackets
        {
            get;
            set;
        }

        public ulong DownDataOverheadFileRequestPackets
        {
            get;
            set;
        }

        public ulong DownDataOverheadServerPackets
        {
            get;
            set;
        }

        public ulong DownDataOverheadKadPackets
        {
            get;
            set;
        }

        public ulong DownDataOverheadOtherPackets
        {
            get;
            set;
        }

        public void CompUpDatarateOverhead()
        {
            throw new NotImplementedException();
        }

        public void ResetUpDatarateOverhead()
        {
            throw new NotImplementedException();
        }

        public void AddUpDataOverheadSourceExchange(uint data)
        {
            throw new NotImplementedException();
        }

        public void AddUpDataOverheadServer(uint data)
        {
            throw new NotImplementedException();
        }

        public void AddUpDataOverheadKad(uint data)
        {
            throw new NotImplementedException();
        }

        public void AddUpDataOverheadOther(uint data)
        {
            throw new NotImplementedException();
        }

        public void AddUpDataOverheadCrypt(uint data)
        {
            throw new NotImplementedException();
        }

        public uint UpDatarateOverhead
        {
            get;
            set;
        }

        public ulong UpDataOverheadSourceExchange
        {
            get;
            set;
        }

        public ulong UpDataOverheadFileRequest
        {
            get;
            set;
        }

        public ulong UpDataOverheadServer
        {
            get;
            set;
        }

        public ulong UpDataOverheadKad
        {
            get;
            set;
        }

        public ulong UpDataOverheadOther
        {
            get;
            set;
        }

        public ulong UpDataOverheadSourceExchangePackets
        {
            get;
            set;
        }

        public ulong UpDataOverheadFileRequestPackets
        {
            get;
            set;
        }

        public ulong UpDataOverheadServerPackets
        {
            get;
            set;
        }

        public ulong UpDataOverheadKadPackets
        {
            get;
            set;
        }

        public ulong UpDataOverheadOtherPackets
        {
            get;
            set;
        }

        public float MaxDown
        {
            get;
            set;
        }

        public float MaxDownAverage
        {
            get;
            set;
        }

        public float CumulativeDownAverage
        {
            get;
            set;
        }

        public float MaxCumulativeDownAverage
        {
            get;
            set;
        }

        public float MaxCumulativeDown
        {
            get;
            set;
        }

        public float CumulativeUpAverage
        {
            get;
            set;
        }

        public float MaxCumulativeUpAverage
        {
            get;
            set;
        }

        public float MaxCumulativeUp
        {
            get;
            set;
        }

        public float MaxUp
        {
            get;
            set;
        }

        public float MaxUpAverage
        {
            get;
            set;
        }

        public float RateDown
        {
            get;
            set;
        }

        public float RateUp
        {
            get;
            set;
        }

        public uint TimeTransfers
        {
            get;
            set;
        }

        public uint TimeDownloads
        {
            get;
            set;
        }

        public uint TimeUploads
        {
            get;
            set;
        }

        public uint StartTimeTransfers
        {
            get;
            set;
        }

        public uint StartTimeDownloads
        {
            get;
            set;
        }

        public uint StartTimeUploads
        {
            get;
            set;
        }

        public uint TimeThisTransfer
        {
            get;
            set;
        }

        public uint TimeThisDownload
        {
            get;
            set;
        }

        public uint TimeThisUpload
        {
            get;
            set;
        }

        public uint TimeServerDuration
        {
            get;
            set;
        }

        public uint TimethisServerDuration
        {
            get;
            set;
        }

        public uint OverallStatus
        {
            get;
            set;
        }

        public float GlobalDone
        {
            get;
            set;
        }

        public float GlobalSize
        {
            get;
            set;
        }

        public ulong SessionReceivedBytes
        {
            get;
            set;
        }

        public ulong SessionSentBytes
        {
            get;
            set;
        }

        public ulong SessionSentBytesToFriend
        {
            get;
            set;
        }

        public ushort Reconnects
        {
            get;
            set;
        }

        public uint TransferStartTime
        {
            get;
            set;
        }

        public uint ServerConnectTime
        {
            get;
            set;
        }

        public uint Filteredclients
        {
            get;
            set;
        }

        public uint StartTime
        {
            get;
            set;
        }

        public void Load()
        {
        }

        #endregion

        #region MuleStatistics Members


        public void Save()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
