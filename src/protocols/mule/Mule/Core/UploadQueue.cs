using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mule.Core
{
    public interface UploadQueue
    {
        void AddClientToQueue(UpDownClient client_);

        void RemoveFromUploadQueue(UpDownClient client_, string p);
    }
}
