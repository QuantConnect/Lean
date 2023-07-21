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
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Securities.Option;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using QuantConnect.Data;
using System.Linq;

namespace QuantConnect.Tests.Engine
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class DefaultOptionAssignmentModelTests
    {
        private static readonly SecurityExchangeHours SecurityExchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork);

        [TestCase(SecurityType.Equity)]
        [TestCase(SecurityType.Index)]
        public void SimulatesAssignment(SecurityType securityType)
        {
            var underlyingSymbol = securityType == SecurityType.Index ? Symbols.SPX : Symbols.SPY;
            var settlementType = securityType == SecurityType.Index ? SettlementType.Cash : SettlementType.PhysicalDelivery;
            var algorithm = new QCAlgorithm();
            var sim = new DefaultOptionAssignmentModel();
            var securities = new SecurityManager(TimeKeeper);

            algorithm.Securities = securities;

            // dictionaries with expected and actual results
            var expected = new Dictionary<Option, bool>();
            var actual = new Dictionary<Option, bool>();

            // we build option chain at expiration
            var expiration = new DateTime(2016, 02, 19);
            var today = expiration.AddDays(-3);
            algorithm.SetDateTime(today);

            // we define option chain with expected results for each contract (if it is optimal to exercise it or not)
            var optionChain = new[] { new { Right = OptionRight.Call, StrikePrice = 190.0m, BidPrice = 27.81m, AskPrice = 28.01m, Exercise = true },
                                    new { Right = OptionRight.Call, StrikePrice = 193.0m, BidPrice = 24.87m, AskPrice = 24.99m, Exercise = true },
                                    new { Right = OptionRight.Call, StrikePrice = 196.0m, BidPrice = 21.50m, AskPrice = 21.63m, Exercise = true },
                                    new { Right = OptionRight.Call, StrikePrice = 198.0m, BidPrice = 18.79m, AskPrice = 18.96m, Exercise = true },
                                    new { Right = OptionRight.Call, StrikePrice = 200.0m, BidPrice = 17.77m, AskPrice = 17.96m, Exercise = true },
                                    new { Right = OptionRight.Call, StrikePrice = 202.0m, BidPrice = 15.31m, AskPrice = 15.47m, Exercise = true },
                                    new { Right = OptionRight.Call, StrikePrice = 220.0m, BidPrice = 15.31m, AskPrice = 15.47m, Exercise = false },

                                    new { Right = OptionRight.Put, StrikePrice = 225.0m, BidPrice = 7.071m, AskPrice = 7.26m, Exercise = false },
                                    new { Right = OptionRight.Put, StrikePrice = 226.0m, BidPrice = 8.07m, AskPrice = 8.24m, Exercise = false },
                                    new { Right = OptionRight.Put, StrikePrice = 227.0m, BidPrice = 9.59m, AskPrice = 9.77m, Exercise = false },
                                    new { Right = OptionRight.Put, StrikePrice = 230.0m, BidPrice = 12.01m, AskPrice = 12.34m, Exercise = true },
                                    new { Right = OptionRight.Put, StrikePrice = 240.0m, BidPrice = 22.01m, AskPrice = 22.32m, Exercise = true } };

            Func<OptionRight, decimal, decimal, decimal, Option> optionDef =
                (right, strikePrice, bidPrice, askPrice) =>
                {
                    var symbol = Symbol.CreateOption(underlyingSymbol, Market.USA, OptionStyle.American, right, strikePrice, expiration);
                    var option = new Option(
                        SecurityExchangeHours,
                        CreateTradeBarDataConfig(SecurityType.Option, symbol),
                        new Cash(Currencies.USD, 0, 1m),
                        new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                        ErrorCurrencyConverter.Instance,
                        RegisteredSecurityDataTypesProvider.Null
                    ) { ExerciseSettlement = settlementType };
                    securities.Add(symbol, option);

                    securities[symbol].Holdings.SetHoldings(1, -1000);
                    securities[symbol].SetMarketPrice(new Tick { Symbol = symbol, AskPrice = askPrice, BidPrice = bidPrice, Value = (askPrice + bidPrice)/2.0m, Time = today });
                    option.Underlying = securities[symbol.Underlying];
                    return option;
                };

            // setting up the underlying instrument
            securities.Add(
                underlyingSymbol,
                new Security(
                    SecurityExchangeHours,
                    CreateTradeBarDataConfig(securityType, underlyingSymbol),
                    new Cash(Currencies.USD, 0, 1m),
                    SymbolProperties.GetDefault(Currencies.USD),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                )
            );
            securities[underlyingSymbol].SetMarketPrice(new Tick { Symbol = underlyingSymbol, AskPrice = 217.94m, BidPrice = 217.86m, Value = 217.90m, Time = securities.UtcTime });

            foreach (var def in optionChain)
            {
                expected.Add(optionDef(def.Right, def.StrikePrice, def.BidPrice, def.AskPrice), def.Exercise);
            }
            // running the simulation

            // checking results
            foreach (var option in algorithm.Securities.Values.Where(security => security.Symbol.SecurityType.IsOption()).OrderBy(security => security.Symbol))
            {
                var result = sim.GetAssignment(new OptionAssignmentParameters((Option)option));

                Assert.AreEqual(expected[(Option)option], result.Quantity > 0, $"Failed on strike: {option.Symbol.ID.StrikePrice}");
            }
        }

        private SubscriptionDataConfig CreateTradeBarDataConfig(SecurityType type, Symbol symbol)
        {
            if (type == SecurityType.Equity)
                return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true);
            if (type == SecurityType.Forex)
                return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true);
            if (type == SecurityType.Option)
                return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true);
            if (type == SecurityType.Index)
                return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true);
            throw new NotImplementedException(type.ToString());
        }
        private static TimeKeeper TimeKeeper
        {
            get { return new TimeKeeper(DateTime.Now, new[] { TimeZones.NewYork }); }
        }
    }
}
