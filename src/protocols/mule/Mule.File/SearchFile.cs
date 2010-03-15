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
using Mpd.Generic.IO;

namespace Mule.File
{
    public enum KnownTypeEnum
    {
        NotDetermined,
        Shared,
        Downloading,
        Downloaded,
        Cancelled,
        Unknown
    };

    public interface SearchServer
    {
        uint IP { get;set;}
        ushort Port { get;set;}
        uint Avail { get;set;}
        bool UDPAnswer { get;set;}
    };

    public interface SearchClient
    {
        uint IP { get;set;}
        uint ServerIP { get;set; }
        ushort Port { get;set;}
        ushort ServerPort { get;set;}
    };

    public interface SearchFile : AbstractFile
    {
        bool IsKademlia { get;set;}
        bool IsServerUDPAnswer { get;set;}
        uint AddSources(uint count);
        uint GetSourceCount();
        uint AddCompleteSources(uint count);
        uint GetCompleteSourceCount();
        int IsComplete();
        int IsComplete(uint uSources, uint uCompleteSources);
        ulong LastSeenComplete { get;}
        uint SearchID { get;}
        string SearchDirectory { get;}

        uint ClientID { get;set;}
        ushort ClientPort { get;set;}
        uint ClientServerIP { get;set;}
        ushort ClientServerPort { get;set;}
        int ClientsCount { get;}
        uint KadPublishInfo { get;set;}

        // Spamfilter
        string NameWithoutKeyword { get;set;}
        uint SpamRating { get;set;}
        bool IsConsideredSpam { get;}

        // GUI helpers
        SearchFile GetListParent { get;set;}
        uint ListChildCount { get;set;}
        void AddListChildCount(int cnt);
        bool IsListExpanded { get;set;}

        void StoreToFile(FileDataIO rFile);


        void AddClient(SearchClient client);
        List<SearchClient> SearchClients { get; }

        void AddServer(SearchServer server);
        List<SearchServer> SearchServers { get;}
        SearchServer GetServerAt(int iServer);

        void AddPreviewImage(CxImage.CxImage img);
        List<CxImage.CxImage> PreviewImagess { get;}
        bool IsPreviewPossible { get;set;}

        KnownTypeEnum KnownType { get;set;}
    }
}
