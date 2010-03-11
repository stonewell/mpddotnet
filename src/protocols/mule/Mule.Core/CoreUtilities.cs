using System;
using System.Collections.Generic;
using System.Text;
using Mule.AICH;
using System.IO;
using Mule.Core.Impl;
using Mule.File;

namespace Mule.Core
{
    class CoreUtilities : MuleBaseObjectImpl
    {
        public bool WriteOptED2KUTF8Tag(Mule.File.FileDataIO file,
            string filename, byte uTagName)
        {
            if (!NeedUTF8String(filename))
                return false;
            Tag tag = MuleEngine.CoreObjectManager.CreateTag(uTagName, filename);
            tag.WriteTagToFile(file, Utf8StrEnum.utf8strOptBOM);
            return true;
        }

        public bool NeedUTF8String(string filename)
        {
            for (int i = 0; i < filename.Length; i++)
            {
                if (filename[i] >= 0x100U)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
