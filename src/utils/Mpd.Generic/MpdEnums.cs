using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mpd.Generic
{
    public enum Utf8StrEnum
    {
        utf8strNone,
        utf8strOptBOM,
        utf8strRaw
    };

    public enum EDebugLogPriority
    {
        DLP_VERYLOW = 0,
        DLP_LOW,
        DLP_DEFAULT,
        DLP_HIGH,
        DLP_VERYHIGH
    };
}
