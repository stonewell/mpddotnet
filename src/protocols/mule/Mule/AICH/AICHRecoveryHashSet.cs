using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Generic.IO;
using Mule.File;
using Mule.Core;

namespace Mule.AICH
{
    public interface AICHRecoveryHashSet
    {
        bool CreatePartRecoveryData(ulong nPartStartPos, FileDataIO fileDataOut);
        bool CreatePartRecoveryData(ulong nPartStartPos, FileDataIO fileDataOut, bool bDbgDontLoad);
        bool ReadRecoveryData(ulong nPartStartPos, SafeMemFile fileDataIn);
        bool ReCalculateHash();
        bool ReCalculateHash(bool bDontReplace);
        bool VerifyHashTree(bool bDeleteBadTrees);
        void UntrustedHashReceived(AICHHash Hash, uint dwFromIP);
        bool IsPartDataAvailable(ulong nPartStartPos);
        AICHStatusEnum Status { get; set; }
        void SetOwner(KnownFile val);

        void FreeHashSet();
        void SetFileSize(ulong nSize);

        AICHHash MasterHash { get; }
        void SetMasterHash(AICHHash hash, AICHStatusEnum eNewStatus);
        bool HasValidMasterHash { get; }
        bool GetPartHashs(IList<AICHHash> rResult);
        AICHHashTree FindPartHash(ushort nPart);

        bool SaveHashSet();
        bool LoadHashSet(); // Loading from known2.met

        AICHHashAlgorithm GetNewHashAlgo();
        void ClientAICHRequestFailed(UpDownClient pClient);
        void RemoveClientAICHRequest(UpDownClient pClient);
        bool IsClientRequestPending(PartFile pForFile, ushort nPart);
        AICHRequestedData GetAICHReqDetails(UpDownClient pClient);
        void AddStoredAICHHash(AICHHash Hash);
        void DbgTest();

        AICHHashTree HashTree { get; set; }
    }
}
