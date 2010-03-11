using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Generic.Types.IO;

namespace Mpd.Generic.Types
{
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
