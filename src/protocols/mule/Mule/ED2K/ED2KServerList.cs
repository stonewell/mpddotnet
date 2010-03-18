using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.ED2K
{
    public interface ED2KServerList
    {
        bool Init();
        void Sort();
        void MoveServerDown(ED2KServer pServer);
        void AutoUpdate();

        bool AddServer(ED2KServer pServer);
        bool AddServer(ED2KServer pServer, bool bAddTail);
        void RemoveServer(ED2KServer pServer);
        void RemoveAllServers();
        void RemoveDuplicatesByAddress(ED2KServer pExceptThis);
        void RemoveDuplicatesByIP(ED2KServer pExceptThis);

        uint ServerCount { get; }
        ED2KServer this[int index] { get; }
        ED2KServer GetSuccServer(ED2KServer lastserver);
        ED2KServer GetNextServer(bool bOnlyObfuscated);
        ED2KServer GetServerByAddress(string address, ushort port);
        ED2KServer GetServerByIP(uint nIP);
        ED2KServer GetServerByIPTCP(uint nIP, ushort nTCPPort);
        ED2KServer GetServerByIPUDP(uint nIP, ushort nUDPPort);
        ED2KServer GetServerByIPUDP(uint nIP, ushort nUDPPort, bool bObfuscationPorts);
        int GetPositionOfServer(ED2KServer pServer);

        uint ServerPostion { get; set; }

        void ResetSearchServerPos();
        ED2KServer GetNextSearchServer();

        void ServerStats();
        ED2KServer GetNextStatServer();

        bool IsGoodServerIP(ED2KServer pServer);
        void GetStatus(ref uint total, ref uint failed, ref uint user, ref uint file, ref uint lowiduser,
                              ref uint totaluser, ref uint totalfile, ref float occ);
        void GetAvgFile(ref uint average);
        void GetUserFileStatus(ref uint user, ref uint file);
        uint DeletedServerCount { get; }

        bool GiveServersForTraceRoute();

        void CheckForExpiredUDPKeys();
    }
}
