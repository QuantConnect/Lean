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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Cfd;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Margin
{
    [TestFixture]
    public class OptionMarginModelTests
    {

        [Test]
        public void MarginModelInitializationTests()
        {
            var tz = TimeZones.NewYork;
            var option = new Option(SecurityExchangeHours.AlwaysOpen(tz), new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY_P_192_Feb19_2016, Resolution.Minute, tz, tz, true, false, false), new Cash(CashBook.AccountCurrency, 0, 1m), new OptionSymbolProperties(SymbolProperties.GetDefault(CashBook.AccountCurrency)));
            var marginModel = new OptionMarginModel();

            // we test that options dont have leverage (100%) and it cannot be changed
            Assert.AreEqual(1m, marginModel.GetLeverage(option));
            Assert.Throws<InvalidOperationException>(() => marginModel.SetLeverage(option, 10m));
            Assert.AreEqual(1m, marginModel.GetLeverage(option));
        }

        [Test]
        public void TestLongCallsPuts()
        {
            const decimal price = 1.2345m;
            const decimal underlyingPrice = 200m;
            var tz = TimeZones.NewYork;

            var equity = new Equity(SecurityExchangeHours.AlwaysOpen(tz), new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, tz, tz, true, false, false), new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency));
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });

            var optionPut = new Option(SecurityExchangeHours.AlwaysOpen(tz), new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY_P_192_Feb19_2016, Resolution.Minute, tz, tz, true, false, false), new Cash(CashBook.AccountCurrency, 0, 1m), new OptionSymbolProperties(SymbolProperties.GetDefault(CashBook.AccountCurrency)));
            optionPut.SetMarketPrice(new Tick { Value = price });
            optionPut.Underlying = equity;
            optionPut.Holdings.SetHoldings(1, 2);

            var optionCall = new Option(SecurityExchangeHours.AlwaysOpen(tz), new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY_C_192_Feb19_2016, Resolution.Minute, tz, tz, true, false, false), new Cash(CashBook.AccountCurrency, 0, 1m), new OptionSymbolProperties(SymbolProperties.GetDefault(CashBook.AccountCurrency)));
            optionCall.SetMarketPrice(new Tick { Value = price });
            optionCall.Underlying = equity;
            optionCall.Holdings.SetHoldings(1.5m, 2);

            var marginModel = new OptionMarginModel();

            // we expect long positions to be 100% charged. 
            Assert.AreEqual(marginModel.GetMaintenanceMargin(optionPut), optionPut.Holdings.AbsoluteHoldingsCost);
            Assert.AreEqual(marginModel.GetMaintenanceMargin(optionCall), optionCall.Holdings.AbsoluteHoldingsCost);
        }


        [Test]
        public void TestShortCallsITM()
        {
            const decimal price = 14m;
            const decimal underlyingPrice = 196m;
            var tz = TimeZones.NewYork;

            var equity = new Equity(SecurityExchangeHours.AlwaysOpen(tz), new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, tz, tz, true, false, false), new Cash(CashBook.AccountCurrency, 0, 1m), new SymbolProperties("", CashBook.AccountCurrency.ToUpper(), 1, 0.01m, 1));
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            

            var optionCall = new Option(SecurityExchangeHours.AlwaysOpen(tz), new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY_C_192_Feb19_2016, Resolution.Minute, tz, tz, true, false, false), new Cash(CashBook.AccountCurrency, 0, 1m), new OptionSymbolProperties("", CashBook.AccountCurrency.ToUpper(), 100, 0.01m, 1));
            optionCall.SetMarketPrice(new Tick { Value = price });
            optionCall.Underlying = equity;
            optionCall.Holdings.SetHoldings(1, -2);

            var marginModel = new OptionMarginModel();

            // short option positions are very expensive in terms of margin. 
            Assert.AreEqual(marginModel.GetMaintenanceMargin(optionCall), (1m + 14m * 0.2m) * optionCall.Holdings.AbsoluteHoldingsCost);
        }

        [Test]
        public void TestShortCallsOTM()
        {
            const decimal price = 14m;
            const decimal underlyingPrice = 180m;
            var tz = TimeZones.NewYork;

            var equity = new Equity(SecurityExchangeHours.AlwaysOpen(tz), new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, tz, tz, true, false, false), new Cash(CashBook.AccountCurrency, 0, 1m), new SymbolProperties("", CashBook.AccountCurrency.ToUpper(), 1, 0.01m, 1));
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });


            var optionCall = new Option(SecurityExchangeHours.AlwaysOpen(tz), new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY_C_192_Feb19_2016, Resolution.Minute, tz, tz, true, false, false), new Cash(CashBook.AccountCurrency, 0, 1m), new OptionSymbolProperties("", CashBook.AccountCurrency.ToUpper(), 100, 0.01m, 1));
            optionCall.SetMarketPrice(new Tick { Value = price });
            optionCall.Underlying = equity;
            optionCall.Holdings.SetHoldings(1, -2);

            var marginModel = new OptionMarginModel();

            // short option positions are very expensive in terms of margin.
            Assert.AreEqual((double)marginModel.GetMaintenanceMargin(optionCall), 542.857, 0.01);
        }


        [Test]
        public void TestShortPutsITM()
        {
            const decimal price = 14m;
            const decimal underlyingPrice = 182m;
            var tz = TimeZones.NewYork;

            var equity = new Equity(SecurityExchangeHours.AlwaysOpen(tz), new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, tz, tz, true, false, false), new Cash(CashBook.AccountCurrency, 0, 1m), new SymbolProperties("", CashBook.AccountCurrency.ToUpper(), 1, 0.01m, 1));
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });

            var optionPut = new Option(SecurityExchangeHours.AlwaysOpen(tz), new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY_P_192_Feb19_2016, Resolution.Minute, tz, tz, true, false, false), new Cash(CashBook.AccountCurrency, 0, 1m), new OptionSymbolProperties(SymbolProperties.GetDefault(CashBook.AccountCurrency)));
            optionPut.SetMarketPrice(new Tick { Value = price });
            optionPut.Underlying = equity;
            optionPut.Holdings.SetHoldings(1, -2);

            var marginModel = new OptionMarginModel();

            // short option positions are very expensive in terms of margin.
            Assert.AreEqual(marginModel.GetMaintenanceMargin(optionPut), (1m + 13m * 0.2m) * optionPut.Holdings.AbsoluteHoldingsCost);
        }

        [Test]
        public void TestShortPutsOTM()
        {
            const decimal price = 14m;
            const decimal underlyingPrice = 196m;
            var tz = TimeZones.NewYork;

            var equity = new Equity(SecurityExchangeHours.AlwaysOpen(tz), new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, tz, tz, true, false, false), new Cash(CashBook.AccountCurrency, 0, 1m), new SymbolProperties("", CashBook.AccountCurrency.ToUpper(), 1, 0.01m, 1));
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });


            var optionCall = new Option(SecurityExchangeHours.AlwaysOpen(tz), new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY_P_192_Feb19_2016, Resolution.Minute, tz, tz, true, false, false), new Cash(CashBook.AccountCurrency, 0, 1m), new OptionSymbolProperties("", CashBook.AccountCurrency.ToUpper(), 100, 0.01m, 1));
            optionCall.SetMarketPrice(new Tick { Value = price });
            optionCall.Underlying = equity;
            optionCall.Holdings.SetHoldings(1, -2);

            var marginModel = new OptionMarginModel();

            // short option positions are very expensive in terms of margin.
            Assert.AreEqual((double)marginModel.GetMaintenanceMargin(optionCall), 702.857, 0.01);
        }
    }
}
