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

using QLNet;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm sends a list of portfolio targets to Collective2 API every time the ema indicators crosses between themselves
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="securities and portfolio" />
    public class Collective2SignalExportDemonstrationAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Collective2 API: This value is provided by Collective2 in their webpage in your account section (See https://collective2.com/account-info)
        /// </summary>
        private const string _collective2ApiKey = "";

        /// <summary>
        /// Collective2 System ID: This value is found beside the system's name (strategy's name) on the main system page
        /// </summary>
        private const int _collective2SystemId = 0;

        /// <summary>
        /// Field to set your platform ID given by Collective2 (See https://collective2.com/api-docs/latest) (Optional)
        /// </summary>
        private const string _collective2PlatformId = "";

        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;
        private bool _emaFastWasAbove;
        private bool _emaFastIsNotSet;

        private PortfolioTarget[] _targets = new PortfolioTarget[2];
        
        /// <summary>
        /// Symbols accepted by Collective2. Collective2 accepts stock,
        /// future, forex and option symbols
        /// </summary>
        private List<Pair<string, SecurityType>> _symbols = new()
        {
            new Pair<string, SecurityType>("SPY", SecurityType.Equity),
            new Pair<string, SecurityType>("EURUSD", SecurityType.Forex)
        };

        /// <summary>
        /// Initialize the date and add all equity symbols present in _symbols list.
        /// Besides, make a new PortfolioTarget for each symbol in _symbols, assign it
        /// an initial quantity of 0.05 and save it in _targets array
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100 * 1000);

            var index = 0;
            foreach (var item in _symbols)
            {
                var symbol = AddSecurity(item.second, item.first).Symbol;
                _targets[index] = new PortfolioTarget(symbol, (decimal)0.05);
                index++;
            }

            _fast = EMA("SPY", 10);
            _slow = EMA("SPY", 100);

            // Initialize this flag, to check when the ema indicators crosses between themselves
            _emaFastIsNotSet = true;

            // Set Collective2 signal export provider
            SignalExport.AddSignalExportProviders(new Collective2SignalExport(_collective2ApiKey, _collective2SystemId, _collective2PlatformId));
        }

        /// <summary>
        /// Reduce the quantity of holdings for SPY or increase it, depending the case, 
        /// when the EMA's indicators crosses between themselves, then send a signal to 
        /// Collective2 API
        /// </summary>
        /// <param name="slice"></param>
        public override void OnData(Slice slice)
        {
            // Wait for our indicators to be ready
            if (!_fast.IsReady || !_slow.IsReady) return;

            // Set the value of flag _emaFastWasAbove, to know when the ema indicators crosses between themselves
            if (_emaFastIsNotSet)
            {
                if (_fast > _slow * 1.001m)
                {
                    _emaFastWasAbove = true;
                }
                else
                {
                    _emaFastWasAbove = false;
                }
                _emaFastIsNotSet = false;
            }

            // Check whether ema fast and ema slow crosses. If they do, set holdings to SPY
            // or reduce its holdings, change its value in _targets array and send signals
            // to the Collective2 API from _targets array
            if ((_fast > _slow * 1.001m) && (!_emaFastWasAbove))
            {
                SetHoldings("SPY", 0.1);
                _targets[0] = new PortfolioTarget(Portfolio["SPY"].Symbol, (decimal)0.1);
                SignalExport.SetTargetPortfolio(_targets);
            }
            else if ((_fast < _slow * 0.999m) && (_emaFastWasAbove))
            {
                SetHoldings("SPY", 0.01);
                _targets[0] = new PortfolioTarget(Portfolio["SPY"].Symbol, (decimal)0.01);
                SignalExport.SetTargetPortfolio(_targets);
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3944;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 7594;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "6"},
            {"Average Win", "0%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "9.315%"},
            {"Drawdown", "0.200%"},
            {"Expectancy", "-1"},
            {"Net Profit", "0.114%"},
            {"Sharpe Ratio", "5.01"},
            {"Probabilistic Sharpe Ratio", "66.849%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.085"},
            {"Beta", "0.098"},
            {"Annual Standard Deviation", "0.022"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-9.336"},
            {"Tracking Error", "0.201"},
            {"Treynor Ratio", "1.115"},
            {"Total Fees", "$6.00"},
            {"Estimated Strategy Capacity", "$28000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "2.13%"},
            {"OrderListHash", "f2f9bba2b756b2b7456e7ab705a08d49"}
        };
    }
}
