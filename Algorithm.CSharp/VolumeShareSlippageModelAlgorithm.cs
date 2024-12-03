
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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using QuantConnect.Orders.Slippage;

namespace QuantConnect.Algorithm.CSharp
{
    public class VolumeShareSlippageModelAlgorithm : QCAlgorithm
    {
        private List<Symbol> _longs = new();
        private List<Symbol> _shorts = new();

        public override void Initialize()
        {
            SetStartDate(2021, 1, 1);
            SetEndDate(2022, 1, 1);
            // To set the slippage model to limit to fill only 30% volume of the historical volume, with 5% slippage impact.
            SetSecurityInitializer((security) => security.SetSlippageModel(new VolumeShareSlippageModel(0.3m, 0.05m)));

            // Request extended market hour SPY data for trading.
            var qqq = AddEquity("QQQ").Symbol;

            // Weekly updating the portfolio to allow time to capitalize from the popularity gap.
            UniverseSettings.Schedule.On(DateRules.WeekStart());
            // Add universe to trade on the most and least liquid stocks among QQQ constituents.
            AddUniverse(
                // First we select from all QQQ constituents to the next filter on liquidity.
                Universe.ETF(qqq, Market.USA, UniverseSettings, (constituents) => constituents.Select(c => c.Symbol)),
                FundamentalSelection
            );

            // Set a schedule event to rebalance the portfolio every week start.
            Schedule.On(
                DateRules.WeekStart(qqq),
                TimeRules.AfterMarketOpen(qqq),
                Rebalance
            );
        }

        private IEnumerable<Symbol> FundamentalSelection(IEnumerable<Fundamental> fundamentals)
        {
            var sortedByDollarVolume = fundamentals.OrderBy(x => x.DollarVolume).ToList();
            // Add the 10 most liquid stocks to the universe to long later.
            _longs = sortedByDollarVolume.TakeLast(10)
                .Select(x => x.Symbol)
                .ToList();
            // Add the 10 least liquid stocks to the universe to short later.
            _shorts = sortedByDollarVolume.Take(10)
                .Select(x => x.Symbol)
                .ToList();

            return _longs.Union(_shorts);
        }

        private void Rebalance()
        {
            // Equally invest into the selected stocks to evenly dissipate capital risk.
            // Dollar neutral of long and short stocks to eliminate systematic risk, only capitalize the popularity gap.
            var targets = _longs.Select(symbol => new PortfolioTarget(symbol, 0.05m)).ToList();
            targets.AddRange(_shorts.Select(symbol => new PortfolioTarget(symbol, -0.05m)).ToList());

            // Liquidate the ones not being the most and least popularity stocks to release fund for higher expected return trades.
            SetHoldings(targets, liquidateExistingHoldings: true);
        }
    }
}