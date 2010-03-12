#region File Header

//
//  Copyright (C) 2008 Jingnan Si
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
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

using Mule.Core.Network;
using Mule.File;
using Mule.AICH;
using System.Collections.Generic;
using Mule.Definitions;
using Mpd.Generic.Types.IO;
using Mpd.Generic.Types;

namespace Mule.Core
{
    public class UpDownClientList : List<UpDownClient>
    {
    }

    public interface UpDownClient
    {
        void StartDownload();
        void CheckDownloadTimeout();
        void SendCancelTransfer(Packet packet);
        bool IsEd2kClient { get; }
        bool Disconnected(string pszReason, bool bFromSocket);
        bool TryToConnect(bool bIgnoreMaxCon, bool bNoCallbacks);
        void Connect();
        void ConnectionEstablished();
        void OnSocketConnected(int nErrorCode);
        bool CheckHandshakeFinished();
        void CheckFailedFileIdReqs(char[] aucFileHash);
        uint UserIDHybrid { get; set; }
        string UserName { get; set; }
        uint IP { get; set; }
        bool HasLowID { get; }
        uint ConnectIP { get; }
        ushort UserPort { get; set; }
        uint TransferredUp { get; }
        uint TransferredDown { get; }
        uint ServerIP { get; set; }
        ushort ServerPort { get; set; }
        byte[] UserHash { get; set;}

        bool HasValidHash { get; }

        int HashType { get; }
        char[] BuddyID { get; set; }
        bool HasValidBuddyID { get; }
        uint BuddyIP { get; set; }
        ushort BuddyPort { get; set; }
        ClientSoftwareEnum ClientSoft { get; }
        string ClientSoftVer { get; }
        string ClientModVer { get; }
        void InitClientSoftwareVersion();
        uint Version { get; }
        byte MuleVersion { get; }
        bool ExtProtocolAvailable { get; }
        bool SupportMultiPacket { get;}
        bool SupportExtMultiPacket { get; }
        bool SupportPeerCache { get; }
        bool SupportsLargeFiles { get; }
        bool IsEmuleClient { get; }
        byte SourceExchange1Version { get; }
        bool SupportsSourceExchange2 { get; }
        ClientCredits Credits { get; }
        bool IsBanned { get; }
        string ClientFilename { get; set;}
        ushort UDPPort { get; set;}
        byte UDPVersion { get; }
        bool SupportsUDP { get; }
        ushort KadPort { get; set;}
        byte ExtendedRequestsVersion { get; }
        void RequestSharedFileList();
        void ProcessSharedFileList(char[] pachPacket, uint nSize, string pszDirectory);
        ConnectingStateEnum ConnectingState { get; }

        void ClearHelloProperties();
        bool ProcessHelloAnswer(char[] pachPacket, uint nSize);
        bool ProcessHelloPacket(char[] pachPacket, uint nSize);
        void SendHelloAnswer();
        void SendHelloPacket();
        void SendMuleInfoPacket(bool bAnswer);
        void ProcessMuleInfoPacket(char[] pachPacket, uint nSize);
        void ProcessMuleCommentPacket(char[] pachPacket, uint nSize);
        void ProcessEmuleQueueRank(char[] packet, uint size);
        void ProcessEdonkeyQueueRank(char[] packet, uint size);
        void CheckQueueRankFlood();
        bool Compare(UpDownClient tocomp, bool bIgnoreUserhash);
        void ResetFileStatusInfo();
        uint LastSrcReqTime { get; set;}
        uint LastSrcAnswerTime { get; set;}
        uint LastAskedForSources { get; set;}
        bool FriendSlot { get; set;}
        bool IsFriend { get; }
        Friend GetFriend();
        void SetCommentDirty(bool bDirty);
        bool SentCancelTransfer { get; set;}
        void ProcessPublicIPAnswer(byte[] pbyData);
        void SendPublicIPRequest();
        byte KadVersion { get; }
        bool SendBuddyPingPong { get; }
        bool AllowIncomeingBuddyPingPong { get; }
        void SetLastBuddyPingPongTime();
        void ProcessFirewallCheckUDPRequest(SafeMemFile data);
        // secure ident
        void SendPublicKeyPacket();
        void SendSignaturePacket();
        void ProcessPublicKeyPacket(char[] pachPacket, uint nSize);
        void ProcessSignaturePacket(char[] pachPacket, uint nSize);
        byte SecureIdentState { get; }
        void SendSecIdentStatePacket();
        void ProcessSecIdentStatePacket(char[] pachPacket, uint nSize);
        byte InfoPacketsReceived { get; set; }
        bool HasPassedSecureIdent(bool bPassIfUnavailable);
        // preview
        void SendPreviewRequest(AbstractFile pForFile);
        void SendPreviewAnswer(KnownFile pForFile, CxImage.CxImage imgFrames, byte nCount);
        void ProcessPreviewReq(char[] pachPacket, uint nSize);
        void ProcessPreviewAnswer(char[] pachPacket, uint nSize);
        bool PreviewSupport { get;}
        bool ViewSharedFilesSupport { get;}
        bool SafeSendPacket(Packet packet);
        void CheckForGPLEvilDoer();
        // Encryption / Obfuscation / Connectoptions
        bool SupportsCryptLayer { get; set;}
        bool RequestsCryptLayer { get; set;}
        bool RequiresCryptLayer { get; set;}
        bool SupportsDirectUDPCallback { get; set;}
        // shortcut, sets crypt, callback etc based from the tagvalue we recieve
        void SetConnectOptions(byte byOptions, bool bEncryption,
            bool bCallback);
        bool IsObfuscatedConnectionEstablished { get; }
        bool ShouldReceiveCryptUDPPackets { get; }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Upload
        UploadStateEnum UploadState { get; set;}
        void SetUploadState(UploadStateEnum news);
        uint WaitStartTime { get; set;}
        void ClearWaitStartTime();
        uint WaitTime { get; }
        bool IsDownloading { get; }
        bool HasBlocks { get; }
        uint NumberOfRequestedBlocksInQueue { get; }
        uint Datarate { get; }
        uint GetScore(bool sysvalue, bool isdownloading, bool onlybasevalue);
        void AddReqBlock(RequestedBlock reqblock);
        void CreateNextBlockPackage();
        uint UpStartTimeDelay { get; }
        void SetUpStartTime();
        void SendHashsetPacket(char[] fileid);
        byte[] UploadFileID { get;}
        void SetUploadFileID(KnownFile newreqfile);
        uint SendBlockData();
        void ClearUploadBlockRequests();
        void SendRankingInfo();
        void SendCommentInfo(KnownFile file);
        void AddRequestCount(char[] fileid);
        void UnBan();
        void Ban(string pszReason);
        uint AskedCount { get;set; }
        void AddAskedCount();
        // call this when you stop upload, or the socket might be not able to send
        void FlushSendBlocks();
        uint LastUpRequest { get; set; }

