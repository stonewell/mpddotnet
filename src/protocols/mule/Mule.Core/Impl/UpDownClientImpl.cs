using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.Network;


namespace Mule.Core.Impl
{
    class UpDownClientImpl : UpDownClient
    {
        //    protected int PacketReceivedSEH(Packet packet)
        //    {
        //        throw new Exception("hh");
        //    }

        //    protected bool PacketReceivedCppEH(Packet packet)
        //    {
        //        throw new Exception("hh");
        //    }

        //    protected bool ProcessPacket(byte[] packet, uint size, uint opcodeInt)
        //    {
        //try
        //{
        //    try
        //    {
        //        OperationCodeEnum opcode = (OperationCodeEnum)opcodeInt;

        //        if (client_ == null && opcode != OperationCodeEnum.OP_HELLO)
        //        {
        //            MuleEngine.Preference.Statistics.AddDownDataOverheadOther(size);
        //            throw new MuleException("No Hello");
        //        }
        //        else if (client_ != null && 
        //            opcode != OperationCodeEnum.OP_HELLO && 
        //            opcode != OperationCodeEnum.OP_HELLOANSWER)
        //            CheckHandshakeFinished();
        //        switch(opcode)
        //        {
        //            case OperationCodeEnum.OP_HELLOANSWER:
        //            {
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadOther(size);
        //                ProcessHelloAnswer(packet,size);

        //                // start secure identification, if
        //                //  - we have received OP_EMULEINFO and OP_HELLOANSWER (old eMule)
        //                //	- we have received eMule-OP_HELLOANSWER (new eMule)
        //                if (GetInfoPacketsReceived() == InfoPacketStateEnum.IP_BOTH)
        //                    InfoPacketsReceived();

        //                if (client_ != null)
        //                {
        //                    ConnectionEstablished();
        //                }
        //                break;
        //            }
        //            case OperationCodeEnum.OP_HELLO:
        //            {
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadOther(size);

        //                bool bNewClient = client_ == null;
        //                if (bNewClient)
        //                {
        //                    // create new client_ to save standart informations
        //                    client_ = MuleEngine.CoreObjectManager.CreateUpDownClient(this);
        //                }

        //                bool bIsMuleHello = false;
        //                try
        //                {
        //                    bIsMuleHello = ProcessHelloPacket(packet,size);
        //                }
        //                catch
        //                {
        //                    if (bNewClient)
        //                    {
        //                        // Don't let CUpDownClient::Disconnected be processed for a client_ which is not in the list of clients.
        //                        client_ = null;
        //                    }
        //                    throw;
        //                }

        //                // now we check if we know this client_ already. if yes this socket will
        //                // be attached to the known client_, the new client_ will be deleted
        //                // and the var. "client_" will point to the known 
        //                // if not we keep our new-constructed client_ ;)
        //                if (MuleEngine.ClientList.AttachToAlreadyKnown(out client_,this))
        //                {
        //                    // update the old client_ informations
        //                    bIsMuleHello = ProcessHelloPacket(packet,size);
        //                }
        //                else 
        //                {
        //                    MuleEngine.ClientList.AddClient(client_);
        //                    SetCommentDirty();
        //                }

        //                // send a response packet with standart informations
        //                if (HashType == (int)ClientSoftwareEnum.SO_EMULE && !bIsMuleHello)
        //                    SendMuleInfoPacket(false);

        //                SendHelloAnswer();

        //                if (client_ !=null)
        //                    ConnectionEstablished();

        //                Debug.Assert( client_ != null);

        //                if(client_ !=null)
        //                {
        //                    // start secure identification, if
        //                    //	- we have received eMule-OP_HELLO (new eMule)
        //                    if (GetInfoPacketsReceived() == InfoPacketStateEnum.IP_BOTH)
        //                        InfoPacketsReceived();

        //                    if( KadPort != 0 && KadVersion > 1)
        //                        MuleEngine.KadEngine.Bootstrap(IP, KadPort);
        //                }
        //                break;
        //            }
        //            case OperationCodeEnum.OP_REQUESTFILENAME:
        //            {
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadFileRequest(size);

        //                if (size >= 16)
        //                {
        //                    if (0 != GetWaitStartTime())
        //                        SetWaitStartTime();

        //                    SafeMemFile data_in = MpdObjectManager.CreateSafeMemFile(packet, size);
        //                    byte[] reqfilehash = new byte[16];
        //                    data_in.ReadHash16(reqfilehash);

        //                    KnownFile reqfile;
        //                    if ( (reqfile = MuleEngine.SharedFiles.GetFileByID(reqfilehash)) == null ){
        //                        if ( !((reqfile = MuleEngine.DownloadQueue.GetFileByID(reqfilehash)) != null
        //                               && reqfile.FileSize > (ulong)MuleConstants.PARTSIZE) )
        //                        {
        //                            CheckFailedFileIdReqs(reqfilehash);
        //                            break;
        //                        }
        //                    }

        //                    if (reqfile.IsLargeFile && !SupportsLargeFiles){
        //                        MpdUtilities.DebugLogWarning(("Client without 64bit file support requested large file; %s, File=\"%s\""), DbgGetClientInfo(), reqfile.FileName);
        //                        break;
        //                    }

        //                    // check to see if this is a new file they are asking for
        //                    if (MpdUtilities.Md4Cmp(UploadFileID, reqfilehash) != 0)
        //                        SetCommentDirty();
        //                    SetUploadFileID(reqfile);

        //                    if (!ProcessExtendedInfo(data_in, reqfile)){
        //                        Packet replypacket = NetworkObjectManager.CreatePacket(OperationCodeEnum.OP_FILEREQANSNOFIL, 16);
        //                        MpdUtilities.Md4Cpy(replypacket.Buffer, reqfile.FileHash);
        //                        MuleEngine.Preference.Statistics.AddUpDataOverheadFileRequest(replypacket.Size);
        //                        SendPacket(replypacket, true);
        //                        MpdUtilities.DebugLogWarning(("Partcount mismatch on requested file, sending FNF; {0}, File=\"{1}\""), DbgGetClientInfo(), reqfile.FileName);
        //                        break;
        //                    }

        //                    // if we are downloading this file, this could be a new source
        //                    // no passive adding of files with only one part
        //                    if (reqfile.IsPartFile && reqfile.FileSize > (ulong)MuleConstants.PARTSIZE)
        //                    {
        //                        if ((reqfile as PartFile).MaxSources > (reqfile as PartFile).SourceCount)
        //                            MuleEngine.DownloadQueue.CheckAndAddKnownSource(reqfile as PartFile, client_, true);
        //                    }

        //                    // send filename etc
        //                    SafeMemFile data_out = MpdObjectManager.CreateSafeMemFile(128);
        //                    data_out.WriteHash16(reqfile.FileHash);
        //                    data_out.WriteString(reqfile.FileName, UnicodeSupport);
        //                    Packet outPacket = NetworkObjectManager.CreatePacket(data_out);
        //                    outPacket.OperationCode = OperationCodeEnum.OP_REQFILENAMEANSWER;
        //                    MuleEngine.Preference.Statistics.AddUpDataOverheadFileRequest(outPacket.Size);
        //                    SendPacket(outPacket, true);

        //                    SendCommentInfo(reqfile);
        //                    break;
        //                }
        //                throw new MuleException("Wrong Packet Size");
        //                break;
        //            }
        //            case OperationCodeEnum.OP_SETREQFILEID:
        //            {
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadFileRequest(size);

        //                if (size == 16)
        //                {
        //                    if (GetWaitStartTime() == 0)
        //                        SetWaitStartTime();

