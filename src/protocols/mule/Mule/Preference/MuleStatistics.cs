using System;
using System.Collections.Generic;
using System.Text;

namespace Mule.Preference
{
    [Flags]
    public enum TBPSTATES
    {
        STATE_DOWNLOADING = 0x01,
        STATE_ERROROUS = 0x10
    };

    public interface MuleStatistics
    {
        void Init();
        void RecordRate();
        float GetAvgDownloadRate(int averageType);
        float GetAvgUploadRate(int averageType);

        uint TransferTime { get; }
        uint UploadTime { get; }
        uint DownloadTime { get; }
        uint ServerDuration { get; }
        void Add2TotalServerDuration();
        void UpdateConnectionStats(float uploadrate, float downloadrate);


        ///////////////////////////////////////////////////////////////////////////
        // Down Overhead
        //
        void CompDownDatarateOverhead();
        void ResetDownDatarateOverhead();
        void AddDownDataOverheadSourceExchange(uint data);
        void AddDownDataOverheadFileRequest(uint data);
        void AddDownDataOverheadServer(uint data);
        void AddDownDataOverheadOther(uint data);
        void AddDownDataOverheadKad(uint data);
        void AddDownDataOverheadCrypt(uint data);
        uint DownDatarateOverhead { get; }
        ulong DownDataOverheadSourceExchange { get; }
        ulong DownDataOverheadFileRequest { get; }
        ulong DownDataOverheadServer { get; }
        ulong DownDataOverheadKad { get; }
        ulong DownDataOverheadOther { get; }
        ulong DownDataOverheadSourceExchangePackets { get; }
        ulong DownDataOverheadFileRequestPackets { get; }
        ulong DownDataOverheadServerPackets { get; }
        ulong DownDataOverheadKadPackets { get; }
        ulong DownDataOverheadOtherPackets { get; }


        ///////////////////////////////////////////////////////////////////////////
        // Up Overhead
        //
        void CompUpDatarateOverhead();
        void ResetUpDatarateOverhead();
        void AddUpDataOverheadSourceExchange(uint data);
        void AddUpDataOverheadFileRequest(uint data);
        void AddUpDataOverheadServer(uint data);
        void AddUpDataOverheadKad(uint data);
        void AddUpDataOverheadOther(uint data);
        void AddUpDataOverheadCrypt(uint data);

        uint UpDatarateOverhead { get; }
        ulong UpDataOverheadSourceExchange { get; }
        ulong UpDataOverheadFileRequest { get; }
        ulong UpDataOverheadServer { get; }
        ulong UpDataOverheadKad { get; }
        ulong UpDataOverheadOther { get; }
        ulong UpDataOverheadSourceExchangePackets { get; }
        ulong UpDataOverheadFileRequestPackets { get; }
        ulong UpDataOverheadServerPackets { get; }
        ulong UpDataOverheadKadPackets { get; }
        ulong UpDataOverheadOtherPackets { get; }

        //	Cumulative Stats
        float MaxDown { get; set; }
        float MaxDownAverage { get; set; }
        float CumulativeDownAverage { get; set; }
        float MaxCumulativeDownAverage { get; set; }
        float MaxCumulativeDown { get; set; }
        float CumulativeUpAverage { get; set; }
        float MaxCumulativeUpAverage { get; set; }
        float MaxCumulativeUp { get; set; }
        float MaxUp { get; set; }
        float MaxUpAverage { get; set; }
        float RateDown { get; set; }
        float RateUp { get; set; }
        uint TimeTransfers { get; set; }
        uint TimeDownloads { get; set; }
        uint TimeUploads { get; set; }
        uint StartTimeTransfers { get; set; }
        uint StartTimeDownloads { get; set; }
        uint StartTimeUploads { get; set; }
        uint TimeThisTransfer { get; set; }
        uint TimeThisDownload { get; set; }
        uint TimeThisUpload { get; set; }
        uint TimeServerDuration { get; set; }
        uint TimethisServerDuration { get; set; }
        uint OverallStatus { get; set; }
        float GlobalDone { get; set; }
        float GlobalSize { get; set; }

        ulong SessionReceivedBytes { get; set; }
        ulong SessionSentBytes { get; set; }
        ulong SessionSentBytesToFriend { get; set; }
        ushort Reconnects { get; set; }
        uint TransferStartTime { get; set; }
        uint ServerConnectTime { get; set; }
        uint Filteredclients { get; set; }
        uint StartTime { get; set; }

        void Load();
    }
}
