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

namespace Mule.Core
{
    public enum ClientSoftwareEnum
    {
        SO_EMULE = 0,	// default
        SO_CDONKEY = 1,	// ET_COMPATIBLECLIENT
        SO_XMULE = 2,	// ET_COMPATIBLECLIENT
        SO_AMULE = 3,	// ET_COMPATIBLECLIENT
        SO_SHAREAZA = 4,	// ET_COMPATIBLECLIENT
        SO_MLDONKEY = 10,	// ET_COMPATIBLECLIENT
        SO_LPHANT = 20,	// ET_COMPATIBLECLIENT
        // other client types which are not identified with ET_COMPATIBLECLIENT
        SO_EDONKEYHYBRID = 50,
        SO_EDONKEY,
        SO_OLDEMULE,
        SO_URL,
        SO_UNKNOWN
    };

    public enum ConnectingStateEnum
    {
        CCS_NONE = 0,
        CCS_DIRECTTCP,
        CCS_DIRECTCALLBACK,
        CCS_KADCALLBACK,
        CCS_SERVERCALLBACK,
        CCS_PRECONDITIONS
    };

    public enum UploadStateEnum
    {
        US_UPLOADING,
        US_ONUPLOADQUEUE,
        US_CONNECTING,
        US_BANNED,
        US_NONE
    };

    public enum DownloadStateEnum
    {
        DS_DOWNLOADING,
        DS_ONQUEUE,
        DS_CONNECTED,
        DS_CONNECTING,
        DS_WAITCALLBACK,
        DS_WAITCALLBACKKAD,
        DS_REQHASHSET,
        DS_NONEEDEDPARTS,
        DS_TOOMANYCONNS,
        DS_TOOMANYCONNSKAD,
        DS_LOWTOLOWIP,
        DS_BANNED,
        DS_ERROR,
        DS_NONE,
        DS_REMOTEQUEUEFULL  // not used yet, except in statistics
    };

    public enum SourceFromEnum
    {
        SF_SERVER = 0,
        SF_KADEMLIA = 1,
        SF_SOURCE_EXCHANGE = 2,
        SF_PASSIVE = 3,
        SF_LINK = 4
    };

    public enum ChatStateEnum
    {
        MS_NONE,
        MS_CHATTING,
        MS_CONNECTING,
        MS_UNABLETOCONNECT
    };

    public enum ChatCaptchaStateEnum
    {
        CA_NONE = 0,
        CA_CHALLENGESENT,
        CA_CAPTCHASOLVED,
        CA_ACCEPTING,
        CA_CAPTCHARECV,
        CA_SOLUTIONSENT
    };

    public enum KadStateEnum
    {
        KS_NONE,
        KS_QUEUED_FWCHECK,
        KS_CONNECTING_FWCHECK,
        KS_CONNECTED_FWCHECK,
        KS_QUEUED_BUDDY,
        KS_INCOMING_BUDDY,
        KS_CONNECTING_BUDDY,
        KS_CONNECTED_BUDDY,
        KS_QUEUED_FWCHECK_UDP,
        KS_FWCHECK_UDP,
        KS_CONNECTING_FWCHECK_UDP
    };

    public enum PeerCacheDownStateEnum
    {
        PCDS_NONE = 0,
        PCDS_WAIT_CLIENT_REPLY,
        PCDS_WAIT_CACHE_REPLY,
        PCDS_DOWNLOADING
    };

    public enum PeerCacheUpStateEnum
    {
        PCUS_NONE = 0,
        PCUS_WAIT_CACHE_REPLY,
        PCUS_UPLOADING
    };

    public enum PartFileStatusEnum
    {
        PS_READY = 0,
        PS_EMPTY = 1,
        PS_WAITINGFORHASH = 2,
        PS_HASHING = 3,
        PS_ERROR = 4,
        PS_INSUFFICIENT = 5,
        PS_UNKNOWN = 6,
        PS_PAUSED = 7,
        PS_COMPLETING = 8,
        PS_COMPLETE = 9
    };

    public enum PriorityEnum
    {
        //I Had to change this because it didn't save negative number correctly.. 
        //Had to modify the sort function for this change..
        PR_VERYLOW = 4,
        PR_LOW = 0,
        // Don't change this - needed for edonkey clients and server!
        PR_NORMAL = 1,
        //*
        PR_HIGH = 2,
        PR_VERYHIGH = 3,
        //UAP Hunter
        PR_AUTO = 5,
    }

    public enum PartFileFormatEnum
    {
        PMT_UNKNOWN = 0,
        PMT_DEFAULTOLD,
        PMT_SPLITTED,
        PMT_NEWOLD,
        PMT_SHAREAZA,
        PMT_BADFORMAT
    };

