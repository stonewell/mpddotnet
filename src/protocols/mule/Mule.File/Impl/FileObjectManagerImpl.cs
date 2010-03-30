using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.File.Impl;
using Mpd.Generic;

namespace Mule.File.Impl
{
    class FileObjectManagerImpl : FileObjectManager
    {
        #region FileObjectManager Members
        public StatisticFile CreateStatisticFile()
        {
            return new StatisticFileImpl();
        }

        public PartFile CreatePartFile(params object[] parameters)
        {
            return MpdObjectManager.CreateObject(typeof(PartFileImpl), parameters) as PartFile;
        }

        public KnownFile CreateKnownFile()
        {
            return MpdObjectManager.CreateObject(typeof(KnownFileImpl)) as KnownFile;
        }

        public FileIdentifier CreateFileIdentifier(params object[] args)
        {
            return MpdObjectManager.CreateObject(typeof(FileIdentifierImpl), args) as FileIdentifier;
        }

        public KnownFileList CreateKnownFileList()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
