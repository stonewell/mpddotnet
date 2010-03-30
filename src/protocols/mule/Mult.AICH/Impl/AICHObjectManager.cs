using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.AICH.Impl
{
    class AICHObjectManagerImpl : AICHObjectManager
    {
        #region AICHObjectManager Members
        public AICHHashAlgorithm CreateAICHHashAlgorithm()
        {
            return new SHA.Impl.SHAImpl();
        }

        public AICHHashTree CreateAICHHashTree(ulong nLeft, bool bLeftBranch, ulong nBaseSize)
        {
            return new Impl.AICHHashTreeImpl(nLeft, bLeftBranch, nBaseSize);
        }

        public AICHHashSet CreateAICHHashSet()
        {
            return new Impl.AICHHashSetImpl();
        }

        public AICHHash CreateAICHHash(Mpd.Generic.IO.FileDataIO fileInput)
        {
            AICHHash hash = CreateAICHHash();
            hash.Read(fileInput);

            return hash;
        }

        public AICHHash CreateAICHHash()
        {
            AICHHash hash = new Impl.AICHHashImpl();

            return hash;
        }

        public AICHRecoveryHashSet CreateAICHRecoveryHashSet(params object[] args)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
