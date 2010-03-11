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
using System.Threading;
using Mule.Core.Network;
using Mule.Core.File;
using System.IO;
using Mule.Core.Impl;

namespace Mule.Core.AICH.Impl
{
    class AICHHashSetImpl : MuleBaseObjectImpl, AICHHashSet
    {
        #region Fields
        private AICHHashTree hashTree_ = null;

        private KnownFile owner_;
        private AICHStatusEnum status_ = AICHStatusEnum.AICH_EMPTY;
        private List<AICHUntrustedHash> untrustedHashs_ =
            new List<AICHUntrustedHash>();
        #endregion

        #region Overrides
        public override MuleEngine MuleEngine
        {
            get
            {
                return base.MuleEngine;
            }
            set
            {
                base.MuleEngine = value;

                hashTree_ = MuleEngine.CoreObjectManager.CreateAICHHashTree(0, true, CoreConstants.PARTSIZE);
            }
        }
        #endregion

        #region Constructors
        public AICHHashSetImpl(KnownFile pOwner)
        {
            owner_ = pOwner;
        }
        #endregion

        #region AICHHashSet Members
        public bool CreatePartRecoveryData(ulong nPartStartPos, FileDataIO fileDataOut, bool bDbgDontLoad)
        {
            if (owner_.IsPartFile || status_ != AICHStatusEnum.AICH_HASHSETCOMPLETE)
            {
                return false;
            }
            if (HashTree.DataSize <= CoreConstants.EMBLOCKSIZE)
            {
                return false;
            }
            if (!bDbgDontLoad)
            {
                if (!LoadHashSet())
                {
                    //TODO:Log
                    //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Created RecoveryData error: failed to load hashset (file: %s)"), owner_.GetFileName());
                    Status = AICHStatusEnum.AICH_ERROR;
                    return false;
                }
            }
            bool bResult;
            byte nLevel = 0;
            uint nPartSize = (uint)Math.Min(CoreConstants.PARTSIZE, (ulong)owner_.FileSize - nPartStartPos);
            HashTree.FindHash(nPartStartPos, nPartSize, ref nLevel);
            UInt16 nHashsToWrite = (UInt16)(((ulong)nLevel - 1) + (ulong)nPartSize / CoreConstants.EMBLOCKSIZE + (((ulong)nPartSize % CoreConstants.EMBLOCKSIZE != 0) ? (ulong)1 : (ulong)0));
            bool bUse32BitIdentifier = owner_.IsLargeFile;

            if (bUse32BitIdentifier)
                fileDataOut.WriteUInt16(0); // no 16bit hashs to write
            fileDataOut.WriteUInt16(nHashsToWrite);
            ulong nCheckFilePos = (ulong)fileDataOut.Position;
            if (HashTree.CreatePartRecoveryData(nPartStartPos, nPartSize, fileDataOut, 0, bUse32BitIdentifier))
            {
                if ((ulong)nHashsToWrite *
                        (CoreConstants.HASHSIZE +
                            (bUse32BitIdentifier ? (ulong)4 : (ulong)2)) != (ulong)fileDataOut.Position - nCheckFilePos)
                {
                    //TODO:Log
                    //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Created RecoveryData has wrong length (file: %s)"), owner_.GetFileName());
                    bResult = false;
                    Status = AICHStatusEnum.AICH_ERROR;
                }
                else
                    bResult = true;
            }
            else
            {
                //TODO:Log
                //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Failed to create RecoveryData for %s"), owner_.GetFileName());
                bResult = false;
                Status = AICHStatusEnum.AICH_ERROR;
            }
            if (!bUse32BitIdentifier)
                fileDataOut.WriteUInt16(0); // no 32bit hashs to write

            if (!bDbgDontLoad)
            {
                FreeHashSet();
            }
            return bResult;
        }

