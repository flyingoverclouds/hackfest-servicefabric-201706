using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace com.mega.queuecontract
{
    [DataContract]
    public class QueueMessage
    {
        [DataMember]
        public string Language { get; set; }

        [DataMember]
        public DateTime CreatedDateTime { get; set; }
    }
}
