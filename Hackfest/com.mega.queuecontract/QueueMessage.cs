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
        public Guid MessageId { get; set; }

        [DataMember]
        public string SessionType { get; set; }

        [DataMember]
        public string UserName { get; set; }

        [DataMember]
        public DateTime CreatedDateTime { get; set; }

        public QueueMessage(string sessionType, string username)
        {
            this.SessionType = sessionType;
            this.UserName = username;
        }
    }
}
