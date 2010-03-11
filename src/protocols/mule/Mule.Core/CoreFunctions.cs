//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Mule.AICH;
//using Kademlia;
//using Mpd.Generic.Types.IO;
//using Mule.File;

//namespace Mule.Core
//{
//    public static class CoreFunctions
//    {
//        public static void PreparePacketForTags(this TagIO pbyPacket, KnownFile pFile)
//        {
//        }

//        public static void StoreToFile(this KadUDPKey kadUDPKey, FileDataIO file)
//        {
//        }

//        public static void ReadFromFile(this KadUDPKey kadUDPKey, FileDataIO file)
//        {
//        }

//        public static void ClientAICHRequestFailed(this AICHHashSetStatics statics, UpDownClient pClient)
//        {
//        }

//        public static void RemoveClientAICHRequest(this AICHHashSetStatics statics, UpDownClient pClient)
//        {
//        }

//        public static bool IsClientRequestPending(this AICHHashSetStatics statics, PartFile pForFile, ushort nPart)
//        {
//            return false;
//        }

//        public static AICHRequestedData GetAICHReqDetails(this AICHHashSetStatics statics, UpDownClient pClient)
//        {
//            return null;
//        }

//        public static bool CreatePartRecoveryData(this AICHHashSet aichHashSet, KnownFile owner, ulong nPartStartPos, FileDataIO fileDataOut, bool bDbgDontLoad)
//        {
//            if (owner.IsPartFile || aichHashSet.Status != AICHStatusEnum.AICH_HASHSETCOMPLETE)
//            {
//                return false;
//            }
//            if (aichHashSet.HashTree.DataSize <= MuleConstants.EMBLOCKSIZE)
//            {
//                return false;
//            }
//            if (!bDbgDontLoad)
//            {
//                if (!aichHashSet.LoadHashSet())
//                {
//                    //TODO:Log
//                    //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Created RecoveryData error: failed to load hashset (file: %s)"), owner_.GetFileName());
//                    aichHashSet.Status = AICHStatusEnum.AICH_ERROR;
//                    return false;
//                }
//            }
//            bool bResult;
//            byte nLevel = 0;
//            uint nPartSize = (uint)Math.Min(MuleConstants.PARTSIZE, (ulong)owner.FileSize - nPartStartPos);
//            aichHashSet.HashTree.FindHash(nPartStartPos, nPartSize, ref nLevel);
//            ushort nHashsToWrite = (ushort)(((ulong)nLevel - 1) + (ulong)nPartSize / MuleConstants.EMBLOCKSIZE + (((ulong)nPartSize % MuleConstants.EMBLOCKSIZE != 0) ? (ulong)1 : (ulong)0));
//            bool bUse32BitIdentifier = owner.IsLargeFile;

//            if (bUse32BitIdentifier)
//                fileDataOut.WriteUInt16(0); // no 16bit hashs to write
//            fileDataOut.WriteUInt16(nHashsToWrite);
//            ulong nCheckFilePos = (ulong)fileDataOut.Position;
//            if (aichHashSet.HashTree.CreatePartRecoveryData(nPartStartPos, nPartSize, fileDataOut, 0, bUse32BitIdentifier))
//            {
//                if ((ulong)nHashsToWrite *
//                        (MuleConstants.HASHSIZE +
//                            (bUse32BitIdentifier ? (ulong)4 : (ulong)2)) != (ulong)fileDataOut.Position - nCheckFilePos)
//                {
//                    //TODO:Log
//                    //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Created RecoveryData has wrong length (file: %s)"), owner_.GetFileName());
//                    bResult = false;
//                    aichHashSet.Status = AICHStatusEnum.AICH_ERROR;
//                }
//                else
//                    bResult = true;
//            }
//            else
//            {
//                //TODO:Log
//                //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Failed to create RecoveryData for %s"), owner_.GetFileName());
//                bResult = false;
//                aichHashSet.Status = AICHStatusEnum.AICH_ERROR;
//            }
//            if (!bUse32BitIdentifier)
//                fileDataOut.WriteUInt16(0); // no 32bit hashs to write

//            if (!bDbgDontLoad)
//            {
//                aichHashSet.FreeHashSet();
//            }
//            return bResult;
//        }

//        public static bool ReadRecoveryData(this AICHHashSet aichHashSet, KnownFile owner, ulong nPartStartPos, SafeMemFile fileDataIn)
//        {
//            if (/*TODO !Owner.IsPartFile() ||*/
//                !(aichHashSet.Status == AICHStatusEnum.AICH_VERIFIED ||
//                aichHashSet.Status == AICHStatusEnum.AICH_TRUSTED))
//            {