        //                    KnownFile reqfile;
        //                    if ( (reqfile = MuleEngine.SharedFiles.GetFileByID(packet)) == null ){
        //                        if ( !((reqfile = MuleEngine.DownloadQueue.GetFileByID(packet)) != null
        //                               && reqfile.FileSize > (ulong)MuleConstants.PARTSIZE) )
        //                        {
        //                            // send file request no such file packet (0x48)
        //                            Packet replypacket = NetworkObjectManager.CreatePacket(OperationCodeEnum.OP_FILEREQANSNOFIL, 16);
        //                            MpdUtilities.Md4Cpy(replypacket.Buffer, packet);
        //                            MuleEngine.Preference.Statistics.AddUpDataOverheadFileRequest(replypacket.Size);
        //                            SendPacket(replypacket, true);
        //                            CheckFailedFileIdReqs(packet);
        //                            break;
        //                        }
        //                    }
        //                    if (reqfile.IsLargeFile && !SupportsLargeFiles){
        //                        Packet replypacket = NetworkObjectManager.CreatePacket(OperationCodeEnum.OP_FILEREQANSNOFIL, 16);
        //                        MpdUtilities.Md4Cpy(replypacket.Buffer, packet);
        //                        MuleEngine.Preference.Statistics.AddUpDataOverheadFileRequest(replypacket.Size);
        //                        SendPacket(replypacket, true);
        //                        MpdUtilities.DebugLogWarning(("Client without 64bit file support requested large file; %s, File=\"%s\""), DbgGetClientInfo(), reqfile.FileName);
        //                        break;
        //                    }

        //                    // check to see if this is a new file they are asking for
        //                    if (MpdUtilities.Md4Cmp(UploadFileID, packet) != 0)
        //                        SetCommentDirty();

        //                    SetUploadFileID(reqfile);

        //                    // send filestatus
        //                    SafeMemFile data = MpdObjectManager.CreateSafeMemFile(16+16);
        //                    data.WriteHash16(reqfile.FileHash);
        //                    if (reqfile.IsPartFile)
        //                        (reqfile as PartFile).WritePartStatus(data);
        //                    else
        //                        data.WriteUInt16(0);
        //                    Packet statuspacket = NetworkObjectManager.CreatePacket(data);
        //                    statuspacket.OperationCode = OperationCodeEnum.OP_FILESTATUS;
        //                    MuleEngine.Preference.Statistics.AddUpDataOverheadFileRequest(statuspacket.Size);
        //                    SendPacket(statuspacket, true);
        //                    break;
        //                }
        //                throw new MuleException("Wrong Packet Size");
        //                break;
        //            }
        //            case OperationCodeEnum.OP_FILEREQANSNOFIL:
        //            {
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadFileRequest(size);
        //                if (size == 16)
        //                {
        //                    PartFile reqfile = MuleEngine.DownloadQueue.GetFileByID(packet);
        //                    if (reqfile == null){
        //                        CheckFailedFileIdReqs(packet);
        //                        break;
        //                    }
        //                    else
        //                        reqfile.DeadSourceList.AddDeadSource(client_ );
        //                    // if that client_ does not have my file maybe has another different
        //                    // we try to swap to another file ignoring no needed parts files
        //                    switch (DownloadState)
        //                    {
        //                        case DownloadStateEnum.DS_CONNECTED:
        //                        case DownloadStateEnum.DS_ONQUEUE:
        //                        case DownloadStateEnum.DS_NONEEDEDPARTS:
        //                            DontSwapTo(RequestFile); // ZZ:DownloadManager
        //                            if (!SwapToAnotherFile(("Source says it doesn't have the file. CClientReqSocket::ProcessPacket()"), true, true, true, null, false, false)) { // ZZ:DownloadManager
        //                                MuleEngine.DownloadQueue.RemoveSource(client_);
        //                            }
        //                        break;
        //                    }
        //                    break;
        //                }
        //                throw new MuleException("Wrong Packet Size");
        //                break;
        //            }
        //            case OperationCodeEnum.OP_REQFILENAMEANSWER:
        //            {
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadFileRequest(size);

        //                SafeMemFile data = MpdObjectManager.CreateSafeMemFile(packet, size);
        //                byte[] cfilehash = new byte[16];
        //                data.ReadHash16(cfilehash);
        //                PartFile file = MuleEngine.DownloadQueue.GetFileByID(cfilehash);
        //                if (file == null)
        //                    CheckFailedFileIdReqs(cfilehash);
        //                ProcessFileInfo(data, file);
        //                break;
        //            }
        //            case OperationCodeEnum.OP_FILESTATUS:
        //            {
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadFileRequest(size);

        //                SafeMemFile data = MpdObjectManager.CreateSafeMemFile(packet, size);
        //                byte[] cfilehash = new byte[16];
        //                data.ReadHash16(cfilehash);
        //                PartFile file = MuleEngine.DownloadQueue.GetFileByID(cfilehash);
        //                if (file == null)
        //                    CheckFailedFileIdReqs(cfilehash);
        //                ProcessFileStatus(false, data, file);
        //                break;
        //            }
        //            case OperationCodeEnum.OP_STARTUPLOADREQ:
        //            {
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadFileRequest(size);

        //                if (!CheckHandshakeFinished())
        //                    break;
        //                if (size == 16)
        //                {
        //                    KnownFile reqfile = MuleEngine.SharedFiles.GetFileByID(packet);
        //                    if (reqfile != null)
        //                    {
        //                        if (MpdUtilities.Md4Cmp(UploadFileID, packet) != 0)
        //                            SetCommentDirty();
        //                        SetUploadFileID(reqfile);
        //                        SendCommentInfo(reqfile);
        //                        MuleEngine.UploadQueue.AddClientToQueue(client_);
        //                    }
        //                    else
        //                        CheckFailedFileIdReqs(packet);
        //                }
        //                break;
        //            }
        //            case OperationCodeEnum.OP_QUEUERANK:
        //            {
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadFileRequest(size);
        //                ProcessEdonkeyQueueRank(packet, size);
        //                break;
        //            }
        //            case OperationCodeEnum.OP_ACCEPTUPLOADREQ:
        //            {
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadFileRequest(size);
        //                ProcessAcceptUpload();
        //                break;
        //            }
        //            case OperationCodeEnum.OP_REQUESTPARTS:
        //            {
        //                // see also OP_REQUESTPARTS_I64
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadFileRequest(size);

        //                SafeMemFile data = MpdObjectManager.CreateSafeMemFile(packet, size);
        //                byte[] reqfilehash = new byte[16];
        //                data.ReadHash16(reqfilehash);

        //                uint[] auStartOffsets = new uint[3];
        //                auStartOffsets[0] = data.ReadUInt32();
        //                auStartOffsets[1] = data.ReadUInt32();
        //                auStartOffsets[2] = data.ReadUInt32();

        //                uint[] auEndOffsets = new uint[3];
        //                auEndOffsets[0] = data.ReadUInt32();
        //                auEndOffsets[1] = data.ReadUInt32();
        //                auEndOffsets[2] = data.ReadUInt32();

