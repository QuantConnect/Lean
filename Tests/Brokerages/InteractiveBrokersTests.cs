using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Krs.Ats.IBNet;
using NUnit.Framework;
using QuantConnect.Brokerages.InteractiveBrokers;
using Order = QuantConnect.Orders.Order;
using OrderStatus = QuantConnect.Orders.OrderStatus;
using OrderType = QuantConnect.Orders.OrderType;

namespace QuantConnect.Tests.Brokerages
{
    [TestFixture]
    public class InteractiveBrokersTests
    {
        [Test]
        public void ClientConnects()
        {
            var ib = new IBBrokerage();
            ib.Connect();
        }

        [Test]
        public void ClientPlacesMarketOrder()
        {
            bool orderFilled = false;
            var manualResetEvent = new ManualResetEvent(false);
            var ib = new IBBrokerage();
            ib.Connect();

            ib.OrderFilled += (sender, args) =>
            {
                orderFilled = true;
                manualResetEvent.Set();
            };

            const int buyQuantity = 1;
            ib.PlaceOrder(new Order("AAPL", SecurityType.Equity, buyQuantity, OrderType.Market, DateTime.Now));

            manualResetEvent.WaitOne(2500);

            Assert.IsTrue(orderFilled);
        }

        [Test]
        public void ClientSellsMarketOrder()
        {
            bool orderFilled = false;
            var manualResetEvent = new ManualResetEvent(false);

            var ib = new IBBrokerage();
            ib.Connect();

            ib.OrderFilled += (sender, args) =>
            {
                orderFilled = true;
                manualResetEvent.Set();
            };

            // sell a single share
            ib.PlaceOrder(new Order("AAPL", SecurityType.Equity, -1, OrderType.Market, DateTime.UtcNow));

            manualResetEvent.WaitOne(2500);

            Assert.IsTrue(orderFilled);
        }

        [Test]
        public void ClientPlacesLimitOrder()
        {
            bool orderFilled = false;
            var manualResetEvent = new ManualResetEvent(false);
            var ib = new IBBrokerage();
            ib.Connect();

            decimal aapl = 0m;
            ib.OrderFilled += (sender, args) =>
            {
                orderFilled = true;
                aapl = args.FillPrice;
                manualResetEvent.Set();
            };

            // get the current market price, couldn't get RequestMarketData to fire tick events
            int id = 0;
            ib.PlaceOrder(new Order("AAPL", SecurityType.Equity, 1, OrderType.Market, DateTime.UtcNow) { Id = ++id });

            manualResetEvent.WaitOne(2000);
            manualResetEvent.Reset();
            Assert.IsTrue(orderFilled);

            orderFilled = false;

            // make a box around the current price +- a little

            const int quantity = 1;
            ib.PlaceOrder(new Order("AAPL", SecurityType.Equity, +quantity, OrderType.Limit, DateTime.Now, aapl - 0.02m) { Id = ++id });
            ib.PlaceOrder(new Order("AAPL", SecurityType.Equity, -quantity, OrderType.Limit, DateTime.Now, aapl + 0.02m) { Id = ++id });

            manualResetEvent.WaitOne(1000);

            Assert.IsTrue(orderFilled);
        }

        [Test]
        public void ClientPlacesStopLimitOrder()
        {
            bool orderFilled = false;
            var manualResetEvent = new ManualResetEvent(false);
            var ib = new IBBrokerage();
            ib.Connect();

            decimal aapl = 0m;
            ib.OrderFilled += (sender, args) =>
            {
                orderFilled = true;
                aapl = args.FillPrice;
                manualResetEvent.Set();
            };

            // get the current market price, couldn't get RequestMarketData to fire tick events
            int id = 0;
            ib.PlaceOrder(new Order("AAPL", SecurityType.Equity, 1, OrderType.Market, DateTime.UtcNow) {Id = ++id});

            manualResetEvent.WaitOne(2000);
            manualResetEvent.Reset();
            Assert.IsTrue(orderFilled);

            orderFilled = false;

            // make a box around the current price +- a little

            const int quantity = 1;
            ib.PlaceOrder(new Order("AAPL", SecurityType.Equity, +quantity, OrderType.StopMarket, DateTime.Now, aapl - 0.02m) {Id = ++id});
            ib.PlaceOrder(new Order("AAPL", SecurityType.Equity, -quantity, OrderType.StopMarket, DateTime.Now, aapl + 0.02m) {Id = ++id});

            manualResetEvent.WaitOne(1000);

            Assert.IsTrue(orderFilled);
        }

        [Test]
        public void ClientCancelsLimitOrder()
        {
            OrderStatus status = OrderStatus.New;
            var manualResetEvent = new ManualResetEvent(false);

            var ib = new IBBrokerage();
            ib.Connect();

            ib.OrderFilled += (sender, args) =>
            {
                status = args.Status;
                manualResetEvent.Set();
            };

            // try to sell a single share at a ridiculous price, we'll cancel this later
            var order = new Order("AAPL", SecurityType.Equity, -1, OrderType.Limit, DateTime.UtcNow, 100000);
            ib.PlaceOrder(order);
            manualResetEvent.WaitOne(2500);

            ib.CancelOrder(order);

            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2500);

            Assert.AreEqual(OrderStatus.Canceled, status);
        }
    }
}
