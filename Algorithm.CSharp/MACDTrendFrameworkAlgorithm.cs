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
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration algorithm showing how to easily convert an old algorithm into the framework.
    ///
    /// 0. Make class derive from QCAlgorithmFramework instead of QCAlgorithm.
    /// 1. Set manual universe selection model and null implementation for all other models
    /// 2. When making orders, also create insights for the correct direction (up/down)
    /// 3. Profit :)
    /// </summary>
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="indicator classes" />
    /// <meta name="tag" content="plotting indicators" />
    public class MACDTrendFrameworkAlgorithm : QCAlgorithmFramework // 0 - derive from QCAlgorithmFramwork
    {
        private DateTime _previous;
        private MovingAverageConvergenceDivergence _macd;
        private readonly string _symbol = "SPY";

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2004, 01, 01);
            SetEndDate(2015, 01, 01);

            AddSecurity(SecurityType.Equity, _symbol, Resolution.Daily);

            // define our daily macd(12,26) with a 9 day signal
            _macd = MACD(_symbol, 12, 26, 9, MovingAverageType.Exponential, Resolution.Daily);

            // 1. Set algorithm framework models
            // use manual selection model with our manually added symbol
            SetUniverseSelection(new ManualUniverseSelectionModel(_symbol));

            // all other models are set to the null implementation
            SetAlpha(new NullAlphaModel());
            SetPortfolioConstruction(new NullPortfolioConstructionModel());
            SetExecution(new NullExecutionModel());
            SetRiskManagement(new NullRiskManagementModel());
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            // only once per day
            if (_previous.Date == Time.Date) return;

            if (!_macd.IsReady) return;

            var holding = Portfolio[_symbol];

            var signalDeltaPercent = (_macd - _macd.Signal) / _macd.Fast;
            var tolerance = 0.0025m;

            // if our macd is greater than our signal, then let's go long
            if (holding.Quantity <= 0 && signalDeltaPercent > tolerance) // 0.01%
            {
                // longterm says buy as well
                SetHoldings(_symbol, 1.0);

                // 2. Call OnInsightsGenerated with insights created in correct direction, here we're going long
                OnInsightsGenerated(new []
                {
                    Insight.Price(_symbol, TimeSpan.FromDays(12), InsightDirection.Up)
                });
            }
            // of our macd is less than our signal, then let's go short
            else if (holding.Quantity >= 0 && signalDeltaPercent < -tolerance)
            {
                SetHoldings(_symbol, -1.0);

                // 2. Call OnInsightsGenerated with insights created in correct direction, here we're going short
                OnInsightsGenerated(new[]
                {
                    Insight.Price(_symbol, TimeSpan.FromDays(12), InsightDirection.Down)
                });
            }

            // plot both lines
            Plot("MACD", _macd, _macd.Signal);
            Plot(_symbol, "Open", data[_symbol].Open);
            Plot(_symbol, _macd.Fast, _macd.Slow);

            _previous = Time;
        }
    }
}