        //                for (int i = 0; i < auStartOffsets.Length; i++)
        //                {
        //                    if (auEndOffsets[i] > auStartOffsets[i])
        //                    {
        //                        RequestedBlock reqblock;
        //                        reqblock.StartOffset = auStartOffsets[i];
        //                        reqblock.EndOffset = auEndOffsets[i];
        //                        MpdUtilities.Md4Cpy(reqblock.FileID, reqfilehash);
        //                        reqblock.Transferred = 0;
        //                        AddReqBlock(reqblock);
        //                    }
        //                }
        //                break;
        //            }
        //            case OperationCodeEnum.OP_CANCELTRANSFER:
        //            {
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadFileRequest(size);
        //                MuleEngine.UploadQueue.RemoveFromUploadQueue(client_, ("Remote client_ cancelled transfer."));
        //                break;
        //            }
        //            case OperationCodeEnum.OP_END_OF_DOWNLOAD:
        //            {
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadFileRequest(size);
        //                if (size>=16 && MpdUtilities.Md4Cmp(UploadFileID,packet) == 0)
        //                    MuleEngine.UploadQueue.RemoveFromUploadQueue(client_, ("Remote client_ ended transfer."));
        //                else
        //                    CheckFailedFileIdReqs(packet);
        //                break;
        //            }
        //            case OperationCodeEnum.OP_HASHSETREQUEST:
        //            {
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadFileRequest(size);

        //                if (size != 16)
        //                    throw new MuleException("Wrong Packet Size");
        //                SendHashsetPacket(packet, 16, false);
        //                break;
        //            }
        //            case OperationCodeEnum.OP_HASHSETANSWER:
        //            {
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadFileRequest(size);
        //                ProcessHashSet(packet, size, false);
        //                break;
        //            }
        //            case OperationCodeEnum.OP_SENDINGPART:
        //            {
        //                // see also OP_SENDINGPART_I64
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadFileRequest(24);
        //                if (RequestFile != null && 
        //                    !RequestFile.IsStopped && 
        //                    (RequestFile.Status==PartFileStatusEnum.PS_READY || RequestFile.Status==PartFileStatusEnum.PS_EMPTY))
        //                {
        //                    ProcessBlockPacket(packet, size, false, false);
        //                    if (RequestFile.IsStopped || RequestFile.Status==PartFileStatusEnum.PS_PAUSED || RequestFile.Status==PartFileStatusEnum.PS_ERROR)
        //                    {
        //                        SendCancelTransfer();
        //                        DownloadState = 
        //                            RequestFile.IsStopped ? DownloadStateEnum.DS_NONE : DownloadStateEnum.DS_ONQUEUE;
        //                    }
        //                }
        //                else
        //                {
        //                    SendCancelTransfer();
        //                    DownloadState = (RequestFile==null || 
        //                        RequestFile.IsStopped) ? DownloadStateEnum.DS_NONE : DownloadStateEnum.DS_ONQUEUE;
        //                }
        //                break;
        //            }
        //            case OperationCodeEnum.OP_OUTOFPARTREQS:
        //            {
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadFileRequest(size);
        //                if (DownloadState == DownloadStateEnum.DS_DOWNLOADING)
        //                {
        //                    SetDownloadState(DownloadStateEnum.DS_ONQUEUE, ("The remote client_ decided to stop/complete the transfer (got OP_OutOfPartReqs)."));
        //                }
        //                break;
        //            }
        //            case OperationCodeEnum.OP_CHANGE_CLIENT_ID:
        //            {
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadOther(size);

        //                SafeMemFile data = MpdObjectManager.CreateSafeMemFile(packet, size);
        //                uint nNewUserID = data.ReadUInt32();
        //                uint nNewServerIP = data.ReadUInt32();
        //                if (MuleUtilities.IsLowID(nNewUserID))
        //                {	// client_ changed server and has a LowID
        //                    ED2KServer pNewServer = MuleEngine.ServerList.GetServerByIP(nNewServerIP);
        //                    if (pNewServer != null)
        //                    {
        //                        UserIDHybrid = (nNewUserID); // update UserID only if we know the server
        //                        ServerIP = (nNewServerIP);
        //                        ServerPort = (pNewServer.Port);
        //                    }
        //                }
        //                else if (nNewUserID == IP)
        //                {	// client_ changed server and has a HighID(IP)
        //                    UserIDHybrid = ((nNewUserID));
        //                    ED2KServer pNewServer = MuleEngine.ServerList.GetServerByIP(nNewServerIP);
        //                    if (pNewServer != null)
        //                    {
        //                        ServerIP = (nNewServerIP);
        //                        ServerPort = (pNewServer.Port);
        //                    }
        //                }
        //                break;
        //            }
        //            case OperationCodeEnum.OP_CHANGE_SLOT:
        //            {
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadFileRequest(size);

        //                // sometimes sent by Hybrid
        //                break;
        //            }
        //            case OperationCodeEnum.OP_MESSAGE:
        //            {
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadOther(size);

        //                if (size < 2)
        //                    throw new MuleException(("invalid message packet"));
        //                SafeMemFile data = MpdObjectManager.CreateSafeMemFile(packet, size);
        //                uint length = data.ReadUInt16();
        //                if (length+2 != size)
        //                    throw new MuleException(("invalid message packet"));

        //                if (length > MuleConstants.MAX_CLIENT_MSG_LEN){
        //                    length = MuleConstants.MAX_CLIENT_MSG_LEN;
        //                }					

        //                ProcessChatMessage(data, length);
        //                break;
        //            }
        //            case OperationCodeEnum.OP_ASKSHAREDFILES:
        //            {	
        //                // client_ wants to know what we have in share, let's see if we allow him to know that
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadOther(size);


        //                List<object> list = new List<object>();
        //                if (MuleEngine.Preference.CanSeeShares== ViewSharedFilesAccessEnum.vsfaEverybody || 
        //                    (MuleEngine.Preference.CanSeeShares==ViewSharedFilesAccessEnum.vsfaFriends && 
        //                    IsFriend))
        //                {
        //                    Dictionary<MapCKey, KnownFile>.Enumerator it = 
        //                        MuleEngine.SharedFiles.FilesMap.GetEnumerator();

        //                    while(it.MoveNext())
        //                    {
        //                        KnownFile cur_file = it.Current.Value;
        //                        if (!cur_file.IsLargeFile || SupportsLargeFiles)
        //                            list.Add(cur_file);
        //                    }
        //                }

        //                // now create the memfile for the packet
        //                uint iTotalCount = list.GetCount();
        //                CSafeMemFile tempfile(80);
        //                tempfile.WriteUInt32(iTotalCount);
        //                while (list.GetCount())
        //                {
        //                    MuleEngine.SharedFiles.CreateOfferedFilePacket((KnownFile)list.GetHead(), &tempfile, null, client_);
        //                    list.RemoveHead();
        //                }

        //                // create a packet and send it
        //                if (MuleEngine.Preference.GetDebugClientTCPLevel() > 0)
        //                    DebugSend("OP__AskSharedFilesAnswer", client_);
        //                Packet* replypacket = new Packet(&tempfile);
        //                replypacket.opcode = OperationCodeEnum.OP_ASKSHAREDFILESANSWER;
        //                MuleEngine.Preference.Statistics.AddUpDataOverheadOther(replypacket.size);
        //                SendPacket(replypacket, true, true);
        //                break;
        //            }
        //            case OperationCodeEnum.OP_ASKSHAREDFILESANSWER:
        //            {
        //                if (MuleEngine.Preference.GetDebugClientTCPLevel() > 0)
        //                    DebugRecv("OP_AskSharedFilesAnswer", client_);
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadOther(size);
        //                ProcessSharedFileList(packet,size);
        //                break;
        //            }
        //            case OperationCodeEnum.OP_ASKSHAREDDIRS:
        //            {
        //                if (MuleEngine.Preference.GetDebugClientTCPLevel() > 0)
        //                    DebugRecv("OP_AskSharedDirectories", client_);
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadOther(size);

