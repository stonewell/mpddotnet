﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.File.Impl;
using Mpd.Generic;

namespace Mule.File.Impl
{
    class FileObjectManagerImpl : FileObjectManager
    {
        public StatisticFile CreateStatisticFile()
        {
            throw new NotImplementedException();
        }

        public PartFile CreatePartFile(params object[] parameters)
        {
            return MpdObjectManager.CreateObject(typeof(PartFileImpl), parameters) as PartFile;
        }

        public KnownFile CreateKnownFile()
        {
            return MpdObjectManager.CreateObject(typeof(KnownFileImpl)) as KnownFile;
        }

        #region FileObjectManager Members


        public FileIdentifier CreateFileIdentifier(params object[] args)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
