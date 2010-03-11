using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.AICH
{
    public class AICHObjectManager
    {
        public static AICHHashAlgorithm CreateAICHHashAlgorithm()
        {
            return new SHA.Impl.SHAImpl();
        }

        public static AICHHashTree CreateAICHHashTree(ulong nLeft, bool bLeftBranch, ulong nBaseSize)
        {
            return new Impl.AICHHashTreeImpl( nLeft, bLeftBranch, nBaseSize);
        }

        public static AICHHashSet CreateAICHHashSet()
        {
            return new Impl.AICHHashSetImpl();
        }

        public static AICHHash CreateAICHHash(Mpd.Generic.Types.IO.FileDataIO fileInput)
        {
            AICHHash hash = CreateAICHHash();
            hash.Read(fileInput);

            return hash;
        }

        public static AICHHash CreateAICHHash()
        {
            AICHHash hash = new Impl.AICHHashImpl();

            return hash;
        }
    }
}