        bool HasCollectionUploadSlot { get; set; }

        uint SessionUp { get;}
        void ResetSessionUp();

        uint SessionDown { get;}
        uint SessionPayloadDown { get;}
        void ResetSessionDown();
        uint QueueSessionPayloadUp { get;}
        uint PayloadInBuffer { get;}

        bool ProcessExtendedInfo(SafeMemFile packet, KnownFile tempreqfile);
        ushort UpPartCount { get;}
        bool IsUpPartAvailable(uint iPart);
        byte[] UpPartStatus { get;}
        float CombinedFilePrioAndCredit { get;}

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Download
        uint AskedCountDown { get;set;}
        void AddAskedCountDown();
        DownloadStateEnum DownloadState { get;set;}
        uint GetLastAskedTime(PartFile partFile);
        void SetLastAskedTime();
        bool IsPartAvailable(uint iPart);
        List<bool> PartStatus { get;}
        ushort PartCount { get;}
        uint DownloadDatarate { get;}
        uint RemoteQueueRank { get;}
        void SetRemoteQueueRank(uint nr, bool bUpdateDisplay);
        bool IsRemoteQueueFull { get;set;}
        bool DoesAskForDownload { get;}
        void SendFileRequest();
        void SendStartupLoadReq();
        void ProcessFileInfo(SafeMemFile data, PartFile file);
        void ProcessFileStatus(bool bUdpPacket, SafeMemFile data, PartFile file);
        void ProcessHashSet(char[] data, uint size);
        void ProcessAcceptUpload();
        bool AddRequestForAnotherFile(PartFile file);
        void CreateBlockRequests(int iMaxBlocks);
        void SendBlockRequests();
        bool SendHttpBlockRequests();
        void ProcessBlockPacket(char[] packet, uint size, bool packed, bool bI64Offsets);
        void ProcessHttpBlockPacket(byte[] pucData);
        void ClearDownloadBlockRequests();
        void SendOutOfPartReqsAndAddToWaitingQueue();
        uint CalculateDownloadRate();
        ushort GetAvailablePartCount();
        bool SwapToAnotherFile(string pszReason, bool bIgnoreNoNeeded,
                    bool ignoreSuspensions,
                    bool bRemoveCompletely,
                    PartFile toFile,
                    bool allowSame,
                    bool isAboutToAsk);
        bool SwapToAnotherFile(string pszReason, bool bIgnoreNoNeeded,
            bool ignoreSuspensions,
            bool bRemoveCompletely,
            PartFile toFile,
            bool allowSame,
            bool isAboutToAsk, bool debug);
        // ZZ:DownloadManager
        void DontSwapTo(PartFile file);
        bool IsSwapSuspended(PartFile file,
            bool allowShortReaskTime, bool fileIsNNP);
        // ZZ:DownloadManager
        uint GetTimeUntilReask();
        uint GetTimeUntilReask(PartFile file);
        uint GetTimeUntilReask(PartFile file,
            bool allowShortReaskTime, bool useGivenNNP,
            bool givenNNP);
        void UDPReaskACK(ushort nNewQR);
        void UDPReaskFNF();
        void UDPReaskForDownload();
        bool UDPPacketPending { get;}
        bool IsSourceRequestAllowed();
        bool IsSourceRequestAllowed(PartFile partfile, bool sourceExchangeCheck); // ZZ:DownloadManager

