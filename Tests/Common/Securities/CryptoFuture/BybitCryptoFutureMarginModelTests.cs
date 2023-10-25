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
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Securities.CryptoFuture;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities.CryptoFuture
{
    [TestFixture]
    public class BybitCryptoFutureMarginModelTests
    {
        [TestCase("BTCUSDT", 0.5, 10, 1600)]        // Bybit value: 1580
        [TestCase("BTCUSDT", -0.5, 10, 1600)]
        [TestCase("BTCUSDT", 0.5, 25, 650)]         // Bybit value: 640
        [TestCase("BTCUSDT", -0.5, 25, 650)]
        [TestCase("BTCUSD", 15000, 10, 0.05)]       // Bybit value: 0.0477
        [TestCase("BTCUSD", -15000, 10, 0.05)]
        [TestCase("BTCUSD", 15000, 25, 0.02)]       // Bybit value: 0.0192
        [TestCase("BTCUSD", -15000, 25, 0.02)]
        public void BybitInitialMarginRequirement(string ticker, decimal quantity, decimal leverage, decimal expectedMargin)
        {
            var algo = GetAlgorithm();
            var cryptoFuture = algo.AddCryptoFuture(ticker);
            cryptoFuture.SetLeverage(leverage);
            SetPrice(cryptoFuture, 31300);

            var parameters = new InitialMarginParameters(cryptoFuture, quantity);
            var result = cryptoFuture.BuyingPowerModel.GetInitialMarginRequirement(parameters);

            if (cryptoFuture.IsCryptoCoinFuture())
            {
                // Convert to USD
                expectedMargin *= cryptoFuture.Price;
            }

            Assert.AreEqual((double)expectedMargin, (double)result.Value, (double)(0.05m * expectedMargin));
        }

        [TestCase("BTCUSDT", 0.5, 0.005, 87)]       // Bybit value: 86.69.      Margin rate 0.5%
        [TestCase("BTCUSDT", -0.5, 0.005, 87)]
        [TestCase("BTCUSDT", 0.5, 0.02, 320)]       // Bybit value: 323.2.      Margin rate 2%
        [TestCase("BTCUSDT", -0.5, 0.02, 320)]
        [TestCase("BTCUSD", 15000, 0.005, 75)]      //                          Margin rate 0.5%
        [TestCase("BTCUSD", -15000, 0.005, 75)]
        [TestCase("BTCUSD", 15000, 0.02, 300)]      //                          Margin rate 2%
        [TestCase("BTCUSD", -15000, 0.02, 300)]
        public void BybitMaintenanceMargin(string ticker, decimal quantity, decimal marginRate, decimal expectedMargin)
        {
            var algo = GetAlgorithm();
            var cryptoFuture = algo.AddCryptoFuture(ticker);
            cryptoFuture.SetBuyingPowerModel(new CryptoFutureMarginModel(25m, marginRate));
            SetPrice(cryptoFuture, 31300);
            cryptoFuture.Holdings.SetHoldings(0.5m, quantity);

            var parameters = MaintenanceMarginParameters.ForCurrentHoldings(cryptoFuture);
            var result = cryptoFuture.BuyingPowerModel.GetMaintenanceMargin(parameters);

            Assert.AreEqual((double)expectedMargin, (double)result.Value, (double)(0.15m * expectedMargin));
        }

        private static QCAlgorithm GetAlgorithm()
        {
            var algo = new AlgorithmStub();
            algo.SetBrokerageModel(BrokerageName.Bybit, AccountType.Margin);
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
