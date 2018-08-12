using NUnit.Framework;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    [TestFixture]
    public class BitfinexFeeModelTests
    {
        protected Symbol Symbol => Symbol.Create("ETHUSD", SecurityType.Crypto, Market.Bitfinex);
        protected Security Security
        {
            get
            {
                var security = new Security(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    new SubscriptionDataConfig(typeof(TradeBar), Symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, false, false, false),
                    new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency));
                security.SetMarketPrice(new Tick(DateTime.UtcNow, Symbol, LowPrice, HighPrice));

                return security;
            }
        }
        protected OrderSubmissionData OrderSubmissionData => new OrderSubmissionData(Security.BidPrice, Security.AskPrice, (Security.BidPrice + Security.AskPrice) / 2);

        protected decimal HighPrice => 1000m;
        protected decimal LowPrice => 100m;

        protected decimal Quantity => 1m;

        public TestCaseData[] MakerOrders => new[]
        {
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice)),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice) { OrderSubmissionData = OrderSubmissionData}),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new BitfinexOrderProperties())),
            new TestCaseData(new LimitOrderTestParameters(Symbol, LowPrice, HighPrice, new BitfinexOrderProperties() { PostOnly = true }){ OrderSubmissionData = OrderSubmissionData}),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new BitfinexOrderProperties() { PostOnly = true }))
        };

        public TestCaseData[] TakerOrders => new[]
        {
            new TestCaseData(new MarketOrderTestParameters(Symbol)),
            new TestCaseData(new MarketOrderTestParameters(Symbol, new BitfinexOrderProperties() { PostOnly = true })),
            new TestCaseData(new LimitOrderTestParameters(Symbol, LowPrice, HighPrice) { OrderSubmissionData = OrderSubmissionData}),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new BitfinexOrderProperties() { Hidden = true })),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new BitfinexOrderProperties() { PostOnly = true, Hidden = true }))
        };

        [Test]
        [TestCaseSource("MakerOrders")]
        public void ReturnShortOrderMakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new BitfinexFeeModel();

            Order order = parameters.CreateShortOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : LowPrice;

            Assert.AreEqual(
                BitfinexFeeModel.MakerFee * price * Math.Abs(Quantity),
                feeModel.GetOrderFee(Security, order));
        }

        [Test]
        [TestCaseSource("TakerOrders")]
        public void ReturnShortOrderTakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new BitfinexFeeModel();

            Order order = parameters.CreateShortOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : LowPrice;

            Assert.AreEqual(
                BitfinexFeeModel.TakerFee * price * Math.Abs(Quantity),
                feeModel.GetOrderFee(Security, order));
        }

        [Test]
        [TestCaseSource("MakerOrders")]
        public void ReturnLongOrderMakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new BitfinexFeeModel();

            Order order = parameters.CreateLongOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : HighPrice;

            Assert.AreEqual(
                BitfinexFeeModel.MakerFee * price * Math.Abs(Quantity),
                feeModel.GetOrderFee(Security, order));
        }

        [Test]
        [TestCaseSource("TakerOrders")]
        public void ReturnLongOrderTakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new BitfinexFeeModel();

            Order order = parameters.CreateLongOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : HighPrice;

            Assert.AreEqual(
                BitfinexFeeModel.TakerFee * price * Math.Abs(Quantity),
                feeModel.GetOrderFee(Security, order));
        }
    }
}
