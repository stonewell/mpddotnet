using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.AICH
{
    public interface AICHObjectManager
    {
         AICHHashAlgorithm CreateAICHHashAlgorithm();

         AICHHashTree CreateAICHHashTree(ulong nLeft, bool bLeftBranch, ulong nBaseSize);
         AICHHashSet CreateAICHHashSet();

         AICHHash CreateAICHHash(Mpd.Generic.IO.FileDataIO fileInput);

         AICHHash CreateAICHHash();
    }
}