    public enum FileCompletionThreadErrorCodeEnum
    {
        FILE_COMPLETION_THREAD_FAILED = 0x0000,
        FILE_COMPLETION_THREAD_SUCCESS = 0x0001,
        FILE_COMPLETION_THREAD_RENAMED = 0x0002,
    };

    public enum PartFileOpEnum
    {
        PFOP_NONE = 0,
        PFOP_HASHING,
        PFOP_COPYING,
        PFOP_UNCOMPRESSING
    };

    public enum AICHStatusEnum
    {
        AICH_ERROR = 0,
        AICH_EMPTY,
        AICH_UNTRUSTED,
        AICH_TRUSTED,
        AICH_VERIFIED,
        AICH_HASHSETCOMPLETE
    };

    public enum EMSocketStateEnum
    {
        ES_DISCONNECTED = 0xFF,
        ES_NOTCONNECTED = 0x00,
        ES_CONNECTED = 0x01
    };

    public enum EMSocketErrorCodeEnum
    {
        ERR_WRONGHEADER = 0x01,
        ERR_TOOBIG = 0x02,
        ERR_ENCRYPTION = 0x03,
        ERR_ENCRYPTION_NOTALLOWED = 0x04
    };

    public enum StreamCryptStateEnum
    {
        ECS_NONE = 0,			// Disabled or not available
        ECS_UNKNOWN,			// Incoming connection, will test the first incoming data for encrypted protocol
        ECS_PENDING,			// Outgoing connection, will start sending encryption protocol
        ECS_PENDING_SERVER,		// Outgoing serverconnection, will start sending encryption protocol
        ECS_NEGOTIATING,		// Encryption supported, handshake still uncompleted
        ECS_ENCRYPTING			// Encryption enabled
    };

    public enum NegotiatingStateEnum
    {
        ONS_NONE,

        ONS_BASIC_CLIENTA_RANDOMPART,
        ONS_BASIC_CLIENTA_MAGICVALUE,
        ONS_BASIC_CLIENTA_METHODTAGSPADLEN,
        ONS_BASIC_CLIENTA_PADDING,

        ONS_BASIC_CLIENTB_MAGICVALUE,
        ONS_BASIC_CLIENTB_METHODTAGSPADLEN,
        ONS_BASIC_CLIENTB_PADDING,

        ONS_BASIC_SERVER_DHANSWER,
        ONS_BASIC_SERVER_MAGICVALUE,
        ONS_BASIC_SERVER_METHODTAGSPADLEN,
        ONS_BASIC_SERVER_PADDING,
        ONS_BASIC_SERVER_DELAYEDSENDING,

        ONS_COMPLETE
    };

    public enum EncryptionMethodsEnum
    {
        ENM_OBFUSCATION = 0x00
    };

    public enum ViewSharedFilesAccessEnum
    {
        vsfaEverybody = 0,
        vsfaFriends = 1,
        vsfaNobody = 2
    };

    public enum NotifierSoundTypeEnum
    {
        ntfstNoSound = 0,
        ntfstSoundFile = 1,
        ntfstSpeech = 2
    };

    public enum DefaultDirectoryEnum
    {
        EMULE_CONFIGDIR,
        EMULE_TEMPDIR,
        EMULE_INCOMINGDIR,
        EMULE_LOGDIR,
        EMULE_DATABASEDIR, // the parent directory of the incoming/temp folder
        EMULE_CONFIGBASEDIR, // the parent directory of the config folder 
        EMULE_EXPANSIONDIR // this is a base directory accessable for all users for things eMule installs
    };

    public enum LogFileFormatEnum
    {
        Unicode = 0,
        Utf8
    };

    public enum Utf8StrEnum
    {
        utf8strNone,
        utf8strOptBOM,
        utf8strRaw
    };

