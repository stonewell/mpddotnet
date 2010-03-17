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


namespace Mule.AICH.Impl
{
    class AICHHashTreeImpl : AICHHashTree
    {
        #region Fields
        private AICHHashTree leftTree_;
        private AICHHashTree rightTree_;

        private AICHHash hash_ = new AICHHashImpl();
        public ulong dataSize_;		// size of data which is covered by this hash
        public ulong baseSize_;		// blocksize on which the lowest hash is based on
        public bool isLeftBranch_;	// left or right branch of the tree
        private bool hashValid_;		// the hash is valid and not empty    
        #endregion

        #region Constructors
        public AICHHashTreeImpl(ulong nDataSize, bool bLeftBranch, ulong nBaseSize)
        {
            dataSize_ = nDataSize;
            baseSize_ = nBaseSize;
            isLeftBranch_ = bLeftBranch;
            leftTree_ = null;
            rightTree_ = null;
            hashValid_ = false;
        }
        #endregion

        #region Methods
        private AICHHashTree LeftTree
        {
            get { return leftTree_; }
            set { leftTree_ = value; }
        }

        private AICHHashTree RightTree
        {
            get { return rightTree_; }
            set { rightTree_ = value; }
        }

        public bool CreatePartRecoveryData(ulong nStartPos, ulong nSize, FileDataIO fileDataOut, uint wHashIdent, bool b32BitIdent)
        {
            if (nStartPos + nSize > DataSize)
            { // sanity
                
                return false;
            }
            if (nSize > DataSize)
            { // sanity
                
                return false;
            }

            if (nStartPos == 0 && nSize == DataSize)
            {
                // this is the searched part, now write all blocks of this part
                // hashident for this level will be adjsuted by WriteLowestLevelHash
                return WriteLowestLevelHashs(fileDataOut, wHashIdent, false, b32BitIdent);
            }
            else if (DataSize <= BaseSize)
            { // sanity
                // this is already the last level, cant go deeper
                
                return false;
            }
            else
            {
                wHashIdent <<= 1;
                wHashIdent |= (IsLeftBranch) ? (uint)1 : (uint)0;

                ulong nBlocks = DataSize / BaseSize + ((DataSize % BaseSize != 0) ? (ulong)1 : (ulong)0);
                ulong nLeft = (((IsLeftBranch) ? nBlocks + 1 : nBlocks) / 2) * BaseSize;
                ulong nRight = DataSize - nLeft;
                if (leftTree_ == null || rightTree_ == null)
                {
                    
                    return false;
                }
                if (nStartPos < nLeft)
                {
                    if (nStartPos + nSize > nLeft || !rightTree_.HashValid)
                    { // sanity
                        
                        return false;
                    }
                    rightTree_.WriteHash(fileDataOut, wHashIdent, b32BitIdent);
                    return leftTree_.CreatePartRecoveryData(nStartPos, nSize, fileDataOut, wHashIdent, b32BitIdent);
                }
                else
                {
                    nStartPos -= nLeft;
                    if (nStartPos + nSize > nRight || !leftTree_.HashValid)
                    { // sanity
                        
                        return false;
                    }
                    leftTree_.WriteHash(fileDataOut, wHashIdent, b32BitIdent);
                    return rightTree_.CreatePartRecoveryData(nStartPos, nSize, fileDataOut, wHashIdent, b32BitIdent);

                }
            }
        }

        public void WriteHash(FileDataIO fileDataOut, uint wHashIdent, bool b32BitIdent)
        {
            wHashIdent <<= 1;
            wHashIdent |= (IsLeftBranch) ? (uint)1 : (uint)0;
            
            if (!b32BitIdent)
            {
                fileDataOut.WriteUInt16((ushort)wHashIdent);
            }
            else
                fileDataOut.WriteUInt32(wHashIdent);
            
            Hash.Write(fileDataOut);
        }