        public bool ReadRecoveryData(ulong nPartStartPos, SafeMemFile fileDataIn)
        {
            if (/*TODO !Owner.IsPartFile() ||*/
                !(Status == AICHStatusEnum.AICH_VERIFIED ||
                Status == AICHStatusEnum.AICH_TRUSTED))
            {

                return false;
            }
            /* V2 AICH Hash Packet:
                <count1 UInt16>											16bit-hashs-to-read
                (<identifier UInt16><hash CoreConstants.HASHSIZE>)[count1]			AICH hashs
                <count2 UInt16>											32bit-hashs-to-read
                (<identifier uint><hash CoreConstants.HASHSIZE>)[count2]			AICH hashs
            */

            // at this time we check the recoverydata for the correct ammounts of hashs only
            // all hash are then taken into the tree, depending on there hashidentifier (except the masterhash)

            byte nLevel = 0;
            uint nPartSize = (uint)Math.Min(CoreConstants.PARTSIZE, (ulong)Owner.FileSize - nPartStartPos);
            HashTree.FindHash(nPartStartPos, nPartSize, ref nLevel);
            UInt16 nHashsToRead =
                (UInt16)(((ulong)nLevel - 1) + (ulong)nPartSize / CoreConstants.EMBLOCKSIZE +
                    (((ulong)nPartSize % CoreConstants.EMBLOCKSIZE != 0) ? (ulong)1 : (ulong)0));

            // read hashs with 16 bit identifier
            UInt16 nHashsAvailable = fileDataIn.ReadUInt16();
            if ((ulong)fileDataIn.Length - (ulong)fileDataIn.Position <
                (ulong)nHashsToRead * (CoreConstants.HASHSIZE + 2) ||
                (nHashsToRead != nHashsAvailable && nHashsAvailable != 0))
            {
                // this check is redunant, CSafememfile would catch such an error too
                // TODO:Log
                //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Failed to read RecoveryData for %s - Received datasize/amounts of hashs was invalid (1)"), Owner.GetFileName());
                return false;
            }
            //TODO:Log
            //DEBUG_ONLY(theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("read RecoveryData for %s - Received packet with  %u 16bit hash identifiers)"), Owner.GetFileName(), nHashsAvailable));
            for (uint i = 0; i != nHashsAvailable; i++)
            {
                UInt16 wHashIdent = fileDataIn.ReadUInt16();
                if (wHashIdent == 1 /*never allow masterhash to be overwritten*/
                    || !HashTree.SetHash(fileDataIn, wHashIdent, (-1), false))
                {
                    //TODO:Log
                    //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Failed to read RecoveryData for %s - Error when trying to read hash into tree (1)"), Owner.GetFileName());
                    // remove invalid hashs which we have already written
                    VerifyHashTree(true);
                    return false;
                }
            }

            // read hashs with 32bit identifier
            if (nHashsAvailable == 0 &&
                fileDataIn.Length - fileDataIn.Position >= 2)
            {
                nHashsAvailable = fileDataIn.ReadUInt16();
                if ((ulong)fileDataIn.Length - (ulong)fileDataIn.Position <
                    (ulong)nHashsToRead * (CoreConstants.HASHSIZE + 4) ||
                    (nHashsToRead != nHashsAvailable && nHashsAvailable != 0))
                {
                    // this check is redunant, CSafememfile would catch such an error too
                    //TODO:LOg
                    //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Failed to read RecoveryData for %s - Received datasize/amounts of hashs was invalid (2)"), Owner.GetFileName());
                    return false;
                }
                //TODO:LOg
                //DEBUG_ONLY(theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("read RecoveryData for %s - Received packet with  %u 32bit hash identifiers)"), Owner.GetFileName(), nHashsAvailable));
                for (uint i = 0; i != nHashsToRead; i++)
                {
                    uint wHashIdent = fileDataIn.ReadUInt32();
                    /*never allow masterhash to be overwritten*/
                    if (wHashIdent == 1
                        || wHashIdent > 0x400000
                        || !HashTree.SetHash(fileDataIn, wHashIdent, (-1), false))
                    {
                        //TODO:LOg
                        //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Failed to read RecoveryData for %s - Error when trying to read hash into tree (2)"), Owner.GetFileName());
                        // remove invalid hashs which we have already written
                        VerifyHashTree(true);
                        return false;
                    }
                }
            }

            if (nHashsAvailable == 0)
            {
                //TODO:LOg
                //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Failed to read RecoveryData for %s - Packet didn't contained any hashs"), Owner.GetFileName());
                return false;
            }


            if (VerifyHashTree(true))
            {
                // some final check if all hashs we wanted are there
                for (ulong nPartPos = 0; nPartPos < nPartSize; nPartPos += CoreConstants.EMBLOCKSIZE)
                {
                    AICHHashTree phtToCheck = HashTree.FindHash(nPartStartPos + nPartPos, Math.Min(CoreConstants.EMBLOCKSIZE, nPartSize - nPartPos));
                    if (phtToCheck == null || !phtToCheck.HashValid)
                    {
                        //TODO:LOg
                        //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Failed to read RecoveryData for %s - Error while verifying presence of all lowest level hashs"), Owner.GetFileName());
                        return false;
                    }
                }
                // all done
                return true;
            }
            else
            {
                //TODO:LOg
                //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Failed to read RecoveryData for %s - Verifying received hashtree failed"), Owner.GetFileName());
                return false;
            }
        }


