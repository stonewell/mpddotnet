using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Generic.IO;

namespace Mpd.Generic
{
    public enum TagTypeEnum : uint
    {
        CT_NAME = 0x01,
        CT_PORT = 0x0f,
        CT_VERSION = 0x11,
        CT_SERVER_FLAGS = 0x20,	// currently only used to inform a server about supported features,
        CT_MOD_VERSION = 0x55,
        CT_EMULECOMPAT_OPTIONS1 = 0xef,
        CT_EMULE_RESERVED1 = 0xf0,
        CT_EMULE_RESERVED2 = 0xf1,
        CT_EMULE_RESERVED3 = 0xf2,
        CT_EMULE_RESERVED4 = 0xf3,
        CT_EMULE_RESERVED5 = 0xf4,
        CT_EMULE_RESERVED6 = 0xf5,
        CT_EMULE_RESERVED7 = 0xf6,
        CT_EMULE_RESERVED8 = 0xf7,
        CT_EMULE_RESERVED9 = 0xf8,
        CT_EMULE_UDPPORTS = 0xf9,
        CT_EMULE_MISCOPTIONS1 = 0xfa,
        CT_EMULE_VERSION = 0xfb,
        CT_EMULE_BUDDYIP = 0xfc,
        CT_EMULE_BUDDYUDP = 0xfd,
        CT_EMULE_MISCOPTIONS2 = 0xfe,
        CT_EMULE_RESERVED13 = 0xff,
        CT_SERVER_UDPSEARCH_FLAGS = 0x0E,
    }

    [Flags]
    public enum ServerFlagsEnum
    {
        SRVCAP_ZLIB = 0x0001,
        SRVCAP_IP_IN_LOGIN = 0x0002,
        SRVCAP_AUXPORT = 0x0004,
        SRVCAP_NEWTAGS = 0x0008,
        SRVCAP_UNICODE = 0x0010,
        SRVCAP_LARGEFILES = 0x0100,
        SRVCAP_SUPPORTCRYPT = 0x0200,
        SRVCAP_REQUESTCRYPT = 0x0400,
        SRVCAP_REQUIRECRYPT = 0x0800,
    }

    public class TagList : List<Tag>
    {
    }

    public delegate string DbgGetFileMetaTagName(uint uMetaTagID);

    public interface Tag
    {
        uint TagType { get; }
        uint NameID { get; }
        string Name { get; }

        bool IsStr { get; }
        bool IsInt { get; }
        bool IsFloat { get; }
        bool IsHash { get; }
        bool IsBlob { get; }
        bool IsInt64();
        bool IsInt64(bool bOrInt32);

        uint Int { get; set; }
        ulong Int64 { get; set; }
        string Str { get; set; }
        float Float { get; set; }
        byte[] Hash { get; set; }
        uint BlobSize { get; set; }
        byte[] Blob { get; set; }

        Tag CloneTag();

        bool WriteTagToFile(TagIO file);
        bool WriteTagToFile(TagIO file, Utf8StrEnum eStrEncode);
        bool WriteNewEd2kTag(TagIO file);
        bool WriteNewEd2kTag(TagIO file, Utf8StrEnum eStrEncode);

        string GetFullInfo(DbgGetFileMetaTagName fn);
    }
}
