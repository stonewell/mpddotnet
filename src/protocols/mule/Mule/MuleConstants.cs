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
using System.Collections.Generic;
using System.Text;

namespace Mule
{
    public sealed class MuleConstants
    {
        public MuleConstants()
        {
        }

        // Max milliseconds before forcing a flush
        public const uint BUFFER_TIME_LIMIT = 60000;

        public const string PARTMET_BAK_EXT = ".bak";
        public const string PARTMET_TMP_EXT = ".backup";

        public const uint STATES_COUNT = 17;

        public const int HASHSIZE = 20;
        public const string KNOWN2_MET_FILENAME = "known2_64.met";
        public const string OLD_KNOWN2_MET_FILENAME = "known2.met";
        public const uint KNOWN2_MET_VERSION = 0x02;

        public const uint PACKET_HEADER_SIZE = 6;

        public const ulong PARTSIZE = 9728000;
        public const ulong MAXFRAGSIZE = 1300;
        public const ulong EMBLOCKSIZE = 184320;
        public const byte OP_EDONKEYHEADER = 0xE3;
        public const byte OP_KADEMLIAHEADER = 0xE4;
        public const byte OP_KADEMLIAPACKEDPROT = 0xE5;
        public const byte OP_EDONKEYPROT = OP_EDONKEYHEADER;
        public const byte OP_PACKEDPROT = 0xD4;
        public const byte OP_EMULEPROT = 0xC5;
        public const byte OP_UDPRESERVEDPROT1 = 0xA3;	// reserved for later UDP headers (important for EncryptedDatagramSocket)
        public const byte OP_UDPRESERVEDPROT2 = 0xB2;	// reserved for later UDP headers (important for EncryptedDatagramSocket)
        public const byte OP_MLDONKEYPROT = 0x00;
        public const byte MET_HEADER = 0x0E;
        public const byte MET_HEADER_I64TAGS = 0x0F;

        public const uint SHA1_BLOCK_SIZE = 64;
        public const uint SHA1_DIGEST_SIZE = 20;

        public const uint SHORT_ED2K_STR = 256;
        public const uint SHORT_RAW_ED2K_MB_STR = (SHORT_ED2K_STR * 2);
        public const uint SHORT_RAW_ED2K_UTF8_STR = (SHORT_ED2K_STR * 4);

        public const ulong MAX_EMULE_FILE_SIZE = 0x4000000000; // = 2^38 = 256GB
        public const ulong OLD_MAX_EMULE_FILE_SIZE = 4290048000;	// (4294967295/PARTSIZE)*PARTSIZE = ~4GB

        public const int MAXFILECOMMENTLEN = 50;

