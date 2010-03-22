using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Generic.IO;
using Mule.AICH;

namespace Mule.File
{
    public interface FileIdentifierBase
    {
        ulong FileSize { get; }

        void WriteIdentifier(FileDataIO pFile);
        void WriteIdentifier(FileDataIO pFile, bool bKadExcludeMD4);
        bool CompareRelaxed(FileIdentifierBase rFileIdentifier);
        bool CompareStrict(FileIdentifierBase rFileIdentifier);

        string DbgInfo();

        byte[] MD4Hash { get; set; }
        void SetMD4Hash(FileDataIO pFile);

        AICHHash AICHHash { get; set; }
        bool HasAICHHash { get; set; }
    }
}
