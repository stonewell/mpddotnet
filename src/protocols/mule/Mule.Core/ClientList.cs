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
using Kademlia;
using Mule.Core.Network;
using Mpd.Generic.Types;

namespace Mule.Core
{
    public interface ClientList
    {
	// Clients
	void	AddClient(UpDownClient toadd/*,bool bSkipDupTest = false*/);
	void	AddClient(UpDownClient toadd,bool bSkipDupTest);
	void	RemoveClient(UpDownClient toremove/*, LPCTSTR pszReason = NULL*/);
	void	RemoveClient(UpDownClient toremove, string pszReason);
	void	GetStatistics(ref uint totalclient, int[] stats,
						  Dictionary<uint,uint> clientVersionEDonkey,
						  Dictionary<uint,uint> clientVersionEDonkeyHybrid,
						  Dictionary<uint,uint> clientVersionEMule,
						  Dictionary<uint,uint> clientVersionAMule);
	uint	GetClientCount {get;}
	void	DeleteAll();
	bool	AttachToAlreadyKnown(out UpDownClient client, ClientRequestSocket sender);
	UpDownClient FindClientByIP(uint clientip, uint port);
	UpDownClient FindClientByUserHash(byte[] clienthash, uint dwIP, ushort nTCPPort) ;
	UpDownClient FindClientByIP(uint clientip);
	UpDownClient FindClientByIP_UDP(uint clientip, uint nUDPport);
	UpDownClient FindClientByServerID(uint uServerIP, uint uUserID);
	UpDownClient FindClientByUserID_KadPort(uint clientID,ushort kadPort);
	UpDownClient FindClientByIP_KadPort(uint ip, ushort port);

	// Banned clients
	void	AddBannedClient(uint dwIP);
	bool	IsBannedClient(uint dwIP) ;
	void	RemoveBannedClient(uint dwIP);
	uint	BannedCount {get;}
	void	RemoveAllBannedClients();

	// Tracked clients
	void	AddTrackClient(UpDownClient toadd);
	bool	ComparePriorUserhash(uint dwIP, ushort nPort, byte[] pNewHash);
	uint	GetClientsFromIP(uint dwIP) ;
	void	TrackBadRequest( UpDownClient upcClient, int nIncreaseCounter);
	uint	GetBadRequests( UpDownClient upcClient) ;
	uint	TrackedCount {get;}
	void	RemoveAllTrackedClients();

	// Kad client list, buddy handling
	bool	RequestTCP(KadContact contact, byte byConnectOptions);
	void	RequestBuddy(KadContact contact, byte byConnectOptions);
	bool	IncomingBuddy(KadContact contact, UInt128 buddyID);
	void	RemoveFromKadList(UpDownClient torem);
	void	AddToKadList(UpDownClient toadd);
	bool	DoRequestFirewallCheckUDP( KadContact contact);
        byte BuddyStatus { get;}
        UpDownClient Buddy { get;}

	void	AddKadFirewallRequest(uint dwIP);
	bool	IsKadFirewallCheckIP(uint dwIP) ;

	// Direct Callback List
	void	AddTrackCallbackRequests(uint dwIP);
	bool	AllowCalbackRequest(uint dwIP) ;

	// Connecting Clients
	void	AddConnectingClient(UpDownClient pToAdd);
	void	RemoveConnectingClient(UpDownClient pToRemove);

	void	Process();
	bool	IsValidClient(UpDownClient tocheck) ;
	void	Debug_SocketDeleted(ClientRequestSocket deleted) ;

    // ZZ:UploadSpeedSense -->
        bool GiveClientsForTraceRoute { get;}
	// ZZ:UploadSpeedSense <--

    void	ProcessA4AFClients() ; // ZZ:DownloadManager
    }
}
