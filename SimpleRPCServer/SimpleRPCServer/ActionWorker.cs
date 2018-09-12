using System;
using System.Collections.Generic;
using System.Text;

namespace RPCServer
{
    public class ActionWorker
    {
        public Action<object> Worker { get; set; }
        public Type WorkerType { get; set; }
    }
}