        //                if (MuleEngine.Preference.CanSeeShares()==vsfaEverybody || (MuleEngine.Preference.CanSeeShares()==vsfaFriends && IsFriend()))
        //                {
        //                    AddLogLine(true, GetResString(IDS_SHAREDREQ1), GetUserName(), GetUserIDHybrid(), GetResString(IDS_ACCEPTED));
        //                    SendSharedDirectories();
        //                }
        //                else
        //                {
        //                    DebugLog(GetResString(IDS_SHAREDREQ1), GetUserName(), GetUserIDHybrid(), GetResString(IDS_DENIED));
        //                    if (MuleEngine.Preference.GetDebugClientTCPLevel() > 0)
        //                        DebugSend("OP__AskSharedDeniedAnswer", client_);
        //                    Packet* replypacket = new Packet(OP_ASKSHAREDDENIEDANS, 0);
        //                    MuleEngine.Preference.Statistics.AddUpDataOverheadOther(replypacket.size);
        //                    SendPacket(replypacket, true, true);
        //                }
        //                break;
        //            }
        //            case OperationCodeEnum.OP_ASKSHAREDFILESDIR:
        //            {
        //                if (MuleEngine.Preference.GetDebugClientTCPLevel() > 0)
        //                    DebugRecv("OP_AskSharedFilesInDirectory", client_);
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadOther(size);

        //                SafeMemFile data = MpdObjectManager.CreateSafeMemFile(packet, size);
        //                CString strReqDir = data.ReadString(GetUnicodeSupport()!=utf8strNone);
        //                CString strOrgReqDir = strReqDir;
        //                if (MuleEngine.Preference.CanSeeShares()==vsfaEverybody || (MuleEngine.Preference.CanSeeShares()==vsfaFriends && IsFriend()))
        //                {
        //                    AddLogLine(true, GetResString(IDS_SHAREDREQ2), GetUserName(), GetUserIDHybrid(), strReqDir, GetResString(IDS_ACCEPTED));
        //                    Debug.Assert( data.GetPosition() == data.GetLength() );
        //                    CTypedPtrList<CPtrList, KnownFile> list;
        //                    if (strReqDir == OperationCodeEnum.OP_INCOMPLETE_SHARED_FILES)
        //                    {
        //                        // get all shared files from download queue
        //                        int iQueuedFiles = MuleEngine.DownloadQueue.GetFileCount();
        //                        for (int i = 0; i < iQueuedFiles; i++)
        //                        {
        //                            PartFile pFile = MuleEngine.DownloadQueue.GetFileByIndex(i);
        //                            if (pFile == null || pFile.GetStatus(true) != PartFileStatusEnum.PS_READY || (pFile.IsLargeFile() && !SupportsLargeFiles()))
        //                                continue;
        //                            list.AddTail(pFile);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        bool bSingleSharedFiles = strReqDir == OperationCodeEnum.OP_OTHER_SHARED_FILES;
        //                        if (!bSingleSharedFiles)
        //                            strReqDir = MuleEngine.SharedFiles.GetDirNameByPseudo(strReqDir);
        //                        if (!strReqDir.IsEmpty())
        //                        {
        //                            // get all shared files from requested directory
        //                            for (POSITION pos = MuleEngine.SharedFiles.m_Files_map.GetStartPosition();pos != 0;)
        //                            {
        //                                CCKey bufKey;
        //                                KnownFile cur_file;
        //                                MuleEngine.SharedFiles.m_Files_map.GetNextAssoc(pos, bufKey, cur_file);

        //                                // all files which are not within a shared directory have to be single shared files
        //                                if (((!bSingleSharedFiles && CompareDirectories(strReqDir, cur_file.GetSharedDirectory()) == 0) || (bSingleSharedFiles && !MuleEngine.SharedFiles.ShouldBeShared(cur_file.GetSharedDirectory(), (""), false)))
        //                                    && (!cur_file.IsLargeFile() || SupportsLargeFiles()))
        //                                {
        //                                    list.AddTail(cur_file);
        //                                }

        //                            }
        //                        }
        //                        else
        //                            DebugLogError(("View shared files: Pseudonym for requested Directory (%s) was not found - sending empty result"), strOrgReqDir);	
        //                    }

        //                    // Currently we are sending each shared directory, even if it does not contain any files.
        //                    // Because of this we also have to send an empty shared files list..
        //                    CSafeMemFile tempfile(80);
        //                    tempfile.WriteString(strOrgReqDir, GetUnicodeSupport());
        //                    tempfile.WriteUInt32(list.GetCount());
        //                    while (list.GetCount())
        //                    {
        //                        MuleEngine.SharedFiles.CreateOfferedFilePacket(list.GetHead(), &tempfile, null, client_);
        //                        list.RemoveHead();
        //                    }

        //                    if (MuleEngine.Preference.GetDebugClientTCPLevel() > 0)
        //                        DebugSend("OP__AskSharedFilesInDirectoryAnswer", client_);
        //                    Packet* replypacket = new Packet(&tempfile);
        //                    replypacket.opcode = OperationCodeEnum.OP_ASKSHAREDFILESDIRANS;
        //                    MuleEngine.Preference.Statistics.AddUpDataOverheadOther(replypacket.size);
        //                    SendPacket(replypacket, true, true);
        //                }
        //                else
        //                {
        //                    DebugLog(GetResString(IDS_SHAREDREQ2), GetUserName(), GetUserIDHybrid(), strReqDir, GetResString(IDS_DENIED));
        //                    if (MuleEngine.Preference.GetDebugClientTCPLevel() > 0)
        //                        DebugSend("OP__AskSharedDeniedAnswer", client_);
        //                    Packet* replypacket = new Packet(OP_ASKSHAREDDENIEDANS, 0);
        //                    MuleEngine.Preference.Statistics.AddUpDataOverheadOther(replypacket.size);
        //                    SendPacket(replypacket, true, true);
        //                }
        //                break;
        //            }
        //            case OperationCodeEnum.OP_ASKSHAREDDIRSANS:
        //            {
        //                if (MuleEngine.Preference.GetDebugClientTCPLevel() > 0)
        //                    DebugRecv("OP_AskSharedDirectoriesAnswer", client_);
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadOther(size);
        //                if (GetFileListRequested() == 1)
        //                {
        //                    SafeMemFile data = MpdObjectManager.CreateSafeMemFile(packet, size);
        //                    uint uDirs = data.ReadUInt32();
        //                    for (uint i = 0; i < uDirs; i++)
        //                    {
        //                        CString strDir = data.ReadString(GetUnicodeSupport()!=utf8strNone);
        //                        // Better send the received and untouched directory string back to that client_
        //                        //PathRemoveBackslash(strDir.GetBuffer());
        //                        //strDir.ReleaseBuffer();
        //                        AddLogLine(true, GetResString(IDS_SHAREDANSW), GetUserName(), GetUserIDHybrid(), strDir);

        //                        if (MuleEngine.Preference.GetDebugClientTCPLevel() > 0)
        //                            DebugSend("OP__AskSharedFilesInDirectory", client_);
        //                        CSafeMemFile tempfile(80);
        //                        tempfile.WriteString(strDir, GetUnicodeSupport());
        //                        Packet* replypacket = new Packet(&tempfile);
        //                        replypacket.opcode = OperationCodeEnum.OP_ASKSHAREDFILESDIR;
        //                        MuleEngine.Preference.Statistics.AddUpDataOverheadOther(replypacket.size);
        //                        SendPacket(replypacket, true, true);
        //                    }
        //                    Debug.Assert( data.GetPosition() == data.GetLength() );
        //                    SetFileListRequested(uDirs);
        //                }
        //                else
        //                    AddLogLine(true, GetResString(IDS_SHAREDANSW2), GetUserName(), GetUserIDHybrid());
        //                break;
        //            }
        //            case OperationCodeEnum.OP_ASKSHAREDFILESDIRANS:
        //            {
        //                if (MuleEngine.Preference.GetDebugClientTCPLevel() > 0)
        //                    DebugRecv("OP_AskSharedFilesInDirectoryAnswer", client_);
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadOther(size);

