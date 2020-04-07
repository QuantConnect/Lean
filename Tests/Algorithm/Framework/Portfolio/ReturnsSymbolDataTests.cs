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

using Accord.Math;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Python;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class ReturnsSymbolDataTests
    {
        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void ReturnsSymbolDataMatrixIsSimilarToPandasDataFrame(bool odd)
        {
            var symbols = new[] { "A", "B", "C", "D", "E" }
                .Select(x => Symbol.Create(x, SecurityType.Equity, Market.USA, x));

            var slices = GetHistory(symbols, odd);

            var expected = GetExpectedValue(slices);

            var symbolDataDict = new Dictionary<Symbol, ReturnsSymbolData>();

            slices.PushThrough(bar =>
            {
                ReturnsSymbolData symbolData;
                if (!symbolDataDict.TryGetValue(bar.Symbol, out symbolData))
                {
                    symbolData = new ReturnsSymbolData(bar.Symbol, 1, 5);
                    symbolDataDict.Add(bar.Symbol, symbolData);
                }
                symbolData.Update(bar.EndTime, bar.Value);
            });

            var matrix = symbolDataDict.FormReturnsMatrix(symbols);
            var actual = matrix.Determinant();

            Assert.AreEqual(expected, actual, 0.000001);
        }

        [Test]
        public void DuplicateKeyPortfolioConstructionModelDoesNotThrow()
        {
            var algorithm = new QCAlgorithm();
            var timezone = algorithm.TimeZone;
            algorithm.SetDateTime(new DateTime(2018, 8, 7).ConvertToUtc(timezone));
            algorithm.SetPortfolioConstruction(new DuplicateKeyPortfolioConstructionModel());

            var symbol = Symbols.SPY;

            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(timezone),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    symbol,
                    Resolution.Daily,
                    timezone,
                    timezone,
                    true,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            security.SetMarketPrice(new Tick(algorithm.Time, symbol, 1m, 1m));
            algorithm.Securities.Add(symbol, security);

            algorithm.PortfolioConstruction.OnSecuritiesChanged(algorithm, SecurityChanges.Added(security));

            var insights = new[] {Insight.Price(symbol, Time.OneMinute, InsightDirection.Up, .1)};

            Assert.DoesNotThrow(() => algorithm.PortfolioConstruction.CreateTargets(algorithm, insights));
        }

        private List<Slice> GetHistory(IEnumerable<Symbol> symbols, bool odd)
        {
            var reference = DateTime.Now;
            var rnd = new Random(reference.Second);
            var symbolsArray = symbols.ToArray();

            return Enumerable.Range(0, 10).Select(t =>
            {
                var time = reference.AddDays(t);

                var data = Enumerable.Range(0, symbolsArray.Length).Select(i =>
                {
                    // Odd data means that one of the symbols have a different timestamp
                    if (odd && i == symbolsArray.Length - 1)
                    {
                        time = time.AddMinutes(10);
                    }
                    return new Tick(time, symbolsArray[i], (decimal)rnd.NextDouble(), 0, 0) {TickType = TickType.Trade};
                });

                return new Slice(time, data);
            }).ToList();
        }

        private double GetExpectedValue(List<Slice> history)
        {
            var code = @"
import numpy as np
import pandas as pd
import math
from BlackLittermanOptimizationPortfolioConstructionModel import BlackLittermanOptimizationPortfolioConstructionModel as blopcm

def GetDeterminantFromHistory(history):
    returns = dict()
    history = history.lastprice.unstack(0)

    for symbol, df in history.items():
        symbolData = blopcm.BlackLittermanSymbolData(symbol, 1, 5)
        for time, close in df.dropna().items():
            symbolData.Update(time, close)

        returns[symbol] = symbolData.Return
    
    df = pd.DataFrame(returns)
    if df.isna().sum().sum() > 0:
        df[df.columns[-1]] = df[df.columns[-1]].bfill()
        df = df.dropna()

    return np.linalg.det(df)";

            using (Py.GIL())
            {
                dynamic GetDeterminantFromHistory = PythonEngine
                    .ModuleFromString("GetDeterminantFromHistory", code)
                    .GetAttr("GetDeterminantFromHistory");

                dynamic df = new PandasConverter().GetDataFrame(history);
                return GetDeterminantFromHistory(df);
            }
        }

        private class DuplicateKeyPortfolioConstructionModel : IPortfolioConstructionModel
        {
            private readonly Dictionary<Symbol, ReturnsSymbolData> _symbolDataDict = new Dictionary<Symbol, ReturnsSymbolData>();

            public IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
            {
                // Updates the ReturnsSymbolData with insights
                foreach (var insight in insights)
                {
                    ReturnsSymbolData symbolData;
                    if (_symbolDataDict.TryGetValue(insight.Symbol, out symbolData))
                    {
                        symbolData.Add(algorithm.Time, .1m);
                    }
                }

                Assert.DoesNotThrow(() => _symbolDataDict.FormReturnsMatrix(insights.Select(x => x.Symbol)));

                return Enumerable.Empty<PortfolioTarget>();
            }

            public void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
            {
                const int period = 2;
                var reference = algorithm.Time.AddDays(-period);

                foreach (var security in changes.AddedSecurities)
                {
                    var symbol = security.Symbol;
                    var symbolData = new ReturnsSymbolData(symbol, 1, period);

                    for (var i = 0; i <= period; i++)
                    {
                        symbolData.Update(reference.AddDays(i), 1);
                    }

                    _symbolDataDict[symbol] = symbolData;
                }
            }
        }
    }
}