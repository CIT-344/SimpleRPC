using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace RPCServer
{
    public class CommunicationModel
    {
        public String EventName { get; set; }
        public DateTime Sent { get; set; }
        public Object Body { get; set; }
        public String ClientId { get; set; }
    }
}