        //file tags
        public const byte FT_FILENAME = 0x01;	// <string>
        public const string TAG_FILENAME = "\x01";	// <string>
        public const byte FT_FILESIZE = 0x02;	// <uint32> (or <uint64> when supported)
        public const string TAG_FILESIZE = "\x02";	// <uint32>
        public const byte FT_FILESIZE_HI = 0x3A;	// <uint32>
        public const string TAG_FILESIZE_HI = "\x3A";	// <uint32>
        public const byte FT_FILETYPE = 0x03;	// <string>
        public const string TAG_FILETYPE = "\x03";	// <string>
        public const byte FT_FILEFORMAT = 0x04;	// <string>
        public const string TAG_FILEFORMAT = "\x04";	// <string>
        public const byte FT_LASTSEENCOMPLETE = 0x05;	// <uint32>
        public const string TAG_COLLECTION = "\x05";
        public const string TAG_PART_PATH = "\x06";	// <string>
        public const string TAG_PART_HASH = "\x07";
        public const byte FT_TRANSFERRED = 0x08;	// <uint32>
        public const string TAG_TRANSFERRED = "\x08";	// <uint32>
        public const byte FT_GAPSTART = 0x09;	// <uint32>
        public const string TAG_GAPSTART = "\x09";	// <uint32>
        public const byte FT_GAPEND = 0x0A;	// <uint32>
        public const string TAG_GAPEND = "\x0A";	// <uint32>
        public const byte FT_DESCRIPTION = 0x0B;	// <string>
        public const string TAG_DESCRIPTION = "\x0B";	// <string>
        public const string TAG_PING = "\x0C";
        public const string TAG_FAIL = "\x0D";
        public const string TAG_PREFERENCE = "\x0E";
        public const string TAG_PORT = "\x0F";
        public const string TAG_IP_ADDRESS = "\x10";
        public const string TAG_VERSION = "\x11";	// <string>
        public const byte FT_PARTFILENAME = 0x12;	// <string>
        public const string TAG_PARTFILENAME = "\x12";	// <string>
        //public const byte FT_PRIORITY			 = 0x13;	// Not used anymore
        public const string TAG_PRIORITY = "\x13";	// <uint32>
        public const byte FT_STATUS = 0x14;	// <uint32>
        public const string TAG_STATUS = "\x14";	// <uint32>
        public const byte FT_SOURCES = 0x15;	// <uint32>
        public const string TAG_SOURCES = "\x15";	// <uint32>
        public const byte FT_PERMISSIONS = 0x16;	// <uint32>
        public const string TAG_PERMISSIONS = "\x16";
        //public const byte FT_ULPRIORITY			 = 0x17;	// Not used anymore
        public const string TAG_PARTS = "\x17";
        public const byte FT_DLPRIORITY = 0x18;	// Was 13
        public const byte FT_ULPRIORITY = 0x19;	// Was 17
        public const byte FT_COMPRESSION = 0x1A;
        public const byte FT_CORRUPTED = 0x1B;
        public const byte FT_KADLASTPUBLISHKEY = 0x20;	// <uint32>
        public const byte FT_KADLASTPUBLISHSRC = 0x21;	// <uint32>
        public const byte FT_FLAGS = 0x22;	// <uint32>
        public const byte FT_DL_ACTIVE_TIME = 0x23;	// <uint32>
        public const byte FT_CORRUPTEDPARTS = 0x24;	// <string>
        public const byte FT_DL_PREVIEW = 0x25;
        public const byte FT_KADLASTPUBLISHNOTES = 0x26;	// <uint32> 
        public const byte FT_AICH_HASH = 0x27;
        public const byte FT_FILEHASH = 0x28;
        public const byte FT_COMPLETE_SOURCES = 0x30;	// nr. of sources which share a complete version of the associated file (supported by eserver 16.46+)
        public const string TAG_COMPLETE_SOURCES = "\x30";
        public const byte FT_COLLECTIONAUTHOR = 0x31;
        public const byte FT_COLLECTIONAUTHORKEY = 0x32;
        public const byte FT_PUBLISHINFO = 0x33;	// <uint32>
        public const string TAG_PUBLISHINFO = "\x33";	// <uint32>

        // statistic
        public const byte FT_ATTRANSFERRED = 0x50;	// <uint32>
        public const byte FT_ATREQUESTED = 0x51;	// <uint32>
        public const byte FT_ATACCEPTED = 0x52;	// <uint32>
        public const byte FT_CATEGORY = 0x53;	// <uint32>
        public const byte FT_ATTRANSFERREDHI = 0x54;	// <uint32>
        public const byte FT_MAXSOURCES = 0x55;	// <uint32>
        public const byte FT_MEDIA_ARTIST = 0xD0;	// <string>
        public const string TAG_MEDIA_ARTIST = "\xD0";	// <string>
        public const byte FT_MEDIA_ALBUM = 0xD1;	// <string>
        public const string TAG_MEDIA_ALBUM = "\xD1";	// <string>
        public const byte FT_MEDIA_TITLE = 0xD2;	// <string>
        public const string TAG_MEDIA_TITLE = "\xD2";	// <string>
        public const byte FT_MEDIA_LENGTH = 0xD3;	// <uint32> !!!
        public const string TAG_MEDIA_LENGTH = "\xD3";	// <uint32> !!!
        public const byte FT_MEDIA_BITRATE = 0xD4;	// <uint32>
        public const string TAG_MEDIA_BITRATE = "\xD4";	// <uint32>
        public const byte FT_MEDIA_CODEC = 0xD5;	// <string>
        public const string TAG_MEDIA_CODEC = "\xD5";	// <string>
        public const string TAG_KADMISCOPTIONS = "\xF2";	// <uint8>
        public const string TAG_ENCRYPTION = "\xF3";	// <uint8>
        public const string TAG_USER_COUNT = "\xF4";	// <uint32>
        public const string TAG_FILE_COUNT = "\xF5";	// <uint32>
        public const byte FT_FILECOMMENT = 0xF6;	// <string>
        public const string TAG_FILECOMMENT = "\xF6";	// <string>
        public const byte FT_FILERATING = 0xF7;	// <uint8>
        public const string TAG_FILERATING = "\xF7";	// <uint8>
        public const string TAG_BUDDYHASH = "\xF8";	// <string>
        public const string TAG_CLIENTLOWID = "\xF9";	// <uint32>
        public const string TAG_SERVERPORT = "\xFA";	// <uint16>
        public const string TAG_SERVERIP = "\xFB";	// <uint32>
        public const string TAG_SOURCEUPORT = "\xFC";	// <uint16>
        public const string TAG_SOURCEPORT = "\xFD";	// <uint16>
        public const string TAG_SOURCEIP = "\xFE";	// <uint32>
        public const string TAG_SOURCETYPE = "\xFF";	// <uint8>