        public bool ReCalculateHash(bool bDontReplace)
        {
            return false;
        }

        public bool VerifyHashTree(bool bDeleteBadTrees)
        {
            return false;
        }

        public void UntrustedHashReceived(AICHHash Hash, uint dwFromIP)
        {
            return;
        }

        public bool IsPartDataAvailable(ulong nPartStartPos)
        {
            return false;
        }

        public AICHStatusEnum Status
        {
            get { return status_; }
            set { status_ = value; }
        }

        public KnownFile Owner
        {
            get { return owner_; }
            set { owner_ = value; }
        }

        public void FreeHashSet()
        {
        }

        public void SetFileSize(ulong nSize)
        {
        }

        public AICHHash GetMasterHash()
        {
            return hashTree_.Hash;
        }

        public void SetMasterHash(AICHHash hash, AICHStatusEnum newStatus)
        {
        }

        public bool HasValidMasterHash
        {
            get { return hashTree_.HashValid; }
        }

        public bool SaveHashSet()
        {
            if (Status != AICHStatusEnum.AICH_HASHSETCOMPLETE)
            {

                return false;
            }
            if (!HashTree.HashValid || HashTree.DataSize != Owner.FileSize)
            {

                return false;
            }

            if (!AICHHashSetStatics.Known2File.WaitOne(5000, true))
                return false;

            string fullpath =
                MuleEngine.CoreObjectManager.Preference.GetMuleDirectory(DefaultDirectoryEnum.EMULE_CONFIGDIR);
            fullpath += CoreConstants.KNOWN2_MET_FILENAME;

            SafeFile file =
                MuleEngine.CoreObjectManager.OpenSafeFile(fullpath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

            if (file == null)
            {
                //TODO: log here
                return false;
            }
            try
            {
                //setvbuf(file.Stream, NULL, _IOFBF, 16384);
                byte header = file.ReadUInt8();
                if (header != CoreConstants.KNOWN2_MET_VERSION)
                {
                    throw new ApplicationException("end of file:" + fullpath);
                }
                // first we check if the hashset we want to write is already stored
                AICHHash CurrentHash = MuleEngine.CoreObjectManager.CreateAICHHash();
                uint nExistingSize = (uint)file.Length;
                uint nHashCount;
                while (file.Position < nExistingSize)
                {
                    CurrentHash.Read(file);
                    if (HashTree.Hash == CurrentHash)
                    {
                        // this hashset if already available, no need to save it again
                        return true;
                    }
                    nHashCount = file.ReadUInt32();
                    if (file.Position + nHashCount * CoreConstants.HASHSIZE > nExistingSize)
                    {
                        throw new ApplicationException("end of file:" + fullpath);
                    }
                    // skip the rest of this hashset
                    file.Seek(nHashCount * CoreConstants.HASHSIZE, SeekOrigin.Current);
                }
                // write hashset
                HashTree.Hash.Write(file);
                //use to remove the warning;
                ulong tmp_part_size =
                    (CoreConstants.PARTSIZE);
                nHashCount =
                    (uint)((CoreConstants.PARTSIZE / CoreConstants.EMBLOCKSIZE +
                        ((tmp_part_size % CoreConstants.EMBLOCKSIZE != 0) ? (ulong)1 : (ulong)0)) *
                        (HashTree.DataSize / CoreConstants.PARTSIZE));

                if (HashTree.DataSize % CoreConstants.PARTSIZE != 0)
                    nHashCount += (uint)(((ulong)HashTree.DataSize % CoreConstants.PARTSIZE) / CoreConstants.EMBLOCKSIZE + (((HashTree.DataSize % CoreConstants.PARTSIZE) % CoreConstants.EMBLOCKSIZE != 0) ? (ulong)1 : (ulong)0));
                file.WriteUInt32(nHashCount);
                if (!HashTree.WriteLowestLevelHashs(file, 0, true, true))
                {
                    // thats bad... really
                    file.SetLength(nExistingSize);
                    //TODO:Log
                    //theApp.QueueDebugLogLine(true, _T("Failed to save HashSet: WriteLowestLevelHashs() failed!"));
                    return false;
                }
                if (file.Length != nExistingSize + (nHashCount + 1) * CoreConstants.HASHSIZE + 4)
                {
                    // thats even worse
                    file.SetLength(nExistingSize);
                    //TODO:Log
                    //theApp.QueueDebugLogLine(true, _T("Failed to save HashSet: Calculated and real size of hashset differ!"));
                    return false;
                }
                //TODO:Log
                //theApp.QueueDebugLogLine(false, _T("Successfully saved eMuleAC Hashset, %u Hashs + 1 Masterhash written"), nHashCount);
                file.Flush();
                file.Close();
            }
            catch
            {
                //TODO:Log
                return false;
            }
            FreeHashSet();
            return true;
        }

        // only call directly when debugging
        public bool LoadHashSet()
        {
            if (Status != AICHStatusEnum.AICH_HASHSETCOMPLETE)
            {

                return false;
            }
            if (!HashTree.HashValid ||
                HashTree.DataSize != Owner.FileSize ||
                HashTree.DataSize == 0)
            {

                return false;
            }
            string fullpath =
                MuleEngine.CoreObjectManager.Preference.GetMuleDirectory(DefaultDirectoryEnum.EMULE_CONFIGDIR);
            fullpath += (CoreConstants.KNOWN2_MET_FILENAME);
            SafeFile file =
                MuleEngine.CoreObjectManager.OpenSafeFile(fullpath, 
                    FileMode.OpenOrCreate, 
                    FileAccess.Read, 
                    FileShare.ReadWrite);
            if (file == null)
            {
                //TODO:Log
                return false;
            }
            try
            {
                byte header = file.ReadUInt8();
                if (header != CoreConstants.KNOWN2_MET_VERSION)
                {
                    throw new ApplicationException("Invalid file header!");
                }
                AICHHash CurrentHash = 
                    MuleEngine.CoreObjectManager.CreateAICHHash();
                uint nExistingSize = (uint)file.Length;
                uint nHashCount;
                while (file.Position < nExistingSize)
                {
                    CurrentHash.Read(file);
                    if (HashTree.Hash == CurrentHash)
                    {
                        // found Hashset
                        ulong tmp_part_size = CoreConstants.PARTSIZE; //for remove warning
                        uint nExpectedCount =
                            (uint)((CoreConstants.PARTSIZE / CoreConstants.EMBLOCKSIZE + ((tmp_part_size % CoreConstants.EMBLOCKSIZE != 0) ? (ulong)1 : (ulong)0)) * (HashTree.DataSize / CoreConstants.PARTSIZE));
                        if (HashTree.DataSize % CoreConstants.PARTSIZE != 0)
                            nExpectedCount += (uint)((HashTree.DataSize % CoreConstants.PARTSIZE) / CoreConstants.EMBLOCKSIZE + (((HashTree.DataSize % CoreConstants.PARTSIZE) % CoreConstants.EMBLOCKSIZE != 0) ? (ulong)1 : (ulong)0));
                        nHashCount = file.ReadUInt32();
                        if (nHashCount != nExpectedCount)
                        {
                            //TODO:Log
                            //theApp.QueueDebugLogLine(true, _T("Failed to load HashSet: Available Hashs and expected hashcount differ!"));
                            return false;
                        }
                        //uint dbgPos = file.Position;
                        if (!HashTree.LoadLowestLevelHashs(file))
                        {
                            //TODO:Log
                            //theApp.QueueDebugLogLine(true, _T("Failed to load HashSet: LoadLowestLevelHashs failed!"));
                            return false;
                        }
                        //uint dbgHashRead = (file.Position-dbgPos)/HASHSIZE;
                        if (!ReCalculateHash(false))
                        {
                            //TODO:Log
                            //theApp.QueueDebugLogLine(true, _T("Failed to load HashSet: Calculating loaded hashs failed!"));
                            return false;
                        }
                        if (CurrentHash != HashTree.Hash)
                        {
                            //TODO:Log
                            //theApp.QueueDebugLogLine(true, _T("Failed to load HashSet: Calculated Masterhash differs from given Masterhash - hashset corrupt!"));
                            return false;
                        }
                        return true;
                    }
                    nHashCount = file.ReadUInt32();
                    if (file.Position + nHashCount * CoreConstants.HASHSIZE > nExistingSize)
                    {
                        throw new ApplicationException("End of fill," + fullpath);
                    }
                    // skip the rest of this hashset
                    file.Seek(nHashCount * CoreConstants.HASHSIZE, SeekOrigin.Current);
                }
                //TODO:Log
                //theApp.QueueDebugLogLine(true, _T("Failed to load HashSet: HashSet not found!"));
            }
            catch
            {
                //TODO: Log
            }
            return false;
        }

        public void DbgTest()
        {
        }

        public AICHHashTree HashTree
        {
            get { return hashTree_; }
        }

        public AICHHashAlgo NewHashAlgo
        {
            get { return MuleEngine.CoreObjectManager.CreateSHA(); }
        }

        #endregion
    }
}
