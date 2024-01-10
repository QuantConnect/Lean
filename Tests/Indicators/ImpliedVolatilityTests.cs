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
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class ImpliedVolatilityTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override string TestColumnName => "ImpliedVolatility";

        private DateTime _reference = new DateTime(2022, 9, 1, 10, 0, 0);
        private Symbol _symbol;
        private Symbol _underlying;

        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            var indicator = new ImpliedVolatility("testImpliedVolatilityIndicator", _symbol, 0.04m);
            return indicator;
        }

        [SetUp]
        public void SetUp()
        {
            _symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 450m, new DateTime(2023, 9, 1));
            _underlying = _symbol.Underlying;
        }

        [TestCase("SPX230811C04300000")]
        [TestCase("SPX230811C04500000")]
        [TestCase("SPX230811C04700000")]
        [TestCase("SPX230811P04300000")]
        [TestCase("SPX230811P04500000")]
        [TestCase("SPX230811P04700000")]
        [TestCase("SPX230901C04300000")]
        [TestCase("SPX230901C04500000")]
        [TestCase("SPX230901C04700000")]
        [TestCase("SPX230901P04300000")]
        [TestCase("SPX230901P04500000")]
        [TestCase("SPX230901P04700000")]
        [TestCase("SPY230811C00430000")]
        [TestCase("SPY230811C00450000")]
        [TestCase("SPY230811C00470000")]
        [TestCase("SPY230811P00430000")]
        [TestCase("SPY230811P00450000")]
        [TestCase("SPY230811P00470000")]
        [TestCase("SPY230901C00430000")]
        [TestCase("SPY230901C00450000")]
        [TestCase("SPY230901C00470000")]
        [TestCase("SPY230901P00430000")]
        [TestCase("SPY230901P00450000")]
        [TestCase("SPY230901P00470000")]
        public void ComparesAgainstExternalData(string fileName)
        {
            var path = Path.Combine("TestData", "greeks", $"{fileName}.csv");
            var symbol = ParseOptionSymbol(fileName);
            var underlying = symbol.Underlying;

            var indicator = new ImpliedVolatility(symbol, 0.04m);
            RunTestIndicator(path, indicator, symbol, underlying);
        }

        [Test]
        public override void ComparesAgainstExternalData()
        {
            // Not used
        }

        [TestCase("SPX230811C04300000")]
        [TestCase("SPX230811C04500000")]
        [TestCase("SPX230811C04700000")]
        [TestCase("SPX230811P04300000")]
        [TestCase("SPX230811P04500000")]
        [TestCase("SPX230811P04700000")]
        [TestCase("SPX230901C04300000")]
        [TestCase("SPX230901C04500000")]
        [TestCase("SPX230901C04700000")]
        [TestCase("SPX230901P04300000")]
        [TestCase("SPX230901P04500000")]
        [TestCase("SPX230901P04700000")]
        [TestCase("SPY230811C00430000")]
        [TestCase("SPY230811C00450000")]
        [TestCase("SPY230811C00470000")]
        [TestCase("SPY230811P00430000")]
        [TestCase("SPY230811P00450000")]
        [TestCase("SPY230811P00470000")]
        [TestCase("SPY230901C00430000")]
        [TestCase("SPY230901C00450000")]
        [TestCase("SPY230901C00470000")]
        [TestCase("SPY230901P00430000")]
        [TestCase("SPY230901P00450000")]
        [TestCase("SPY230901P00470000")]
        public void ComparesAgainstExternalDataAfterReset(string fileName)
        {
            var path = Path.Combine("TestData", "greeks", $"{fileName}.csv");
            var symbol = ParseOptionSymbol(fileName);
            var underlying = symbol.Underlying;

            var indicator = new ImpliedVolatility(symbol, 0.04m);
            RunTestIndicator(path, indicator, symbol, underlying);

            indicator.Reset();
            RunTestIndicator(path, indicator, symbol, underlying);
        }

        [Test]
        public override void ComparesAgainstExternalDataAfterReset()
        {
            // Not used
        }

        [TestCase(27.50, 450.0, OptionRight.Call, 60, 0.084)]
        [TestCase(29.35, 450.0, OptionRight.Put, 60, 0.093)]
        [TestCase(37.86, 470.0, OptionRight.Call, 60, 0.021)]
        [TestCase(5.74, 470.0, OptionRight.Put, 60, 0.0)]      // Volatility of deep OTM American put option will not converge in CRR model
        [TestCase(3.44, 430.0, OptionRight.Call, 60, 0.026)]
        [TestCase(40.13, 430.0, OptionRight.Put, 60, 0.189)]
        [TestCase(17.74, 450.0, OptionRight.Call, 180, 0.014)]
        [TestCase(19.72, 450.0, OptionRight.Put, 180, 0.040)]
        [TestCase(38.45, 470.0, OptionRight.Call, 180, 0.038)]
        [TestCase(0.43, 470.0, OptionRight.Put, 180, 0.0)]     // Volatility of deep OTM American put option will not converge in CRR model
        [TestCase(1.73, 430.0, OptionRight.Call, 180, 0.016)]
        [TestCase(12.46, 430.0, OptionRight.Put, 180, 0.072)]
        public void ComparesIVOnCRRModel(decimal price, decimal spotPrice, OptionRight right, int expiry, double refIV)
        {
            // Under CRR framework
            var symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, right, 450m, _reference.AddDays(expiry));
            var indicator = new ImpliedVolatility(_symbol, 0.04m, optionModel: OptionPricingModelType.BinomialCoxRossRubinstein);

            var optionTradeBar = new TradeBar(_reference, _symbol, price, price, price, price, 0m);
            var spotTradeBar = new TradeBar(_reference, _underlying, spotPrice, spotPrice, spotPrice, spotPrice, 0m);
            indicator.Update(optionTradeBar);
            indicator.Update(spotTradeBar);

            Assert.AreEqual(refIV, (double)indicator.Current.Value, 0.03d);
        }

        private Symbol ParseOptionSymbol(string fileName)
        {
            var ticker = fileName.Substring(0, 3);
            var expiry = DateTime.ParseExact(fileName.Substring(3, 6), "yyMMdd", CultureInfo.InvariantCulture);
            var right = fileName[9] == 'C' ? OptionRight.Call : OptionRight.Put;
            var strike = Parse.Decimal(fileName.Substring(10, 8)) / 1000m;
            var style = ticker == "SPY" ? OptionStyle.American : OptionStyle.European;

            return Symbol.CreateOption(ticker, Market.USA, style, right, strike, expiry);
        }

        private void RunTestIndicator(string path, ImpliedVolatility indicator, Symbol symbol, Symbol underlying)
        {
            foreach (var line in File.ReadAllLines(path).Skip(1))
            {
                var items = line.Split(',');

                var time = DateTime.ParseExact(items[0], "yyyyMMdd HH:mm:ss.ffffff", CultureInfo.InvariantCulture);
                var price = Parse.Decimal(items[1]);
                var spotPrice = Parse.Decimal(items[^1]);
                var refIV = Parse.Double(items[2]);

                var optionTradeBar = new TradeBar(time.AddSeconds(-1), symbol, price, price, price, price, 0m, TimeSpan.FromSeconds(1));
                var spotTradeBar = new TradeBar(time.AddSeconds(-1), underlying, spotPrice, spotPrice, spotPrice, spotPrice, 0m, TimeSpan.FromSeconds(1));
                indicator.Update(optionTradeBar);
                indicator.Update(spotTradeBar);

                // We're not sure IB's parameters and models, we'll accept a larger error from far OTM/ITM & close-to-expiry option
                var acceptRange = Math.Max(0.03m, Math.Abs(symbol.ID.StrikePrice - spotPrice) / spotPrice * 30 / (decimal)(symbol.ID.Date - time).TotalDays);
                Assert.AreEqual(refIV, (double)indicator.Current.Value, (double)acceptRange);
            }
        }

        [Test]
        public override void ResetsProperly()
        {
            var indicator = new ImpliedVolatility(_symbol, 0.04m);

            for (var i = 0; i < 5; i++)
            {
                var price = 500m;
                var optionPrice = Math.Max(price - 450, 0) * 1.1m;

                indicator.Update(new TradeBar() { Symbol = _symbol, Low = optionPrice, High = optionPrice, Volume = 100, Close = optionPrice, Time = _reference.AddDays(1 + i) });
                indicator.Update(new TradeBar() { Symbol = _underlying, Low = price, High = price, Volume = 100, Close = price, Time = _reference.AddDays(1 + i) });
            }

            Assert.IsTrue(indicator.IsReady);

            indicator.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(indicator);
        }

        [Test]
        public override void TimeMovesForward()
        {
            var indicator = CreateIndicator();

            for (var i = 10; i > 0; i--)
            {
                var price = 500m;
                var optionPrice = Math.Max(price - 450, 0) * 1.1m;

                indicator.Update(new TradeBar() { Symbol = _symbol, Low = optionPrice, High = optionPrice, Volume = 100, Close = optionPrice, Time = _reference.AddDays(1 + i) });
                indicator.Update(new TradeBar() { Symbol = _underlying, Low = price, High = price, Volume = 100, Close = price, Time = _reference.AddDays(1 + i) });
            }

            Assert.AreEqual(2, indicator.Samples);
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var period = 5;
            var indicator = new ImpliedVolatility("testImpliedVolatilityIndicator", _symbol, period: period);
            var warmUpPeriod = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            if (!warmUpPeriod.HasValue)
            {
                Assert.Ignore($"{indicator.Name} is not IIndicatorWarmUpPeriodProvider");
                return;
            }

            // warmup period is 5 + 1
            for (var i = 1; i <= warmUpPeriod.Value; i++)
            {
                var time = _reference.AddDays(i);
                var price = 500m;
                var optionPrice = Math.Max(price - 450, 0) * 1.1m;

                indicator.Update(new TradeBar() { Symbol = _symbol, Low = optionPrice, High = optionPrice, Volume = 100, Close = optionPrice, Time = time });

                Assert.IsFalse(indicator.IsReady);

                indicator.Update(new TradeBar() { Symbol = _underlying, Low = price, High = price, Volume = 100, Close = price, Time = time });

                // At least 2 days data for historical daily volatility
                if (time <= _reference.AddDays(3))
                {
                    Assert.IsFalse(indicator.IsReady);
                }
                else
                {
                    Assert.IsTrue(indicator.IsReady);
                }

            }

            Assert.AreEqual(2 * warmUpPeriod.Value, indicator.Samples);
        }

        [Test]
        public override void AcceptsRenkoBarsAsInput()
        {
            var indicator = CreateIndicator();
            var firstRenkoConsolidator = new RenkoConsolidator(0.5m);
            var secondRenkoConsolidator = new RenkoConsolidator(0.5m);
            firstRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
            {
                Assert.DoesNotThrow(() => indicator.Update(renkoBar));
            };

            secondRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
            {
                Assert.DoesNotThrow(() => indicator.Update(renkoBar));
            };

            for (int i = 1; i <= 300; i++)
            {
                var price = 550m - i;
                var optionPrice = Math.Max(price - 450, 0) * 1.1m;

                var tradeBar1 = new TradeBar(_reference.AddDays(i), _symbol, optionPrice, optionPrice, optionPrice, optionPrice, 150m);
                firstRenkoConsolidator.Update(tradeBar1);
                var tradeBar2 = new TradeBar(_reference.AddDays(i), _underlying, price, price, price, price, 1200m);
                secondRenkoConsolidator.Update(tradeBar2);
            }

            Assert.AreNotEqual(0, indicator.Samples);
            firstRenkoConsolidator.Dispose();
            secondRenkoConsolidator.Dispose();
        }

        [Test]
        public override void AcceptsVolumeRenkoBarsAsInput()
        {
            var indicator = CreateIndicator();
            var firstVolumeRenkoConsolidator = new VolumeRenkoConsolidator(100);
            var secondVolumeRenkoConsolidator = new VolumeRenkoConsolidator(1000);
            firstVolumeRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
            {
                Assert.DoesNotThrow(() => indicator.Update(renkoBar));
            };

            secondVolumeRenkoConsolidator.DataConsolidated += (sender, renkoBar) =>
            {
                Assert.DoesNotThrow(() => indicator.Update(renkoBar));
            };

            for (int i = 1; i <= 300; i++)
            {
                var price = 550m - i;
                var optionPrice = Math.Max(price - 450, 0) * 1.1m;

                var tradeBar1 = new TradeBar(_reference.AddDays(i), _symbol, optionPrice, optionPrice, optionPrice, optionPrice, 150m);
                firstVolumeRenkoConsolidator.Update(tradeBar1);
                var tradeBar2 = new TradeBar(_reference.AddDays(i), _underlying, price, price, price, price, 1200m);
                secondVolumeRenkoConsolidator.Update(tradeBar2);
            }

            Assert.AreNotEqual(0, indicator.Samples);
            firstVolumeRenkoConsolidator.Dispose();
            secondVolumeRenkoConsolidator.Dispose();
        }

        [Test]
        public void AcceptsQuoteBarsAsInput()
        {
            var indicator = CreateIndicator();

            for (var i = 1; i <= 100; i++)
            {
                var price = 500m;
                var optionPrice = Math.Max(price - 450, 0) * 1.1m;

                indicator.Update(new QuoteBar { 
                    Symbol = _symbol, 
                    Ask = new Bar(optionPrice, optionPrice, optionPrice, optionPrice), 
                    Bid = new Bar(optionPrice, optionPrice, optionPrice, optionPrice),
                    Time = _reference.AddDays(1 + i) 
                });
                indicator.Update(new QuoteBar { Symbol = _underlying, Ask = new Bar(price, price, price, price), Time = _reference.AddDays(1 + i) });
            }

            Assert.AreEqual(200, indicator.Samples);
        }

        // Not used
        protected override string TestFileName => string.Empty;
    }
}
