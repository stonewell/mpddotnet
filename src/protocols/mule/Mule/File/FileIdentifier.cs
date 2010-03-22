using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Generic.IO;
using Mule.AICH;

namespace Mule.File
{
    public interface FileIdentifier : FileIdentifierBase
    {
        void WriteHashSetsToPacket(FileDataIO pFile, bool bMD4, bool bAICH); // not compatible with old single md4 hashset
        bool ReadHashSetsFromPacket(FileDataIO pFile, ref bool rbMD4, ref bool rbAICH);  // not compatible with old single md4 hashset

        bool CalculateMD4HashByHashSet(bool bVerifyOnly);
        bool CalculateMD4HashByHashSet(bool bVerifyOnly, bool bDeleteOnVerifyFail);
        bool LoadMD4HashsetFromFile(FileDataIO file, bool bVerifyExistingHash);
        void WriteMD4HashsetToFile(FileDataIO pFile);

        bool SetMD4HashSet(ByteArrayArray aHashset);
        byte[] GetMD4PartHash(uint part);
        void DeleteMD4Hashset();

        ushort TheoreticalMD4PartHashCount { get; }
        ushort AvailableMD4PartHashCount { get; }
        bool HasExpectedMD4HashCount { get; }

        ByteArrayArray RawMD4HashSet { get; }

        bool LoadAICHHashsetFromFile(FileDataIO pFile);
        // only set verify to false if you call VerifyAICHHashSet yourself immediately after
        bool LoadAICHHashsetFromFile(FileDataIO pFile, bool bVerify);
        void WriteAICHHashsetToFile(FileDataIO pFile);

        bool SetAICHHashSet(AICHRecoveryHashSet rSourceHashSet);
        bool SetAICHHashSet(FileIdentifier rSourceHashSet);

        bool VerifyAICHHashSet();
        ushort TheoreticalAICHPartHashCount { get; }
        ushort AvailableAICHPartHashCount { get; }
        bool HasExpectedAICHHashCount { get; }

        IList<AICHHash> RawAICHHashSet { get; }
    }
}