        public virtual bool WriteLowestLevelHashs(FileDataIO fileDataOut, uint wHashIdent, bool bNoIdent, bool b32BitIdent)
        {
            wHashIdent <<= 1;
            wHashIdent |= (IsLeftBranch) ? (uint)1 : (uint)0;
            if (LeftTree == null && RightTree == null)
            {
                if (DataSize <= BaseSize && HashValid)
                {
                    if (!bNoIdent && !b32BitIdent)
                    {
                        fileDataOut.WriteUInt16((ushort)wHashIdent);
                    }
                    else if (!bNoIdent && b32BitIdent)
                        fileDataOut.WriteUInt32(wHashIdent);
                    Hash.Write(fileDataOut);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (LeftTree == null || RightTree == null)
            {
                return false;
            }
            else
            {
                return LeftTree.WriteLowestLevelHashs(fileDataOut, wHashIdent, bNoIdent, b32BitIdent)
                        && RightTree.WriteLowestLevelHashs(fileDataOut, wHashIdent, bNoIdent, b32BitIdent);
            }
        }

        public virtual bool LoadLowestLevelHashs(FileDataIO fileInput)
        {
            if (DataSize <= BaseSize)
            { // sanity
                // lowest level, read hash
                Hash.Read(fileInput);
                //theApp.AddDebugLogLine(false,Hash.GetString());
                HashValid = true;
                return true;
            }
            else
            {
                ulong nBlocks = DataSize / BaseSize + ((DataSize % BaseSize != 0) ? (ulong)1 : (ulong)0);
                ulong nLeft = (((IsLeftBranch) ? nBlocks + 1 : nBlocks) / 2) * BaseSize;
                ulong nRight = DataSize - nLeft;
                if (LeftTree == null)
                    LeftTree = AICHObjectManager.CreateAICHHashTree(nLeft, true, (nLeft <= MuleConstants.PARTSIZE) ? MuleConstants.EMBLOCKSIZE : MuleConstants.PARTSIZE);
                if (RightTree == null)
                    RightTree = AICHObjectManager.CreateAICHHashTree(nRight, false, (nRight <= MuleConstants.PARTSIZE) ? MuleConstants.EMBLOCKSIZE : MuleConstants.PARTSIZE);
                return LeftTree.LoadLowestLevelHashs(fileInput)
                        && RightTree.LoadLowestLevelHashs(fileInput);
            }
        }

        public bool SetHash(FileDataIO fileInput, uint wHashIdent, sbyte nLevel)
        {
            return SetHash(fileInput, wHashIdent, nLevel, true);
        }

        public bool SetHash(FileDataIO fileInput, uint wHashIdent)
        {
            return SetHash(fileInput, wHashIdent, -1);
        }

        public bool SetHash(FileDataIO fileInput, uint wHashIdent, sbyte nLevel, bool bAllowOverwrite)
        {
            if (nLevel == -1)
            {
                // first call, check how many level we need to go
                byte i;
                for (i = 0; i != 32 && (wHashIdent & 0x80000000) == 0; i++)
                {
                    wHashIdent <<= 1;
                }

                if (i > 31)
                {
                    //TODO:Log
                    //theApp.QueueDebugLogLine(/*DLP_HIGH,*/ false, _T("CAICHHashTree::SetHash - found invalid HashIdent (0)"));
                    return false;
                }
                else
                {
                    nLevel = (sbyte)(31 - i);
                }
            }
            if (nLevel == 0)
            {
                // this is the searched hash
                if (HashValid && !bAllowOverwrite)
                {
                    // not allowed to overwrite this hash, however move the filepointer by reading a hash
                    AICHHash hash = AICHObjectManager.CreateAICHHash(fileInput);

                    return true;
                }
                
                Hash.Read(fileInput);
                HashValid = true;
                return true;
            }
            else if (DataSize <= BaseSize)
            { // sanity
                // this is already the last level, cant go deeper
                
                return false;
            }
            else
            {
                // adjust ident to point the path to the next node
                wHashIdent <<= 1;
                nLevel--;
                ulong nBlocks = DataSize / BaseSize + ((DataSize % BaseSize != 0) ? (ulong)1 : (ulong)0);
                ulong nLeft = (((IsLeftBranch) ? nBlocks + 1 : nBlocks) / 2) * BaseSize;
                ulong nRight = DataSize - nLeft;
                if ((wHashIdent & 0x80000000) > 0)
                {
                    if (LeftTree == null)
                        LeftTree = AICHObjectManager.CreateAICHHashTree(nLeft, true, (nLeft <= MuleConstants.PARTSIZE) ? MuleConstants.EMBLOCKSIZE : MuleConstants.PARTSIZE);
                    return LeftTree.SetHash(fileInput, wHashIdent, nLevel);
                }
                else
                {
                    if (RightTree == null)
                        RightTree = AICHObjectManager.CreateAICHHashTree(nRight, false, (nRight <= MuleConstants.PARTSIZE) ? MuleConstants.EMBLOCKSIZE : MuleConstants.PARTSIZE);
                    return RightTree.SetHash(fileInput, wHashIdent, nLevel);
                }
            }
        }
        #endregion

        #region AICHHashTree Members

        public AICHHash Hash
        {
            get { return hash_; }
        }

        public bool HashValid
        {
            get { return hashValid_; }
            set { hashValid_ = value; }
        }

        public ulong DataSize
        {
            get { return dataSize_; }
        }

        public ulong BaseSize
        {
            get { return baseSize_; }
        }

        public bool IsLeftBranch
        {
            get { return isLeftBranch_; }
        }

        public void SetBlockHash(ulong nSize, ulong nStartPos, AICHHashAlgorithm pHashAlg)
        {
            byte nLevel = 0;

            AICHHashTree pToInsert = FindHash(nStartPos, nSize, ref nLevel);
            if (pToInsert == null)
            { // sanity
                //TODO:Log
                //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Critical Error: Failed to Insert SHA-HashBlock, FindHash() failed!"));
                return;
            }

            //sanity
            if (pToInsert.BaseSize != MuleConstants.EMBLOCKSIZE || pToInsert.DataSize != nSize)
            {
                //TODO:Log
                //theApp.QueueDebugLogLine(/*DLP_VERYHIGH,*/ false, _T("Critical Error: Logical error on values in SetBlockHashFromData"));
                return;
            }

            pHashAlg.Finish(pToInsert.Hash);
            pToInsert.HashValid = true;
        }

        public bool ReCalculateHash(AICHHashAlgorithm hashalg, bool bDontReplace)
        {
            if (leftTree_ != null && rightTree_ != null)
            {
                if (!leftTree_.ReCalculateHash(hashalg, bDontReplace) || 
                    !rightTree_.ReCalculateHash(hashalg, bDontReplace))
                    return false;
                if (bDontReplace && HashValid)
                    return true;
                if (rightTree_.HashValid && leftTree_.HashValid)
                {
                    hashalg.Reset();
                    hashalg.Add(leftTree_.Hash.RawHash);
                    hashalg.Add(rightTree_.Hash.RawHash);
                    hashalg.Finish(Hash);
                    hashValid_ = true;
                    return true;
                }
                else
                    return HashValid;
            }
            else
                return true;
        }

        public bool VerifyHashTree(AICHHashAlgorithm hashalg, bool bDeleteBadTrees)
        {
            if (!HashValid)
            {
                if (bDeleteBadTrees)
                {
                    leftTree_ = null;
                    rightTree_ = null;
                }
                //TODO: Log
                //theApp.QueueDebugLogLine(/*DLP_HIGH,*/ false, _T("VerifyHashTree - No masterhash available"));
                return false;
            }

            // calculated missing hashs without overwriting anything
            if (leftTree_ != null && !leftTree_.HashValid)
                leftTree_.ReCalculateHash(hashalg, true);
            if (rightTree_  != null && !rightTree_.HashValid)
                rightTree_.ReCalculateHash(hashalg, true);

            if ((rightTree_  != null && rightTree_.HashValid) ^ (leftTree_  != null && leftTree_.HashValid))
            {
                // one branch can never be verified
                if (bDeleteBadTrees)
                {
                    leftTree_ = null;
                    rightTree_ = null;
                }
                //TODO: Log
                //theApp.QueueDebugLogLine(/*DLP_HIGH,*/ false, _T("VerifyHashSet failed - Hashtree incomplete"));
                return false;
            }
            if ((rightTree_  != null && rightTree_.HashValid) && (leftTree_  != null && leftTree_.HashValid))
            {
                // check verify the hashs of both child nodes against my hash 

                AICHHash CmpHash = AICHObjectManager.CreateAICHHash();
                hashalg.Reset();
                hashalg.Add(leftTree_.Hash.RawHash);
                hashalg.Add(rightTree_.Hash.RawHash);
                hashalg.Finish(CmpHash);

                if (!Hash.Equals(CmpHash))
                {
                    if (bDeleteBadTrees)
                    {
                        leftTree_ = null;
                        rightTree_ = null;
                    }

                    return false;
                }
                return leftTree_.VerifyHashTree(hashalg, bDeleteBadTrees) && rightTree_.VerifyHashTree(hashalg, bDeleteBadTrees);
            }
            else
                // last hash in branch - nothing below to verify
                return true;
        }

        public AICHHashTree FindHash(ulong nStartPos, ulong nSize)
        {
            byte buffer = 0; 
            
            return FindHash(nStartPos, nSize, ref buffer);
        }

        public AICHHashTree FindHash(ulong nStartPos, ulong nSize, ref byte nLevel)
        {
            nLevel++;
            if (nLevel > 22)
            { // sanity
                return null;
            }
            if (nStartPos + nSize > dataSize_)
            { // sanity
                return null;
            }
            if (nSize > dataSize_)
            { // sanity

                return null;
            }

            if (nStartPos == 0 && nSize == dataSize_)
            {
                // this is the searched hash
                return this;
            }
            else if (dataSize_ <= baseSize_)
            { // sanity
                // this is already the last level, cant go deeper

                return null;
            }
            else
            {
                ulong nBlocks = dataSize_ / baseSize_ + ((dataSize_ % baseSize_ != 0) ? (ulong)1 : (ulong)0);
                ulong nLeft = (((isLeftBranch_) ? nBlocks + 1 : nBlocks) / 2) * baseSize_;
                ulong nRight = dataSize_ - nLeft;
                if (nStartPos < nLeft)
                {
                    if (nStartPos + nSize > nLeft)
                    { // sanity

                        return null;
                    }
                    if (leftTree_ == null)
                        leftTree_ = AICHObjectManager.CreateAICHHashTree(nLeft, true, (nLeft <= MuleConstants.PARTSIZE) ? MuleConstants.EMBLOCKSIZE : MuleConstants.PARTSIZE);

                    return leftTree_.FindHash(nStartPos, nSize, ref nLevel);
                }
                else
                {
                    nStartPos -= nLeft;
                    if (nStartPos + nSize > nRight)
                    { // sanity

                        return null;
                    }
                    if (rightTree_ == null)
                        rightTree_ = AICHObjectManager.CreateAICHHashTree(nRight, false, (nRight <= MuleConstants.PARTSIZE) ? MuleConstants.EMBLOCKSIZE : MuleConstants.PARTSIZE);

                    return leftTree_.FindHash(nStartPos, nSize, ref nLevel);
                }
            }
        }
        #endregion
    }
}