        //                SafeMemFile data = MpdObjectManager.CreateSafeMemFile(packet, size);
        //                CString strDir = data.ReadString(GetUnicodeSupport()!=utf8strNone);
        //                PathRemoveBackslash(strDir.GetBuffer());
        //                strDir.ReleaseBuffer();
        //                if (GetFileListRequested() > 0)
        //                {
        //                    AddLogLine(true, GetResString(IDS_SHAREDINFO1), GetUserName(), GetUserIDHybrid(), strDir);
        //                    ProcessSharedFileList(packet + (uint)data.GetPosition(), (uint)(size - data.GetPosition()), strDir);
        //                    if (GetFileListRequested() == 0)
        //                        AddLogLine(true, GetResString(IDS_SHAREDINFO2), GetUserName(), GetUserIDHybrid());
        //                }
        //                else
        //                    AddLogLine(true, GetResString(IDS_SHAREDANSW3), GetUserName(), GetUserIDHybrid(), strDir);
        //                break;
        //            }
        //            case OperationCodeEnum.OP_ASKSHAREDDENIEDANS:
        //            {
        //                if (MuleEngine.Preference.GetDebugClientTCPLevel() > 0)
        //                    DebugRecv("OP_AskSharedDeniedAnswer", client_);
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadOther(size);

        //                AddLogLine(true, GetResString(IDS_SHAREDREQDENIED), GetUserName(), GetUserIDHybrid());
        //                SetFileListRequested(0);
        //                break;
        //            }
        //            default:
        //                MuleEngine.Preference.Statistics.AddDownDataOverheadOther(size);
        //                PacketToDebugLogLine(("eDonkey"), packet, size, opcode);
        //                break;
        //        }
        //    }
        //    catch(CFileException* error)
        //    {
        //        error.Delete();
        //        throw GetResString(IDS_ERR_INVALIDPACKAGE);
        //    }
        //    catch(CMemoryException* error)
        //    {
        //        error.Delete();
        //        throw new MuleException(("Memory exception"));
        //    }
        //}
        //catch(CClientException* ex) // nearly same as the 'CString' exception but with optional deleting of the client_
        //{
        //    if (MuleEngine.Preference.GetVerbose() && !ex.m_strMsg.IsEmpty())
        //        DebugLogWarning(("Error: %s - while processing eDonkey packet: opcode=%s  size=%u; %s"), ex.m_strMsg, DbgGetDonkeyClientTCPOpcode(opcode), size, DbgGetClientInfo());
        //    if (client_ && ex.m_bDelete)
        //        SetDownloadState(DS_ERROR, ("Error while processing eDonkey packet (CClientException): ") + ex.m_strMsg);
        //    Disconnect(ex.m_strMsg);
        //    ex.Delete();
        //    return false;
        //}
        //catch(CString error)
        //{
        //    if (MuleEngine.Preference.GetVerbose() && !error.IsEmpty()){
        //        if (opcode == OperationCodeEnum.OP_REQUESTFILENAME /*low priority for OP_REQUESTFILENAME*/)
        //            DebugLogWarning(("Error: %s - while processing eDonkey packet: opcode=%s  size=%u; %s"), error, DbgGetDonkeyClientTCPOpcode(opcode), size, DbgGetClientInfo());
        //        else
        //            DebugLogWarning(("Error: %s - while processing eDonkey packet: opcode=%s  size=%u; %s"), error, DbgGetDonkeyClientTCPOpcode(opcode), size, DbgGetClientInfo());
        //    }
        //    if (client_ !=null)
        //        SetDownloadState(DS_ERROR, ("Error while processing eDonkey packet (CString exception): ") + error);	
        //    Disconnect(("Error when processing packet.") + error);
        //    return false;
        //}
        //return true;
        //    }
        //    protected bool ProcessExtPacket(byte[] packet, uint size, uint opcode, uint uRawSize)
        //    {
        //        throw new Exception("hh");
        //    }
        #region UpDownClient Members

        public void StartDownload()
        {
            throw new NotImplementedException();
        }

        public void CheckDownloadTimeout()
        {
            throw new NotImplementedException();
        }

        public void SendCancelTransfer(Mule.Network.Packet packet)
        {
            throw new NotImplementedException();
        }

        public bool IsEd2kClient
        {
            get { throw new NotImplementedException(); }
        }

        public bool Disconnected(string pszReason, bool bFromSocket)
        {
            throw new NotImplementedException();
        }

        public bool TryToConnect(bool bIgnoreMaxCon, bool bNoCallbacks)
        {
            throw new NotImplementedException();
        }

        public void Connect()
        {
            throw new NotImplementedException();
        }

        public void ConnectionEstablished()
        {
            throw new NotImplementedException();
        }

        public void OnSocketConnected(int nErrorCode)
        {
            throw new NotImplementedException();
        }

        public bool CheckHandshakeFinished()
        {
            throw new NotImplementedException();
        }

        public void CheckFailedFileIdReqs(byte[] aucFileHash)
        {
            throw new NotImplementedException();
        }

        public uint UserIDHybrid
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

        public string UserName
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

        public uint IP
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

        public bool HasLowID
        {
            get { throw new NotImplementedException(); }
        }

        public uint ConnectIP
        {
            get { throw new NotImplementedException(); }
        }

        public ushort UserPort
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

        public uint TransferredUp
        {
            get { throw new NotImplementedException(); }
        }

        public uint TransferredDown
        {
            get { throw new NotImplementedException(); }
        }

        public uint ServerIP
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

        public ushort ServerPort
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

        public byte[] UserHash
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

        public bool HasValidHash
        {
            get { throw new NotImplementedException(); }
        }

        public int HashType
        {
            get { throw new NotImplementedException(); }
        }

        public byte[] BuddyID
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

        public bool HasValidBuddyID
        {
            get { throw new NotImplementedException(); }
        }

        public uint BuddyIP
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

        public ushort BuddyPort
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

        public Mule.ClientSoftwareEnum ClientSoft
        {
            get { throw new NotImplementedException(); }
        }

        public string ClientSoftVer
        {
            get { throw new NotImplementedException(); }
        }

        public string ClientModVer
        {
            get { throw new NotImplementedException(); }
        }

        public void InitClientSoftwareVersion()
        {
            throw new NotImplementedException();
        }

        public uint Version
        {
            get { throw new NotImplementedException(); }
        }

        public byte MuleVersion
        {
            get { throw new NotImplementedException(); }
        }

        public bool ExtProtocolAvailable
        {
            get { throw new NotImplementedException(); }
        }

        public bool SupportMultiPacket
        {
            get { throw new NotImplementedException(); }
        }

        public bool SupportExtMultiPacket
        {
            get { throw new NotImplementedException(); }
        }

        public bool SupportPeerCache
        {
            get { throw new NotImplementedException(); }
        }

        public bool SupportsLargeFiles
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsEmuleClient
        {
            get { throw new NotImplementedException(); }
        }

        public byte SourceExchange1Version
        {
            get { throw new NotImplementedException(); }
        }

        public bool SupportsSourceExchange2
        {
            get { throw new NotImplementedException(); }
        }