        bool IsValidSource { get;}
        SourceFromEnum SourceFrom { get;set;}

        void SetDownStartTime();
        uint GetDownTimeDifference(bool clear);
        bool TransferredDownMini { get;set;}
        void InitTransferredDownMini();
        uint A4AFCount { get;}

        ushort UpCompleteSourcesCount { get;set;}

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Chat
        ChatStateEnum ChatState { get; set; }
        ChatCaptchaStateEnum ChatCaptchaState { get; set; }
        void ProcessChatMessage(SafeMemFile data, uint nLength);
        void SendChatMessage(string strMessage);
        void ProcessCaptchaRequest(SafeMemFile data);
        void ProcessCaptchaReqRes(byte nStatus);
        // message filtering
        byte MessagesReceived { get; set; }
        void IncMessagesReceived();
        byte MessagesSent { get; set; }
        void IncMessagesSent();
        bool IsSpammer { get; set; }
        bool MessageFiltered { get;set; }


        //KadIPCheck
        KadStateEnum KadState { get; set; }

        //File Comment
        bool HasFileComment { get; }
        string FileComment { get; set;}

        bool HasFileRating { get; }
        byte GetFileRating { get; set;}

        // Barry - Process zip file as it arrives, don't need to wait until end of block
        int Unzip(PendingBlock block,
            byte[] zipped,
            byte[] unzipped, int iRecursion);
        void UpdateDisplayedInfo(bool force);
        int FileListRequested { get; set;}

        PartFile RequestFile { get; set;}

        // AICH Stuff
        AICHHash ReqFileAICHHash { get;set;}
        bool IsSupportingAICH { get;}
        void SendAICHRequest(PartFile pForFile, ushort nPart);
        bool IsAICHReqPending { get;}
        void ProcessAICHAnswer(char[] packet, uint size);
        void ProcessAICHRequest(char[] packet, uint size);
        void ProcessAICHFileHash(SafeMemFile data, PartFile file);

        Utf8StrEnum UnicodeSupport { get;}

        string DownloadStateDisplayString { get;}
        string UploadStateDisplayString { get;}

        string DbgDownloadState { get;}
        string DbgUploadState { get;}
        string DbgKadState { get;}
        string DbgGetClientInfo(bool bFormatIP);
        string DbgFullClientSoftVer { get;}
        string DbgGetHelloInfo();
        string DbgGetMuleInfo();

        // ZZ:DownloadManager -->
        bool IsInNoNeededList(PartFile fileToCheck);
        bool SwapToRightFile(PartFile SwapTo,
            PartFile cur_file, bool ignoreSuspensions,
            bool SwapToIsNNPFile, bool isNNPFile,
            bool wasSkippedDueToSourceExchange,
            bool doAgressiveSwapping,
            bool debug);
        uint LastTriedToConnectTime { get; }
        // <-- ZZ:DownloadManager

        ClientRequestSocket ClientRequestSocket { get;}
        Friend Friend { get;}
        PartFileList OtherRequestsList { get;}
        PartFileList OtherNoNeededList { get;}
        ushort LastPartAsked { get; set; }
        bool DoesAddNextConnect { get;}


        uint SlotNumber { get; set;}
        EMSocket GetFileUploadSocket(bool log);

        ///////////////////////////////////////////////////////////////////////////
        // PeerCache client
        //
        bool IsDownloadingFromPeerCache { get;}
        bool IsUploadingToPeerCache {get;}
        void SetPeerCacheDownState(PeerCacheDownStateEnum eState);
        void SetPeerCacheUpState(PeerCacheUpStateEnum eState);

        int HttpSendState { get; set;}

        bool SendPeerCacheFileRequest();
        bool ProcessPeerCacheQuery(char[] packet, uint size);
        bool ProcessPeerCacheAnswer(char[] packet, uint size);
        bool ProcessPeerCacheAcknowledge(char[] packet, uint size);
        void OnPeerCacheDownSocketClosed(int nErrorCode);
        bool OnPeerCacheDownSocketTimeout();

        bool ProcessPeerCacheDownHttpResponse(string[] astrHeaders);
        bool ProcessPeerCacheDownHttpResponseBody(byte[] pucData);
        void ProcessPeerCacheUpHttpResponse(string[] astrHeaders);
        uint ProcessPeerCacheUpHttpRequest(string[] astrHeaders);

        bool ProcessHttpDownResponse(string[] astrHeaders);
        bool ProcessHttpDownResponseBody(byte[] pucDatae);

        PeerCacheDownloadSocket PeerCacheDownloadSocket { get;}
        PeerCacheUploadSocket PeerCacheUploadSocket { get;}
    }
}
