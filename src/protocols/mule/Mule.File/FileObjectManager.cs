using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.File.Impl;
using Mpd.Generic.Types;

namespace Mule.File
{
    public class FileObjectManager
    {
        public static StatisticFile CreateStatisticFile()
        {
            throw new NotImplementedException();
        }

        public static PartFile CreatePartFile(params object[] parameters)
        {
            return MpdGenericObjectManager.CreateObject(typeof(PartFileImpl), parameters) as PartFile;
        }

        public static KnownFile CreateKnownFile()
        {
            return MpdGenericObjectManager.CreateObject(typeof(KnownFileImpl)) as KnownFile;
        }
    }
}