//                return false;
//            }
//            /* V2 AICH Hash Packet:
//                <count1 ushort>											16bit-hashs-to-read
//                (<identifier ushort><hash MuleConstants.HASHSIZE>)[count1]			AICH hashs
//                <count2 ushort>											32bit-hashs-to-read
//                (<identifier uint><hash MuleConstants.HASHSIZE>)[count2]			AICH hashs
//            */

//            // at this time we check the recoverydata for the correct ammounts of hashs only
//            // all hash are then taken into the tree, depending on there hashidentifier (except the masterhash)

//            byte nLevel = 0;
//            uint nPartSize = (uint)Math.Min(MuleConstants.PARTSIZE, (ulong)owner.FileSize - nPartStartPos);
//            aichHashSet.HashTree.FindHash(nPartStartPos, nPartSize, ref nLevel);
//            ushort nHashsToRead =
//                (ushort)(((ulong)nLevel - 1) + (ulong)nPartSize / MuleConstants.EMBLOCKSIZE +
//                    (((ulong)nPartSize % MuleConstants.EMBLOCKSIZE != 0) ? (ulong)1 : (ulong)0));

//            // read hashs with 16 bit identifier
//            ushort nHashsAvailable = fileDataIn.ReadUInt16();
//            if ((ulong)fileDataIn.Length - (ulong)fileDataIn.Position <
//                (ulong)nHashsToRead * (MuleConstants.HASHSIZE + 2) ||
//                (nHashsToRead != nHashsAvailable && nHashsAvailable != 0))
//            {
//                // this check is redunant, CSafememfile would catch such an error too
//                // TODO:Log
//                //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Failed to read RecoveryData for %s - Received datasize/amounts of hashs was invalid (1)"), Owner.GetFileName());
//                return false;
//            }
//            //TODO:Log
//            //DEBUG_ONLY(theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("read RecoveryData for %s - Received packet with  %u 16bit hash identifiers)"), Owner.GetFileName(), nHashsAvailable));
//            for (uint i = 0; i != nHashsAvailable; i++)
//            {
//                ushort wHashIdent = fileDataIn.ReadUInt16();
//                if (wHashIdent == 1 /*never allow masterhash to be overwritten*/
//                    || !aichHashSet.HashTree.SetHash(fileDataIn, wHashIdent, (-1), false))
//                {
//                    //TODO:Log
//                    //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Failed to read RecoveryData for %s - Error when trying to read hash into tree (1)"), Owner.GetFileName());
//                    // remove invalid hashs which we have already written
//                    aichHashSet.VerifyHashTree(true);
//                    return false;
//                }
//            }

//            // read hashs with 32bit identifier
//            if (nHashsAvailable == 0 &&
//                fileDataIn.Length - fileDataIn.Position >= 2)
//            {
//                nHashsAvailable = fileDataIn.ReadUInt16();
//                if ((ulong)fileDataIn.Length - (ulong)fileDataIn.Position <
//                    (ulong)nHashsToRead * (MuleConstants.HASHSIZE + 4) ||
//                    (nHashsToRead != nHashsAvailable && nHashsAvailable != 0))
//                {
//                    // this check is redunant, CSafememfile would catch such an error too
//                    //TODO:LOg
//                    //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Failed to read RecoveryData for %s - Received datasize/amounts of hashs was invalid (2)"), Owner.GetFileName());
//                    return false;
//                }
//                //TODO:LOg
//                //DEBUG_ONLY(theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("read RecoveryData for %s - Received packet with  %u 32bit hash identifiers)"), Owner.GetFileName(), nHashsAvailable));
//                for (uint i = 0; i != nHashsToRead; i++)
//                {
//                    uint wHashIdent = fileDataIn.ReadUInt32();
//                    /*never allow masterhash to be overwritten*/
//                    if (wHashIdent == 1
//                        || wHashIdent > 0x400000
//                        || !aichHashSet.HashTree.SetHash(fileDataIn, wHashIdent, (-1), false))
//                    {
//                        //TODO:LOg
//                        //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Failed to read RecoveryData for %s - Error when trying to read hash into tree (2)"), Owner.GetFileName());
//                        // remove invalid hashs which we have already written
//                        aichHashSet.VerifyHashTree(true);
//                        return false;
//                    }
//                }
//            }

//            if (nHashsAvailable == 0)
//            {
//                //TODO:LOg
//                //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Failed to read RecoveryData for %s - Packet didn't contained any hashs"), Owner.GetFileName());
//                return false;
//            }


