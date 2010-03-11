using System;
using System.Collections.Generic;
using System.Text;
using Mule.File;
using System.Threading;
using Mule.Core.Impl;
using System.IO;
using Mule.Definitions;

namespace Mule.Core
{
    internal class AddFileThread : MuleBaseObjectImpl
    {
        private SharedFileList owner_ = null;
        private string directory_ = null;
        private string filename_ = null;
        private PartFile partFile_ = null;

        public void SetValues(SharedFileList owner, string directory, string filename, PartFile partFile)
        {
            owner_ = owner;
            directory_ = directory;
            filename_ = filename;
            partFile_ = partFile;
        }

        public void Start()
        {
            Thread t = new Thread(new ParameterizedThreadStart(AddFileThreadStart));

            t.Start(this);
        }

        private static void AddFileThreadStart(object target)
        {
            AddFileThread thread = target as AddFileThread;

            lock (thread.MuleEngine.HashLocker)
            {
                string filePath = Path.Combine(thread.directory_, thread.filename_);

                KnownFile knownFile = FileObjectManager.CreateKnownFile();

                if (knownFile.CreateFromFile(thread.directory_, thread.filename_))
                {
                    if (thread.partFile_ != null &&
                        thread.partFile_.FileOp == PartFileOpEnum.PFOP_HASHING)
                    {
                        thread.partFile_.FileOp = PartFileOpEnum.PFOP_NONE;
                    }

                    //TODO:Post finish hash event
                }
                else
                {
                    if (thread.partFile_ != null &&
                        thread.partFile_.FileOp == PartFileOpEnum.PFOP_HASHING)
                    {
                        thread.partFile_.FileOp = PartFileOpEnum.PFOP_NONE;
                    }

                    //TODO: Post hash error event
                }
            }
        }
    }
}
