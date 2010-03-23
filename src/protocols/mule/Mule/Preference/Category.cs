using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Preference
{
    public struct Category
    {
        public string IncomingPath;
        public string Title;
        public string Comment;
        public uint Color;
        public uint Priority;
        public string AutoCategory;
        public string Regexp;
        public int Filter;
        public bool FilterNeg;
        public bool Care4All;
        public bool RegExpEval;
        public bool DownloadInAlphabeticalOrder;
    }
}
