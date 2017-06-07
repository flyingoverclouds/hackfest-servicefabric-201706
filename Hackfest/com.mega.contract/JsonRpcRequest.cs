using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mega.contract
{
    public class JsonRpcRequest
    {
        public string EnvironmentId { get; set; }
        public string RepositoryId { get; set; }
        public string ProfileId { get; set; }
        public string LanguageId { get; set; }
        public string DesktopId { get; set; }
        public string UserId { get; set; }
        public string Service { get; set; }
        public string Parameters { get; set; }
    }
}
