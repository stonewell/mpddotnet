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

namespace Mule.Core
{
    public sealed class MuleEngine
    {
        #region Fields
        private SharedFileList sharedFiles_ = null;
        private CoreObjectManager coreObjectManager_ = null;
        private CoreUtilities coreUtilities_ = null;
        private Kademlia.KadEngine kadEngine_ = null;
        private ServerConnect serverConnect_ = null;
        private ClientList clientList_ = null;
        private DownloadQueue downloadQueue_ = null;
        private object hashLocker_ = new object();
        #endregion

        #region Constructors
        public MuleEngine()
        {
            kadEngine_ = new Kademlia.KadEngine();
            coreObjectManager_ = new CoreObjectManager(this);

            coreUtilities_ = coreObjectManager_.CreateCoreUtilities();

            sharedFiles_ = coreObjectManager_.CreateSharedFileList();
        }

        #endregion

        #region Properties
        public DownloadQueue DownloadQueue
        {
            get { return downloadQueue_; }
        }

        public Kademlia.KadEngine KadEngine
        {
            get { return kadEngine_; }
        }

        public Kademlia.KadObjectManager KadObjectManager
        {
            get { return kadEngine_.ObjectManager; }
        }

        public SharedFileList SharedFiles
        {
            get { return sharedFiles_; }
        }

        internal CoreObjectManager CoreObjectManager
        {
            get { return coreObjectManager_; }
        }

        internal CoreUtilities CoreUtilities
        {
            get { return coreUtilities_; }
        }

        internal bool IsFirewalled
        {
            get
            {
                if (serverConnect_.IsConnected && !serverConnect_.IsLowID)
                    return false; // we have an eD2K HighID . not firewalled

                if (KadEngine.IsConnected && !KadEngine.IsFirewalled)
                    return false; // we have an Kad HighID . not firewalled

                return true; // firewalled
            }
        }

        internal ClientList ClientList
        {
            get
            {
                return clientList_;
            }
        }
        #endregion

        internal ServerConnect ServerConnect
        {
            get { return serverConnect_; }
        }

        public uint PublicIP
        {
            get;
            set;
        }

        internal bool CanDoCallback(UpDownClient client)
        {
            if (KadEngine.IsConnected)
            {
                if (serverConnect_.IsConnected)
                {
                    if (serverConnect_.IsLowID)
                    {
                        if (KadEngine.IsFirewalled)
                        {
                            //Both Connected - Both Firewalled
                            return false;
                        }
                        else
                        {
                            if (client.ServerIP == serverConnect_.CurrentServer.IP &&
                                client.ServerPort == serverConnect_.CurrentServer.Port)
                            {
                                //Both Connected - Server lowID, Kad Open - Client on same server
                                //We prevent a callback to the server as this breaks the protocol and will get you banned.
                                return false;
                            }
                            else
                            {
                                //Both Connected - Server lowID, Kad Open - Client on remote server
                                return true;
                            }
                        }
                    }
                    else
                    {
                        //Both Connected - Server HighID, Kad don't care
                        return true;
                    }
                }
                else
                {
                    if (KadEngine.IsFirewalled)
                    {
                        //Only Kad Connected - Kad Firewalled
                        return false;
                    }
                    else
                    {
                        //Only Kad Conected - Kad Open
                        return true;
                    }
                }
            }
            else
            {
                if (serverConnect_.IsConnected)
                {
                    if (serverConnect_.IsLowID)
                    {
                        //Only Server Connected - Server LowID
                        return false;
                    }
                    else
                    {
                        //Only Server Connected - Server HighID
                        return true;
                    }
                }
                else
                {
                    //We are not connected at all!
                    return false;
                }
            }
        }

        internal bool IsConnected
        {
            get
            {
                return (serverConnect_.IsConnected || KadEngine.IsConnected);
            }
        }

        internal object HashLocker
        {
            get { return hashLocker_; }
        }
    }
}
