using System;
using System.Threading.Tasks;
using Grpc.Core;
using Mega;
using System.Collections.Concurrent;
using System.Timers;

namespace com.mega.sproexe
{
  
  class Program
  {  

    static void Main(string[] args)
    {
      int port;
            if (!int.TryParse(Environment.GetEnvironmentVariable("Fabric_Endpoint_com.mega.SproGuestExeTypeEndpoint"), out port))
            {
                if (port != 0)
                {

                    var nativeSessionImpl = new NativeSessionImpl();
                    Server server = new Server
                    {
                        Services = { NativeSession.BindService(nativeSessionImpl) },
                        Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
                    };
                    server.Start();

                    nativeSessionImpl.EmptiedSpro += (object sender, EventArgs e) =>
                    {
                        Console.WriteLine("SPRO is empty, initiating shutdown");
                        server.ShutdownAsync().ContinueWith(t =>
                          {
                              Console.WriteLine("Shutdown complete");
                              Environment.Exit(0);
                          });
                    };

                    Console.WriteLine("SPRO NativeSession server listening on port " + port);
                    Console.WriteLine("Press any key to stop the server...");
                    Console.ReadKey();
                    server.ShutdownAsync().Wait();
                }
            }
            else
            {
                Console.WriteLine("SPRO NativeSession server has not been instatiated because no port has been set");
                Console.WriteLine("Press any key to stop the server...");
                Console.ReadKey();
            }
    }
  }

  class NativeSessionImpl : NativeSession.NativeSessionBase
  {
    public NativeSessionImpl()
    {
      _initTimeout.Elapsed += (object sender, ElapsedEventArgs e) =>
      {
        //EmptiedSpro.Invoke(this, null);
      };
      _initTimeout.Start();
    }

    public override Task<NewSessionReply> OpenSession(NewSessionRequest request, ServerCallContext context)
    {
      Console.WriteLine($"SPRO OpenSession for {request.Username} / {request.Type}");
      string idSession = "";
      if (request.Type != null && (_sessionsType == null || _sessionsType == request.Type))
      {
        _sessionsType = request.Type;
        var userTimer = new Timer(Config.TIMEOUT);
        lock (_lock)
        {
          if (_usersInSession.Count < Config.MAX_USER_COUNT &&
            _usersInSession.TryAdd(request.Username, userTimer))
          {
            idSession = Guid.NewGuid().ToString();

            userTimer.Start();
            userTimer.Elapsed += (object sender, ElapsedEventArgs e) =>
            {
              //RemoveUser(request.Username);
            };

            _initTimeout.Stop();
            Console.WriteLine($"SPRO OpenSession for {request.Username} / {request.Type} GRANTED {idSession}");
          }
        }
      }
      return Task.FromResult(new NewSessionReply { Idsession = idSession });
    }

    public override Task<GenerateReply> Generate(GenerateRequest request, ServerCallContext context)
    {
      Console.WriteLine($"SPRO Generate for {request.Username} / {request.Type}");
      string response = "";
      Timer userTimer;
      if (_sessionsType != null && _sessionsType == request.Type
        && _usersInSession.TryGetValue(request.Username, out userTimer) )
      {
        userTimer.Stop();
        response = request.Payload;
        userTimer.Start();
        Console.WriteLine($"SPRO Generate for {request.Username} / {request.Type} GRANTED");
      }
      return Task.FromResult(new GenerateReply { Response = response });
    }

    public override Task<CloseSessionReply> CloseSession(CloseSessionRequest request, ServerCallContext context)
    {
      Console.WriteLine($"SPRO CloseSession for {request.Username} / {request.Type}");
      string ok = "nok";
      if (_sessionsType != null && _sessionsType == request.Type
        && RemoveUser(request.Username))
      {
        ok = "ok";
        Console.WriteLine($"SPRO CloseSession for {request.Username} / {request.Type} GRANTED");

      }
      return Task.FromResult(new CloseSessionReply { Ok = ok });
    }

    public delegate void EmptySproHandler(object sender, EventArgs e);

    private string _sessionsType = null;
    private ConcurrentDictionary<string, Timer> _usersInSession = new ConcurrentDictionary<string, Timer>();
    private object _lock = new object();
    private Timer _initTimeout = new Timer(Config.TIMEOUT);

    private bool RemoveUser(string userName)
    {
      Console.WriteLine($"SPRO Removing user {userName}");

      Timer userTimer;
      var success = _usersInSession.TryRemove(userName, out userTimer);
      if (success) {
        userTimer.Stop();
        lock (_lock)
        {
          if (_usersInSession.Count == 0)
          {
            //EmptiedSpro.Invoke(this, null);
          }
        }
        Console.WriteLine($"SPRO Removing user {userName} GRANTED");
      }
      return success;
    }

    class Config
    {
      public const int TIMEOUT = 10000;
      public const int MAX_USER_COUNT = 20;
    }

  }


}
