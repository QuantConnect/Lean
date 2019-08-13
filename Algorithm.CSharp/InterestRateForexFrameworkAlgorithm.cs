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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Orders;
using System.Globalization;
using QuantConnect.Data.Custom.TradingEconomics;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    public class InterestRateForexFrameworkAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2018, 1, 1);       // Set Start Date
            SetEndDate(2019, 4, 30);        // Set End Date
            SetCash(100000);                // Set Strategy Cash

            UniverseSettings.Resolution = Resolution.Daily;     // Set requested data resolution

            var symbols = new[]
                {
                    "AUDUSD", "EURUSD", "NZDUSD", "GBPUSD",
                    "USDCAD", "USDMXN", "USDJPY", "USDSEK"
                }
                .Select(x => QuantConnect.Symbol.Create(x, SecurityType.Forex, Market.Oanda));

            UniverseSettings.Resolution = Resolution.Daily;
            SetUniverseSelection(new ManualUniverseSelectionModel(symbols));
            SetAlpha(new InterestReleaseAlphaModel(this));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
            SetRiskManagement(new NullRiskManagementModel());
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            var order = Transactions.GetOrderById(orderEvent.OrderId);
            Debug($"{Time}: {order.Type}: {orderEvent}");
        }
    }
}

namespace QuantConnect.Algorithm.Framework.Alphas
{
    public class InterestReleaseAlphaModel : AlphaModel
    {
        private TimeSpan _predictionInterval;
        private IEnumerable<Symbol> _pairs;
        private Symbol _calendar;

        /// <summary>
        /// Alpha model that uses the Interest rate released by Fed to create insights
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="period">The prediction interval period</param>
        /// <param name="resolution">The resolution of data</param>
        public InterestReleaseAlphaModel(QCAlgorithm algorithm, int period = 30, Resolution resolution = Resolution.Daily)
        {
            _predictionInterval = Time.Multiply(Extensions.ToTimeSpan(resolution), period);
            _calendar = algorithm.AddData<TradingEconomicsCalendar>(TradingEconomics.Calendar.UnitedStates.InterestRate).Symbol;

            Name = $"{nameof(InterestReleaseAlphaModel)}({period}, {resolution})";
        }

        /// <summary>
        /// Updates this alpha model with the latest data from the algorithm.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="data">The new data available</param>
        /// <returns>The new insights generated</returns>
        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            if (!data.ContainsKey(_calendar))
            {
                return Enumerable.Empty<Insight>();
            }

            var insights = new List<Insight>();

            // Forecast Interest Rate
            var foreIR = System.Convert.ToDecimal(data[_calendar].Forecast.Replace("%", ""), CultureInfo.InvariantCulture);
            // Previous released actual Interest Rate
            var prevIR = System.Convert.ToDecimal(data[_calendar].Previous.Replace("%", ""), CultureInfo.InvariantCulture);
            var usdValueUp = foreIR >= prevIR;

            foreach (var pair in _pairs)
            {
                var direction = pair.Value.StartsWith("USD") && usdValueUp ||
                                pair.Value.EndsWith("USD") && !usdValueUp
                    ? InsightDirection.Up
                    : InsightDirection.Down;

                insights.Add(Insight.Price(pair, _predictionInterval, direction));
            }

            return insights;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed.
        /// This initializes the MACD for each added security and cleans up the indicator for each removed security.
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            _pairs = changes.AddedSecurities.Select(x => x.Symbol);
        }
    }
}