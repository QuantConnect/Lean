/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    public class BrokerageFeeModelOverrideTests
    {
        private static readonly TestCaseData[] FeeModelTestCases =
        {
            new TestCaseData(new WolverineBrokerageModel(), SecurityType.Equity, 1000m, 5m).SetName("WolverineEquity"),
            new TestCaseData(new RBIBrokerageModel(), SecurityType.Equity, 1000m, 5m).SetName("RBIEquity"),
            new TestCaseData(new TDAmeritradeBrokerageModel(), SecurityType.Option, 10m, 6.5m).SetName("TDAmeritradeOption")
        };

        [TestCaseSource(nameof(FeeModelTestCases))]
        public void GetOrderFeeThroughBrokerageModelFeeModel(IBrokerageModel brokerageModel, SecurityType securityType,
            decimal quantity, decimal expectedFee)
        {
            var security = CreateSecurity(securityType);
            var order = new MarketOrder(security.Symbol, quantity, new DateTime(2024, 1, 3, 15, 0, 0));

            // the engine always calls the fee model through the IFeeModel interface
            IFeeModel feeModel = brokerageModel.GetFeeModel(security);
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.AreEqual(expectedFee, fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        private static Security CreateSecurity(SecurityType securityType)
        {
            var symbol = Symbols.SPY;
            if (securityType == SecurityType.Option)
            {
                symbol = Symbol.CreateOption(symbol, Market.USA, OptionStyle.American, OptionRight.Call, 450m, new DateTime(2024, 1, 19));
            }

            var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute,
                TimeZones.NewYork, TimeZones.NewYork, true, true, false);

            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            security.SetMarketPrice(new Tick(DateTime.UtcNow, symbol, 10m, 10m));
            return security;
        }
    }
}
