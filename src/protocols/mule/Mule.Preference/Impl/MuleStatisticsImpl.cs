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

        #endregion

        #region MuleStatistics Members


        public void AddUpDataOverheadServer(int nPacketLen)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region MuleStatistics Members

        public void Init()
        {
            throw new NotImplementedException();
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
            get { throw new NotImplementedException(); }
        }

        public uint UploadTime
        {
            get { throw new NotImplementedException(); }
        }

        public uint DownloadTime
        {
            get { throw new NotImplementedException(); }
        }

        public uint ServerDuration
        {
            get { throw new NotImplementedException(); }
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
            get { throw new NotImplementedException(); }
        }

        public ulong DownDataOverheadSourceExchange
        {
            get { throw new NotImplementedException(); }
        }

        public ulong DownDataOverheadFileRequest
        {
            get { throw new NotImplementedException(); }
        }

        public ulong DownDataOverheadServer
        {
            get { throw new NotImplementedException(); }
        }

        public ulong DownDataOverheadKad
        {
            get { throw new NotImplementedException(); }
        }

        public ulong DownDataOverheadOther
        {
            get { throw new NotImplementedException(); }
        }

        public ulong DownDataOverheadSourceExchangePackets
        {
            get { throw new NotImplementedException(); }
        }

        public ulong DownDataOverheadFileRequestPackets
        {
            get { throw new NotImplementedException(); }
        }

        public ulong DownDataOverheadServerPackets
        {
            get { throw new NotImplementedException(); }
        }

        public ulong DownDataOverheadKadPackets
        {
            get { throw new NotImplementedException(); }
        }

        public ulong DownDataOverheadOtherPackets
        {
            get { throw new NotImplementedException(); }
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
            get { throw new NotImplementedException(); }
        }

        public ulong UpDataOverheadSourceExchange
        {
            get { throw new NotImplementedException(); }
        }

        public ulong UpDataOverheadFileRequest
        {
            get { throw new NotImplementedException(); }
        }

        public ulong UpDataOverheadServer
        {
            get { throw new NotImplementedException(); }
        }

        public ulong UpDataOverheadKad
        {
            get { throw new NotImplementedException(); }
        }

        public ulong UpDataOverheadOther
        {
            get { throw new NotImplementedException(); }
        }

        public ulong UpDataOverheadSourceExchangePackets
        {
            get { throw new NotImplementedException(); }
        }

        public ulong UpDataOverheadFileRequestPackets
        {
            get { throw new NotImplementedException(); }
        }

        public ulong UpDataOverheadServerPackets
        {
            get { throw new NotImplementedException(); }
        }

        public ulong UpDataOverheadKadPackets
        {
            get { throw new NotImplementedException(); }
        }

        public ulong UpDataOverheadOtherPackets
        {
            get { throw new NotImplementedException(); }
        }

        public float MaxDown
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float MaxDownAverage
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float CumulativeDownAverage
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float MaxCumulativeDownAverage
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float MaxCumulativeDown
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float CumulativeUpAverage
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float MaxCumulativeUpAverage
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float MaxCumulativeUp
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float MaxUp
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float MaxUpAverage
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float RateDown
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float RateUp
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public uint TimeTransfers
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public uint TimeDownloads
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public uint TimeUploads
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public uint StartTimeTransfers
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public uint StartTimeDownloads
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public uint StartTimeUploads
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public uint TimeThisTransfer
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public uint TimeThisDownload
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public uint TimeThisUpload
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public uint TimeServerDuration
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public uint TimethisServerDuration
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public uint OverallStatus
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float GlobalDone
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float GlobalSize
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public ulong SessionReceivedBytes
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public ulong SessionSentBytes
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public ulong SessionSentBytesToFriend
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public ushort Reconnects
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public uint TransferStartTime
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public uint ServerConnectTime
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public uint Filteredclients
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public uint StartTime
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
