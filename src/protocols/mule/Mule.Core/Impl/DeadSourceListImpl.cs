using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core.Impl
{
    class DeadSourceListImpl : DeadSourceList
    {
        #region DeadSourceList Members

        public void AddDeadSource(UpDownClient pToAdd)
        {
            throw new NotImplementedException();
        }

        public void RemoveDeadSource(UpDownClient client)
        {
            throw new NotImplementedException();
        }

        public bool IsDeadSource(UpDownClient pToCheck)
        {
            throw new NotImplementedException();
        }

        public uint DeadSourcesCount
        {
            get { throw new NotImplementedException(); }
        }

        public void Init(bool bGlobalList)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
