using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Efrei.ExchangeServer;
using Grpc.Core;

namespace tradingServer
{
    class ExchangeServerImp : Efrei.ExchangeServer.ExchangeEngine.ExchangeEngineBase
    {
        public override global::System.Threading.Tasks.Task<global::Efrei.ExchangeServer.SubscribeResponse> Subscribe(global::Efrei.ExchangeServer.SubscribeArgs request, ServerCallContext context)
        {
            return Task.FromResult<global::Efrei.ExchangeServer.SubscribeResponse>(new global::Efrei.ExchangeServer.SubscribeResponse());
        }

        public override global::System.Threading.Tasks.Task<global::Efrei.ExchangeServer.SendOrderResponse> SendOrder(global::Efrei.ExchangeServer.SendOrderArgs request, ServerCallContext context)
        {
            return Task.FromResult<global::Efrei.ExchangeServer.SendOrderResponse>(new global::Efrei.ExchangeServer.SendOrderResponse());
        }

        public override global::System.Threading.Tasks.Task<global::Efrei.ExchangeServer.Void> PingSrv(global::Efrei.ExchangeServer.Void request, ServerCallContext context)
        {
            return Task.FromResult<global::Efrei.ExchangeServer.Void>(new global::Efrei.ExchangeServer.Void());
        }
    }


    class Program
    {
        public class ExchangeClientClient
        {
            readonly ExchangeEngine.ExchangeEngineClient client;
            NewPriceArgs newPrice = new NewPriceArgs();
            public ExchangeClientClient(ExchangeEngine.ExchangeEngineClient client)
            {
                this.client = client;
            }
        }
        const int port = 12002;

        static void Main(string[] args)
        {
            Server server = new Server
            {
                Services = { ExchangeEngine.BindService(new ExchangeServerImp()) },
                Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
            };
            server.Start();

            Channel channel = new Channel("localhost:1547", ChannelCredentials.Insecure);
            var exchangeServer = new ExchangeClient.ExchangeClientClient(channel);
            //exchangeServer.Ping(request: new Efrei.ExchangeServer.Void());
            
            Console.WriteLine("trading server1 listening on port " + port);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();
            server.ShutdownAsync().Wait();
        }
    }
}
