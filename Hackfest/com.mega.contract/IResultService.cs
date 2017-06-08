using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mega.contract
{
  public interface IResultService : IService
  {
    Task<string> Get(Guid key);

    Task Set(Guid key, string value);
  }
}