        public ClientCredits Credits
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsBanned
        {
            get { throw new NotImplementedException(); }
        }

        public string ClientFilename
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

        public ushort UDPPort
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

        public byte UDPVersion
        {
            get { throw new NotImplementedException(); }
        }

        public bool SupportsUDP
        {
            get { throw new NotImplementedException(); }
        }

        public ushort KadPort
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

        public byte ExtendedRequestsVersion
        {
            get { throw new NotImplementedException(); }
        }

        public void RequestSharedFileList()
        {
            throw new NotImplementedException();
        }

        public void ProcessSharedFileList(byte[] pachPacket, uint nSize, string pszDirectory)
        {
            throw new NotImplementedException();
        }

        public Mule.ConnectingStateEnum ConnectingState
        {
            get { throw new NotImplementedException(); }
        }

        public void ClearHelloProperties()
        {
            throw new NotImplementedException();
        }

        public bool ProcessHelloAnswer(byte[] pachPacket, uint nSize)
        {
            throw new NotImplementedException();
        }

        public bool ProcessHelloPacket(byte[] pachPacket, uint nSize)
        {
            throw new NotImplementedException();
        }

        public void SendHelloAnswer()
        {
            throw new NotImplementedException();
        }

        public void SendHelloPacket()
        {
            throw new NotImplementedException();
        }

        public void SendMuleInfoPacket(bool bAnswer)
        {
            throw new NotImplementedException();
        }

        public void ProcessMuleInfoPacket(byte[] pachPacket, uint nSize)
        {
            throw new NotImplementedException();
        }

        public void ProcessMuleCommentPacket(byte[] pachPacket, uint nSize)
        {
            throw new NotImplementedException();
        }

        public void ProcessEmuleQueueRank(byte[] packet, uint size)
        {
            throw new NotImplementedException();
        }

        public void ProcessEdonkeyQueueRank(byte[] packet, uint size)
        {
            throw new NotImplementedException();
        }

        public void CheckQueueRankFlood()
        {
            throw new NotImplementedException();
        }

        public bool Compare(UpDownClient tocomp, bool bIgnoreUserhash)
        {
            throw new NotImplementedException();
        }

        public void ResetFileStatusInfo()
        {
            throw new NotImplementedException();
        }

        public uint LastSrcReqTime
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

        public uint LastSrcAnswerTime
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

        public uint LastAskedForSources
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

        public bool FriendSlot
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

        public bool IsFriend
        {
            get { throw new NotImplementedException(); }
        }

        public Friend GetFriend()
        {
            throw new NotImplementedException();
        }

        public void SetCommentDirty()
        {
            throw new NotImplementedException();
        }

        public void SetCommentDirty(bool bDirty)
        {
            throw new NotImplementedException();
        }

        public bool SentCancelTransfer
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

        public void ProcessPublicIPAnswer(byte[] pbyData)
        {
            throw new NotImplementedException();
        }

        public void SendPublicIPRequest()
        {
            throw new NotImplementedException();
        }

        public byte KadVersion
        {
            get { throw new NotImplementedException(); }
        }

        public bool SendBuddyPingPong
        {
            get { throw new NotImplementedException(); }
        }

        public bool AllowIncomeingBuddyPingPong
        {
            get { throw new NotImplementedException(); }
        }

        public void SetLastBuddyPingPongTime()
        {
            throw new NotImplementedException();
        }

        public void ProcessFirewallCheckUDPRequest(Mpd.Generic.IO.SafeMemFile data)
        {
            throw new NotImplementedException();
        }

        public void SendPublicKeyPacket()
        {
            throw new NotImplementedException();
        }

        public void SendSignaturePacket()
        {
            throw new NotImplementedException();
        }

        public void ProcessPublicKeyPacket(byte[] pachPacket, uint nSize)
        {
            throw new NotImplementedException();
        }

        public void ProcessSignaturePacket(byte[] pachPacket, uint nSize)
        {
            throw new NotImplementedException();
        }

        public byte SecureIdentState
        {
            get { throw new NotImplementedException(); }
        }

        public void SendSecIdentStatePacket()
        {
            throw new NotImplementedException();
        }

        public void ProcessSecIdentStatePacket(byte[] pachPacket, uint nSize)
        {
            throw new NotImplementedException();
        }

        public Mule.InfoPacketStateEnum GetInfoPacketsReceived()
        {
            throw new NotImplementedException();
        }

        public void InfoPacketsReceived()
        {
            throw new NotImplementedException();
        }

        public bool HasPassedSecureIdent(bool bPassIfUnavailable)
        {
            throw new NotImplementedException();
        }

        public void SendPreviewRequest(Mule.File.AbstractFile pForFile)
        {
            throw new NotImplementedException();
        }

        public void SendPreviewAnswer(Mule.File.KnownFile pForFile, CxImage.CxImage imgFrames, byte nCount)
        {
            throw new NotImplementedException();
        }

        public void ProcessPreviewReq(byte[] pachPacket, uint nSize)
        {
            throw new NotImplementedException();
        }

        public void ProcessPreviewAnswer(byte[] pachPacket, uint nSize)
        {
            throw new NotImplementedException();
        }

        public bool PreviewSupport
        {
            get { throw new NotImplementedException(); }
        }

        public bool ViewSharedFilesSupport
        {
            get { throw new NotImplementedException(); }
        }

        public bool SafeSendPacket(Mule.Network.Packet packet)
        {
            throw new NotImplementedException();
        }

        public void CheckForGPLEvilDoer()
        {
            throw new NotImplementedException();
        }

        public bool SupportsCryptLayer
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

        public bool RequestsCryptLayer
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

        public bool RequiresCryptLayer
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

        public bool SupportsDirectUDPCallback
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

        public void SetConnectOptions(byte byOptions, bool bEncryption, bool bCallback)
        {
            throw new NotImplementedException();
        }

        public bool IsObfuscatedConnectionEstablished
        {
            get { throw new NotImplementedException(); }
        }

        public bool ShouldReceiveCryptUDPPackets
        {
            get { throw new NotImplementedException(); }
        }

        public Mule.UploadStateEnum UploadState
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

        public void SetUploadState(Mule.UploadStateEnum news)
        {
            throw new NotImplementedException();
        }

        public uint GetWaitStartTime()
        {
            throw new NotImplementedException();
        }

        public void SetWaitStartTime()
        {
            throw new NotImplementedException();
        }

        public void ClearWaitStartTime()
        {
            throw new NotImplementedException();
        }

        public uint WaitTime
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsDownloading
        {
            get { throw new NotImplementedException(); }
        }

        public bool HasBlocks
        {
            get { throw new NotImplementedException(); }
        }

        public uint NumberOfRequestedBlocksInQueue
        {
            get { throw new NotImplementedException(); }
        }

        public uint Datarate
        {
            get { throw new NotImplementedException(); }
        }

        public uint GetScore(bool sysvalue, bool isdownloading, bool onlybasevalue)
        {
            throw new NotImplementedException();
        }

        public void AddReqBlock(Mule.File.RequestedBlock reqblock)
        {
            throw new NotImplementedException();
        }

        public void CreateNextBlockPackage()
        {
            throw new NotImplementedException();
        }

        public uint UpStartTimeDelay
        {
            get { throw new NotImplementedException(); }
        }

        public void SetUpStartTime()
        {
            throw new NotImplementedException();
        }

        public void SendHashsetPacket(byte[] fileid)
        {
            throw new NotImplementedException();
        }

        public byte[] UploadFileID
        {
            get { throw new NotImplementedException(); }
        }

        public void SetUploadFileID(Mule.File.KnownFile newreqfile)
        {
            throw new NotImplementedException();
        }

