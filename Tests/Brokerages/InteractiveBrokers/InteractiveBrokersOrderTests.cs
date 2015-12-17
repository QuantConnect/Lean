using System;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Brokerages.InteractiveBrokers;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages.InteractiveBrokers
{
    [TestFixture, Ignore("These tests require the IBController and IB TraderWorkstation to be installed.")]
    public class InteractiveBrokersForexOrderTests : BrokerageTests
    {
        // set to true to disable launch of gateway from tests
        private const bool _manualGatewayControl = false;
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

        protected override Symbol Symbol
        {
            get { return Symbols.USDJPY; }
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

        protected override decimal GetAskPrice(Symbol symbol)
        {
            throw new NotImplementedException();
        }

        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            if (!_manualGatewayControl && !_gatewayLaunched)
            {
                _gatewayLaunched = true;
                InteractiveBrokersGatewayRunner.Start(Config.Get("ib-controller-dir"),
                    Config.Get("ib-tws-dir"),
                    Config.Get("ib-user-name"),
                    Config.Get("ib-password"),
                    Config.GetBool("ib-use-tws")
                    );
            }
            return new InteractiveBrokersBrokerage(orderProvider, securityProvider);
        }

        protected override void DisposeBrokerage(IBrokerage brokerage)
        {
            if (!_manualGatewayControl && brokerage != null)
            {
                brokerage.Disconnect();
            }
        }
    }
}