        public const byte TAGTYPE_HASH = 0x01;
        public const byte TAGTYPE_STRING = 0x02;
        public const byte TAGTYPE_UINT32 = 0x03;
        public const byte TAGTYPE_FLOAT32 = 0x04;
        public const byte TAGTYPE_BOOL = 0x05;
        public const byte TAGTYPE_BOOLARRAY = 0x06;
        public const byte TAGTYPE_BLOB = 0x07;
        public const byte TAGTYPE_UINT16 = 0x08;
        public const byte TAGTYPE_UINT8 = 0x09;
        public const byte TAGTYPE_BSOB = 0x0A;
        public const byte TAGTYPE_UINT64 = 0x0B;

        public const byte TAGTYPE_STR1 = 0x11;
        public const byte TAGTYPE_STR2 = 0x12;
        public const byte TAGTYPE_STR3 = 0x13;
        public const byte TAGTYPE_STR4 = 0x14;
        public const byte TAGTYPE_STR5 = 0x15;
        public const byte TAGTYPE_STR6 = 0x16;
        public const byte TAGTYPE_STR7 = 0x17;
        public const byte TAGTYPE_STR8 = 0x18;
        public const byte TAGTYPE_STR9 = 0x19;
        public const byte TAGTYPE_STR10 = 0x1A;
        public const byte TAGTYPE_STR11 = 0x1B;
        public const byte TAGTYPE_STR12 = 0x1C;
        public const byte TAGTYPE_STR13 = 0x1D;
        public const byte TAGTYPE_STR14 = 0x1E;
        public const byte TAGTYPE_STR15 = 0x1F;
        public const byte TAGTYPE_STR16 = 0x20;
        public const byte TAGTYPE_STR17 = 0x21;	// accepted by eMule 0.42f (02-Mai-2004) in receiving code only because of a flaw, those tags are handled correctly, but should not be handled at all
        public const byte TAGTYPE_STR18 = 0x22;	// accepted by eMule 0.42f (02-Mai-2004) in receiving code only because of a flaw, those tags are handled correctly, but should not be handled at all
        public const byte TAGTYPE_STR19 = 0x23;	// accepted by eMule 0.42f (02-Mai-2004) in receiving code only because of a flaw, those tags are handled correctly, but should not be handled at all
        public const byte TAGTYPE_STR20 = 0x24;	// accepted by eMule 0.42f (02-Mai-2004) in receiving code only because of a flaw, those tags are handled correctly, but should not be handled at all
        public const byte TAGTYPE_STR21 = 0x25;	// accepted by eMule 0.42f (02-Mai-2004) in receiving code only because of a flaw, those tags are handled correctly, but should not be handled at all
        public const byte TAGTYPE_STR22 = 0x26;	// accepted by eMule 0.42f (02-Mai-2004) in receiving code only because of a flaw, those tags are handled correctly, but should not be handled at all

        public const uint META_DATA_VER = 1;

        public const string FT_ED2K_MEDIA_ARTIST = "Artist";	// <string>
        public const string FT_ED2K_MEDIA_ALBUM = "Album";		// <string>
        public const string FT_ED2K_MEDIA_TITLE = "Title";		// <string>
        public const string FT_ED2K_MEDIA_LENGTH = "length";	// <string> !!!
        public const string FT_ED2K_MEDIA_BITRATE = "bitrate";	// <uint32>
        public const string FT_ED2K_MEDIA_CODEC = "codec";		// <string>
        public const string TAG_NSENT = "# Sent";
        public const string TAG_ONIP = "ip";
        public const string TAG_ONPORT = "port";