//            if (aichHashSet.VerifyHashTree(true))
//            {
//                // some final check if all hashs we wanted are there
//                for (ulong nPartPos = 0; nPartPos < nPartSize; nPartPos += MuleConstants.EMBLOCKSIZE)
//                {
//                    AICHHashTree phtToCheck = aichHashSet.HashTree.FindHash(nPartStartPos + nPartPos, Math.Min(MuleConstants.EMBLOCKSIZE, nPartSize - nPartPos));
//                    if (phtToCheck == null || !phtToCheck.HashValid)
//                    {
//                        //TODO:LOg
//                        //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Failed to read RecoveryData for %s - Error while verifying presence of all lowest level hashs"), Owner.GetFileName());
//                        return false;
//                    }
//                }
//                // all done
//                return true;
//            }
//            else
//            {
//                //TODO:LOg
//                //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Failed to read RecoveryData for %s - Verifying received hashtree failed"), Owner.GetFileName());
//                return false;
//            }
//        }

//        public static bool SaveHashSet(this KnownFile knownFile)
//        {
//            if (Status != AICHStatusEnum.AICH_HASHSETCOMPLETE)
//            {

//                return false;
//            }

//            if (!HashTree.HashValid || HashTree.DataSize != Owner.FileSize)
//            {

//                return false;
//            }

//            if (!AICHHashSetStatics.Known2File.WaitOne(5000, true))
//                return false;

//            string fullpath =
//                MuleEngine.CoreObjectManager.Preference.GetMuleDirectory(DefaultDirectoryEnum.EMULE_CONFIGDIR);
//            fullpath += MuleConstants.KNOWN2_MET_FILENAME;

//            SafeFile file =
//                MuleEngine.CoreObjectManager.OpenSafeFile(fullpath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

//            if (file == null)
//            {
//                //TODO: log here
//                return false;
//            }
//            try
//            {
//                //setvbuf(file.Stream, NULL, _IOFBF, 16384);
//                byte header = file.ReadUInt8();
//                if (header != MuleConstants.KNOWN2_MET_VERSION)
//                {
//                    throw new ApplicationException("end of file:" + fullpath);
//                }
//                // first we check if the hashset we want to write is already stored
//                AICHHash CurrentHash = AICHObjectManager.CreateAICHHash();
//                uint nExistingSize = (uint)file.Length;
//                uint nHashCount;
//                while (file.Position < nExistingSize)
//                {
//                    CurrentHash.Read(file);
//                    if (HashTree.Hash == CurrentHash)
//                    {
//                        // this hashset if already available, no need to save it again
//                        return true;
//                    }
//                    nHashCount = file.ReadUInt32();
//                    if (file.Position + nHashCount * MuleConstants.HASHSIZE > nExistingSize)
//                    {
//                        throw new ApplicationException("end of file:" + fullpath);
//                    }
//                    // skip the rest of this hashset
//                    file.Seek(nHashCount * MuleConstants.HASHSIZE, SeekOrigin.Current);
//                }
//                // write hashset
//                HashTree.Hash.Write(file);
//                //use to remove the warning;
//                ulong tmp_part_size =
//                    (MuleConstants.PARTSIZE);
//                nHashCount =
//                    (uint)((MuleConstants.PARTSIZE / MuleConstants.EMBLOCKSIZE +
//                        ((tmp_part_size % MuleConstants.EMBLOCKSIZE != 0) ? (ulong)1 : (ulong)0)) *
//                        (HashTree.DataSize / MuleConstants.PARTSIZE));

//                if (HashTree.DataSize % MuleConstants.PARTSIZE != 0)
//                    nHashCount += (uint)(((ulong)HashTree.DataSize % MuleConstants.PARTSIZE) / MuleConstants.EMBLOCKSIZE + (((HashTree.DataSize % MuleConstants.PARTSIZE) % MuleConstants.EMBLOCKSIZE != 0) ? (ulong)1 : (ulong)0));
//                file.WriteUInt32(nHashCount);
//                if (!HashTree.WriteLowestLevelHashs(file, 0, true, true))
//                {
//                    // thats bad... really
//                    file.SetLength(nExistingSize);
//                    //TODO:Log
//                    //theApp.QueueDebugLogLine(true, _T("Failed to save HashSet: WriteLowestLevelHashs() failed!"));
//                    return false;
//                }
//                if (file.Length != nExistingSize + (nHashCount + 1) * MuleConstants.HASHSIZE + 4)
//                {
//                    // thats even worse
//                    file.SetLength(nExistingSize);
//                    //TODO:Log
//                    //theApp.QueueDebugLogLine(true, _T("Failed to save HashSet: Calculated and real size of hashset differ!"));
//                    return false;
//                }
//                //TODO:Log
//                //theApp.QueueDebugLogLine(false, _T("Successfully saved eMuleAC Hashset, %u Hashs + 1 Masterhash written"), nHashCount);
//                file.Flush();
//                file.Close();
//            }
//            catch
//            {
//                //TODO:Log
//                return false;
//            }
//            FreeHashSet();
//            return true;
//        }
//        // only call directly when debugging
//        public static bool LoadHashSet(this KnownFile knowFile)
//        {
//            if (Status != AICHStatusEnum.AICH_HASHSETCOMPLETE)
//            {

