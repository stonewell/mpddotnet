using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core
{
    public interface DeadSourceList
    {
        void AddDeadSource(UpDownClient pToAdd);
        void RemoveDeadSource(UpDownClient client);
        bool IsDeadSource(UpDownClient pToCheck);
        uint DeadSourcesCount { get; }
        void Init(bool bGlobalList);
    }
}