        public const uint ONE_SEC_MS = 1000;
        public const uint ONE_MIN_MS = ONE_SEC_MS * 60;
        public const uint ONE_HOUR_MS = 60 * ONE_MIN_MS;
        public const uint ONE_DAY_MS = 24 * ONE_HOUR_MS;
        public const uint ONE_MIN_SEC = 60;
        public const uint ONE_HOUR_SEC = 60 * ONE_MIN_SEC;
        public const uint ONE_DAY_SEC = 24 * ONE_HOUR_SEC;

        public const uint MAX_SOURCES_FILE_SOFT = 500;
        public const uint MAX_SOURCES_FILE_UDP = 50;

        public const uint MAGICVALUE_REQUESTER = 34;							// modification of the requester-send and server-receive key
        public const uint MAGICVALUE_SERVER = 203;						// modification of the server-send and requester-send key
        public const uint MAGICVALUE_SYNC = 0x835E6FC4;					// value to check if we have a working encrypted stream 
        public const uint DHAGREEMENT_A_BITS = 128;

        public const uint PRIMESIZE_BYTES = 96;
        // MOD Note: Do not change this part - Merkur
        public const uint UDPSEARCHSPEED = ONE_SEC_MS * 1;	//1 sec - if this value is too low you will miss sources
        public const uint MAX_RESULTS = 100;			// max global search results
        public const uint MAX_MORE_SEARCH_REQ = 5;			// this gives a max. total search results of (1+5)*201 = 1206 or (1+5)*300 = 1800
        public const uint MAX_CLIENTCONNECTIONTRY = 2;
        public const uint CONNECTION_TIMEOUT = ONE_SEC_MS * 40;	//40 secs - set his lower if you want less connections at once, set it higher if you have enough sockets (edonkey has its own timout too, so a very high value won't effect this
        public const uint FILEREASKTIME = ONE_MIN_MS * 29;	//29 mins
        public const uint SERVERREASKTIME = ONE_MIN_MS * 15;	//15 mins - don't set this too low, it wont speed up anything, but it could kill emule or your internetconnection
        public const uint UDPSERVERREASKTIME = ONE_MIN_MS * 30;	//30 mins
        public const uint MAX_SERVERFAILCOUNT = 10;
        public const uint SOURCECLIENTREASKS = ONE_MIN_MS * 40;	//40 mins
        public const uint SOURCECLIENTREASKF = ONE_MIN_MS * 5;	//5 mins
        public const uint KADEMLIAASKTIME = ONE_SEC_MS * 1;	//1 second
        public const uint KADEMLIATOTALFILE = 5;			//Total files to search sources for.
        public const uint KADEMLIAREASKTIME = ONE_HOUR_MS * 1;	//1 hour
        public const uint KADEMLIAPUBLISHTIME = 2;		//2 second
        public const uint KADEMLIATOTALSTORENOTES = 1;			//Total hashes to store.
        public const uint KADEMLIATOTALSTORESRC = 3;			//Total hashes to store.
        public const uint KADEMLIATOTALSTOREKEY = 2;			//Total hashes to store.
        public const uint KADEMLIAREPUBLISHTIMES = ONE_HOUR_SEC * 5;		//5 hours
        public const uint KADEMLIAREPUBLISHTIMEN = ONE_HOUR_SEC * 24;	//24 hours
        public const uint KADEMLIAREPUBLISHTIMEK = ONE_HOUR_SEC * 24;	//24 hours
        public const uint KADEMLIADISCONNECTDELAY = ONE_MIN_SEC * 20;	//20 mins
        public const uint KADEMLIAMAXINDEX = 50000;		//Total keyword indexes.
        public const uint KADEMLIAMAXENTRIES = 60000;		//Total keyword entries.
        public const uint KADEMLIAMAXSOUCEPERFILE = 1000;		//Max number of sources per file in index.
        public const uint KADEMLIAMAXNOTESPERFILE = 150;			//Max number of notes per entry in index.
        public const uint KADEMLIAFIREWALLCHECKS = 4;		//Firewallcheck Request at a time


