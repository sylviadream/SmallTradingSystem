using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Efrei.ExchangeServer;
using Grpc.Core;

namespace tradingClient
{
    
    class ExchangeServerClientImpl : Efrei.ExchangeServer.ExchangeClient.ExchangeClientBase
    {

        NewPriceArgs request0 = new NewPriceArgs();
        NewPriceArgs request1 = new NewPriceArgs();
        PortfolioSingleProduct ptf = new PortfolioSingleProduct();

        public NewPriceArgs newPrice = new NewPriceArgs();

        public override global::System.Threading.Tasks.Task<global::Efrei.ExchangeServer.Void> Ping(global::Efrei.ExchangeServer.Void request, ServerCallContext context)
        {
            //Console.WriteLine(request);
            return Task.FromResult<global::Efrei.ExchangeServer.Void>(new Efrei.ExchangeServer.Void());
        }

        public override global::System.Threading.Tasks.Task<global::Efrei.ExchangeServer.Void> NewPrice(global::Efrei.ExchangeServer.NewPriceArgs request, ServerCallContext context)
        {
            if (request.InstrumentId == 0)
            {
                request0 = request;
            }

            if (request.InstrumentId == 1)
            {
                request1 = request;
            }
            if (!(request0 == null || request1 == null))
            {
                if ((request0.Bid > request1.Ask) || (request1.Bid > request0.Ask))
                {
                    // Console.WriteLine("NEW opportunity!");


                }
            }

            //Console.WriteLine(request);
            newPrice = request;
            return Task.FromResult<global::Efrei.ExchangeServer.Void>(new Efrei.ExchangeServer.Void());
        }

        public override global::System.Threading.Tasks.Task<global::Efrei.ExchangeServer.Void> OrderEvent(global::Efrei.ExchangeServer.OrderEventArg request, ServerCallContext context)
        {
            Console.WriteLine(request);
            Console.WriteLine(request.Status);


            update_portfolio(ptf, request.Deal);
            return Task.FromResult<global::Efrei.ExchangeServer.Void>(new Efrei.ExchangeServer.Void());
        }

        void update_portfolio(global::Efrei.ExchangeServer.PortfolioSingleProduct p, global::Efrei.ExchangeServer.Deal d)
        {

            //new 一个portfolio然后每次send order完用deal更新他
            p.ExecutedQty = p.ExecutedQty + Convert.ToUInt64(Math.Abs(d.Qty));
            var deal_qty_left = Convert.ToInt64(d.Qty);
            int ideal_qty_left = Math.Abs(d.Qty);
            var udeal_qty_left = Convert.ToUInt64(d.Qty);
            while (deal_qty_left != 0)
            {
                if (p.OpenQty * deal_qty_left >= 0)
                {
                    p.OpenQty = p.OpenQty + deal_qty_left;
                    global::Efrei.ExchangeServer.Deal newdeal = new Deal();
                    newdeal.Price = d.Price;
                    newdeal.Qty = ideal_qty_left;
                    p.OpenDeals.Add(newdeal);
                    Console.WriteLine(p.Pnl);
                    return;
                }
                var nxt_prc = p.OpenDeals[p.OpenDeals.Count - 1].Price;
                var nxt_qty = p.OpenDeals[p.OpenDeals.Count - 1].Qty;

                if (Math.Abs(nxt_qty) >= Math.Abs(deal_qty_left))
                {
                    if ((p.OpenQty + deal_qty_left) == 0)
                    {
                        p.OpenQty = 0;
                        p.Pnl = p.Pnl + (long)(udeal_qty_left * (nxt_prc - d.Price));
                        var lastdeal = p.OpenDeals[p.OpenDeals.Count - 1];
                        p.OpenDeals.Remove(lastdeal);

                        return;
                    }

                    p.OpenQty = p.OpenQty + deal_qty_left;
                    p.Pnl = p.Pnl + (long)(udeal_qty_left * (nxt_prc - d.Price));
                    p.OpenDeals[p.OpenDeals.Count - 1].Qty = nxt_qty + ideal_qty_left;
                }
                else
                {
                    p.OpenQty = p.OpenQty - nxt_qty;
                    p.Pnl = p.Pnl - (long)((ulong)nxt_qty * (nxt_prc - d.Price));
                    var lastdeal = p.OpenDeals[p.OpenDeals.Count - 1];
                    p.OpenDeals.Remove(lastdeal);
                    deal_qty_left += nxt_qty;
                }


            }
            Console.WriteLine(p);
        }

    }

    class Program
    {
        public class ExchangeEngineClient
        {
            readonly ExchangeServerClientImpl exserverclient;//有问题
            readonly ExchangeEngine.ExchangeEngineClient client;
            private SubscribeResponse reply = new SubscribeResponse();

            public ExchangeEngineClient(ExchangeEngine.ExchangeEngineClient client)
            {
                this.client = client;
            }
            public void Subscribe()
            {

                var reply = client.Subscribe(new SubscribeArgs { Name = "High tech", Endpoint = "localhost:1547" });
                
                Console.WriteLine("subscribe called client ID:" + reply.ClientId);
            }

            public void SendOrder()
            {
                NewPriceArgs newprice = exserverclient.newPrice;//We dont know how to get this price from exchange server
                
                SendOrderArgs orderArgs = new SendOrderArgs { ClientId = (ulong)reply.ClientId, InstrumentId = newprice.InstrumentId, Price = newprice.Ask, Qty = (int)newprice.AskQty };
                client.SendOrder(orderArgs);
            }

            public void PingSrv()
            {
                client.PingSrv(new Efrei.ExchangeServer.Void());
                Console.WriteLine("PING called!");
            }

        }
        const int port = 1547;

        static void Main(string[] args)
        {
            Server server = new Server
            {
                Services = { ExchangeClient.BindService(new ExchangeServerClientImpl()) },
                Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.WriteLine("trading server2 listening on port " + port);
            
            Channel channel = new Channel("localhost:10000", ChannelCredentials.Insecure);
            var client = new ExchangeEngineClient(new ExchangeEngine.ExchangeEngineClient(channel));
            client.PingSrv();
            client.Subscribe();
            client.SendOrder();
           

            //client.SendOrder(new SendOrderArgs { ClientId = (ulong)reply.ClientId, InstrumentId = 1, Price = 9999999, Qty = 1 });
            //client.SendOrder(new SendOrderArgs { ClientId = (ulong)reply.ClientId, InstrumentId = 1, Price = 999999, Qty = 1 });
            
            //NewPriceArgs newprice = new NewPriceArgs();//We dont know how to get this price from exchange server

            //SendOrderArgs orderArgs = new SendOrderArgs { ClientId = (ulong)reply.ClientId, InstrumentId = newprice.InstrumentId, Price = newprice.Ask, Qty = newprice.AskQty };
            //client.SendOrder(orderArgs);
            
            channel.ShutdownAsync().Wait();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

        }
    }
}
