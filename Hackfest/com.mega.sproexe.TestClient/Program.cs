using Grpc.Core;
using System;
using Mega;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace com.mega.sproexe.TestClient
{
  class Program
  {
    static void Main(string[] args)
    {
      Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);

      var client = new NativeSession.NativeSessionClient(channel);
      var openSessionResponse = client.OpenSession(new NewSessionRequest { Username="A", Type="en" });
      Trace.Assert(openSessionResponse.Idsession != "");
      openSessionResponse = client.OpenSession(new NewSessionRequest { Username = "B", Type = "en" });
      Trace.Assert(openSessionResponse.Idsession != "");
      openSessionResponse = client.OpenSession(new NewSessionRequest { Username = "C", Type = "fr" });
      Trace.Assert(openSessionResponse.Idsession == "");

      const string payload = "123";
      var generateResponse = client.Generate(new GenerateRequest { Username = "A", Type = "en", Payload = payload });
      Trace.Assert(generateResponse.Response == payload);
      generateResponse = client.Generate(new GenerateRequest { Username = "A", Type = "en", Payload = payload });
      Trace.Assert(generateResponse.Response == payload);
      generateResponse = client.Generate(new GenerateRequest { Username = "B", Type = "en", Payload = payload });
      Trace.Assert(generateResponse.Response == payload);
      generateResponse = client.Generate(new GenerateRequest { Username = "C", Type = "fr", Payload = payload });
      Trace.Assert(generateResponse.Response != payload);
      generateResponse = client.Generate(new GenerateRequest { Username = "C", Type = "en", Payload = payload });
      Trace.Assert(generateResponse.Response != payload);

      Thread.Sleep(1000);
      generateResponse = client.Generate(new GenerateRequest { Username = "A", Type = "en", Payload = payload });
      Trace.Assert(generateResponse.Response == payload);
      Thread.Sleep(5000);
      generateResponse = client.Generate(new GenerateRequest { Username = "A", Type = "en", Payload = payload });
      Trace.Assert(generateResponse.Response == payload);
      Thread.Sleep(5000);
      var closeResponse = client.CloseSession(new CloseSessionRequest { Username = "A", Type = "en" });
      Trace.Assert(closeResponse.Ok == "ok");


      channel.ShutdownAsync().Wait();
      Console.WriteLine("Press any key to exit...");
      Console.ReadKey();
    }
  }
}