        public const uint ED2KREPUBLISHTIME = ONE_MIN_MS * 1;	//1 min
        public const uint MINCOMMONPENALTY = 4;
        public const uint UDPSERVERSTATTIME = ONE_SEC_MS * 5;	//5 secs
        public const uint UDPSERVSTATREASKTIME = ONE_HOUR_SEC * 9 / 2;	//4.5 hours (A random time of up to one hour is reduced during runtime after each ping;
        public const uint UDPSERVSTATMINREASKTIME = ONE_MIN_SEC * 20;	//minimum time between two pings even when trying to force a premature ping for a new UDP key
        public const uint UDPSERVERPORT = 4665;		//default udp port
        public const uint UDPMAXQUEUETIME = ONE_SEC_MS * 30;	//30 Seconds
        public const uint RSAKEYSIZE = 384;			//384 bits
        public const uint MAX_SOP_JOIN_ROOMOURCES_FILE_SOFT = 750;
        public const ulong SESSIONMAXTRANS = (PARTSIZE + 20 * 1024); // "Try to send complete chunks" always sends this amount of data
        public const uint SESSIONMAXTIME = ONE_HOUR_MS * 1;	//1 hour
        // MOD Note: end

        public const string CONFIGFOLDER = "config\\";
        public const uint MAXCONPER5SEC = 20;
        public const uint MAXCON5WIN9X = 10;
        public const uint UPLOAD_CHECK_CLIENT_DR = 2048;
        public const uint UPLOAD_CLIENT_DATARATE = 3072;		// uploadspeed per client in bytes - you may want to adjust this if you have a slow connection or T1-T3 ;)
        public const uint MAX_UP_CLIENTS_ALLOWED = 100;			// max. clients allowed regardless UPLOAD_CLIENT_DATARATE or any other factors. Don't set this too low, use DATARATE to adjust uploadspeed per client
        public const uint MIN_UP_CLIENTS_ALLOWED = 2;		// min. clients allowed to download regardless UPLOAD_CLIENT_DATARATE or any other factors. Don't set this too high
        public const uint DOWNLOADTIMEOUT = ONE_SEC_MS * 100;
        public const uint CONSERVTIMEOUT = ONE_SEC_MS * 25;	// agelimit for pending connection attempts
        public const uint RARE_FILE = 50;
        public const uint BADCLIENTBAN = 4;
        public const uint MIN_REQUESTTIME = ONE_MIN_MS * 10;
        public const uint MAX_PURGEQUEUETIME = ONE_HOUR_MS * 1;
        public const uint PURGESOURCESWAPSTOP = ONE_MIN_MS * 15;	// (15 mins), how long forbid swapping a source to a certain file (NNP,...;
        public const uint CONNECTION_LATENCY = 22050;		// latency for responces
        public const uint MINWAIT_BEFORE_DLDISPLAY_WINDOWUPDATE = 1000;
        public const uint MINWAIT_BEFORE_ULDISPLAY_WINDOWUPDATE = 1000;
        public const uint CLIENTBANTIME = ONE_HOUR_MS * 2;	// 2h
        public const uint TRACKED_CLEANUP_TIME = ONE_HOUR_MS * 1;	// 1 hour
        public const uint KEEPTRACK_TIME = ONE_HOUR_MS * 2;	// 2h	//how long to keep track of clients which were once in the uploadqueue
        public const uint LOCALSERVERREQUESTS = 20000;	// only one local src request during this timespan (WHERE IS THIS USED?)
        public const uint DISKSPACERECHECKTIME = ONE_MIN_MS * 15;
        public const uint CLIENTLIST_CLEANUP_TIME = ONE_MIN_MS * 34;	// 34 min
        public const uint MAXPRIORITYCOLL_SIZE = 50 * 1024;		// max file size for collection file which are allowed to bypass the queue
        public const uint SEARCH_SPAM_THRESHOLD = 60;
        public const uint OLDFILES_PARTIALLYPURGE = ONE_DAY_SEC * 31;	// time after which some data about a know file in the known.met and known2.met is deleted

        public const uint PeerCacheSocketUploadTimeout = DOWNLOADTIMEOUT + ONE_SEC_MS * (20 + 30);
        public const uint PeerCacheSocketDownloadTimeout = DOWNLOADTIMEOUT + ONE_SEC_MS * (20);
        public const uint MAX_CLIENT_MSG_LEN = 450;	// using 200 is just too short
        public const uint MAX_IRC_MSG_LEN = 450;	// 450 = same as in mIRC
    }
}
