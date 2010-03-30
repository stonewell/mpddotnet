using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Mule.Core
{
    public interface ClientCreditsList
    {
        byte CreateSignature(ClientCredits pTarget,
            byte[] pachOutput,
            byte nMaxSize,
            uint ChallengeIP,
            byte byChaIPKind);

        byte CreateSignature(ClientCredits pTarget,
            byte[] pachOutput,
            byte nMaxSize,
            uint ChallengeIP,
            byte byChaIPKind,
            RSAPKCS1SignatureFormatter sigkey);

        bool VerifyIdent(ClientCredits pTarget,
            byte[] pachSignature,
            byte nInputSize,
            uint dwForIP, byte
            byChaIPKind);

        ClientCredits GetCredit(byte[] key);
        void Process();
        byte PubKeyLength { get; }
        byte[] PublicKey { get; }
        bool IsCryptoAvailable { get; }

        void CleanUp();
    }
}