    public enum OperationCodeEnum
    {
        OP_EMULEINFO = 0x01,//
        OP_EMULEINFOANSWER = 0x02,//
        OP_COMPRESSEDPART = 0x40,// <HASH 16><von 4><size 4><Daten len:size>
        OP_QUEUERANKING = 0x60,// <RANG 2>
        OP_FILEDESC = 0x61,// <len 2><NAME len>
        OP_REQUESTSOURCES = 0x81,// <HASH 16>
        OP_ANSWERSOURCES = 0x82,//
        OP_REQUESTSOURCES2 = 0x83,// <HASH 16><Version 1><Options 2>
        OP_ANSWERSOURCES2 = 0x84,// <Version 1>[content]
        OP_PUBLICKEY = 0x85,// <len 1><pubkey len>
        OP_SIGNATURE = 0x86,// v1: <len 1><signature len>  v2:<len 1><signature len><sigIPused 1>
        OP_SECIDENTSTATE = 0x87,// <state 1><rndchallenge 4>
        OP_REQUESTPREVIEW = 0x90,// <HASH 16>
        OP_PREVIEWANSWER = 0x91,// <HASH 16><frames 1>{frames * <len 4><frame len>}
        OP_MULTIPACKET = 0x92,
        OP_MULTIPACKETANSWER = 0x93,
        OP_PEERCACHE_QUERY = 0x94,
        OP_PEERCACHE_ANSWER = 0x95,
        OP_PEERCACHE_ACK = 0x96,
        OP_PUBLICIP_REQ = 0x97,
        OP_PUBLICIP_ANSWER = 0x98,
        OP_CALLBACK = 0x99,// <HASH 16><HASH 16><uint 16>
        OP_REASKCALLBACKTCP = 0x9A,
        OP_AICHREQUEST = 0x9B,// <HASH 16><uint16><HASH aichhashlen>
        OP_AICHANSWER = 0x9C,// <HASH 16><uint16><HASH aichhashlen> <data>
        OP_AICHFILEHASHANS = 0x9D,
        OP_AICHFILEHASHREQ = 0x9E,
        OP_BUDDYPING = 0x9F,
        OP_BUDDYPONG = 0xA0,
        OP_COMPRESSEDPART_I64 = 0xA1,// <HASH 16><von 8><size 4><Daten len:size>
        OP_SENDINGPART_I64 = 0xA2,// <HASH 16><von 8><bis 8><Daten len:(von-bis)>
        OP_REQUESTPARTS_I64 = 0xA3,// <HASH 16><von[3] 8*3><bis[3] 8*3>
        OP_MULTIPACKET_EXT = 0xA4,
        OP_CHATCAPTCHAREQ = 0xA5,// <tags 1>[tags]<Captcha BITMAP>
        OP_CHATCAPTCHARES = 0xA6,// <status 1>
        OP_FWCHECKUDPREQ = 0xA7,// <Inter_Port 2><Extern_Port 2><KadUDPKey 4> *Support required for Kadversion >= 6
        OP_KAD_FWTCPCHECK_ACK = 0xA8,// (null/reserved), replaces KADEMLIA_FIREWALLED_ACK_RES, *Support required for Kadversion >= 7

        // extened prot client <-> extened prot client UDP
        OP_REASKFILEPING = 0x90,// <HASH 16>
        OP_REASKACK = 0x91,// <RANG 2>
        OP_FILENOTFOUND = 0x92,// (null)
        OP_QUEUEFULL = 0x93,// (null)
        OP_REASKCALLBACKUDP = 0x94,
        OP_DIRECTCALLBACKREQ = 0x95,// <TCPPort 2><Userhash 16><ConnectionOptions 1>
        OP_PORTTEST = 0xFE,// Connection Test
    };

    public enum VersionsEnum
    {
        EDONKEYVERSION = 0x3C,
        KADEMLIA_VERSION1_46c = 0x01, /*45b - 46c*/
        KADEMLIA_VERSION2_47a = 0x02, /*47a*/
        KADEMLIA_VERSION3_47b = 0x03, /*47b*/
        KADEMLIA_VERSION5_48a = 0x05, // -0.48a
        KADEMLIA_VERSION6_49aBETA = 0x06, // -0.49aBETA1, needs to support: OP_FWCHECKUDPREQ (!), obfuscation, direct callbacks, source type 6, UDP firewallcheck
        KADEMLIA_VERSION7_49a = 0x07, // -0.49a needs to support OP_KAD_FWTCPCHECK_ACK, KADEMLIA_FIREWALLED2_REQ
        KADEMLIA_VERSION8_49b = 0x08, // TAG_KADMISCOPTIONS, KADEMLIA2_HELLO_RES_ACK
        KADEMLIA_VERSION = 0x08, // Change CT_EMULE_MISCOPTIONS2 if Kadversion becomes >= 15
        PREFFILE_VERSION = 0x14,	//<<-- last change: reduced .dat, by using .ini
        PARTFILE_VERSION = 0xe0,
        PARTFILE_SPLITTEDVERSION = 0xe1,
        PARTFILE_VERSION_LARGEFILE = 0xe2,
        SOURCEEXCHANGE2_VERSION = 4,		// replaces the version sent in MISC_OPTIONS flag fro SX1

        CREDITFILE_VERSION = 0x12,
        CREDITFILE_VERSION_29 = 0x11,
    };

    public enum SecureIdentStateEnum
    {
        IS_UNAVAILABLE = 0,
        IS_ALLREQUESTSSEND = 0,
        IS_SIGNATURENEEDED = 1,
        IS_KEYANDSIGNEEDED = 2,
    };

    public enum InfoPacketStateEnum
    {
        IP_NONE = 0,
        IP_EDONKEYPROTPACK = 1,
        IP_EMULEPROTPACK = 2,
        IP_BOTH = 3,
    };
}