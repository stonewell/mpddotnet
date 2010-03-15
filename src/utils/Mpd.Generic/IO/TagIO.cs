using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mpd.Generic.IO
{
    public interface TagIO : DataIO
    {
        Tag ReadTag(/*bool bOptACP = false*/);
        Tag ReadTag(bool bOptACP);
        void ReadTagList(TagList pTaglist/*, bool bOptACP = false*/);
        void ReadTagList(TagList pTaglist, bool bOptACP);

        void WriteTag(Tag pTag);
        void WriteTag(string szName, byte uValue);
        void WriteTag(string szName, ushort uValue);
        void WriteTag(string szName, uint uValue);
        void WriteTag(string szName, ulong uValue);
        void WriteTag(string szName, float fValue);
        void WriteTagList(TagList tagList);
    }
}
