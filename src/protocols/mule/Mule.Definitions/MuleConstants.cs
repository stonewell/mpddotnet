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

namespace Mule.Definitions
{
    public sealed class MuleConstants
    {
        private MuleConstants()
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

        public const string HOME_URL = "http://code.google.com/p/monomule";

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

        public const uint KADEMLIAREPUBLISHTIMES = 5 * 60 * 60;
        public const uint KADEMLIAREPUBLISHTIMEN = 24 * 60 * 60;

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

        public const uint KADEMLIATOTALFILE = 5;
        public const uint KADEMLIAREASKTIME = 1 * 3600 * 1000;
        public const uint SERVERREASKTIME = 15 * 60 * 1000;
    }
}
