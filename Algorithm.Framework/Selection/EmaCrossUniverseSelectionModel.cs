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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Provides an implementation of <see cref="FundamentalUniverseSelectionModel"/> that subscribes
    /// to symbols with the larger delta by percentage between the two exponential moving average
    /// </summary>
    public class EmaCrossUniverseSelectionModel : FundamentalUniverseSelectionModel
    {
        private const decimal _tolerance = 0.01m;
        private readonly int _fastPeriod;
        private readonly int _slowPeriod;
        private readonly int _universeCount;

        // holds our coarse fundamental indicators by symbol
        private readonly ConcurrentDictionary<Symbol, SelectionData> _averages;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmaCrossUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="fastPeriod">Fast EMA period</param>
        /// <param name="slowPeriod">Slow EMA period</param>
        /// <param name="universeCount">Maximum number of members of this universe selection</param>
        /// <param name="universeSettings">The settings used when adding symbols to the algorithm, specify null to use algorthm.UniverseSettings</param>
        /// <param name="securityInitializer">Optional security initializer invoked when creating new securities, specify null to use algorithm.SecurityInitializer</param>
        public EmaCrossUniverseSelectionModel(
            int fastPeriod = 100,
            int slowPeriod = 300,
            int universeCount = 500,
            UniverseSettings universeSettings = null,
            ISecurityInitializer securityInitializer = null)
            : base(false, universeSettings, securityInitializer)
        {
            _fastPeriod = fastPeriod;
            _slowPeriod = slowPeriod;
            _universeCount = universeCount;
            _averages = new ConcurrentDictionary<Symbol, SelectionData>();
        }

        /// <summary>
        /// Defines the coarse fundamental selection function.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="coarse">The coarse fundamental data used to perform filtering</param>
        /// <returns>An enumerable of symbols passing the filter</returns>
        public override IEnumerable<Symbol> SelectCoarse(QCAlgorithm algorithm, IEnumerable<CoarseFundamental> coarse)
        {
            return (from cf in coarse
                        // grab th SelectionData instance for this symbol
                        let avg = _averages.GetOrAdd(cf.Symbol, sym => new SelectionData(_fastPeriod, _slowPeriod))
                        // Update returns true when the indicators are ready, so don't accept until they are
                        where avg.Update(cf.EndTime, cf.AdjustedPrice)
                        // only pick symbols who have their _fastPeriod-day ema over their _slowPeriod-day ema
                        where avg.Fast > avg.Slow * (1 + _tolerance)
                        // prefer symbols with a larger delta by percentage between the two averages
                        orderby avg.ScaledDelta descending
                        // we only need to return the symbol and return 'Count' symbols
                        select cf.Symbol).Take(_universeCount);
        }

        // class used to improve readability of the coarse selection function
        private class SelectionData
        {
            public readonly ExponentialMovingAverage Fast;
            public readonly ExponentialMovingAverage Slow;

            public SelectionData(int fastPeriod, int slowPeriod)
            {
                Fast = new ExponentialMovingAverage(fastPeriod);
                Slow = new ExponentialMovingAverage(slowPeriod);
            }

            // computes an object score of how much large the fast is than the slow
            public decimal ScaledDelta => (Fast - Slow) / ((Fast + Slow) / 2m);

            // updates the EMAFast and EMASlow indicators, returning true when they're both ready
            public bool Update(DateTime time, decimal value) => Fast.Update(time, value) & Slow.Update(time, value);
        }
    }
}