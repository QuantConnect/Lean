using System;
using NUnit.Framework;
using QuantConnect.Brokerages.InteractiveBrokers;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages.InteractiveBrokers
{
    [TestFixture, Ignore("These tests require the IBController and IB TraderWorkstation to be installed.")]
    public class InteractiveBrokersForexOrderTests : BrokerageTests
    {
        private static bool _gatewayLaunched;
     
        [TestFixtureSetUp]
        public void InitializeBrokerage()
        {
        }

        [TestFixtureTearDown]
        public void DisposeBrokerage()
        {
            InteractiveBrokersGatewayRunner.Stop();
        }

        protected override string Symbol
        {
            get { return "USDJPY"; }
        }

        protected override SecurityType SecurityType
        {
            get { return SecurityType.Forex; }
        }

        protected override decimal HighPrice
        {
            get { return 10000m; }
        }

        protected override decimal LowPrice
        {
            get { return 0.01m; }
        }

        protected override decimal GetAskPrice(string symbol, SecurityType securityType)
        {
            throw new NotImplementedException();
        }

        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, IHoldingsProvider holdingsProvider)
        {
            if (!_gatewayLaunched)
            {
                _gatewayLaunched = true;
                InteractiveBrokersGatewayRunner.Start(Config.Get("ib-controller-dir"),
                    Config.Get("ib-tws-dir"),
                    Config.Get("ib-user-name"),
                    Config.Get("ib-password"),
                    Config.GetBool("ib-use-tws")
                    );
            }
            return new InteractiveBrokersBrokerage(orderProvider);
        }

        protected override void DisposeBrokerage(IBrokerage brokerage)
        {
            if (brokerage != null)
            {
                brokerage.Disconnect();
            }
        }
    }
}
