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
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Securities.Option;
using Moq;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using QuantConnect.Data;

namespace QuantConnect.Tests.Engine
{
    [TestFixture]
    public class BasicOptionAssignmentSimulationTests
    {
        private static readonly SecurityExchangeHours SecurityExchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork);

        [Test]
        public void GenerateSimulationDatesFromOptionExpirations()
        {
            var algorithm = new QCAlgorithm();
            var sim = new BasicOptionAssignmentSimulation();

            algorithm.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage);
            algorithm.SetCash(100000);

            var securities = new SecurityManager(TimeKeeper);

            algorithm.Securities = securities;

            securities.Add(
                Symbols.SPY,
                new Security(
                    SecurityExchangeHours,
                    CreateTradeBarDataConfig(SecurityType.Equity, Symbols.SPY),
                    new Cash(Currencies.USD, 0, 1m),
                    SymbolProperties.GetDefault(Currencies.USD),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null
                )
            );
            securities[Symbols.SPY].SetMarketPrice(new TradeBar { Time = securities.UtcTime, Symbol = Symbols.SPY, Close = 195 });

            var option1 = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 192m, new DateTime(2016, 02, 16));
            securities.Add(
                option1,
                new Option(
                    SecurityExchangeHours,
                    CreateTradeBarDataConfig(SecurityType.Equity, option1),
                    new Cash(Currencies.USD, 0, 1m),
                    new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null
                )
            );

            var option2 = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 193m, new DateTime(2016, 02, 19));
            securities.Add(
                option2,
                new Option(
                    SecurityExchangeHours,
                    CreateTradeBarDataConfig(SecurityType.Equity, option2),
                    new Cash(Currencies.USD, 0, 1m),
                    new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null
                )
            );

            var option3 = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 190m, new DateTime(2016, 03, 18));
            securities.Add(
                option3,
                new Option(
                    SecurityExchangeHours,
                    CreateTradeBarDataConfig(SecurityType.Equity, option3),
                    new Cash(Currencies.USD, 0, 1m),
                    new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null
                )
            );

            securities[option1].Holdings.SetHoldings(1, -100);
            securities[option2].Holdings.SetHoldings(1, -100);
            securities[option3].Holdings.SetHoldings(1, -100);

            var startSim = new DateTime(2016, 01, 22);
            var endSim = new DateTime(2016, 03, 19);
            algorithm.SetDateTime(startSim);

            // we run request for simulation every minute up to the expiration date of option2
            // Option3 is too far in the future - we update dates list every month
            int countSims = 0;
            foreach (var count in Enumerable.Range(0, (int)(endSim - startSim).TotalMinutes))
            {
                algorithm.SetDateTime(startSim.AddMinutes(count));
                countSims += sim.IsReadyToSimulate(algorithm)? 1 : 0;
            }

            // there should be 132 attempts to run simulation
            Assert.AreEqual(132, countSims);

        }
        [Test]
        public void SimulatesAssignment()
        {
            var algorithm = new QCAlgorithm();
            var sim = new BasicOptionAssignmentSimulation();
            var securities = new SecurityManager(TimeKeeper);
            var brokerage = new Mock<BacktestingBrokerage>(algorithm);

            algorithm.Securities = securities;

            // dictionaries with expected and actual results
            var expected = new Dictionary<Option, bool>();
            var actual = new Dictionary<Option, bool>();

            brokerage.Setup(m => m.ActivateOptionAssignment(It.IsAny<Option>(), It.IsAny<int>()))
                                    .Callback<Option, int>((option, quantity) => { actual[option] = true; });

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

                                    new { Right = OptionRight.Put, StrikePrice = 225.0m, BidPrice = 7.071m, AskPrice = 7.26m, Exercise = true },
                                    new { Right = OptionRight.Put, StrikePrice = 226.0m, BidPrice = 8.07m, AskPrice = 8.24m, Exercise = true },
                                    new { Right = OptionRight.Put, StrikePrice = 227.0m, BidPrice = 9.59m, AskPrice = 9.77m, Exercise = true },
                                    new { Right = OptionRight.Put, StrikePrice = 230.0m, BidPrice = 12.01m, AskPrice = 12.34m, Exercise = true },
                                    new { Right = OptionRight.Put, StrikePrice = 240.0m, BidPrice = 22.01m, AskPrice = 22.32m, Exercise = true } };

            Func<OptionRight, decimal, decimal, decimal, Option> optionDef =
                (right, strikePrice, bidPrice, askPrice) =>
                {
                    var symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, right, strikePrice, expiration);
                    var option = new Option(
                        SecurityExchangeHours,
                        CreateTradeBarDataConfig(SecurityType.Option, symbol),
                        new Cash(Currencies.USD, 0, 1m),
                        new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                        ErrorCurrencyConverter.Instance,
                        RegisteredSecurityDataTypesProvider.Null
                    );
                    securities.Add(symbol, option);

                    securities[symbol].Holdings.SetHoldings(1, -1000);
                    securities[symbol].SetMarketPrice(new Tick { Symbol = symbol, AskPrice = askPrice, BidPrice = bidPrice, Value = (askPrice + bidPrice)/2.0m, Time = today });
                    return option;
                };

            // setting up the underlying instrument
            securities.Add(
                Symbols.SPY,
                new Security(
                    SecurityExchangeHours,
                    CreateTradeBarDataConfig(SecurityType.Equity, Symbols.SPY),
                    new Cash(Currencies.USD, 0, 1m),
                    SymbolProperties.GetDefault(Currencies.USD),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null
                )
            );
            securities[Symbols.SPY].SetMarketPrice(new Tick { Symbol = Symbols.SPY, AskPrice = 217.94m, BidPrice = 217.86m, Value = 217.90m, Time = securities.UtcTime });

            foreach (var def in optionChain)
            {
                expected.Add(optionDef(def.Right, def.StrikePrice, def.BidPrice, def.AskPrice), def.Exercise);
            }
            // running the simulation
            sim.SimulateMarketConditions(brokerage.Object, algorithm);

            // checking results
            foreach (var result in actual)
            {
                Assert.AreEqual(expected[result.Key], result.Value);
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
            throw new NotImplementedException(type.ToString());
        }
        private static TimeKeeper TimeKeeper
        {
            get { return new TimeKeeper(DateTime.Now, new[] { TimeZones.NewYork }); }
        }
    }
}
