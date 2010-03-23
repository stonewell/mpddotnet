using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core.Impl
{
    class MuleCollectionImpl : MuleCollection
    {
        #region MuleCollection Members

        public bool InitCollectionFromFile(string sFilePath, string sFileName)
        {
            throw new NotImplementedException();
        }

        public Mule.File.CollectionFile AddFileToCollection(Mule.File.AbstractFile pAbstractFile, bool bCreateClone)
        {
            throw new NotImplementedException();
        }

        public void RemoveFileFromCollection(Mule.File.AbstractFile pAbstractFile)
        {
            throw new NotImplementedException();
        }

        public void WriteToFileAddShared()
        {
            throw new NotImplementedException();
        }

        public void WriteToFileAddShared(CryptoPP.RSASSA_PKCS1v15_SHA_Signer pSignkey)
        {
            throw new NotImplementedException();
        }

        public void SetCollectionAuthorKey(byte[] abyCollectionAuthorKey, uint nSize)
        {
            throw new NotImplementedException();
        }

        public string GetCollectionAuthorKeyString()
        {
            throw new NotImplementedException();
        }

        public string GetAuthorKeyHashString()
        {
            throw new NotImplementedException();
        }

        public string CollectionName
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string CollectionAuthorName
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool TextFormat
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
