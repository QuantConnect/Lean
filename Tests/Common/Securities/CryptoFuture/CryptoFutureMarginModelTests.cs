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
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Securities.CryptoFuture;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities.CryptoFuture
{
    [TestFixture]
    public class CryptoFutureMarginModelTests
    {
        [TestCase("BTCUSD")]
        [TestCase("BTCUSDT")]
        public void DefaultMarginModelType(string ticker)
        {
            var algo = GetAlgorithm();
            var cryptoFuture = algo.AddCryptoFuture(ticker);

            Assert.AreEqual(typeof(CryptoFutureMarginModel), cryptoFuture.BuyingPowerModel.GetType());
        }

        [TestCase("BTCUSD", 10)]
        [TestCase("BTCUSDT", 10)]
        [TestCase("BTCUSD", -10)]
        [TestCase("BTCUSDT", -10)]
        public void InitialMarginRequirement(string ticker, decimal quantity)
        {
            var algo = GetAlgorithm();
            var cryptoFuture = algo.AddCryptoFuture(ticker);
            SetPrice(cryptoFuture, 16000);

            var parameters = new InitialMarginParameters(cryptoFuture, quantity);
            var result = cryptoFuture.BuyingPowerModel.GetInitialMarginRequirement(parameters);

            decimal marginRequirement;
            if (ticker == "BTCUSD")
            {
                // ((quantity * contract mutiplier * price) / leverage) * conversion rate (BTC -> USD)
                marginRequirement = ((parameters.Quantity * 100m * cryptoFuture.Price) / 25m ) *  1 / cryptoFuture.Price;
            }
            else
            {
                // ((quantity * contract mutiplier * price) / leverage) * conversion rate (USDT ~= USD)
                marginRequirement = ((parameters.Quantity * 1m * cryptoFuture.Price) / 25m) * 1;
            }

            Assert.AreEqual(Math.Abs(marginRequirement), result.Value);
        }

        [TestCase("BTCUSD", 10)]
        [TestCase("BTCUSDT", 10)]
        [TestCase("BTCUSD", -10)]
        [TestCase("BTCUSDT", -10)]
        public void GetMaintenanceMargin(string ticker, decimal quantity)
        {
            var algo = GetAlgorithm();
            var cryptoFuture = algo.AddCryptoFuture(ticker);
            SetPrice(cryptoFuture, 16000);
            // entry price 1000, shouldn't matter
            cryptoFuture.Holdings.SetHoldings(1000, quantity);

            var parameters = MaintenanceMarginParameters.ForCurrentHoldings(cryptoFuture);
            var result = cryptoFuture.BuyingPowerModel.GetMaintenanceMargin(parameters);

            decimal marginRequirement;
            if (ticker == "BTCUSD")
            {
                // ((quantity * contract mutiplier * price) * MaintenanceMarginRate) * conversion rate (BTC -> USD)
                marginRequirement = ((parameters.Quantity * 100m * cryptoFuture.Price) * 0.05m) * 1 / cryptoFuture.Price;
            }
            else
            {
                // ((quantity * contract mutiplier * price) * MaintenanceMarginRate) * conversion rate (USDT ~= USD)
                marginRequirement = ((parameters.Quantity * 1m * cryptoFuture.Price) * 0.05m) * 1;
            }

            Assert.AreEqual(Math.Abs(marginRequirement), result.Value);
        }


        private static QCAlgorithm GetAlgorithm()
        {
            // Initialize algorithm
            var algo = new AlgorithmStub();
            algo.SetFinishedWarmingUp();
            return algo;
        }

        private static void SetPrice(Security security, decimal price)
        {
            var cryptoFuture = (QuantConnect.Securities.CryptoFuture.CryptoFuture) security;
            cryptoFuture.BaseCurrency.ConversionRate = price;
            cryptoFuture.QuoteCurrency.ConversionRate = 1;

            security.SetMarketPrice(new TradeBar
            {
                Time = new DateTime(2022, 12, 22),
                Symbol = security.Symbol,
                Open = price,
                High = price,
                Low = price,
                Close = price
            });
        }
    }
}
