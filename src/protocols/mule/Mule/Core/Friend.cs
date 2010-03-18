#region File Header

//
//  Copyright (C) 2008 Jingnan Si
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
//
#endregion

using System;
using System.Collections.Generic;
using System.Text;

using Mule.Network;
using Kademlia;
using Mule.File;
using Mpd.Generic.IO;

namespace Mule.Core
{
    public enum FriendConnectStateEnum
    {
        FCS_NONE = 0,
        FCS_CONNECTING,
        FCS_AUTH,
        FCS_KADSEARCHING,
    };

    public enum FriendConnectReportEnum
    {
        FCR_ESTABLISHED = 0,
        FCR_DISCONNECTED,
        FCR_USERHASHVERIFIED,
        FCR_USERHASHFAILED,
        FCR_SECUREIDENTFAILED,
        FCR_DELETED
    };

    public delegate void ReportConnectionProgressEvent(UpDownClient pClient, string strProgressDesc, bool bNoTimeStamp);
    public delegate void ConnectingResultEvent(UpDownClient pClient, bool bSuccess);
    public delegate void ClientObjectChangedEvent(UpDownClient pOldClient, UpDownClient pNewClient);

    public enum FriendTypeEnum : int
    {
        FF_NAME = 0x01,
        FF_KADID = 0x02,
    };

    public interface Friend
    {
        event ReportConnectionProgressEvent ReportConnectionProgress;
        event ConnectingResultEvent ConnectingResult;
        event ClientObjectChangedEvent ClientObjectChanged;

        UpDownClient GetLinkedClient(bool bValidCheck);
        void SetLinkedClient(UpDownClient linkedClient);
        UpDownClient GetClientForChatSession();

        void LoadFromFile(FileDataIO file);
        void WriteToFile(FileDataIO file);

        bool TryToConnect();
        void UpdateFriendConnectionState(FriendConnectReportEnum eEvent);
        bool IsTryingToConnect { get; }

        bool CancelTryToConnect();
        void FindKadID();

        void KadSearchNodeIDByIPResult(KadClientSearchResEnum eStatus,
        char[] pachNodeID);
        void KadSearchIPByNodeIDResult(KadClientSearchResEnum eStatus,
        uint dwIP, ushort nPort);

        void SendMessage(string strMessage);

        bool FriendSlot { get; set; }

        bool HasUserhash { get; }
        bool HasKadID { get; }
    }
}
