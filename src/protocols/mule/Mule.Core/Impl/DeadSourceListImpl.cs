using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Utilities;

namespace Mule.Core.Impl
{
    class DeadSource
    {
        public DeadSource(DeadSource ds)
        {
            id_ = ds.id_;
            serverIP_ = ds.serverIP_;
            port_ = ds.port_;
            kadPort_ = ds.kadPort_;
            MpdUtilities.Md4Cpy(hash_, ds.hash_);
        }

        public DeadSource()
            : this(0, 0, 0, 0)
        {
        }
        public DeadSource(uint dwID)
            : this(dwID, 0, 0, 0)
        {
        }

        public DeadSource(uint dwID, ushort nPort)
            : this(dwID, nPort, 0, 0)
        {
        }

        public DeadSource(uint dwID, ushort nPort, uint dwServerIP)
            : this(dwID, nPort, dwServerIP, 0)
        {
        }

        public DeadSource(uint dwID, ushort nPort, uint dwServerIP, ushort nKadPort)
        {
            id_ = dwID;
            serverIP_ = dwServerIP;
            port_ = nPort;
            kadPort_ = nKadPort;
            MpdUtilities.Md4Clr(hash_);
        }

        public DeadSource(byte[] paucHash)
        {
            id_ = 0;
            serverIP_ = 0;
            port_ = 0;
            kadPort_ = 0;
            MpdUtilities.Md4Cpy(hash_, paucHash);
        }

        public static bool operator ==(DeadSource ds1, DeadSource ds2)
        {
            return (
                // lowid ed2k and highid kad + ed2k check
                ((ds1.id_ != 0 && ds1.id_ == ds2.id_) &&
                ((ds1.port_ != 0 && ds1.port_ == ds2.port_) ||
                (ds1.kadPort_ != 0 && ds1.kadPort_ == ds2.kadPort_)) &&
                (ds1.serverIP_ == ds2.serverIP_ ||
                !MuleUtilities.IsLowID(ds1.id_)))
                // lowid kad check
                || (MuleUtilities.IsLowID(ds1.id_) &&
                MpdUtilities.IsNullMd4(ds1.hash_) == false &&
                MpdUtilities.Md4Cmp(ds1.hash_, ds2.hash_) == 0));
        }

        public static bool operator !=(DeadSource ds1, DeadSource ds2)
        {
            return !(
                // lowid ed2k and highid kad + ed2k check
                ((ds1.id_ != 0 && ds1.id_ == ds2.id_) &&
                ((ds1.port_ != 0 && ds1.port_ == ds2.port_) ||
                (ds1.kadPort_ != 0 && ds1.kadPort_ == ds2.kadPort_)) &&
                (ds1.serverIP_ == ds2.serverIP_ ||
                !MuleUtilities.IsLowID(ds1.id_)))
                // lowid kad check
                || (MuleUtilities.IsLowID(ds1.id_) &&
                MpdUtilities.IsNullMd4(ds1.hash_) == false &&
                MpdUtilities.Md4Cmp(ds1.hash_, ds2.hash_) == 0));
        }

        public override int GetHashCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(MpdUtilities.EncodeHexString(hash_)).Append("|");
            sb.Append(id_).Append("|")
                .Append(serverIP_).Append("|")
                .Append(kadPort_)
                .Append("|").Append(port_);

            return sb.ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is DeadSource)
            {
                return (obj as DeadSource) == this;
            }

