using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Mpd.Utilities;
using Mpd.Generic;

namespace Mule.ED2K
{
    public interface ED2KObjectManager
    {
        ED2KServerLink CreateED2KServerLink(string ip, string port);

        ED2KNodesListLink CreateED2KNodesListLink(string address);

        ED2KFileLink CreateED2KFileLink(string pszName,
           string pszSize,
           string pszHash,
           string[] allParams,
           string pszSources);

        UnresolvedHostname CreateUnresolvedHostname();

        ED2KServerListLink CreateED2KServerListLink(string address);
        
        ED2KFileTypes CreateED2KFileTypes();

        ED2KLink CreateLinkFromUrl(string strURI);

        ED2KServer CreateED2KServer(ServerMet serverMet);
        ED2KServer CreateED2KServer(ED2KServer server);
        ED2KServer CreateED2KServer(ushort port, string ipaddr);

        ED2KServerList CreateED2KServerList();
    }
}
