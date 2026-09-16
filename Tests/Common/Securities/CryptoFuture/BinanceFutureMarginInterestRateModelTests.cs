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
 *
*/

using System;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Securities.CryptoFuture;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities.CryptoFuture
{
    [TestFixture]
    public class BinanceFutureMarginInterestRateModelTests
    {
        [Test]
        public void DoesNotApplyStaleMarginInterestRate()
        {
            var algorithm = GetAlgorithm();
            var cryptoFuture = algorithm.AddCryptoFuture("BTCUSDT");
            SetPrice(cryptoFuture, 100m, new DateTime(2026, 3, 24, 0, 0, 0));
            cryptoFuture.Holdings.SetHoldings(cryptoFuture.Price, 1m);

            var lastFundingTime = new DateTime(2026, 3, 24, 0, 0, 0);
            StoreMarginInterestRate(cryptoFuture, lastFundingTime, 0.01m);

            var model = new BinanceFutureMarginInterestRateModel();
            model.ApplyMarginInterestRate(new MarginInterestRateParameters(cryptoFuture, lastFundingTime));

            var cashBeforeStaleApplication = cryptoFuture.QuoteCurrency.Amount;
            model.ApplyMarginInterestRate(new MarginInterestRateParameters(cryptoFuture, lastFundingTime.AddHours(8)));

            Assert.AreEqual(cashBeforeStaleApplication, cryptoFuture.QuoteCurrency.Amount);
        }

        [Test]
        public void AppliesMarginInterestRateForCurrentFundingInterval()
        {
            var algorithm = GetAlgorithm();
            var cryptoFuture = algorithm.AddCryptoFuture("BTCUSDT");
            SetPrice(cryptoFuture, 100m, new DateTime(2026, 3, 24, 0, 0, 0));
            cryptoFuture.Holdings.SetHoldings(cryptoFuture.Price, 1m);

            var fundingTime = new DateTime(2026, 3, 24, 8, 0, 0);
            StoreMarginInterestRate(cryptoFuture, fundingTime, 0.01m);

            var model = new BinanceFutureMarginInterestRateModel();
            model.ApplyMarginInterestRate(new MarginInterestRateParameters(cryptoFuture, fundingTime.AddHours(-8)));
            SetPrice(cryptoFuture, 100m, fundingTime);

            var cashBeforeApplication = cryptoFuture.QuoteCurrency.Amount;
            model.ApplyMarginInterestRate(new MarginInterestRateParameters(cryptoFuture, fundingTime));

            Assert.AreEqual(cashBeforeApplication - 1m, cryptoFuture.QuoteCurrency.Amount);
        }

        [Test]
        public void AppliesNewMarginInterestRateAfterStaleData()
        {
            var algorithm = GetAlgorithm();
            var cryptoFuture = algorithm.AddCryptoFuture("BTCUSDT");
            SetPrice(cryptoFuture, 100m, new DateTime(2026, 3, 24, 0, 0, 0));
            cryptoFuture.Holdings.SetHoldings(cryptoFuture.Price, 1m);

            var lastStaleFundingTime = new DateTime(2026, 3, 24, 0, 0, 0);
            StoreMarginInterestRate(cryptoFuture, lastStaleFundingTime, 0.01m);

            var model = new BinanceFutureMarginInterestRateModel();
            model.ApplyMarginInterestRate(new MarginInterestRateParameters(cryptoFuture, lastStaleFundingTime));
            model.ApplyMarginInterestRate(new MarginInterestRateParameters(cryptoFuture, lastStaleFundingTime.AddHours(8)));

            var currentFundingTime = lastStaleFundingTime.AddHours(16);
            SetPrice(cryptoFuture, 100m, currentFundingTime);
            StoreMarginInterestRate(cryptoFuture, currentFundingTime, 0.01m);

            var cashBeforeApplication = cryptoFuture.QuoteCurrency.Amount;
            model.ApplyMarginInterestRate(new MarginInterestRateParameters(cryptoFuture, currentFundingTime));

            Assert.AreEqual(cashBeforeApplication - 1m, cryptoFuture.QuoteCurrency.Amount);
        }

        private static QCAlgorithm GetAlgorithm()
        {
            var algorithm = new AlgorithmStub();
            algorithm.SetFinishedWarmingUp();
            return algorithm;
        }

        private static void StoreMarginInterestRate(
            QuantConnect.Securities.CryptoFuture.CryptoFuture cryptoFuture, DateTime time, decimal interestRate)
        {
            cryptoFuture.Cache.StoreData(new[]
            {
                new MarginInterestRate
                {
                    Symbol = cryptoFuture.Symbol,
                    Time = time,
                    InterestRate = interestRate
                }
            }, typeof(MarginInterestRate));
        }

        private static void SetPrice(Security security, decimal price, DateTime time)
        {
            var cryptoFuture = (QuantConnect.Securities.CryptoFuture.CryptoFuture)security;
            cryptoFuture.BaseCurrency.ConversionRate = price;
            cryptoFuture.QuoteCurrency.ConversionRate = 1;

            security.SetMarketPrice(new TradeBar
            {
                Time = time,
                Symbol = security.Symbol,
                Open = price,
                High = price,
                Low = price,
                Close = price
            });
        }
    }
}