        public uint SendBlockData()
        {
            throw new NotImplementedException();
        }

        public void ClearUploadBlockRequests()
        {
            throw new NotImplementedException();
        }

        public void SendRankingInfo()
        {
            throw new NotImplementedException();
        }

        public void SendCommentInfo(Mule.File.KnownFile file)
        {
            throw new NotImplementedException();
        }

        public void AddRequestCount(byte[] fileid)
        {
            throw new NotImplementedException();
        }

        public void UnBan()
        {
            throw new NotImplementedException();
        }

        public void Ban(string pszReason)
        {
            throw new NotImplementedException();
        }

        public uint AskedCount
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

        public void AddAskedCount()
        {
            throw new NotImplementedException();
        }

        public void FlushSendBlocks()
        {
            throw new NotImplementedException();
        }

        public uint LastUpRequest
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

        public bool HasCollectionUploadSlot
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

        public uint SessionUp
        {
            get { throw new NotImplementedException(); }
        }

        public void ResetSessionUp()
        {
            throw new NotImplementedException();
        }

        public uint SessionDown
        {
            get { throw new NotImplementedException(); }
        }

        public uint SessionPayloadDown
        {
            get { throw new NotImplementedException(); }
        }

        public void ResetSessionDown()
        {
            throw new NotImplementedException();
        }

        public uint QueueSessionPayloadUp
        {
            get { throw new NotImplementedException(); }
        }

        public uint PayloadInBuffer
        {
            get { throw new NotImplementedException(); }
        }

        public bool ProcessExtendedInfo(Mpd.Generic.IO.SafeMemFile packet, Mule.File.KnownFile tempreqfile)
        {
            throw new NotImplementedException();
        }

        public ushort UpPartCount
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsUpPartAvailable(uint iPart)
        {
            throw new NotImplementedException();
        }

        public byte[] UpPartStatus
        {
            get { throw new NotImplementedException(); }
        }

        public float CombinedFilePrioAndCredit
        {
            get { throw new NotImplementedException(); }
        }

        public uint AskedCountDown
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

        public void AddAskedCountDown()
        {
            throw new NotImplementedException();
        }

        public Mule.DownloadStateEnum DownloadState
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

        public uint GetLastAskedTime(Mule.File.PartFile partFile)
        {
            throw new NotImplementedException();
        }

        public void SetLastAskedTime()
        {
            throw new NotImplementedException();
        }

        public bool IsPartAvailable(uint iPart)
        {
            throw new NotImplementedException();
        }

        public List<bool> PartStatus
        {
            get { throw new NotImplementedException(); }
        }

        public ushort PartCount
        {
            get { throw new NotImplementedException(); }
        }

        public uint DownloadDatarate
        {
            get { throw new NotImplementedException(); }
        }

        public uint RemoteQueueRank
        {
            get { throw new NotImplementedException(); }
        }

        public void SetRemoteQueueRank(uint nr, bool bUpdateDisplay)
        {
            throw new NotImplementedException();
        }

        public bool IsRemoteQueueFull
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

        public bool DoesAskForDownload
        {
            get { throw new NotImplementedException(); }
        }

        public void SendFileRequest()
        {
            throw new NotImplementedException();
        }

        public void SendStartupLoadReq()
        {
            throw new NotImplementedException();
        }

        public void ProcessFileInfo(Mpd.Generic.IO.SafeMemFile data, Mule.File.PartFile file)
        {
            throw new NotImplementedException();
        }

        public void ProcessFileStatus(bool bUdpPacket, Mpd.Generic.IO.SafeMemFile data, Mule.File.PartFile file)
        {
            throw new NotImplementedException();
        }

        public void ProcessHashSet(byte[] data, uint size)
        {
            throw new NotImplementedException();
        }

        public void ProcessAcceptUpload()
        {
            throw new NotImplementedException();
        }

        public bool AddRequestForAnotherFile(Mule.File.PartFile file)
        {
            throw new NotImplementedException();
        }

        public void CreateBlockRequests(int iMaxBlocks)
        {
            throw new NotImplementedException();
        }

        public void SendBlockRequests()
        {
            throw new NotImplementedException();
        }

        public bool SendHttpBlockRequests()
        {
            throw new NotImplementedException();
        }

        public void ProcessBlockPacket(byte[] packet, uint size, bool packed, bool bI64Offsets)
        {
            throw new NotImplementedException();
        }

        public void ProcessHttpBlockPacket(byte[] pucData)
        {
            throw new NotImplementedException();
        }

        public void ClearDownloadBlockRequests()
        {
            throw new NotImplementedException();
        }

        public void SendOutOfPartReqsAndAddToWaitingQueue()
        {
            throw new NotImplementedException();
        }

        public uint CalculateDownloadRate()
        {
            throw new NotImplementedException();
        }

        public ushort GetAvailablePartCount()
        {
            throw new NotImplementedException();
        }

        public bool SwapToAnotherFile(string pszReason, bool bIgnoreNoNeeded, bool ignoreSuspensions, bool bRemoveCompletely, Mule.File.PartFile toFile, bool allowSame, bool isAboutToAsk)
        {
            throw new NotImplementedException();
        }

        public bool SwapToAnotherFile(string pszReason, bool bIgnoreNoNeeded, bool ignoreSuspensions, bool bRemoveCompletely, Mule.File.PartFile toFile, bool allowSame, bool isAboutToAsk, bool debug)
        {
            throw new NotImplementedException();
        }

        public void DontSwapTo(Mule.File.PartFile file)
        {
            throw new NotImplementedException();
        }

        public bool IsSwapSuspended(Mule.File.PartFile file, bool allowShortReaskTime, bool fileIsNNP)
        {
            throw new NotImplementedException();
        }

        public uint GetTimeUntilReask()
        {
            throw new NotImplementedException();
        }

        public uint GetTimeUntilReask(Mule.File.PartFile file)
        {
            throw new NotImplementedException();
        }

        public uint GetTimeUntilReask(Mule.File.PartFile file, bool allowShortReaskTime, bool useGivenNNP, bool givenNNP)
        {
            throw new NotImplementedException();
        }

        public void UDPReaskACK(ushort nNewQR)
        {
            throw new NotImplementedException();
        }

        public void UDPReaskFNF()
        {
            throw new NotImplementedException();
        }

        public void UDPReaskForDownload()
        {
            throw new NotImplementedException();
        }

        public bool UDPPacketPending
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsSourceRequestAllowed()
        {
            throw new NotImplementedException();
        }

        public bool IsSourceRequestAllowed(Mule.File.PartFile partfile, bool sourceExchangeCheck)
        {
            throw new NotImplementedException();
        }

        public bool IsValidSource
        {
            get { throw new NotImplementedException(); }
        }

        public Mule.SourceFromEnum SourceFrom
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

        public void SetDownStartTime()
        {
            throw new NotImplementedException();
        }

        public uint GetDownTimeDifference(bool clear)
        {
            throw new NotImplementedException();
        }

        public bool TransferredDownMini
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

        public void InitTransferredDownMini()
        {
            throw new NotImplementedException();
        }

        public uint A4AFCount
        {
            get { throw new NotImplementedException(); }
        }

        public ushort UpCompleteSourcesCount
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

        public Mule.ChatStateEnum ChatState
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

        public Mule.ChatCaptchaStateEnum ChatCaptchaState
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

        public void ProcessChatMessage(Mpd.Generic.IO.SafeMemFile data, uint nLength)
        {
            throw new NotImplementedException();
        }

        public void SendChatMessage(string strMessage)
        {
            throw new NotImplementedException();
        }