//                return false;
//            }
//            if (!HashTree.HashValid ||
//                HashTree.DataSize != Owner.FileSize ||
//                HashTree.DataSize == 0)
//            {

//                return false;
//            }
//            string fullpath =
//                MuleEngine.CoreObjectManager.Preference.GetMuleDirectory(DefaultDirectoryEnum.EMULE_CONFIGDIR);
//            fullpath += (MuleConstants.KNOWN2_MET_FILENAME);
//            SafeFile file =
//                MuleEngine.CoreObjectManager.OpenSafeFile(fullpath,
//                    FileMode.OpenOrCreate,
//                    FileAccess.Read,
//                    FileShare.ReadWrite);
//            if (file == null)
//            {
//                //TODO:Log
//                return false;
//            }
//            try
//            {
//                byte header = file.ReadUInt8();
//                if (header != MuleConstants.KNOWN2_MET_VERSION)
//                {
//                    throw new ApplicationException("Invalid file header!");
//                }
//                AICHHash CurrentHash =
//                    AICHObjectManager.CreateAICHHash();
//                uint nExistingSize = (uint)file.Length;
//                uint nHashCount;
//                while (file.Position < nExistingSize)
//                {
//                    CurrentHash.Read(file);
//                    if (HashTree.Hash == CurrentHash)
//                    {
//                        // found Hashset
//                        ulong tmp_part_size = MuleConstants.PARTSIZE; //for remove warning
//                        uint nExpectedCount =
//                            (uint)((MuleConstants.PARTSIZE / MuleConstants.EMBLOCKSIZE + ((tmp_part_size % MuleConstants.EMBLOCKSIZE != 0) ? (ulong)1 : (ulong)0)) * (HashTree.DataSize / MuleConstants.PARTSIZE));
//                        if (HashTree.DataSize % MuleConstants.PARTSIZE != 0)
//                            nExpectedCount += (uint)((HashTree.DataSize % MuleConstants.PARTSIZE) / MuleConstants.EMBLOCKSIZE + (((HashTree.DataSize % MuleConstants.PARTSIZE) % MuleConstants.EMBLOCKSIZE != 0) ? (ulong)1 : (ulong)0));
//                        nHashCount = file.ReadUInt32();
//                        if (nHashCount != nExpectedCount)
//                        {
//                            //TODO:Log
//                            //theApp.QueueDebugLogLine(true, _T("Failed to load HashSet: Available Hashs and expected hashcount differ!"));
//                            return false;
//                        }
//                        //uint dbgPos = file.Position;
//                        if (!HashTree.LoadLowestLevelHashs(file))
//                        {
//                            //TODO:Log
//                            //theApp.QueueDebugLogLine(true, _T("Failed to load HashSet: LoadLowestLevelHashs failed!"));
//                            return false;
//                        }
//                        //uint dbgHashRead = (file.Position-dbgPos)/HASHSIZE;
//                        if (!ReCalculateHash(false))
//                        {
//                            //TODO:Log
//                            //theApp.QueueDebugLogLine(true, _T("Failed to load HashSet: Calculating loaded hashs failed!"));
//                            return false;
//                        }
//                        if (CurrentHash != HashTree.Hash)
//                        {
//                            //TODO:Log
//                            //theApp.QueueDebugLogLine(true, _T("Failed to load HashSet: Calculated Masterhash differs from given Masterhash - hashset corrupt!"));
//                            return false;
//                        }
//                        return true;
//                    }
//                    nHashCount = file.ReadUInt32();
//                    if (file.Position + nHashCount * MuleConstants.HASHSIZE > nExistingSize)
//                    {
//                        throw new ApplicationException("End of fill," + fullpath);
//                    }
//                    // skip the rest of this hashset
//                    file.Seek(nHashCount * MuleConstants.HASHSIZE, SeekOrigin.Current);
//                }
//                //TODO:Log
//                //theApp.QueueDebugLogLine(true, _T("Failed to load HashSet: HashSet not found!"));
//            }
//            catch
//            {
//                //TODO: Log
//            }
//            return false;
//        }
//    }
//}
