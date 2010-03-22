using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.File
{
    public interface FileObjectManager
    {
        StatisticFile CreateStatisticFile();
        PartFile CreatePartFile(params object[] parameters);
        KnownFile CreateKnownFile();
        FileIdentifier CreateFileIdentifier(params object[] args);
    }
}