        public void ProcessCaptchaRequest(Mpd.Generic.IO.SafeMemFile data)
        {
            throw new NotImplementedException();
        }

        public void ProcessCaptchaReqRes(byte nStatus)
        {
            throw new NotImplementedException();
        }

        public byte MessagesReceived
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

        public void IncMessagesReceived()
        {
            throw new NotImplementedException();
        }

        public byte MessagesSent
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

        public void IncMessagesSent()
        {
            throw new NotImplementedException();
        }

        public bool IsSpammer
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

        public bool MessageFiltered
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

        public Mule.KadStateEnum KadState
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

        public bool HasFileComment
        {
            get { throw new NotImplementedException(); }
        }

        public string FileComment
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

        public bool HasFileRating
        {
            get { throw new NotImplementedException(); }
        }

        public byte GetFileRating
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

        public int Unzip(Mule.File.PendingBlock block, byte[] zipped, byte[] unzipped, int iRecursion)
        {
            throw new NotImplementedException();
        }

        public void UpdateDisplayedInfo(bool force)
        {
            throw new NotImplementedException();
        }

        public int FileListRequested
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

        public Mule.File.PartFile RequestFile
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

        public Mule.AICH.AICHHash ReqFileAICHHash
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

        public bool IsSupportingAICH
        {
            get { throw new NotImplementedException(); }
        }

        public void SendAICHRequest(Mule.File.PartFile pForFile, ushort nPart)
        {
            throw new NotImplementedException();
        }

        public bool IsAICHReqPending
        {
            get { throw new NotImplementedException(); }
        }

        public void ProcessAICHAnswer(byte[] packet, uint size)
        {
            throw new NotImplementedException();
        }

        public void ProcessAICHRequest(byte[] packet, uint size)
        {
            throw new NotImplementedException();
        }

        public void ProcessAICHFileHash(Mpd.Generic.IO.SafeMemFile data, Mule.File.PartFile file)
        {
            throw new NotImplementedException();
        }

        public Mpd.Generic.Utf8StrEnum UnicodeSupport
        {
            get { throw new NotImplementedException(); }
        }

        public string DownloadStateDisplayString
        {
            get { throw new NotImplementedException(); }
        }

        public string UploadStateDisplayString
        {
            get { throw new NotImplementedException(); }
        }

        public string DbgDownloadState
        {
            get { throw new NotImplementedException(); }
        }

        public string DbgUploadState
        {
            get { throw new NotImplementedException(); }
        }

        public string DbgKadState
        {
            get { throw new NotImplementedException(); }
        }

        public string DbgGetClientInfo(bool bFormatIP)
        {
            throw new NotImplementedException();
        }

        public string DbgFullClientSoftVer
        {
            get { throw new NotImplementedException(); }
        }

        public string DbgGetHelloInfo()
        {
            throw new NotImplementedException();
        }

        public string DbgGetMuleInfo()
        {
            throw new NotImplementedException();
        }

        public bool IsInNoNeededList(Mule.File.PartFile fileToCheck)
        {
            throw new NotImplementedException();
        }

        public bool SwapToRightFile(Mule.File.PartFile SwapTo, Mule.File.PartFile cur_file, bool ignoreSuspensions, bool SwapToIsNNPFile, bool isNNPFile, bool wasSkippedDueToSourceExchange, bool doAgressiveSwapping, bool debug)
        {
            throw new NotImplementedException();
        }

        public uint LastTriedToConnectTime
        {
            get { throw new NotImplementedException(); }
        }

        public Mule.Network.ClientReqSocket ClientSocket
        {
            get
            {
                return clientSocket_;
            }

            set
            {
                if (clientSocket_ != null)
                {
                    clientSocket_.CheckClientTimeOut -= CheckClientTimeOut;
                    clientSocket_.QueryClientTimeOut -= QueryClientTimeOut;
                    clientSocket_.SocketClosed -= ClientSafeDelete;
                    clientSocket_.SocketDisconnected -= ClientDisconnect;
                }

                clientSocket_ = value;

                if (clientSocket_ != null)
                {
                    clientSocket_.SocketClosed += ClientSafeDelete;
                    clientSocket_.CheckClientTimeOut += CheckClientTimeOut;
                    clientSocket_.QueryClientTimeOut += QueryClientTimeOut;
                    clientSocket_.SocketDisconnected += ClientDisconnect;
                }
            }
        }

        public Friend Friend
        {
            get { throw new NotImplementedException(); }
        }

        public Mule.File.PartFileList OtherRequestsList
        {
            get { throw new NotImplementedException(); }
        }

        public Mule.File.PartFileList OtherNoNeededList
        {
            get { throw new NotImplementedException(); }
        }

        public ushort LastPartAsked
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

        public bool DoesAddNextConnect
        {
            get { throw new NotImplementedException(); }
        }

        public uint SlotNumber
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

        public Mule.Network.EMSocket GetFileUploadSocket(bool log)
        {
            throw new NotImplementedException();
        }

        public object DbgGetClientInfo()
        {
            throw new NotImplementedException();
        }

        public void SendHashsetPacket(byte[] packet, int p, bool p_3)
        {
            throw new NotImplementedException();
        }

        public void ProcessHashSet(byte[] packet, uint size, bool p)
        {
            throw new NotImplementedException();
        }

        public void SendCancelTransfer()
        {
            throw new NotImplementedException();
        }

        public void SetDownloadState(Mule.DownloadStateEnum downloadStateEnum, string p)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Fields
        ClientReqSocket clientSocket_ = null;
        #endregion

        #region Members
        private uint QueryClientTimeOut(object sender, SocketEventArgs arg)
        {
            // PC-TODO
            // the PC socket may even already be disconnected and deleted and we still need to keep the
            // ed2k socket open because remote client_ may still be downloading from cache.
            //if (IsUploadingToPeerCache &&
            //    (PeerCacheUploadSocket == null || !PeerCacheUploadSocket.IsConnected))
            //{
            //    // we are uploading (or at least allow uploading) but currently no socket
            //    return MuleConstants.PeerCacheSocketUploadTimeout;
            //}
            //else if (PeerCacheUploadSocket != null && PeerCacheUploadSocket.IsConnected)
            //{
            //    // we have an uploading PC socket, but that socket is not used (nor can it be closed)
            //    return PeerCacheUploadSocket.TimeOut;
            //}
            //else if (PeerCacheDownloadSocket != null && PeerCacheDownloadSocket.IsConnected)
            //{
            //    // we have a downloading PC socket
            //    return PeerCacheDownloadSocket.TimeOut;
            //}
            //else
                return 0;
        }

        private uint CheckClientTimeOut(object sender, SocketEventArgs arg)
        {
            if (KadState == KadStateEnum.KS_CONNECTED_BUDDY)
            {
                return MuleConstants.ONE_MIN_MS * (15);
            }
            if (ChatState != ChatStateEnum.MS_NONE)
            {
                //We extend the timeout time here to avoid people chatting from disconnecting to fast.
                return MuleConstants.CONNECTION_TIMEOUT;
            }

            return 0;
        }

        private void ClientSafeDelete(object sender, SocketEventArgs arg)
        {
            ClientReqSocket cs = sender as ClientReqSocket;

            if (cs == null) return;

            if (clientSocket_ == cs)
            {
                ClientSocket = null;
            }
        }

        private void ClientDisconnect(object sender, SocketEventArgs arg)
        {
            ClientReqSocket cs = sender as ClientReqSocket;

            if (cs == null) return;

            Disconnected(cs.DisonnectReason, true);
        }
        #endregion
    }
}
