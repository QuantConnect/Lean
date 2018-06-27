﻿/*
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
using QuantConnect.Securities.CurrencyConversion;
using QuantConnect.Securities.CurrencyConversion.PathProvider;
using System.Linq;

namespace QuantConnect.Tests.Common.Securities.CurrencyConversion.LinearMax2
{
    [TestFixture]
    public class LinearMax2SearchTests {

        //1 leg
        [TestCase("BTC", "USD")]
        [TestCase("ETH", "USD")]
        [TestCase("REN", "USD")]
        [TestCase("ZRX", "USD")]
        [TestCase("BTC", "EUR")]
        //2 legs
        [TestCase("BTC", "XAU")]
        [TestCase("ETH", "XAU")]
        public void CurrencyConversionPathTest(string start, string end)
        {
            var pairs = Currencies.CurrencyPairs
                .Concat(Currencies.CryptoCurrencyPairs)
                .Concat(Currencies.CfdCurrencyPairs)
                .ToList();

            ICurrencyPathProvider linearSearch = new LinearMax2SearchProvider();

            // type of security here doesn't matter, we just want to test shortest path search
            pairs.ForEach(pair => linearSearch.AddEdge(pair, SecurityType.Base));

            linearSearch.FindShortestPath(start, end);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), MatchType = MessageMatch.Contains, ExpectedMessage = "too long")]
        public void ThrowsOnTooLongPath()
        {
            ICurrencyPathProvider linearSearch = new LinearMax2SearchProvider();

            linearSearch.AddEdge("RENETH", SecurityType.Crypto);
            linearSearch.AddEdge("USDETH", SecurityType.Crypto); //inverted path
            linearSearch.AddEdge("USDEUR", SecurityType.Forex);

            linearSearch.FindShortestPath("REN", "EUR");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), MatchType = MessageMatch.Contains, ExpectedMessage = "No path")]
        public void ThrowsOnImpossiblePath()
        {
            ICurrencyPathProvider linearSearch = new LinearMax2SearchProvider();

            linearSearch.AddEdge("RENETH", SecurityType.Crypto);
            linearSearch.AddEdge("BTCETH", SecurityType.Crypto);
            linearSearch.AddEdge("USDEUR", SecurityType.Forex);

            linearSearch.FindShortestPath("REN", "EUR");
        }

        [Test]
        public void CurrencyPath1StepsIsInvertedCorrectly()
        {
            ICurrencyPathProvider linearSearch = new LinearMax2SearchProvider();

            linearSearch.AddEdge("BTCZRX", SecurityType.Crypto); // inverted = false

            var path = linearSearch.FindShortestPath("ZRX", "BTC");

            var step = path.Steps.ToArray();

            Assert.AreEqual(step[0].Inverted, true);
        }

        [Test]
        public void CurrencyPath2StepsIsInvertedCorrectly()
        {
            ICurrencyPathProvider linearSearch = new LinearMax2SearchProvider();

            linearSearch.AddEdge("ZRXBTC", SecurityType.Crypto); // inverted = false
            linearSearch.AddEdge("ETHBTC", SecurityType.Crypto); // inverted = true

            var path = linearSearch.FindShortestPath("ZRX", "ETH");

            var step = path.Steps.ToArray();

            Assert.AreEqual(step[0].Inverted, false);
            Assert.AreEqual(step[1].Inverted, true);
        }

        [Test]
        public void CurrencyPathCorrect()
        {
            ICurrencyPathProvider linearSearch = new LinearMax2SearchProvider();

            linearSearch.AddEdge("ZRXBTC", SecurityType.Crypto);
            linearSearch.AddEdge("ETHBTC", SecurityType.Crypto);
            linearSearch.AddEdge("ZRXETH", SecurityType.Crypto);
            linearSearch.AddEdge("ZRXREN", SecurityType.Crypto);

            //1 step normal
            var path = linearSearch.FindShortestPath("ZRX", "ETH");
            var step = path.Steps.ToArray();

            Assert.AreEqual(step[0].Edge.PairSymbol, "ZRXETH");
            Assert.AreEqual(step[0].Inverted, false);

            //1 step inversed
            path = linearSearch.FindShortestPath("ETH", "ZRX");
            step = path.Steps.ToArray();

            Assert.AreEqual(step[0].Edge.PairSymbol, "ZRXETH");
            Assert.AreEqual(step[0].Inverted, true);

            //// 2 steps
            path = linearSearch.FindShortestPath("REN", "ETH");
            step = path.Steps.ToArray();

            Assert.AreEqual(step[0].Edge.PairSymbol, "ZRXREN");
            Assert.AreEqual(step[0].Inverted, true);

            Assert.AreEqual(step[1].Edge.PairSymbol, "ZRXETH");
            Assert.AreEqual(step[1].Inverted, false);

        }
    }

}
