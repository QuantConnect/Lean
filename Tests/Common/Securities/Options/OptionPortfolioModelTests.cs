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
using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Tests.Engine;

namespace QuantConnect.Tests.Common.Securities.Options
{
    [TestFixture]
    public class OptionPortfolioModelTests
    {
        private IResultHandler _resultHandler;

        [SetUp]
        public void SetUp()
        {
            _resultHandler = new TestResultHandler(Console.WriteLine);
        }

        [TearDown]
        public void TearDown()
        {
            _resultHandler.Exit();
        }

        [Test]
        public void OptionExercise_NonAccountCurrency()
        {
            var algorithm = new QCAlgorithm();
            var securities = new SecurityManager(new TimeKeeper(DateTime.Now, TimeZones.NewYork));
            var transactions = new SecurityTransactionManager(null, securities);
            var transactionHandler = new BacktestingTransactionHandler();
            var portfolio = new SecurityPortfolioManager(securities, transactions);

            var EUR = new Cash("EUR", 100*192, 10);
            portfolio.CashBook.Add("EUR", EUR);
            portfolio.SetCash("USD", 0, 1);
            algorithm.Securities = securities;
            transactionHandler.Initialize(algorithm, new BacktestingBrokerage(algorithm), _resultHandler);
            transactions.SetOrderProcessor(transactionHandler);

            securities.Add(
                Symbols.SPY,
                new Security(
                    SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    CreateTradeBarConfig(Symbols.SPY),
                    EUR,
                    SymbolProperties.GetDefault(EUR.Symbol),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                )
            );
            securities.Add(
                Symbols.SPY_C_192_Feb19_2016,
                new Option(
                    SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    CreateTradeBarConfig(Symbols.SPY_C_192_Feb19_2016),
                    EUR,
                    new OptionSymbolProperties(new SymbolProperties("EUR", "EUR", 100, 0.01m, 1, string.Empty)),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null
                )
            );
            securities[Symbols.SPY_C_192_Feb19_2016].Holdings.SetHoldings(1, 1);
            securities[Symbols.SPY].SetMarketPrice(new Tick { Value = 200 });

            transactions.AddOrder(new SubmitOrderRequest(OrderType.OptionExercise, SecurityType.Option, Symbols.SPY_C_192_Feb19_2016, -1, 0, 0, securities.UtcTime, ""));
            var option = (Option)securities[Symbols.SPY_C_192_Feb19_2016];
            var order = (OptionExerciseOrder)transactions.GetOrders(x => true).First();
            option.Underlying = securities[Symbols.SPY];

            var fills = option.OptionExerciseModel.OptionExercise(option, order).ToList();

            Assert.AreEqual(2, fills.Count);
            Assert.IsFalse(fills[0].IsAssignment);
            Assert.AreEqual("Automatic Exercise", fills[0].Message);
            Assert.AreEqual("Option Exercise", fills[1].Message);

            foreach (var fill in fills)
            {
                portfolio.ProcessFill(fill);
            }

            // now we have long position in SPY with average price equal to strike
            var newUnderlyingHoldings = securities[Symbols.SPY].Holdings;
            // we added 100*192 EUR (strike price) at beginning, all consumed by exercise
            Assert.AreEqual(0, EUR.Amount);
            Assert.AreEqual(0, portfolio.CashBook["USD"].Amount);
            Assert.AreEqual(100, newUnderlyingHoldings.Quantity);
            Assert.AreEqual(192.0, newUnderlyingHoldings.AveragePrice);

            // and long call option position has disappeared
            Assert.AreEqual(0, securities[Symbols.SPY_C_192_Feb19_2016].Holdings.Quantity);
        }

        private static SubscriptionDataConfig CreateTradeBarConfig(Symbol symbol)
        {
            return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
        }
    }
}