            return base.Equals(obj);
        }

        public uint id_;
        public uint serverIP_;
        public ushort port_;
        public ushort kadPort_;
        public byte[] hash_ = new byte[16];
    };

    class DeadSourceListImpl : DeadSourceList
    {
        #region Fields
        private Dictionary<DeadSource, uint> deadSources_ = new Dictionary<DeadSource, uint>();
        private uint lastCleanUp_;
        private bool isGlobalList_;
        private const uint CLEANUPTIME = MuleConstants.ONE_MIN_MS * 60;
        #endregion

        #region Constructors
        public DeadSourceListImpl()
        {
            lastCleanUp_ = 0;
        }
        #endregion

        #region DeadSourceList Members

        public void AddDeadSource(UpDownClient pToAdd)
        {
            if (!pToAdd.HasLowID)
            {
                DeadSource ds =
                    new DeadSource(pToAdd.UserIDHybrid, pToAdd.UserPort,
                        pToAdd.ServerIP, pToAdd.KadPort);
                deadSources_[ds] = BLOCKTIME();
            }
            else
            {
                if (pToAdd.ServerIP != 0)
                {
                    DeadSource ds =
                        new DeadSource(pToAdd.UserIDHybrid, pToAdd.UserPort,
                            pToAdd.ServerIP, 0);
                    deadSources_[ds] = BLOCKTIMEFW();
                }
                if (pToAdd.HasValidBuddyID || pToAdd.SupportsDirectUDPCallback)
                {
                    DeadSource ds = new DeadSource(pToAdd.UserHash);
                    deadSources_[ds] = BLOCKTIMEFW();
                }
            }
            if (MpdUtilities.GetTickCount() - lastCleanUp_ > CLEANUPTIME)
                CleanUp();
        }

        public void RemoveDeadSource(UpDownClient client)
        {
            if (!client.HasLowID)
            {
                DeadSource ds =
                    new DeadSource(client.UserIDHybrid, client.UserPort,
                        client.ServerIP, client.KadPort);
                deadSources_.Remove(ds);
            }
            else
            {
                if (client.ServerIP != 0)
                {
                    DeadSource ds =
                        new DeadSource(client.UserIDHybrid, client.UserPort,
                            client.ServerIP, 0);
                    deadSources_.Remove(ds);
                }
                if (client.HasValidBuddyID || client.SupportsDirectUDPCallback)
                {
                    DeadSource ds =
                        new DeadSource(client.UserHash);
                    deadSources_.Remove(ds);
                }
            }
        }

        public bool IsDeadSource(UpDownClient pToCheck)
        {
            if (!pToCheck.HasLowID || pToCheck.ServerIP != 0)
            {
                DeadSource ds =
                    new DeadSource(pToCheck.UserIDHybrid, pToCheck.UserPort,
                        pToCheck.ServerIP, pToCheck.KadPort);

                if (deadSources_.ContainsKey(ds))
                {
                    if (deadSources_[ds] > MpdUtilities.GetTickCount())
                        return true;
                }
            }
            if (((pToCheck.HasValidBuddyID ||
                pToCheck.SupportsDirectUDPCallback) &&
                !MpdUtilities.IsNullMd4(pToCheck.UserHash)) ||
                (pToCheck.HasLowID && pToCheck.ServerIP == 0))
            {
                DeadSource ds = new DeadSource(pToCheck.UserHash);
                if (deadSources_.ContainsKey(ds))
                {
                    if (deadSources_[ds] > MpdUtilities.GetTickCount())
                        return true;
                }
            }
            return false;
        }

        public uint DeadSourcesCount
        {
            get { return (uint)deadSources_.Count; }
        }

        public void Init(bool bGlobalList)
        {
            lastCleanUp_ = MpdUtilities.GetTickCount();
            isGlobalList_ = bGlobalList;
        }

        #endregion

        #region Protected
        void CleanUp()
        {
            lastCleanUp_ = MpdUtilities.GetTickCount();
            List<DeadSource> toRemove = new List<DeadSource>();
            uint dwTick = MpdUtilities.GetTickCount();

            Dictionary<DeadSource, uint>.Enumerator pos = deadSources_.GetEnumerator();
            while (pos.MoveNext())
            {
                if (pos.Current.Value < dwTick)
                {
                    toRemove.Add(pos.Current.Key);
                }
            }

            foreach (DeadSource ds in toRemove)
                deadSources_.Remove(ds);
        }
        private uint BLOCKTIME()
        {
            return MpdUtilities.GetTickCount() + (isGlobalList_ ? MuleConstants.ONE_MIN_MS * 15 :
                MuleConstants.ONE_MIN_MS * 45);
        }
        private uint BLOCKTIMEFW()
        {
            return MpdUtilities.GetTickCount() + (isGlobalList_ ? MuleConstants.ONE_MIN_MS * 30 :
                MuleConstants.ONE_MIN_MS * 45);
        }
        #endregion
    }
}
